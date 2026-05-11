namespace Sentry.CrashReporter.RuntimeTests;

[TestClass]
[RunsOnUIThread]
public class ErrorViewTests : RuntimeTestBase
{
    [TestMethod]
    public async Task ErrorView_DisplaysException()
    {
        // Arrange
        var exception = new Exception("Something went wrong");
        try
        {
            throw exception;
        }
        catch
        {
            // stack trace
        }

        // Act
        var view = new ErrorView().Error(exception);
        await LoadTestContent(view);

        // Assert
        Assert.IsNotNull(view.FindFirstDescendant<TextBlock>(tb => tb.Text == exception.GetType().FullName));
        Assert.IsNotNull(view.FindFirstDescendant<TextBlock>(tb => tb.Text == exception.Message));

        Assert.IsNotNull(exception.StackTrace);
        Assert.IsNotEmpty(exception.StackTrace);
        Assert.IsNotNull(view.FindFirstDescendant<TextBlock>(tb => tb.Text == exception.StackTrace));
    }
}
