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

    [Test]
    public async Task RequestCloseAsync_WhenAlreadyClosing_DoesNotNotifyClosingAgain()
    {
        // Arrange
        var windowService = new WindowService();
        var started = new TaskCompletionSource();
        var resume = new TaskCompletionSource();
        var count = 0;
        windowService.Closing += async () =>
        {
            count++;
            started.SetResult();
            await resume.Task;
        };

        // Act
        var firstClose = windowService.RequestCloseAsync();
        await started.Task;
        await windowService.RequestCloseAsync();
        resume.SetResult();
        await firstClose;
        await windowService.RequestCloseAsync();

        // Assert
        count.Should().Be(1);
    }
}
