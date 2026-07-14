using System.Diagnostics;
using System.Text.Json.Nodes;

namespace Sentry.CrashReporter.Services;

public record Feedback(string? Name, string? Email, string Message);

public interface ICrashReporter
{
    Feedback? Feedback { get; }
    CacheKeep EffectiveCacheKeep { get; }
    public Task<Envelope?> LoadAsync(CancellationToken cancellationToken = default);
    public Task CacheAsync(Envelope envelope, CancellationToken cancellationToken = default);
    public Task SubmitAsync(Envelope envelope, IProgress<double>? progress = null, CancellationToken cancellationToken = default);
    public void UpdateFeedback(Feedback? feedback);
}

public class CrashReporter(
    IStorageFile? file = null,
    ISentryClient? client = null,
    ICacheService? cache = null,
    AppConfig? config = null) : ICrashReporter
{
    // Default cap on the number of envelopes retained in the on-disk cache (offline-caching
    // spec: SDKs MUST cap the number of stored envelopes). Applies to every write into the
    // cache directory - offline retries and CacheKeep.Always copies alike. Oldest are evicted
    // first. Matches sentry-native's `cache_max_items` default; overridable via
    // AppConfig.MaxCachedEnvelopes.
    public const int DefaultMaxCachedEnvelopes = 30;

    private readonly IStorageFile? _file = file ?? App.Services.GetService<IStorageFile>();
    private readonly ISentryClient _client = client ?? App.Services.GetRequiredService<ISentryClient>();
    private readonly ICacheService _cache = cache ?? App.Services.GetRequiredService<ICacheService>();
    private readonly AppConfig _config = config ?? App.Services.GetRequiredService<AppConfig>();
    private Envelope? _submittingEnvelope;
    private readonly HashSet<Envelope> _submittedEnvelopes = [];
    private Feedback? _feedback = ResolveFeedback();

    public Feedback? Feedback => _feedback;

    public async Task<Envelope?> LoadAsync(CancellationToken cancellationToken = default)
    {
        if (_file is null)
        {
            return null;
        }

        var stopwatch = Stopwatch.StartNew();
        var envelope = await Envelope.FromStorageFileAsync(_file, cancellationToken);
        stopwatch.Stop();
        this.Log().LogInformation($"Loaded {_file.Path} in {stopwatch.ElapsedMilliseconds} ms.");
        return envelope;
    }

    public async Task SubmitAsync(Envelope envelope, IProgress<double>? progress = null, CancellationToken cancellationToken = default)
    {
        var dsn = envelope.TryGetDsn()
                  ?? throw new InvalidOperationException("Envelope does not contain a valid DSN.");

        _submittingEnvelope = envelope;
        try
        {
            var result = await _client.SubmitEnvelopeAsync(dsn, envelope, progress, cancellationToken);
            if (result == SubmitResult.RateLimited)
            {
                // Per the offline-caching spec, a rate-limited (HTTP 429) envelope is
                // discarded and NOT retried - the offline cache is only for network
                // failures, not server responses. So we treat it like a completed send
                // for cache purposes (discard) rather than keeping it to retry, but log it.
                this.Log().LogWarning("Crash envelope was rate-limited (HTTP 429) and discarded without retry.");
            }

            var cachedEnvelopePath = await CacheAsync(envelope, EffectiveCacheKeep, cancellationToken);
            _submittedEnvelopes.Add(envelope);
            DeleteEnvelope(envelope, cachedEnvelopePath);
        }
        catch (HttpRequestException e) when (e.StatusCode is not null)
        {
            // The server responded with an error status (4xx/5xx, including 413). Per the
            // offline-caching spec these are discarded and NOT retried - the offline cache
            // is only for network failures. Drop it from the cache (and mark it done so a
            // window-close does not re-cache it), but still rethrow so the UI can react.
            _submittingEnvelope = null;
            _submittedEnvelopes.Add(envelope);
            DeleteEnvelope(envelope);
            throw;
        }
        catch (Exception)
        {
            // Network/transport failure (or cancellation): keep the crash cached so it can
            // be retried on a later run.
            _submittingEnvelope = null;
            await CacheAsync(envelope, CancellationToken.None);
            throw;
        }
        finally
        {
            if (ReferenceEquals(_submittingEnvelope, envelope))
            {
                _submittingEnvelope = null;
            }
        }

        if (!string.IsNullOrEmpty(_feedback?.Message))
        {
            var feedback = Envelope.FromJson(new JsonObject { ["dsn"] = dsn },
                [
                    (Header: new JsonObject { ["type"] = "feedback" }, Payload: new JsonObject
                    {
                        ["contexts"] = new JsonObject
                        {
                            ["feedback"] = new JsonObject
                            {
                                ["contact_email"] = _feedback.Email,
                                ["name"] = _feedback.Name,
                                ["message"] = _feedback.Message,
                                ["associated_event_id"] = envelope.TryGetEventId()?.Replace("-", "")
                            }
                        }
                    })
                ]
            );
            var feedbackResult = await _client.SubmitEnvelopeAsync(dsn, feedback, null, cancellationToken);
            if (feedbackResult == SubmitResult.RateLimited)
            {
                // Feedback rides its own rate-limit category, so this only happens under a
                // full backoff. It is re-derived from the environment on the next run's
                // retry, so surface it rather than silently reporting success.
                this.Log().LogWarning("User feedback was rate-limited and not delivered.");
            }
        }
    }

    public void UpdateFeedback(Feedback? feedback)
    {
        _feedback = feedback;
    }

    public async Task CacheAsync(Envelope envelope, CancellationToken cancellationToken = default)
    {
        try
        {
            if (_submittedEnvelopes.Contains(envelope))
            {
                DeleteEnvelope(envelope);
                return;
            }
            if (ReferenceEquals(_submittingEnvelope, envelope))
            {
                return;
            }
            if (EffectiveCacheKeep == CacheKeep.None)
            {
                DeleteEnvelope(envelope);
                return;
            }

            string? cacheDir = envelope.TryGetHeader("cache_dir");
            if (string.IsNullOrWhiteSpace(cacheDir))
            {
                DeleteEnvelope(envelope);
                return;
            }

            Directory.CreateDirectory(cacheDir);

            var eventId = GetCacheEventId(envelope);
            var envelopePath = System.IO.Path.Combine(cacheDir, $"{eventId}.envelope");
            EnforceCacheCap(cacheDir, envelopePath);
            if (File.Exists(envelopePath))
            {
                if (DeleteEnvelope(envelope, envelopePath))
                {
                    envelope.FilePath = envelopePath;
                }
                return;
            }

            var minidumps = GetMinidumps(envelope);
            if (minidumps.Count == 0 && TryMoveEnvelope(envelope, envelopePath))
            {
                return;
            }

            await WriteMinidumpsAsync(cacheDir, eventId, minidumps, cancellationToken);
            await WriteCachedEnvelopeAsync(envelope, envelopePath, minidumps, cancellationToken);
            if (DeleteEnvelope(envelope, envelopePath))
            {
                envelope.FilePath = envelopePath;
            }
        }
        catch (Exception e)
        {
            this.Log().LogWarning(e, "Failed to cache crash envelope.");
        }
    }

    public CacheKeep EffectiveCacheKeep => (_cache.CacheKeep ?? _config.CacheKeep ?? CacheKeep.Offline).Normalize();

    // A non-positive configured value is ignored so the cache always stays capped (the spec
    // requires a cap), falling back to the native-matching default.
    private int EffectiveMaxCachedEnvelopes =>
        _config.MaxCachedEnvelopes is > 0 ? _config.MaxCachedEnvelopes.Value : DefaultMaxCachedEnvelopes;

    private async Task<string?> CacheAsync(Envelope envelope, CacheKeep cacheKeep, CancellationToken cancellationToken)
    {
        if (cacheKeep != CacheKeep.Always)
        {
            return null;
        }

        string? cacheDir = envelope.TryGetHeader("cache_dir");
        if (string.IsNullOrWhiteSpace(cacheDir))
        {
            return null;
        }

        try
        {
            Directory.CreateDirectory(cacheDir);

            var eventId = GetCacheEventId(envelope);
            var envelopePath = System.IO.Path.Combine(cacheDir, $"{eventId}.envelope");
            EnforceCacheCap(cacheDir, envelopePath);
            var minidumps = GetMinidumps(envelope);
            await WriteMinidumpsAsync(cacheDir, eventId, minidumps, cancellationToken);
            if (File.Exists(envelopePath))
            {
                return envelopePath;
            }

            await WriteCachedEnvelopeAsync(envelope, envelopePath, minidumps, cancellationToken);
            return envelopePath;
        }
        catch (Exception e)
        {
            this.Log().LogWarning(e, "Failed to cache crash envelope.");
            return null;
        }
    }

    private static List<EnvelopeItem> GetMinidumps(Envelope envelope) =>
        envelope.Items
            .Where(item => item.TryGetHeader("attachment_type") == "event.minidump")
            .ToList();

    private static async Task WriteMinidumpsAsync(
        string cacheDir,
        string eventId,
        IReadOnlyList<EnvelopeItem> minidumps,
        CancellationToken cancellationToken)
    {
        for (var i = 0; i < minidumps.Count; i++)
        {
            var suffix = i == 0 ? ".dmp" : $"-{i}.dmp";
            var path = System.IO.Path.Combine(cacheDir, $"{eventId}{suffix}");
            await File.WriteAllBytesAsync(path, minidumps[i].Payload, cancellationToken);
        }
    }

    private async Task WriteCachedEnvelopeAsync(
        Envelope envelope,
        string envelopePath,
        IReadOnlyCollection<EnvelopeItem> minidumps,
        CancellationToken cancellationToken)
    {
        string? envelopeTempPath = null;
        try
        {
            var directory = System.IO.Path.GetDirectoryName(envelopePath)!;
            var eventId = System.IO.Path.GetFileNameWithoutExtension(envelopePath);
            var cachedEnvelope = new Envelope(envelope.Header, envelope.Items.Except(minidumps));
            envelopeTempPath = System.IO.Path.Combine(directory, $"{eventId}.{Guid.NewGuid():N}.tmp");
            await using (var stream = File.Create(envelopeTempPath))
            {
                await cachedEnvelope.SerializeAsync(stream, cancellationToken);
            }
            File.Move(envelopeTempPath, envelopePath);
            envelopeTempPath = null;
        }
        catch
        {
            DeleteTemporaryCacheEnvelope(envelopeTempPath);
            throw;
        }
    }

    private void DeleteTemporaryCacheEnvelope(string? envelopeTempPath)
    {
        if (envelopeTempPath is null || !File.Exists(envelopeTempPath))
        {
            return;
        }

        try
        {
            File.Delete(envelopeTempPath);
        }
        catch (Exception cleanupException)
        {
            this.Log().LogWarning(cleanupException, "Failed to delete temporary crash envelope cache file.");
        }
    }

    private static string GetCacheEventId(Envelope envelope)
    {
        if (!Guid.TryParse(envelope.TryGetEventId(), out var eventId))
        {
            eventId = Guid.NewGuid();
        }

        var eventIdString = eventId.ToString("D");
        envelope.Header["event_id"] = eventIdString;
        return eventIdString;
    }

    private static bool TryMoveEnvelope(Envelope envelope, string envelopePath)
    {
        var sourcePath = envelope.FilePath;
        if (string.IsNullOrWhiteSpace(sourcePath) || !File.Exists(sourcePath))
        {
            return false;
        }

        if (!SamePath(sourcePath, envelopePath))
        {
            var sourceDir = System.IO.Path.GetDirectoryName(sourcePath);
            var envelopeDir = System.IO.Path.GetDirectoryName(envelopePath);
            if (!string.IsNullOrWhiteSpace(sourceDir) &&
                !string.IsNullOrWhiteSpace(envelopeDir) &&
                Directory.Exists(sourceDir) &&
                Guid.TryParse(envelope.TryGetEventId(), out var eventId))
            {
                foreach (var siblingPath in Directory.EnumerateFiles(sourceDir, $"{eventId:D}-*"))
                {
                    var targetPath = System.IO.Path.Combine(envelopeDir, System.IO.Path.GetFileName(siblingPath));
                    if (!SamePath(siblingPath, targetPath))
                    {
                        File.Move(siblingPath, targetPath);
                    }
                }
            }
            File.Move(sourcePath, envelopePath);
        }
        envelope.FilePath = envelopePath;
        return true;
    }

    private bool DeleteEnvelope(Envelope envelope, string? preservedPath = null)
    {
        var envelopePath = envelope.FilePath;
        if (string.IsNullOrWhiteSpace(envelopePath) || SamePath(envelopePath, preservedPath))
        {
            return true;
        }

        if (File.Exists(envelopePath))
        {
            try
            {
                File.Delete(envelopePath);
                envelope.FilePath = null;
            }
            catch (Exception e)
            {
                this.Log().LogWarning(e, "Failed to delete consumed crash envelope.");
                return false;
            }
        }
        else
        {
            envelope.FilePath = null;
        }

        var directory = System.IO.Path.GetDirectoryName(envelopePath);
        if (string.IsNullOrWhiteSpace(directory) ||
            !Directory.Exists(directory) ||
            !Guid.TryParse(envelope.TryGetEventId(), out var eventId))
        {
            return true;
        }

        if (IsCacheDirectory(directory, envelope))
        {
            DeleteEnvelopeSibling(System.IO.Path.Combine(directory, $"{eventId:D}.dmp"));
        }

        foreach (var siblingPath in Directory.EnumerateFiles(directory, $"{eventId:D}-*"))
        {
            DeleteEnvelopeSibling(siblingPath);
        }
        return true;
    }

    // Enforces the stored-envelope cap on the cache directory, evicting oldest-first and
    // leaving room for `headroom` new entries. `keepPath` is excluded from eviction (it is
    // the envelope currently being cached). Best-effort: never throws to the caller.
    private void EnforceCacheCap(string cacheDir, string? keepPath = null, int headroom = 1)
    {
        try
        {
            if (!Directory.Exists(cacheDir))
            {
                return;
            }

            var envelopes = new DirectoryInfo(cacheDir)
                .EnumerateFiles("*.envelope")
                .Where(f => !SamePath(f.FullName, keepPath))
                .OrderBy(f => f.LastWriteTimeUtc)
                .ToList();

            var maxKeep = Math.Max(0, EffectiveMaxCachedEnvelopes - headroom);
            for (var i = 0; i < envelopes.Count - maxKeep; i++)
            {
                EvictCachedEnvelope(envelopes[i].FullName);
            }
        }
        catch (Exception e)
        {
            this.Log().LogWarning(e, "Failed to enforce crash cache cap.");
        }
    }

    private void EvictCachedEnvelope(string envelopePath)
    {
        DeleteEnvelopeSibling(envelopePath);

        var directory = System.IO.Path.GetDirectoryName(envelopePath);
        var eventId = System.IO.Path.GetFileNameWithoutExtension(envelopePath);
        if (string.IsNullOrEmpty(directory) || string.IsNullOrEmpty(eventId))
        {
            return;
        }

        DeleteEnvelopeSibling(System.IO.Path.Combine(directory, $"{eventId}.dmp"));
        foreach (var sibling in Directory.EnumerateFiles(directory, $"{eventId}-*"))
        {
            DeleteEnvelopeSibling(sibling);
        }
    }

    private void DeleteEnvelopeSibling(string path)
    {
        try
        {
            File.Delete(path);
        }
        catch (Exception e)
        {
            this.Log().LogWarning(e, "Failed to delete crash envelope cache sibling.");
        }
    }

    private static bool IsCacheDirectory(string directory, Envelope envelope)
    {
        try
        {
            return SamePath(directory, envelope.TryGetHeader("cache_dir"));
        }
        catch (ArgumentException)
        {
            return false;
        }
        catch (NotSupportedException)
        {
            return false;
        }
        catch (PathTooLongException)
        {
            return false;
        }
    }

    private static bool SamePath(string? left, string? right)
    {
        if (string.IsNullOrWhiteSpace(left) || string.IsNullOrWhiteSpace(right))
        {
            return false;
        }

        return string.Equals(
            System.IO.Path.GetFullPath(left),
            System.IO.Path.GetFullPath(right),
            OperatingSystem.IsWindows() || OperatingSystem.IsMacOS()
                ? StringComparison.OrdinalIgnoreCase
                : StringComparison.Ordinal);
    }

    private static Feedback? ResolveFeedback()
    {
        var message = Environment.GetEnvironmentVariable("SENTRY_FEEDBACK_MESSAGE");
        var email = Environment.GetEnvironmentVariable("SENTRY_FEEDBACK_EMAIL");
        var name = Environment.GetEnvironmentVariable("SENTRY_FEEDBACK_NAME");
        if (string.IsNullOrWhiteSpace(message) &&
            string.IsNullOrWhiteSpace(email) &&
            string.IsNullOrWhiteSpace(name))
        {
            return null;
        }

        return new Feedback(name, email, message ?? string.Empty);
    }
}
