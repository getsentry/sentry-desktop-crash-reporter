using System.Diagnostics;
using System.Text.Json.Nodes;

namespace Sentry.CrashReporter.Services;

public record Feedback(string? Name, string? Email, string Message);

public interface ICrashReporter
{
    string FilePath { get; }
    public ValueTask<Envelope?> LoadAsync(CancellationToken cancellationToken = default);
    public Task SubmitAsync(CancellationToken cancellationToken = default);
    public void UpdateFeedback(Feedback? feedback);
}

public class CrashReporter(string filePath, ISentryClient? client = null) : ICrashReporter
{
    private readonly ISentryClient _client = client ?? Ioc.Default.GetRequiredService<ISentryClient>();
    private Envelope? _envelope;
    private Feedback? _feedback;

    public string FilePath { get; } = filePath;

    public async ValueTask<Envelope?> LoadAsync(CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(FilePath))
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
            await using var file = File.OpenRead(FilePath);
            var envelope = await Envelope.DeserializeAsync(file, cancellationToken);
            stopwatch.Stop();
            this.Log().LogInformation($"Loaded {FilePath} in {stopwatch.ElapsedMilliseconds} ms.");
            _envelope = envelope;
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
        var dsn = _envelope?.TryGetDsn()
                  ?? throw new InvalidOperationException("Envelope does not contain a valid DSN.");

        await _client.SubmitEnvelopeAsync(_envelope!, cancellationToken);

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
                                ["associated_event_id"] = _envelope.TryGetEventId()?.Replace("-", "")
                            }
                        }
                    })
                ]
            );
            await _client.SubmitEnvelopeAsync(feedback, cancellationToken);
        }
    }

    public void UpdateFeedback(Feedback? feedback)
    {
        _feedback = feedback;
    }
}
