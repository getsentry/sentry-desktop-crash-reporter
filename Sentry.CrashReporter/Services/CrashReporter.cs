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
            return;
        }

        string? cacheDir = envelope.TryGetHeader("cache_dir");
        if (string.IsNullOrWhiteSpace(cacheDir))
        {
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
                return;
            }

            var minidumps = envelope.Items
                .Where(item => item.TryGetHeader("attachment_type") == "event.minidump")
                .ToList();

            for (var i = 0; i < minidumps.Count; i++)
            {
                var suffix = i == 0 ? ".dmp" : $"-{i}.dmp";
                var path = System.IO.Path.Combine(cacheDir, $"{eventId}{suffix}");
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
        if (Guid.TryParse(envelope.TryGetEventId(), out var eventId))
        {
            return eventId.ToString("D");
        }

        eventId = Guid.NewGuid();
        envelope.Header["event_id"] = eventId.ToString("N");
        return eventId.ToString("D");
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
