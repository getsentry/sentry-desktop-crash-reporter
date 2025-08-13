namespace Sentry.CrashReporter.Services;

public record Feedback(string Name, string Email, string Message);


public interface ISentryClient
{
    public void UpdateFeedback(Feedback feedback);
    public Task SubmitEnvelopeAsync(Envelope envelope, CancellationToken cancellationToken = default);
}

public class SentryClient(HttpClient httpClient) : ISentryClient
{
    private Feedback? _feedback;

    public void UpdateFeedback(Feedback feedback)
    {
        _feedback = feedback;
    }

    public async Task SubmitEnvelopeAsync(Envelope envelope, CancellationToken cancellationToken = default)
    {
        var dsn = envelope.TryGetDsn()
                  ?? throw new InvalidOperationException("Envelope does not contain a valid DSN.");

        // TODO: merge feedback into the same envelope
        // if (!string.IsNullOrEmpty(_feedback?.Description))
        // {
        //     envelope = envelope.WithItems([
        //         (Header: new { type = "feedback" }, Payload: new
        //         {
        //             contexts = new
        //             {
        //                 feedback = new
        //                 {
        //                     contact_email = _feedback.Email,
        //                     name = _feedback.Name,
        //                     message = _feedback.Message,
        //                     associated_event_id = envelope.TryGetEventId()?.Replace("-", "")
        //                 }
        //             }
        //         })
        //     ]);
        // }

        // <scheme>://<key>@<host>:<port>/<project-id> ->
        // <scheme>://<key>@<host>:<port>/api/<project-id>/envelope
        var projectId = new Uri(dsn).LocalPath.Trim('/');
        var uriBuilder = new UriBuilder(dsn)
        {
            Path = $"/api/{projectId}/envelope/"
        };

        using var stream = new MemoryStream();
        await envelope.SerializeAsync(stream, cancellationToken).ConfigureAwait(false);
        await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
        stream.Seek(0, SeekOrigin.Begin);

        var request = new HttpRequestMessage(HttpMethod.Post, uriBuilder.Uri)
        {
            Content = new StreamContent(stream)
        };
        using var response = await httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        this.Log().LogInformation(content);

        // TODO: merge feedback into the same envelope
        if (_feedback != null)
        {
            var feedback = Envelope.FromJson(new { dsn },
                [
                    (Header: new { type = "feedback" }, Payload: new
                    {
                        contexts = new
                        {
                            feedback = new
                            {
                                contact_email = _feedback.Email,
                                name = _feedback.Name,
                                message = _feedback.Message,
                                associated_event_id = envelope.TryGetEventId()?.Replace("-", "")
                            }
                        }
                    })
                ]
            );
            _feedback = null;
            await SubmitEnvelopeAsync(feedback, cancellationToken);
        }
    }
}
