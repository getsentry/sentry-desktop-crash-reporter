using System.Diagnostics;
using System.Text.Json.Nodes;

namespace Sentry.CrashReporter.Services;

public record Feedback(string? Name, string? Email, string Message);

public interface ICrashReporter
{
    Feedback? Feedback { get; }
    public Task<Envelope?> LoadAsync(CancellationToken cancellationToken = default);
    public Task CacheAsync(Envelope envelope, CancellationToken cancellationToken = default);
    public Task SubmitAsync(Envelope envelope, CancellationToken cancellationToken = default);
    public void UpdateFeedback(Feedback? feedback);
}

public class CrashReporter(IStorageFile? file = null, ISentryClient? client = null) : ICrashReporter
{
    private readonly IStorageFile? _file = file ?? App.Services.GetService<IStorageFile>();
    private readonly ISentryClient _client = client ?? App.Services.GetRequiredService<ISentryClient>();
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

    public async Task SubmitAsync(Envelope envelope, CancellationToken cancellationToken = default)
    {
        var dsn = envelope.TryGetDsn()
                  ?? throw new InvalidOperationException("Envelope does not contain a valid DSN.");

        try
        {
            await _client.SubmitEnvelopeAsync(dsn, envelope, cancellationToken);
            _submittedEnvelopes.Add(envelope);
            DeleteFiles(envelope);
        }
        catch (Exception) when (!cancellationToken.IsCancellationRequested)
        {
            await CacheAsync(envelope);
            throw;
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
            await _client.SubmitEnvelopeAsync(dsn, feedback, cancellationToken);
        }
    }

    public void UpdateFeedback(Feedback? feedback)
    {
        _feedback = feedback;
    }

    public async Task CacheAsync(Envelope envelope, CancellationToken cancellationToken = default)
    {
        if (_submittedEnvelopes.Contains(envelope))
        {
            DeleteFiles(envelope);
            return;
        }

        string? cacheDir = envelope.TryGetHeader("cache_dir");
        if (string.IsNullOrWhiteSpace(cacheDir))
        {
            DeleteFiles(envelope);
            return;
        }

        string? envelopeTempPath = null;
        try
        {
            Directory.CreateDirectory(cacheDir);

            var eventId = GetCacheEventId(envelope);
            var envelopePath = System.IO.Path.Combine(cacheDir, $"{eventId}.envelope");
            if (File.Exists(envelopePath))
            {
                if (DeleteFiles(envelope, envelopePath))
                {
                    envelope.FilePath = envelopePath;
                }
                return;
            }

            var minidumps = envelope.Items
                .Where(item => item.TryGetHeader("attachment_type") == "event.minidump")
                .ToList();

            if (minidumps.Count == 0 && TryMoveFiles(envelope, envelopePath))
            {
                return;
            }

            for (var i = 0; i < minidumps.Count; i++)
            {
                var path = System.IO.Path.Combine(cacheDir, $"{eventId}-{i}.dmp");
                await File.WriteAllBytesAsync(path, minidumps[i].Payload, cancellationToken);
            }

            var cachedEnvelope = new Envelope(envelope.Header, envelope.Items.Except(minidumps));
            envelopeTempPath = System.IO.Path.Combine(cacheDir, $"{eventId}.{Guid.NewGuid():N}.tmp");
            await using (var stream = File.Create(envelopeTempPath))
            {
                await cachedEnvelope.SerializeAsync(stream, cancellationToken);
            }
            File.Move(envelopeTempPath, envelopePath);
            envelopeTempPath = null;
            if (DeleteFiles(envelope, envelopePath))
            {
                envelope.FilePath = envelopePath;
            }
        }
        catch (Exception e)
        {
            try
            {
                if (envelopeTempPath is not null && File.Exists(envelopeTempPath))
                {
                    File.Delete(envelopeTempPath);
                }
            }
            catch (Exception cleanupException)
            {
                this.Log().LogWarning(cleanupException, "Failed to delete temporary crash envelope cache file.");
            }
            this.Log().LogWarning(e, "Failed to cache crash envelope.");
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

    private static bool TryMoveFiles(Envelope envelope, string envelopePath)
    {
        var sourcePath = envelope.FilePath;
        if (string.IsNullOrWhiteSpace(sourcePath) || !File.Exists(sourcePath))
        {
            return false;
        }

        if (!SamePath(sourcePath, envelopePath))
        {
            MoveCacheSiblings(sourcePath, envelopePath, envelope);
            File.Move(sourcePath, envelopePath);
        }
        envelope.FilePath = envelopePath;
        return true;
    }

    private static void MoveCacheSiblings(string sourcePath, string envelopePath, Envelope envelope)
    {
        if (!Guid.TryParse(envelope.TryGetEventId(), out var eventId))
        {
            return;
        }

        var sourceDir = System.IO.Path.GetDirectoryName(sourcePath);
        var envelopeDir = System.IO.Path.GetDirectoryName(envelopePath);
        if (string.IsNullOrWhiteSpace(sourceDir) || string.IsNullOrWhiteSpace(envelopeDir) ||
            !Directory.Exists(sourceDir))
        {
            return;
        }

        foreach (var siblingPath in Directory.EnumerateFiles(sourceDir, $"{eventId:D}-*"))
        {
            var targetPath = System.IO.Path.Combine(envelopeDir, System.IO.Path.GetFileName(siblingPath));
            if (!SamePath(siblingPath, targetPath))
            {
                File.Move(siblingPath, targetPath);
            }
        }
    }

    private bool DeleteFiles(Envelope envelope, string? preservedPath = null)
    {
        var envelopePath = envelope.FilePath;
        if (!DeleteEnvelope(envelope, preservedPath))
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(envelopePath) || !Guid.TryParse(envelope.TryGetEventId(), out var eventId))
        {
            return true;
        }

        if (SamePath(envelopePath, preservedPath))
        {
            return true;
        }

        var directory = System.IO.Path.GetDirectoryName(envelopePath);
        if (string.IsNullOrWhiteSpace(directory) || !Directory.Exists(directory))
        {
            return true;
        }

        DeleteCacheSiblings(directory, eventId);
        return true;
    }

    private void DeleteCacheSiblings(string directory, Guid eventId)
    {
        foreach (var siblingPath in Directory.EnumerateFiles(directory, $"{eventId:D}-*"))
        {
            try
            {
                File.Delete(siblingPath);
            }
            catch (Exception e)
            {
                this.Log().LogWarning(e, "Failed to delete crash envelope cache sibling.");
            }
        }
    }

    private bool DeleteEnvelope(Envelope envelope, string? preservedPath = null)
    {
        var sourcePath = envelope.FilePath;
        if (string.IsNullOrWhiteSpace(sourcePath) || SamePath(sourcePath, preservedPath))
        {
            return true;
        }

        if (!File.Exists(sourcePath))
        {
            envelope.FilePath = null;
            return true;
        }

        try
        {
            File.Delete(sourcePath);
            envelope.FilePath = null;
            return true;
        }
        catch (Exception e)
        {
            this.Log().LogWarning(e, "Failed to delete consumed crash envelope.");
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
