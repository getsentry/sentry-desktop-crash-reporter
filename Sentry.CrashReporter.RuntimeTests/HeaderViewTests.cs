namespace Sentry.CrashReporter.RuntimeTests;

[TestClass]
[RunsOnUIThread]
public class HeaderViewTests
{
    [TestMethod]
    public void HeaderView_CanBeCreated()
    {
        // Arrange
        var mockReporter = new Mock<ICrashReporter>();
        var viewModel = new HeaderViewModel(mockReporter.Object);

        // Act
        var view = new HeaderView(viewModel);
        var exceptionLabel = view.FindFirstDescendant<FrameworkElement>(d => d.Name == "exceptionLabel");
        var releaseLabel = view.FindFirstDescendant<FrameworkElement>(d => d.Name == "releaseLabel");
        var osLabel = view.FindFirstDescendant<FrameworkElement>(d => d.Name == "osLabel");
        var environmentLabel = view.FindFirstDescendant<FrameworkElement>(d => d.Name == "environmentLabel");

        // Assert
        Assert.IsNotNull(view);

        Assert.IsNotNull(exceptionLabel);
        Assert.AreEqual(Visibility.Collapsed, exceptionLabel.Visibility);

        Assert.IsNotNull(releaseLabel);
        Assert.AreEqual(Visibility.Collapsed, releaseLabel.Visibility);

        Assert.IsNotNull(osLabel);
        Assert.AreEqual(Visibility.Collapsed, osLabel.Visibility);

        Assert.IsNotNull(environmentLabel);
        Assert.AreEqual(Visibility.Collapsed, environmentLabel.Visibility);
    }

    [TestMethod]
    public void HeaderView_ReflectsViewModelData()
    {
        // Arrange
        var envelope = new Envelope(new JsonObject(), [
            new EnvelopeItem(
                new JsonObject { { "type", "event" } },
                System.Text.Encoding.UTF8.GetBytes(new JsonObject
                {
                    { "exception", new JsonObject { { "values", new JsonArray { new JsonObject { { "type", "SIGSEGV" } } } } } },
                    { "release", "my-app@0.1.0" },
                    { "contexts", new JsonObject { { "os", new JsonObject { { "name", "Windows" }, { "version", "11" } } } } },
                    { "environment", "production" }
                }.ToJsonString()))
        ]);
        RxApp.MainThreadScheduler = Scheduler.Immediate;
        var mockReporter = new Mock<ICrashReporter>();
        mockReporter.Setup(x => x.LoadAsync(It.IsAny<CancellationToken>()))
            .Returns<CancellationToken>(ct => new ValueTask<Envelope?>(envelope));

        // Act
        var viewModel = new HeaderViewModel(mockReporter.Object);
        var view = new HeaderView(viewModel);
        var exceptionLabel = view.FindFirstDescendant<TextBlock>(tb => tb.Text == "SIGSEGV");
        var releaseLabel = view.FindFirstDescendant<TextBlock>(tb => tb.Text == "my-app@0.1.0");
        var osLabel = view.FindFirstDescendant<TextBlock>(tb => tb.Text == "Windows 11");
        var environmentLabel = view.FindFirstDescendant<TextBlock>(tb => tb.Text == "production");

        // Assert
        Assert.IsNotNull(exceptionLabel);
        Assert.AreEqual(Visibility.Visible, exceptionLabel.Visibility);
        
        Assert.IsNotNull(releaseLabel);
        Assert.AreEqual(Visibility.Visible, releaseLabel.Visibility);

        Assert.IsNotNull(osLabel);
        Assert.AreEqual(Visibility.Visible, osLabel.Visibility);

        Assert.IsNotNull(environmentLabel);
        Assert.AreEqual(Visibility.Visible, environmentLabel.Visibility);
    }
}
