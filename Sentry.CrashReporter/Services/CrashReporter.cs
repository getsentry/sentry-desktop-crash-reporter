using System.Diagnostics;
using System.Text.Json.Nodes;

namespace Sentry.CrashReporter.Services;

public record Feedback(string? Name, string? Email, string Message);

public interface ICrashReporter
{
    string? Dsn { get; }
    string FilePath { get; }
    Feedback? Feedback { get; }
    public Task<Envelope?> LoadAsync(CancellationToken cancellationToken = default);
    public Task SubmitAsync(CancellationToken cancellationToken = default);
    public void UpdateFeedback(Feedback? feedback);
}

public class CrashReporter : ICrashReporter
{
    private readonly ISentryClient _client;
    private readonly TaskCompletionSource<Envelope?> _envelope = new();
    private Feedback? _feedback;

    public string? Dsn { get; private set; }
    public string FilePath { get; }
    public Feedback? Feedback => _feedback;

    public CrashReporter(StorageFile? file = null, ISentryClient? client = null)
    {
        _client = client ?? App.Services.GetRequiredService<ISentryClient>();
        _feedback = ResolveFeedback();
        Dsn = Environment.GetEnvironmentVariable("SENTRY_TEST_DSN");
        FilePath = file?.Path ?? string.Empty;
        _ = InitAsync(file);
    }

    private async Task InitAsync(StorageFile? file, CancellationToken cancellationToken = default)
    {
        if (file is null)
        {
            _envelope.SetResult(null);
            return;
        }

        try
        {
            var stopwatch = Stopwatch.StartNew();
            await using var stream = await file.OpenStreamForReadAsync();
            var envelope = await Envelope.DeserializeAsync(stream, cancellationToken);
            stopwatch.Stop();
            this.Log().LogInformation($"Loaded {FilePath} in {stopwatch.ElapsedMilliseconds} ms.");
            Dsn ??= envelope.TryGetDsn();
            _envelope.SetResult(envelope);
        }
        catch (Exception ex)
        {
            this.Log().LogError(ex, $"Failed to load envelope from {FilePath}");
        }
    }

    public Task<Envelope?> LoadAsync(CancellationToken cancellationToken = default)
    {
        return _envelope.Task;
    }

    public async Task SubmitAsync(CancellationToken cancellationToken = default)
    {
        var envelope = await LoadAsync(cancellationToken) ?? throw new InvalidOperationException("No envelope to submit.");
        var dsn = Dsn ?? envelope.TryGetDsn()
                  ?? throw new InvalidOperationException("Envelope does not contain a valid DSN.");

        await _client.SubmitEnvelopeAsync(dsn, envelope, cancellationToken);

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
