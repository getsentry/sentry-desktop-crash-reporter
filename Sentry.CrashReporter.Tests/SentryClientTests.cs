using System.Net;
using System.Text.Json.Nodes;
using Moq;
using Moq.Protected;
using Sentry.CrashReporter.Services;

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
    [TestCase("Envelopes/two_items.envelope")]
    public async Task SubmitEnvelope_Sends_Request(string filePath)
    {
        await using var file = File.OpenRead(filePath);
        var envelope = await Envelope.DeserializeAsync(file);

        var requestContent = "";
        _messageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((request, cancellationToken) =>
            {
                requestContent = request.Content!.ReadAsStringAsync(cancellationToken).GetAwaiter().GetResult();
            })
            .ReturnsAsync(new HttpResponseMessage { StatusCode = HttpStatusCode.OK });

        await _client.SubmitEnvelopeAsync(envelope);

        var expected = await File.ReadAllTextAsync(filePath);

        var requestLines = requestContent.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
        var expectedLines = expected.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);

        Assert.That(requestLines, Is.EqualTo(expectedLines));
    }

    [Test]
    public async Task SubmitEnvelope_Sends_Feedback()
    {
        var feedback = new Feedback("John Doe", "john.doe@example.com", "It crashed!");

        await using var file = File.OpenRead("Envelopes/two_items.envelope");
        var envelope = await Envelope.DeserializeAsync(file);
        var eventId = envelope.TryGetEventId()!;

        var capturedContents = new List<string>();
        _messageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((request, cancellationToken) =>
            {
                capturedContents.Add(request.Content!.ReadAsStringAsync(cancellationToken).GetAwaiter().GetResult());
            })
            .ReturnsAsync(new HttpResponseMessage { StatusCode = HttpStatusCode.OK });

        _client.UpdateFeedback(feedback);
        await _client.SubmitEnvelopeAsync(envelope);

        Assert.That(capturedContents, Has.Count.EqualTo(2));

        var feedbackContent = capturedContents[1];
        var feedbackLines = feedbackContent.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        Assert.That(feedbackLines, Has.Length.EqualTo(3));

        var feedbackHeader = JsonNode.Parse(feedbackLines[1])!;
        Assert.That(feedbackHeader["type"]!.GetValue<string>(), Is.EqualTo("feedback"));

        var feedbackPayload = JsonNode.Parse(feedbackLines[2])!;
        var feedbackContext = feedbackPayload["contexts"]!["feedback"]!;
        Assert.That(feedbackContext["name"]!.GetValue<string>(), Is.EqualTo(feedback.Name));
        Assert.That(feedbackContext["contact_email"]!.GetValue<string>(), Is.EqualTo(feedback.Email));
        Assert.That(feedbackContext["message"]!.GetValue<string>(), Is.EqualTo(feedback.Message));
        Assert.That(feedbackContext["associated_event_id"]!.GetValue<string>(), Is.EqualTo(eventId.Replace("-", "")));
    }

    [Test]
    public async Task SubmitEnvelope_Throws()
    {
        await using var file = File.OpenRead("Envelopes/empty_headers_eof.envelope");
        var envelope = await Envelope.DeserializeAsync(file);

        var ex = Assert.ThrowsAsync<InvalidOperationException>(() => _client.SubmitEnvelopeAsync(envelope));
        Assert.That(ex?.Message, Does.Match(@"\bDSN\b"));
    }
}
