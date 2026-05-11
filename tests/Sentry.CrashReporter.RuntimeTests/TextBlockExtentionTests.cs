using Sentry.CrashReporter.Extensions;

namespace Sentry.CrashReporter.RuntimeTests;

[TestClass]
[RunsOnUIThread]
public class TextBlockExtensionTests : RuntimeTestBase
{
    [TestMethod]
    public void TextBlock_WithTextSelection()
    {
        // Act
        var textBlock = new TextBlock().WithTextSelection();

        // Assert
        Assert.IsTrue(textBlock.IsTextSelectionEnabled);
    }
}
