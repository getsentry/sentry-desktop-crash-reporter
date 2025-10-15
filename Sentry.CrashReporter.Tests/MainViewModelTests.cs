namespace Sentry.CrashReporter.Tests;

public class MainViewModelTests
{
    [Test]
    public void MainViewModel_IsExecuting()
    {
        // Arrange
        var taskCompletionSource = new TaskCompletionSource<Envelope?>();
        var mockReporter = new Mock<ICrashReporter>();
        mockReporter.Setup(x => x.LoadAsync(It.IsAny<CancellationToken>()))
            .Returns(taskCompletionSource.Task);

        // Act
        var viewModel = new MainViewModel(mockReporter.Object);

        // Assert
        Assert.That(viewModel.IsExecuting, Is.True);
    }

    [Test]
    public async Task MainViewModel_IsExecuting_False()
    {
        // Arrange
        var taskCompletionSource = new TaskCompletionSource<Envelope?>();
        var mockReporter = new Mock<ICrashReporter>();
        mockReporter.Setup(x => x.LoadAsync(It.IsAny<CancellationToken>()))
            .Returns(taskCompletionSource.Task);

        // Act
        var viewModel = new MainViewModel(mockReporter.Object);
        taskCompletionSource.SetResult(null);
        await Task.Yield();

        // Assert
        Assert.That(viewModel.IsExecuting, Is.False);
    }

    [Test]
    [TestCase(0, "", "Feedback (optional)")]
    [TestCase(1, "", "Event")]
    [TestCase(2, "", "Envelope")]
    [TestCase(2, "test.envelope", "Envelope (test.envelope)")]
    public void MainViewModel_ResolveSubtitle(int index, string filePath, string expectedSubtitle)
    {
        // Arrange
        var mockReporter = new Mock<ICrashReporter>();
        mockReporter.Setup(x => x.FilePath).Returns(filePath);
    
        // Act
        var viewModel = new MainViewModel(mockReporter.Object)
        {
            SelectedIndex = index
        };

        // Assert
        Assert.That(viewModel.Subtitle, Is.EqualTo(expectedSubtitle));
    }
}
