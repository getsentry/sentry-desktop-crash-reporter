namespace Sentry.CrashReporter.RuntimeTests;

[TestClass]
[RunsOnUIThread]
public class SelectableTextBlockTests
{
    [TestMethod]
    public void IsTextSelectionEnabled()
    {
        // Act
        var textBlock = new SelectableTextBlock();

        // Assert
        Assert.IsTrue(textBlock.IsTextSelectionEnabled);
    }
}
