using System.Net;

namespace Sentry.CrashReporter.Services;

public interface ISentryClient
{
    public Task SubmitEnvelopeAsync(string dsn, Envelope envelope, IProgress<double>? progress = null, CancellationToken cancellationToken = default);
}

public class SentryClient(IHttpClientFactory httpClientFactory) : ISentryClient
{
    public async Task SubmitEnvelopeAsync(string dsn, Envelope envelope, IProgress<double>? progress = null, CancellationToken cancellationToken = default)
    {
        // <scheme>://<key>@<host>:<port>/<project-id> ->
        // <scheme>://<host>:<port>/api/<project-id>/envelope
        var uri = new Uri(dsn);
        var projectId = uri.LocalPath.Trim('/');
        var uriBuilder = new UriBuilder()
        {
            Scheme = uri.Scheme,
            Host = uri.Host,
            Port = uri.Port,
            Path = $"/api/{projectId}/envelope/"
        };

        var stream = new MemoryStream();
        await envelope.SerializeAsync(stream, cancellationToken).ConfigureAwait(false);
        await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
        stream.Seek(0, SeekOrigin.Begin);

        progress?.Report(0);

        var request = new HttpRequestMessage(HttpMethod.Post, uriBuilder.Uri)
        {
            Content = new ProgressStreamContent(stream, progress)
        };
        using var httpClient = httpClientFactory.CreateClient(nameof(SentryClient));
        using var response = await httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        progress?.Report(1);

        var content = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        this.Log().LogInformation(content);
    }

    private sealed class ProgressStreamContent(Stream source, IProgress<double>? progress)
        : HttpContent
    {
        private const int BufferSize = 81920;
        private readonly Stream _source = source;
        private readonly IProgress<double>? _progress = progress;

        protected override bool TryComputeLength(out long length)
        {
            length = _source.Length;
            return true;
        }

        protected override async Task SerializeToStreamAsync(Stream stream, TransportContext? context)
        {
            await SerializeToStreamAsyncCore(stream, CancellationToken.None).ConfigureAwait(false);
        }

        protected override async Task SerializeToStreamAsync(
            Stream stream,
            TransportContext? context,
            CancellationToken cancellationToken)
        {
            await SerializeToStreamAsyncCore(stream, cancellationToken).ConfigureAwait(false);
        }

        private async Task SerializeToStreamAsyncCore(Stream stream, CancellationToken cancellationToken)
        {
            if (_source.CanSeek)
            {
                _source.Position = 0;
            }

            var buffer = new byte[BufferSize];
            var totalLength = _source.Length;
            long uploaded = 0;
            int read;

            while ((read = await _source.ReadAsync(buffer, cancellationToken).ConfigureAwait(false)) > 0)
            {
                await stream.WriteAsync(buffer.AsMemory(0, read), cancellationToken).ConfigureAwait(false);
                uploaded += read;
                if (totalLength > 0)
                {
                    _progress?.Report((double)uploaded / totalLength);
                }
            }
        }
    }
}
