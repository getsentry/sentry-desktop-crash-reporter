using Microsoft.Extensions.DependencyInjection;

namespace Sentry.CrashReporter.Tests;

public class SentryClientTests
{
    private Mock<HttpMessageHandler> _messageHandler = null!;
    private SentryClient _client = null!;

    [SetUp]
    public void Setup()
    {
        _messageHandler = new Mock<HttpMessageHandler>();

        var services = new ServiceCollection();
        services.AddHttpClient("SentryClient")
            .ConfigurePrimaryHttpMessageHandler(() => _messageHandler.Object)
            .AddStandardResilienceHandler(options =>
            {
                App.ConfigureResilience(options);
                options.Retry.Delay = TimeSpan.FromMilliseconds(1);
            });
        var serviceProvider = services.BuildServiceProvider();
        var httpClientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();

        _client = new SentryClient(httpClientFactory);
    }

    [Test]
    [TestCase("data/two_items.envelope")]
    public async Task SubmitEnvelope_Sends_Request(string filePath)
    {
        await using var file = File.OpenRead(filePath);
        var envelope = await Envelope.DeserializeAsync(file);
        var dsn = envelope.TryGetDsn()!;

        var requestContent = "";
        _messageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((request, cancellationToken) =>
            {
                requestContent = request.Content!.ReadAsStringAsync(cancellationToken).GetAwaiter().GetResult();
            })
            .ReturnsAsync(new HttpResponseMessage { StatusCode = HttpStatusCode.OK });

        await _client.SubmitEnvelopeAsync(dsn, envelope);

        var expected = await File.ReadAllTextAsync(filePath);

        var requestLines = requestContent.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
        var expectedLines = expected.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);

