namespace Sentry.CrashReporter.RuntimeTests;

[TestClass]
[RunsOnUIThread]
public class HeaderViewTests : RuntimeTestBase
{
    [TestMethod]
    public async Task HeaderView_CanBeCreated()
    {
        // Arrange
        _ = MockCrashReporter();

        // Act
        var view = new HeaderView();
        await LoadTestContent(view);

        // Assert
        var exceptionLabel = view.FindFirstDescendant<FrameworkElement>(d => d.Name == "exceptionLabel");
        Assert.IsNotNull(exceptionLabel);
        Assert.AreEqual(Visibility.Collapsed, exceptionLabel.Visibility);

        var releaseLabel = view.FindFirstDescendant<FrameworkElement>(d => d.Name == "releaseLabel");
        Assert.IsNotNull(releaseLabel);
        Assert.AreEqual(Visibility.Collapsed, releaseLabel.Visibility);

        var osLabel = view.FindFirstDescendant<FrameworkElement>(d => d.Name == "osLabel");
        Assert.IsNotNull(osLabel);
        Assert.AreEqual(Visibility.Collapsed, osLabel.Visibility);

        var environmentLabel = view.FindFirstDescendant<FrameworkElement>(d => d.Name == "environmentLabel");
        Assert.IsNotNull(environmentLabel);
        Assert.AreEqual(Visibility.Collapsed, environmentLabel.Visibility);
    }

    [TestMethod]
    public async Task HeaderView_DisplaysException()
    {
        // Arrange
        var envelope = new Envelope(new JsonObject(), [
            new EnvelopeItem(
                new JsonObject { { "type", "event" } },
                System.Text.Encoding.UTF8.GetBytes(new JsonObject
                {
                    { "exception", new JsonObject { { "values", new JsonArray { new JsonObject { { "type", "SIGSEGV" } } } } } },
                }.ToJsonString()))
        ]);
        _ = MockCrashReporter(envelope);

        // Act
        var view = new HeaderView().Envelope(envelope);
        await LoadTestContent(view);

        // Assert
        var exceptionLabel = view.FindFirstDescendant<TextBlock>(tb => tb.Text == "SIGSEGV");
        Assert.IsNotNull(exceptionLabel);
        Assert.AreEqual(Visibility.Visible, exceptionLabel.Visibility);
    }

    [TestMethod]
    public async Task HeaderView_DisplaysRelease()
    {
        // Arrange
        var envelope = new Envelope(new JsonObject(), [
            new EnvelopeItem(
                new JsonObject { { "type", "event" } },
                System.Text.Encoding.UTF8.GetBytes(new JsonObject
                {
                    { "release", "my-app@0.1.0" },
                }.ToJsonString()))
        ]);
        _ = MockCrashReporter(envelope);

        // Act
        var view = new HeaderView().Envelope(envelope);
        await LoadTestContent(view);

        // Assert
        var releaseLabel = view.FindFirstDescendant<TextBlock>(tb => tb.Text == "my-app@0.1.0");
        Assert.IsNotNull(releaseLabel);
        Assert.AreEqual(Visibility.Visible, releaseLabel.Visibility);
    }

    [TestMethod]
    [DataRow("Windows", "10.0.26100", FA.Windows)]
    [DataRow("Linux", "6.14.0", FA.Linux)]
    [DataRow("macOS", "15.7.1", FA.Apple)]
    [DataRow("iOS", "26", FA.Apple)]
    [DataRow("Android", "16", FA.Android)]
    public async Task HeaderView_DisplaysOs(string os, string version, string brand)
    {
        // Arrange
        var envelope = new Envelope(new JsonObject(), [
            new EnvelopeItem(
                new JsonObject { { "type", "event" } },
                System.Text.Encoding.UTF8.GetBytes(new JsonObject
                {
                    { "contexts", new JsonObject { { "os", new JsonObject { { "name", os }, { "version", version } } } } },
                }.ToJsonString()))
        ]);
        _ = MockCrashReporter(envelope);

        // Act
        var view = new HeaderView().Envelope(envelope);
        await LoadTestContent(view);

        // Assert
        var osLabel = view.FindFirstDescendant<TextBlock>(tb => tb.Text.Contains(os) && tb.Text.Contains(version));
        Assert.IsNotNull(osLabel);
        Assert.AreEqual(Visibility.Visible, osLabel.Visibility);

        var osIcon = view.FindFirstDescendant<FontAwesomeIcon>(fa => fa.Brand == brand);
        Assert.IsNotNull(osIcon);
        Assert.AreEqual(Visibility.Visible, osIcon.Visibility);
    }
    
    [TestMethod]
    public async Task HeaderView_DisplaysEnvironment()
    {
        // Arrange
        var envelope = new Envelope(new JsonObject(), [
            new EnvelopeItem(
                new JsonObject { { "type", "event" } },
                System.Text.Encoding.UTF8.GetBytes(new JsonObject
                {
                    { "environment", "production" }
                }.ToJsonString()))
        ]);
        _ = MockCrashReporter(envelope);

        // Act
        var view = new HeaderView().Envelope(envelope);
        await LoadTestContent(view);

        // Assert
        var environmentLabel = view.FindFirstDescendant<TextBlock>(tb => tb.Text == "production");
        Assert.IsNotNull(environmentLabel);
        Assert.AreEqual(Visibility.Visible, environmentLabel.Visibility);
    }
}
