namespace Sentry.CrashReporter.RuntimeTests;

[TestClass]
[RunsOnUIThread]
public class HeaderViewTests : RuntimeTestBase
{
    [TestMethod]
    public void HeaderView_CanBeCreated()
    {
        // Arrange
        _ = MockCrashReporter();

        // Act
        var view = new HeaderView();
        var exceptionLabel = view.FindFirstDescendant<FrameworkElement>(d => d.Name == "exceptionLabel");
        var releaseLabel = view.FindFirstDescendant<FrameworkElement>(d => d.Name == "releaseLabel");
        var osLabel = view.FindFirstDescendant<FrameworkElement>(d => d.Name == "osLabel");
        var environmentLabel = view.FindFirstDescendant<FrameworkElement>(d => d.Name == "environmentLabel");

        // Assert
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
    public void HeaderView_DisplaysException()
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
        var view = new HeaderView();
        var exceptionLabel = view.FindFirstDescendant<TextBlock>(tb => tb.Text == "SIGSEGV");

        // Assert
        Assert.IsNotNull(exceptionLabel);
        Assert.AreEqual(Visibility.Visible, exceptionLabel.Visibility);
    }

    [TestMethod]
    public void HeaderView_DisplaysRelease()
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
        var view = new HeaderView();
        var releaseLabel = view.FindFirstDescendant<TextBlock>(tb => tb.Text == "my-app@0.1.0");

        // Assert
        Assert.IsNotNull(releaseLabel);
        Assert.AreEqual(Visibility.Visible, releaseLabel.Visibility);
    }

    [TestMethod]
    [DataRow("Windows", "10.0.26100", FA.Windows)]
    [DataRow("Linux", "6.14.0", FA.Linux)]
    [DataRow("macOS", "15.7.1", FA.Apple)]
    [DataRow("iOS", "26", FA.Apple)]
    [DataRow("Android", "16", FA.Android)]
    public void HeaderView_DisplaysOs(string os, string version, string brand)
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
        var view = new HeaderView();
        var osLabel = view.FindFirstDescendant<TextBlock>(tb => tb.Text.Contains(os) && tb.Text.Contains(version));
        var osIcon = view.FindFirstDescendant<FontAwesomeIcon>(fa => fa.Brand == brand);

        // Assert
        Assert.IsNotNull(osLabel);
        Assert.AreEqual(Visibility.Visible, osLabel.Visibility);

        Assert.IsNotNull(osIcon);
        Assert.AreEqual(Visibility.Visible, osIcon.Visibility);
    }
    
    [TestMethod]
    public void HeaderView_DisplaysEnvironment()
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
        var view = new HeaderView();
        var environmentLabel = view.FindFirstDescendant<TextBlock>(tb => tb.Text == "production");

        // Assert
        Assert.IsNotNull(environmentLabel);
        Assert.AreEqual(Visibility.Visible, environmentLabel.Visibility);
    }
}