        Assert.That(requestLines, Is.EqualTo(expectedLines));
    }

    [Test]
    [TestCase("data/two_items.envelope")]
    public async Task SubmitEnvelope_Retries_On_HttpRequestException(string filePath)
    {
        await using var file = File.OpenRead(filePath);
        var envelope = await Envelope.DeserializeAsync(file);
        var dsn = envelope.TryGetDsn()!;

        var attemptCount = 0;
        _messageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((_, _) =>
            {
                attemptCount++;
            })
            .ThrowsAsync(new HttpRequestException("Network error"));

        var exception = Assert.ThrowsAsync<HttpRequestException>(() => _client.SubmitEnvelopeAsync(dsn, envelope));

        Assert.That(exception!.Message, Is.EqualTo("Network error"));
        Assert.That(attemptCount, Is.EqualTo(4)); // 1 initial + 3 retries
    }

    [Test]
    [TestCase("data/two_items.envelope")]
    public async Task SubmitEnvelope_Retries_On_InternalServerError(string filePath)
    {
        await using var file = File.OpenRead(filePath);
        var envelope = await Envelope.DeserializeAsync(file);
        var dsn = envelope.TryGetDsn()!;

        var attemptCount = 0;
        _messageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((_, _) =>
            {
                attemptCount++;
            })
            .ReturnsAsync(() => new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.InternalServerError,
                Content = new StringContent("Internal Server Error")
            });

        Assert.ThrowsAsync<HttpRequestException>(() => _client.SubmitEnvelopeAsync(dsn, envelope));

        Assert.That(attemptCount, Is.EqualTo(4)); // 1 initial + 3 retries
    }

    [Test]
    [TestCase("data/two_items.envelope")]
    public async Task SubmitEnvelope_Retries_On_BadGateway(string filePath)
    {
        await using var file = File.OpenRead(filePath);
        var envelope = await Envelope.DeserializeAsync(file);
        var dsn = envelope.TryGetDsn()!;

        var attemptCount = 0;
        _messageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((_, _) =>
            {
                attemptCount++;
            })
            .ReturnsAsync(() => new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.BadGateway,
                Content = new StringContent("Bad Gateway")
            });

        Assert.ThrowsAsync<HttpRequestException>(() => _client.SubmitEnvelopeAsync(dsn, envelope));
        Assert.That(attemptCount, Is.EqualTo(4)); // 1 initial + 3 retries
    }

    [Test]
    [TestCase("data/two_items.envelope")]
    public async Task SubmitEnvelope_Retries_On_RequestTimeout(string filePath)
    {
        await using var file = File.OpenRead(filePath);
        var envelope = await Envelope.DeserializeAsync(file);
        var dsn = envelope.TryGetDsn()!;

        var attemptCount = 0;
        _messageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((_, _) =>
            {
                attemptCount++;
            })
            .ReturnsAsync(() => new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.RequestTimeout,
                Content = new StringContent("Request Timeout")
            });

        Assert.ThrowsAsync<HttpRequestException>(() => _client.SubmitEnvelopeAsync(dsn, envelope));

        Assert.That(attemptCount, Is.EqualTo(4)); // 1 initial + 3 retries
    }

    [Test]
    [TestCase("data/two_items.envelope")]
    public async Task SubmitEnvelope_DoesNot_Retry_On_BadRequest(string filePath)
    {
        await using var file = File.OpenRead(filePath);
        var envelope = await Envelope.DeserializeAsync(file);
        var dsn = envelope.TryGetDsn()!;

        var attemptCount = 0;
        _messageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((_, _) =>
            {
                attemptCount++;
            })
            .ReturnsAsync(new HttpResponseMessage { StatusCode = HttpStatusCode.BadRequest });

        Assert.ThrowsAsync<HttpRequestException>(() => _client.SubmitEnvelopeAsync(dsn, envelope));

        Assert.That(attemptCount, Is.EqualTo(1)); // No retries for client errors
    }

    [Test]
    [TestCase("data/two_items.envelope")]
    public async Task SubmitEnvelope_DoesNot_Retry_On_NotFound(string filePath)
    {
        await using var file = File.OpenRead(filePath);
        var envelope = await Envelope.DeserializeAsync(file);
        var dsn = envelope.TryGetDsn()!;

        var attemptCount = 0;
        _messageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((_, _) =>
            {
                attemptCount++;
            })
            .ReturnsAsync(new HttpResponseMessage { StatusCode = HttpStatusCode.NotFound });

        Assert.ThrowsAsync<HttpRequestException>(() => _client.SubmitEnvelopeAsync(dsn, envelope));

        Assert.That(attemptCount, Is.EqualTo(1)); // No retries for client errors
    }

    [Test]
    [TestCase("data/two_items.envelope")]
    public async Task SubmitEnvelope_Succeeds_After_Retry(string filePath)
    {
        await using var file = File.OpenRead(filePath);
        var envelope = await Envelope.DeserializeAsync(file);
        var dsn = envelope.TryGetDsn()!;

        var attemptCount = 0;
        _messageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .Returns<HttpRequestMessage, CancellationToken>((_, _) =>
            {
                attemptCount++;
                // Fail the first two attempts, succeed on the third
                if (attemptCount < 3)
                {
                    return Task.FromResult(new HttpResponseMessage { StatusCode = HttpStatusCode.InternalServerError });
                }
                return Task.FromResult(new HttpResponseMessage { StatusCode = HttpStatusCode.OK });
            });

        // Should not throw - succeeds on third attempt
        await _client.SubmitEnvelopeAsync(dsn, envelope);

        Assert.That(attemptCount, Is.EqualTo(3)); // 1 initial + 2 retries before success
    }

    [Test]
    [TestCase("data/two_items.envelope")]
    public async Task SubmitEnvelope_Retry_Preserves_Request_Content(string filePath)
    {
        await using var file = File.OpenRead(filePath);
        var envelope = await Envelope.DeserializeAsync(file);
        var dsn = envelope.TryGetDsn()!;

        var requestContents = new List<string>();
        var attemptCount = 0;

        _messageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((request, cancellationToken) =>
            {
                attemptCount++;
                var content = request.Content!.ReadAsStringAsync(cancellationToken).GetAwaiter().GetResult();
                requestContents.Add(content);
            })
            .Returns<HttpRequestMessage, CancellationToken>((_, _) =>
            {
                // Fail first attempt, succeed on second
                if (attemptCount == 1)
                {
                    return Task.FromResult(new HttpResponseMessage { StatusCode = HttpStatusCode.InternalServerError });
                }
                return Task.FromResult(new HttpResponseMessage { StatusCode = HttpStatusCode.OK });
            });

        await _client.SubmitEnvelopeAsync(dsn, envelope);

        Assert.That(requestContents, Has.Count.EqualTo(2));
        Assert.That(requestContents[0], Is.EqualTo(requestContents[1])); // Content should be identical across retries
        Assert.That(requestContents[0], Is.Not.Empty);
    }
}
