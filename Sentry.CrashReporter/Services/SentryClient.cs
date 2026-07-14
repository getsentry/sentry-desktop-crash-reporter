using System.Net;

namespace Sentry.CrashReporter.Services;

public interface ISentryClient
{
    public Task SubmitEnvelopeAsync(string dsn, Envelope envelope, IProgress<double>? progress = null, CancellationToken cancellationToken = default);
}

public class SentryClient(IHttpClientFactory httpClientFactory) : ISentryClient
{
    // A 429 disables at most one new category per response, and there are only a
    // handful of categories, so this cap is just a safety net against looping.
    private const int MaxRateLimitAttempts = 5;

    private readonly RateLimiter _rateLimiter = new();

    public async Task SubmitEnvelopeAsync(string dsn, Envelope envelope, IProgress<double>? progress = null, CancellationToken cancellationToken = default)
    {
        var endpoint = BuildEndpoint(dsn);

        // Mirror sentry-native: filter rate-limited categories out before serializing,
        // and on a 429 back off and resend whatever is still allowed. This lets the
        // submission complete (and the window close) even when some telemetry is
        // dropped, instead of failing the whole envelope on a single limited category.
        for (var attempt = 0; attempt < MaxRateLimitAttempts; attempt++)
        {
            var items = envelope.Items.Where(NotRateLimited).ToList();
            if (items.Count < envelope.Items.Count)
            {
                this.Log().LogWarning("Skipping {Count} rate-limited envelope item(s).",
                    envelope.Items.Count - items.Count);
            }

            if (items.Count == 0)
            {
                // Every category is backed off; there is nothing left to send.
                progress?.Report(1);
                return;
            }

            var stream = new MemoryStream();
            await new Envelope(envelope.Header, items).SerializeAsync(stream, cancellationToken).ConfigureAwait(false);
            await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
            stream.Seek(0, SeekOrigin.Begin);

            progress?.Report(0);

            var request = new HttpRequestMessage(HttpMethod.Post, endpoint)
            {
                Content = new ProgressStreamContent(stream, progress)
            };
            using var httpClient = httpClientFactory.CreateClient(nameof(SentryClient));
            using var response = await httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);

            _rateLimiter.Update(response);

            if (response.StatusCode != HttpStatusCode.TooManyRequests)
            {
                response.EnsureSuccessStatusCode();
                progress?.Report(1);

                var content = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                this.Log().LogInformation(content);
                return;
            }

            // 429: the limiter was updated above. Resend the still-allowed subset on the
            // next iteration. If none of the items we just tried got disabled we cannot
            // make progress, so stop rather than resend the same payload indefinitely.
            if (items.All(NotRateLimited))
            {
                this.Log().LogWarning("Rate limited (HTTP 429); no further items can be sent.");
                progress?.Report(1);
                return;
            }
        }

        this.Log().LogWarning("Rate limited (HTTP 429); giving up after {Attempts} attempts.", MaxRateLimitAttempts);
        progress?.Report(1);

        bool NotRateLimited(EnvelopeItem item)
        {
            var category = RateLimiter.GetCategory(item);
            return category is null || !_rateLimiter.IsDisabled(category.Value);
        }
    }

    private static Uri BuildEndpoint(string dsn)
    {
        // <scheme>://<key>@<host>:<port>/<project-id> ->
        // <scheme>://<host>:<port>/api/<project-id>/envelope
        var uri = new Uri(dsn);
        var projectId = uri.LocalPath.Trim('/');
        return new UriBuilder
        {
            Scheme = uri.Scheme,
            Host = uri.Host,
            Port = uri.Port,
            Path = $"/api/{projectId}/envelope/"
        }.Uri;
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
