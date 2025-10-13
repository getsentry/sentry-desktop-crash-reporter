namespace Sentry.CrashReporter.RuntimeTests;

[TestClass]
[RunsOnUIThread]
public class FooterViewTests : RuntimeTestBase
{
    [TestMethod]
    public void FooterView_CanBeCreated()
    {
        // Arrange
        var mockReporter = new Mock<ICrashReporter>();
        var viewModel = new FooterViewModel(mockReporter.Object);

        // Act
        var view = new FooterView(viewModel);
        var eventIdLabel = view.FindFirstDescendant<FrameworkElement>(d => d.Name == "eventIdLabel");
        var cancelButton = view.FindFirstDescendant<Button>(d => d.Name == "cancelButton");
        var submitButton = view.FindFirstDescendant<Button>(d => d.Name == "submitButton");

        // Assert
        Assert.IsNotNull(view);

        Assert.IsNotNull(eventIdLabel);
        Assert.AreEqual(Visibility.Collapsed, eventIdLabel.Visibility);

        Assert.IsNotNull(cancelButton);
        Assert.AreEqual(Visibility.Visible, cancelButton.Visibility);
        Assert.IsTrue(cancelButton.IsEnabled);

        Assert.IsNotNull(submitButton);
        Assert.AreEqual(Visibility.Visible, submitButton.Visibility);
        Assert.IsFalse(submitButton.IsEnabled);
    }

    [TestMethod]
    public void FooterView_ReflectsViewModelData()
    {
        // Arrange
        var envelope = new Envelope(new JsonObject() { { "dsn", "https://foo@bar.com/123" }, { "event_id" , "12345678901234567890123456789012" } }, [
            new EnvelopeItem(
                new JsonObject { { "type", "event" } },
                System.Text.Encoding.UTF8.GetBytes(new JsonObject
                {
                    {
                        "exception",
                        new JsonObject { { "values", new JsonArray { new JsonObject { { "type", "SIGSEGV" } } } } }
                    },
                    { "release", "my-app@0.1.0" },
                    { "contexts", new JsonObject { { "os", new JsonObject { { "name", "Windows 11" } } } } },
                    { "environment", "production" }
                }.ToJsonString()))
        ]);
        var mockReporter = new Mock<ICrashReporter>();
        mockReporter.Setup(x => x.LoadAsync(It.IsAny<CancellationToken>()))
            .Returns<CancellationToken>(ct => new ValueTask<Envelope?>(envelope));

        // Act
        var viewModel = new FooterViewModel(mockReporter.Object);
        var view = new FooterView(viewModel);
        var eventIdLabel = view.FindFirstDescendant<TextBlock>(tb => tb.Text.StartsWith("123456"));
        var cancelButton = view.FindFirstDescendant<Button>(b => b.Name == "cancelButton");
        var submitButton = view.FindFirstDescendant<Button>(b => b.Name == "submitButton");

        // Assert
        Assert.IsNotNull(view);

        Assert.IsNotNull(eventIdLabel);
        Assert.AreEqual(Visibility.Visible, eventIdLabel.Visibility);

        Assert.IsNotNull(cancelButton);
        Assert.AreEqual(Visibility.Visible, cancelButton.Visibility);
        Assert.IsTrue(cancelButton.IsEnabled);

        Assert.IsNotNull(submitButton);
        Assert.AreEqual(Visibility.Visible, submitButton.Visibility);
        Assert.IsTrue(submitButton.IsEnabled);
    }

    // TODO: add WindowService & test Cancel & Submit
}
