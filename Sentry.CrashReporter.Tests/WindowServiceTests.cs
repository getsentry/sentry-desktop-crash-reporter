namespace Sentry.CrashReporter.Tests;

public class WindowServiceTests
{
    [Test]
    public async Task RequestCloseAsync_WhenClosingHandlerFails_ContinuesClosing()
    {
        // Arrange
        var windowService = new WindowService();
        var handled = false;
        windowService.Closing += () => throw new InvalidOperationException("closing failed");
        windowService.Closing += () =>
        {
            handled = true;
            return Task.CompletedTask;
        };

        // Act
        var act = () => windowService.RequestCloseAsync();

        // Assert
        await act.Should().NotThrowAsync();
        handled.Should().BeTrue();
    }
}
