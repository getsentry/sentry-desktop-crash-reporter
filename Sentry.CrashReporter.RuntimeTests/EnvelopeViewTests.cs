namespace Sentry.CrashReporter.RuntimeTests;

[TestClass]
[RunsOnUIThread]
public class EnvelopeViewTests : RuntimeTestBase
{
    [TestMethod]
    public void EnvelopeView_CanBeCreated_With_EmptyViewModel()
    {
        // Arrange
        var mockReporter = new Mock<ICrashReporter>();
        var viewModel = new EnvelopeViewModel(mockReporter.Object);

        // Act
        var view = new EnvelopeView(viewModel);

        // Assert
        Assert.IsNotNull(view);
        Assert.IsNotNull(viewModel);
        Assert.IsTrue(string.IsNullOrEmpty(viewModel.FilePath));
        Assert.IsTrue(string.IsNullOrEmpty(viewModel.FileName));
        Assert.IsTrue(string.IsNullOrEmpty(viewModel.Directory));
        Assert.IsNull(viewModel.Formatted);
    }

    [TestMethod]
    public async Task EnvelopeView_DisplaysData_FromViewModel()
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
        var mockReporter = new Mock<ICrashReporter>();
        mockReporter.Setup(x => x.LoadAsync(It.IsAny<CancellationToken>()))
            .Returns<CancellationToken>(ct => new ValueTask<Envelope?>(envelope));

        // Act
        var viewModel = new EnvelopeViewModel(mockReporter.Object);
        var view = new EnvelopeView(viewModel);
        UnitTestsUIContentHelper.EmbeddedTestRoot.SetContent(view);
        await UnitTestsUIContentHelper.WaitForLoaded(view);
        await UnitTestsUIContentHelper.WaitForIdle();

        // Assert
        var header = view.FindFirstDescendant<TextBlock>(tb => tb.Text.Contains(""""dsn": "https://foo@bar.com/123"""") &&
                                                               tb.Text.Contains(""""event_id": "12345678901234567890123456789012""""));
        Assert.IsNotNull(header);

        var content = view.FindFirstDescendant<TextBlock>(tb => tb.Text.Contains(""""platform": "native""""));
        Assert.IsNotNull(content);
    }
}
