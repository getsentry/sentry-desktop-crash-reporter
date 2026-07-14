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
        var envelope = await Envelope.FromFileStreamAsync(file);
        var dsn = envelope.TryGetDsn()!;
        var expectedContent = await File.ReadAllTextAsync(filePath);

        var requestContent = "";
        _messageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((request, cancellationToken) =>
            {
                requestContent = request.Content!.ReadAsStringAsync(cancellationToken).GetAwaiter().GetResult();
            })
            .ReturnsAsync(new HttpResponseMessage { StatusCode = HttpStatusCode.OK });

        await _client.SubmitEnvelopeAsync(dsn, envelope);

        Assert.That(requestContent.RemoveBlankLines(), Is.EqualTo(expectedContent.RemoveBlankLines()));
    }

    [Test]
    [TestCase("data/two_items.envelope")]
    public async Task SubmitEnvelope_Retries_On_HttpRequestException(string filePath)
    {
        await using var file = File.OpenRead(filePath);
        var envelope = await Envelope.DeserializeAsync(file);
        var dsn = envelope.TryGetDsn()!;
        var expectedContent = await File.ReadAllTextAsync(filePath);

        var requestContents = new List<string>();
        _messageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((request, cancellationToken) =>
            {
                var content = request.Content!.ReadAsStringAsync(cancellationToken).GetAwaiter().GetResult();
                requestContents.Add(content);
            })
            .ThrowsAsync(new HttpRequestException("Network error"));

        var exception = Assert.ThrowsAsync<HttpRequestException>(() => _client.SubmitEnvelopeAsync(dsn, envelope));

        Assert.That(exception!.Message, Is.EqualTo("Network error"));
        Assert.That(requestContents, Has.Count.EqualTo(4)); // 1 initial + 3 retries
        foreach (var requestContent in requestContents)
        {
            Assert.That(requestContent.RemoveBlankLines(), Is.EqualTo(expectedContent.RemoveBlankLines()));
        }
    }

    [Test]
    [TestCase("data/two_items.envelope")]
    public async Task SubmitEnvelope_Retries_On_InternalServerError(string filePath)
    {
        await using var file = File.OpenRead(filePath);
        var envelope = await Envelope.DeserializeAsync(file);
        var dsn = envelope.TryGetDsn()!;
        var expectedContent = await File.ReadAllTextAsync(filePath);

        var requestContents = new List<string>();
        _messageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((request, cancellationToken) =>
            {
                var content = request.Content!.ReadAsStringAsync(cancellationToken).GetAwaiter().GetResult();
                requestContents.Add(content);
            })
            .ReturnsAsync(() => new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.InternalServerError,
                Content = new StringContent("Internal Server Error")
            });

        Assert.ThrowsAsync<HttpRequestException>(() => _client.SubmitEnvelopeAsync(dsn, envelope));

        Assert.That(requestContents, Has.Count.EqualTo(4)); // 1 initial + 3 retries
        foreach (var requestContent in requestContents)
        {
            Assert.That(requestContent.RemoveBlankLines(), Is.EqualTo(expectedContent.RemoveBlankLines()));
        }
    }

    [Test]
    [TestCase("data/two_items.envelope")]
    public async Task SubmitEnvelope_Retries_On_BadGateway(string filePath)
    {
        await using var file = File.OpenRead(filePath);
        var envelope = await Envelope.DeserializeAsync(file);
        var dsn = envelope.TryGetDsn()!;
        var expectedContent = await File.ReadAllTextAsync(filePath);

        var requestContents = new List<string>();
        _messageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((request, cancellationToken) =>
            {
                var content = request.Content!.ReadAsStringAsync(cancellationToken).GetAwaiter().GetResult();
                requestContents.Add(content);
            })
            .ReturnsAsync(() => new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.BadGateway,
                Content = new StringContent("Bad Gateway")
            });

        Assert.ThrowsAsync<HttpRequestException>(() => _client.SubmitEnvelopeAsync(dsn, envelope));
        Assert.That(requestContents, Has.Count.EqualTo(4)); // 1 initial + 3 retries
        foreach (var requestContent in requestContents)
        {
            Assert.That(requestContent.RemoveBlankLines(), Is.EqualTo(expectedContent.RemoveBlankLines()));
        }
    }

    [Test]
    [TestCase("data/two_items.envelope")]
    public async Task SubmitEnvelope_Retries_On_RequestTimeout(string filePath)
    {
        await using var file = File.OpenRead(filePath);
        var envelope = await Envelope.DeserializeAsync(file);
        var dsn = envelope.TryGetDsn()!;
        var expectedContent = await File.ReadAllTextAsync(filePath);

        var requestContents = new List<string>();
        _messageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((request, cancellationToken) =>
            {
                var content = request.Content!.ReadAsStringAsync(cancellationToken).GetAwaiter().GetResult();
                requestContents.Add(content);
            })
            .ReturnsAsync(() => new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.RequestTimeout,
                Content = new StringContent("Request Timeout")
            });

        Assert.ThrowsAsync<HttpRequestException>(() => _client.SubmitEnvelopeAsync(dsn, envelope));

        Assert.That(requestContents, Has.Count.EqualTo(4)); // 1 initial + 3 retries
        foreach (var requestContent in requestContents)
        {
            Assert.That(requestContent.RemoveBlankLines(), Is.EqualTo(expectedContent.RemoveBlankLines()));
        }
    }

    [Test]
    [TestCase("data/two_items.envelope")]
    public async Task SubmitEnvelope_DoesNot_Retry_On_BadRequest(string filePath)
    {
        await using var file = File.OpenRead(filePath);
        var envelope = await Envelope.DeserializeAsync(file);
        var dsn = envelope.TryGetDsn()!;
        var expectedContent = await File.ReadAllTextAsync(filePath);

        var requestContents = new List<string>();
        _messageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((request, cancellationToken) =>
            {
                var content = request.Content!.ReadAsStringAsync(cancellationToken).GetAwaiter().GetResult();
                requestContents.Add(content);
            })
            .ReturnsAsync(new HttpResponseMessage { StatusCode = HttpStatusCode.BadRequest });

        Assert.ThrowsAsync<HttpRequestException>(() => _client.SubmitEnvelopeAsync(dsn, envelope));

        Assert.That(requestContents, Has.Count.EqualTo(1)); // No retries for client errors
        Assert.That(requestContents[0].RemoveBlankLines(), Is.EqualTo(expectedContent.RemoveBlankLines()));
    }

    [Test]
    [TestCase("data/two_items.envelope")]
    public async Task SubmitEnvelope_On_RequestEntityTooLarge_Throws_With_Status_And_Does_Not_Retry(string filePath)
    {
        await using var file = File.OpenRead(filePath);
        var envelope = await Envelope.DeserializeAsync(file);
        var dsn = envelope.TryGetDsn()!;

        var requestContents = new List<string>();
        _messageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((request, cancellationToken) =>
            {
                requestContents.Add(request.Content!.ReadAsStringAsync(cancellationToken).GetAwaiter().GetResult());
            })
            .ReturnsAsync(() => new HttpResponseMessage(HttpStatusCode.RequestEntityTooLarge));

        // 413 must surface as a status-bearing exception (so the caller discards it) and
        // must not be retried.
        var exception = Assert.ThrowsAsync<HttpRequestException>(() => _client.SubmitEnvelopeAsync(dsn, envelope));

        Assert.That(exception!.StatusCode, Is.EqualTo(HttpStatusCode.RequestEntityTooLarge));
        Assert.That(requestContents, Has.Count.EqualTo(1)); // No retries for 413
    }

    [Test]
    [TestCase("data/two_items.envelope")]
    public async Task SubmitEnvelope_DoesNot_Retry_On_NotFound(string filePath)
    {
        await using var file = File.OpenRead(filePath);
        var envelope = await Envelope.DeserializeAsync(file);
        var dsn = envelope.TryGetDsn()!;
        var expectedContent = await File.ReadAllTextAsync(filePath);

        var requestContents = new List<string>();
        _messageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((request, cancellationToken) =>
            {
                var content = request.Content!.ReadAsStringAsync(cancellationToken).GetAwaiter().GetResult();
                requestContents.Add(content);
            })
            .ReturnsAsync(new HttpResponseMessage { StatusCode = HttpStatusCode.NotFound });

        Assert.ThrowsAsync<HttpRequestException>(() => _client.SubmitEnvelopeAsync(dsn, envelope));

        Assert.That(requestContents, Has.Count.EqualTo(1)); // No retries for client errors
        Assert.That(requestContents[0].RemoveBlankLines(), Is.EqualTo(expectedContent.RemoveBlankLines()));
    }

    [Test]
    [TestCase("data/two_items.envelope")]
    public async Task SubmitEnvelope_Succeeds_After_Retry(string filePath)
    {
        await using var file = File.OpenRead(filePath);
        var envelope = await Envelope.DeserializeAsync(file);
        var dsn = envelope.TryGetDsn()!;
        var expectedContent = await File.ReadAllTextAsync(filePath);

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
                // Fail the first two attempts, succeed on the third
                if (attemptCount < 3)
                {
                    return Task.FromResult(new HttpResponseMessage { StatusCode = HttpStatusCode.InternalServerError });
                }
                return Task.FromResult(new HttpResponseMessage { StatusCode = HttpStatusCode.OK });
            });

        // Should not throw - succeeds on third attempt
        await _client.SubmitEnvelopeAsync(dsn, envelope);

        Assert.That(requestContents, Has.Count.EqualTo(3)); // 1 initial + 2 retries before success
        foreach (var requestContent in requestContents)
        {
            Assert.That(requestContent.RemoveBlankLines(), Is.EqualTo(expectedContent.RemoveBlankLines()));
        }
    }

    [Test]
    [TestCase("data/two_items.envelope")]
    public async Task SubmitEnvelope_On_TooManyRequests_Drops_Limited_Items_Without_Throwing(string filePath)
    {
        await using var file = File.OpenRead(filePath);
        var envelope = await Envelope.DeserializeAsync(file);
        var dsn = envelope.TryGetDsn()!;

        // two_items.envelope contains an event + attachment, both in the ERROR category.
        var requestContents = new List<string>();
        _messageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((request, cancellationToken) =>
            {
                requestContents.Add(request.Content!.ReadAsStringAsync(cancellationToken).GetAwaiter().GetResult());
            })
            .ReturnsAsync(() =>
            {
                var response = new HttpResponseMessage(HttpStatusCode.TooManyRequests);
                response.Headers.TryAddWithoutValidation("X-Sentry-Rate-Limits", "60:error");
                return response;
            });

        // A 429 must not throw: it backs off the limited category so the submission
        // completes and the crash reporter window can close.
        var result = await _client.SubmitEnvelopeAsync(dsn, envelope);

        // Every item is ERROR, so after the first 429 there is nothing left to resend,
        // and the crash was not delivered.
        Assert.That(requestContents, Has.Count.EqualTo(1));
        Assert.That(result, Is.EqualTo(SubmitResult.RateLimited));
    }

    [Test]
    public async Task SubmitEnvelope_On_TooManyRequests_Resends_Still_Allowed_Items()
    {
        var dsn = "https://key@host/1";
        var envelope = Envelope.FromJson(
            new JsonObject { ["dsn"] = dsn, ["event_id"] = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa" },
            new[]
            {
                (Header: new JsonObject { ["type"] = "event" }, Payload: new JsonObject { ["message"] = "boom" }),
                (Header: new JsonObject { ["type"] = "session" }, Payload: new JsonObject { ["sid"] = "s" })
            });

        var requestContents = new List<string>();
        var attemptCount = 0;
        _messageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((request, cancellationToken) =>
            {
                attemptCount++;
                requestContents.Add(request.Content!.ReadAsStringAsync(cancellationToken).GetAwaiter().GetResult());
            })
            .Returns<HttpRequestMessage, CancellationToken>((_, _) =>
            {
                // First send is rate limited for the session category only; resend succeeds.
                if (attemptCount == 1)
                {
                    var limited = new HttpResponseMessage(HttpStatusCode.TooManyRequests);
                    limited.Headers.TryAddWithoutValidation("X-Sentry-Rate-Limits", "60:session");
                    return Task.FromResult(limited);
                }

                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
            });

        var result = await _client.SubmitEnvelopeAsync(dsn, envelope);

        Assert.That(requestContents, Has.Count.EqualTo(2));
        // First attempt carries both items; the retry drops the rate-limited session
        // and still delivers the event.
        Assert.That(requestContents[0], Does.Contain("\"type\":\"session\""));
        Assert.That(requestContents[1], Does.Contain("\"type\":\"event\""));
        Assert.That(requestContents[1], Does.Not.Contain("\"type\":\"session\""));
        // The crash event was delivered, so this counts as delivered.
        Assert.That(result, Is.EqualTo(SubmitResult.Delivered));
    }

    [Test]
    [TestCase("data/two_items.envelope")]
    public async Task SubmitEnvelope_Retry_Preserves_Request_Content(string filePath)
    {
        await using var file = File.OpenRead(filePath);
        var envelope = await Envelope.DeserializeAsync(file);
        var dsn = envelope.TryGetDsn()!;
        var expectedContent = await File.ReadAllTextAsync(filePath);

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
        foreach (var requestContent in requestContents)
        {
            Assert.That(requestContent.RemoveBlankLines(), Is.EqualTo(expectedContent.RemoveBlankLines()));
        }
    }
}

internal static class TestExtensions
{
    public static string RemoveBlankLines(this string str)
    {
        return string.Join('\n', str.Split('\n', StringSplitOptions.RemoveEmptyEntries));
    }
}
