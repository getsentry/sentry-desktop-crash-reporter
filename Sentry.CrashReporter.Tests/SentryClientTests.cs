namespace Sentry.CrashReporter.Tests;

public class SentryClientTests
{
    private Mock<HttpMessageHandler> _messageHandler = null!;
    private SentryClient _client = null!;

    [SetUp]
    public void Setup()
    {
        _messageHandler = new Mock<HttpMessageHandler>();
        var httpClient = new HttpClient(_messageHandler.Object);
        _client = new SentryClient(httpClient);
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
}
