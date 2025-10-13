namespace Sentry.CrashReporter.RuntimeTests;

[TestClass]
[RunsOnUIThread]
public class SelectableTextBlockTests : RuntimeTestBase
{
    [TestMethod]
    public void SelectableTextBlock_IsTextSelectionEnabled()
    {
        // Act
        var textBlock = new SelectableTextBlock();

        // Assert
        Assert.IsTrue(textBlock.IsTextSelectionEnabled);
    }
}
