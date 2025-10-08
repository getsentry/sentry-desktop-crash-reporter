namespace Sentry.CrashReporter.Tests;

public class CrashReporterTests
{
    [Test]
    [TestCase("Envelopes/two_items.envelope")]
    public async Task SubmitAsync_WithValidEnvelope_CallsSentryClient(string filePath)
    {
        // Arrange
        var client = new Mock<ISentryClient>();
        var reporter = new Services.CrashReporter(filePath, client.Object);

        // Act
        var envelope = await reporter.LoadAsync();
        await reporter.SubmitAsync();

        // Assert
        Assert.That(envelope, Is.Not.Null);
        client.Verify(c => c.SubmitEnvelopeAsync(It.IsAny<string>(),
            envelope!,
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    [TestCase("Envelopes/two_items.envelope")]
    public async Task SubmitAsync_WithFeedback_CallsSentryClientTwice(string filePath)
    {
        // Arrange
        var client = new Mock<ISentryClient>();
        var reporter = new Services.CrashReporter(filePath, client.Object);
        var submittedEnvelopes = new List<Envelope>();
        client.Setup(c => c.SubmitEnvelopeAsync(It.IsAny<string>(), It.IsAny<Envelope>(), It.IsAny<CancellationToken>()))
            .Callback<string, Envelope, CancellationToken>((_, e, _) => submittedEnvelopes.Add(e));
        var feedback = new Feedback("John Doe", "john.doe@example.com", "It crashed!");

        // Act
        var envelope = await reporter.LoadAsync();
        reporter.UpdateFeedback(feedback);
        await reporter.SubmitAsync();

        // Assert
        Assert.That(envelope, Is.Not.Null);
        Assert.That(submittedEnvelopes, Has.Count.EqualTo(2));
        Assert.That(submittedEnvelopes[0], Is.EqualTo(envelope));
        var feedbackEnvelope = submittedEnvelopes[1];
        Assert.That(feedbackEnvelope.Items, Has.Count.EqualTo(1));
        var feedbackItem = feedbackEnvelope.Items[0];
        Assert.That(feedbackItem.Header["type"]!.GetValue<string>(), Is.EqualTo("feedback"));
        var feedbackJson = JsonNode.Parse(feedbackItem.Payload)!.AsObject();
        var feedbackContext = feedbackJson["contexts"]!["feedback"]!;
        Assert.That(feedbackContext["name"]!.GetValue<string>(), Is.EqualTo(feedback.Name));
        Assert.That(feedbackContext["contact_email"]!.GetValue<string>(), Is.EqualTo(feedback.Email));
        Assert.That(feedbackContext["message"]!.GetValue<string>(), Is.EqualTo(feedback.Message));
        var eventId = envelope!.TryGetEventId();
        Assert.That(feedbackContext["associated_event_id"]!.GetValue<string>(), Is.EqualTo(eventId!.Replace("-", "")));
    }

    [Test]
    [TestCase("Envelopes/empty_headers_eof.envelope")]
    public async Task SubmitAsync_NoDsn_Throws(string filePath)
    {
        // Arrange
        var client = new Mock<ISentryClient>();
        var reporter = new Services.CrashReporter(filePath, client.Object);

        // Act
        await reporter.LoadAsync();
        var ex = Assert.ThrowsAsync<InvalidOperationException>(() => reporter.SubmitAsync());

        // Assert
        Assert.That(ex?.Message, Does.Match(@"\bDSN\b"));
    }
}
