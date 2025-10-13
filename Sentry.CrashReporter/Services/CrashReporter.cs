using System.Diagnostics;
using System.Text.Json.Nodes;

namespace Sentry.CrashReporter.Services;

public record Feedback(string? Name, string? Email, string Message);

public interface ICrashReporter
{
    string? Dsn { get; }
    string FilePath { get; }
    Feedback? Feedback { get; }
    public ValueTask<Envelope?> LoadAsync(CancellationToken cancellationToken = default);
    public Task SubmitAsync(CancellationToken cancellationToken = default);
    public void UpdateFeedback(Feedback? feedback);
}

public class CrashReporter(StorageFile? file = null, ISentryClient? client = null) : ICrashReporter
{
    private readonly ISentryClient _client = client ?? App.Services.GetRequiredService<ISentryClient>();
    private Envelope? _envelope;
    private Feedback? _feedback = ResolveFeedback();

    public string? Dsn { get; private set; } = Environment.GetEnvironmentVariable("SENTRY_TEST_DSN");
    public string FilePath { get => file?.Path ?? string.Empty; }
    public Feedback? Feedback { get => _feedback; }

    public async ValueTask<Envelope?> LoadAsync(CancellationToken cancellationToken = default)
    {
        if (file is null)
        {
            return null;
        }

        if (_envelope != null)
        {
            return _envelope;
        }

        try
        {
            var stopwatch = Stopwatch.StartNew();
            await using var stream = await file.OpenStreamForReadAsync();
            var envelope = await Envelope.DeserializeAsync(stream, cancellationToken);
            stopwatch.Stop();
            this.Log().LogInformation($"Loaded {FilePath} in {stopwatch.ElapsedMilliseconds} ms.");
            _envelope = envelope;
            Dsn ??= envelope.TryGetDsn();
            return envelope;
        }
        catch (Exception ex)
        {
            this.Log().LogError(ex, $"Failed to load envelope from {FilePath}");
        }

        return null;
    }

    // private async Task<string> ComputeFileHashAsync(CancellationToken cancellationToken)
    // {
    //     await using var stream = File.OpenRead(filePath);
    //     using var sha256 = SHA256.Create();
    //     var hash = await sha256.ComputeHashAsync(stream, cancellationToken);
    //     return Convert.ToHexString(hash);
    // }

    public async Task SubmitAsync(CancellationToken cancellationToken = default)
    {
        var envelope = await LoadAsync(cancellationToken) ?? throw new InvalidOperationException("No envelope to submit.");
        var dsn = Dsn ?? envelope!.TryGetDsn()
                  ?? throw new InvalidOperationException("Envelope does not contain a valid DSN.");

        await _client.SubmitEnvelopeAsync(dsn, envelope, cancellationToken);

        if (_feedback != null)
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
                                ["associated_event_id"] = _envelope!.TryGetEventId()?.Replace("-", "")
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
