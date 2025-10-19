using System.Diagnostics;
using System.Text.Json.Nodes;

namespace Sentry.CrashReporter.Services;

public record Feedback(string? Name, string? Email, string Message);

public interface ICrashReporter
{
    Feedback? Feedback { get; }
    public Task<Envelope?> LoadAsync(CancellationToken cancellationToken = default);
    public Task SubmitAsync(Envelope envelope, CancellationToken cancellationToken = default);
    public void UpdateFeedback(Feedback? feedback);
}

public class CrashReporter(IStorageFile? file = null, ISentryClient? client = null) : ICrashReporter
{
    private readonly IStorageFile? _file = file ?? App.Services.GetService<IStorageFile>();
    private readonly ISentryClient _client = client ?? App.Services.GetRequiredService<ISentryClient>();
    private Feedback? _feedback = ResolveFeedback();

    public Feedback? Feedback => _feedback;

    public async Task<Envelope?> LoadAsync(CancellationToken cancellationToken = default)
    {
        if (_file is null)
        {
            return null;
        }

        try
        {
            var stopwatch = Stopwatch.StartNew();
            var envelope = await Envelope.FromStorageFileAsync(_file, cancellationToken);
            stopwatch.Stop();
            this.Log().LogInformation($"Loaded {_file.Path} in {stopwatch.ElapsedMilliseconds} ms.");
            return envelope;
        }
        catch (Exception ex)
        {
            this.Log().LogError(ex, $"Failed to load envelope from {_file.Path}");
            // TODO: propagate error
            return null;
        }
    }

    public async Task SubmitAsync(Envelope envelope, CancellationToken cancellationToken = default)
    {
        var dsn = envelope.TryGetDsn()
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
