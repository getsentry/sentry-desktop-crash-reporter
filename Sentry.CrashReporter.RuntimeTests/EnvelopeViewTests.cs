namespace Sentry.CrashReporter.RuntimeTests;

[TestClass]
[RunsOnUIThread]
public class EnvelopeViewTests : RuntimeTestBase
{
    [TestMethod]
    public async Task EnvelopeView_DisplaysData()
    {
        // Arrange
        var envelope = new Envelope(new JsonObject()
        {
            { "dsn" , "https://foo@bar.com/123" },
            { "event_id", "12345678901234567890123456789012" }
        }, [
            new EnvelopeItem(
                new JsonObject { { "type", "event" } },
                System.Text.Encoding.UTF8.GetBytes(new JsonObject
                {
                    { "event_id", "12345678901234567890123456789012" },
                    { "platform", "native" }
                }.ToJsonString())),
        ]);
        _ = MockCrashReporter(envelope);

        // Act
        var view = new EnvelopeView();
        await LoadTestContent(view);

        // Assert
        var header = view.FindFirstDescendant<TextBlock>(tb => tb.Text.Contains(""""dsn": "https://foo@bar.com/123"""") &&
                                                               tb.Text.Contains(""""event_id": "12345678901234567890123456789012""""));
        Assert.IsNotNull(header);

        var content = view.FindFirstDescendant<TextBlock>(tb => tb.Text.Contains(""""platform": "native""""));
        Assert.IsNotNull(content);
    }
}
