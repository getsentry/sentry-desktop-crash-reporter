namespace Sentry.CrashReporter.Services;

public interface ISentryClient
{
    public Task SubmitEnvelopeAsync(string dsn, Envelope envelope, CancellationToken cancellationToken = default);
}

public class SentryClient(HttpClient httpClient) : ISentryClient
{
    public async Task SubmitEnvelopeAsync(string dsn, Envelope envelope, CancellationToken cancellationToken = default)
    {
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
    }
}
