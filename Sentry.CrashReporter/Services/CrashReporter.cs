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
                // The crash was rate-limited before it could be delivered. Keep it in the
                // offline cache so it can be retried later instead of discarding it as if
                // it had been sent. Cache unconditionally (CancellationToken.None) so a
                // cancel cannot drop the still-undelivered crash.
                _submittingEnvelope = null;
                await CacheAsync(envelope, CancellationToken.None);
            }
            else
            {
                var cachedEnvelopePath = await CacheAsync(envelope, EffectiveCacheKeep, cancellationToken);
                _submittedEnvelopes.Add(envelope);
                DeleteEnvelope(envelope, cachedEnvelopePath);
            }
        }
        catch (Exception)
        {
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
