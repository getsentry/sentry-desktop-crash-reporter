namespace Sentry.CrashReporter.Tests;

public class MainViewModelTests
{
    [Test]
    public void MainViewModel_Defaults()
    {
        // Arrange
        var mockReporter = new Mock<ICrashReporter>();
        mockReporter.Setup(x => x.LoadAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult<Envelope?>(null));

        // Act
        var viewModel = new MainViewModel(mockReporter.Object);

        // Assert
        Assert.That(viewModel.IsExecuting, Is.False);
        Assert.That(viewModel.SelectedIndex, Is.EqualTo(0));
        Assert.That(viewModel.Subtitle, Is.EqualTo("Feedback (optional)"));
        Assert.That(viewModel.Attachments, Is.Null.Or.Empty);
        mockReporter.Verify(r => r.LoadAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

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
        mockReporter.Verify(r => r.LoadAsync(It.IsAny<CancellationToken>()), Times.Once);
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
        await Task.Delay(TimeSpan.FromMilliseconds(1));

        // Assert
        Assert.That(viewModel.IsExecuting, Is.False);
        mockReporter.Verify(r => r.LoadAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    [TestCase(0, "", "Feedback (optional)")]
    [TestCase(1, "", "Event")]
    [TestCase(2, "", "Attachments")]
    [TestCase(3, "", "Envelope")]
    [TestCase(3, "test.envelope", "Envelope (test.envelope)")]
    public void MainViewModel_ResolveSubtitle(int index, string filePath, string expectedSubtitle)
    {
        // Arrange
        var envelope = new Envelope(new JsonObject(), [])
        {
            FilePath = filePath
        };
        var mockReporter = new Mock<ICrashReporter>();

        // Act
        var viewModel = new MainViewModel(mockReporter.Object)
        {
            SelectedIndex = index,
            Envelope = envelope
        };

        // Assert
        Assert.That(viewModel.Subtitle, Is.EqualTo(expectedSubtitle));
        mockReporter.Verify(r => r.LoadAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public void MainViewModel_Attachments()
    {
        // Arrange
        var attachment1 = new Attachment("attachment1.txt", [0x01, 0x02, 0x03]);
        var attachment2 = new Attachment("attachment2.txt", [0x04, 0x05, 0x06]);

         var envelope = new Envelope(
            new JsonObject(),
            [
                new EnvelopeItem(
                    new JsonObject { ["type"] = "attachment", ["filename"] = attachment1.Filename },
                    attachment1.Data),
                new EnvelopeItem(
                    new JsonObject { ["type"] = "attachment", ["filename"] = attachment2.Filename },
                    attachment2.Data)
            ]);

         var mockReporter = new Mock<ICrashReporter>();
        mockReporter.Setup(x => x.LoadAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult<Envelope?>(envelope));

         // Act
        var viewModel = new MainViewModel(mockReporter.Object);

         // Assert
        Assert.That(viewModel.Attachments, Is.Not.Null);
        Assert.That(viewModel.Attachments, Has.Count.EqualTo(2));
        Assert.That(viewModel.Attachments![0].Filename, Is.EqualTo(attachment1.Filename));
        Assert.That(viewModel.Attachments[0].Data, Is.EqualTo(attachment1.Data));
        Assert.That(viewModel.Attachments[1].Filename, Is.EqualTo(attachment2.Filename));
        Assert.That(viewModel.Attachments[1].Data, Is.EqualTo(attachment2.Data));
        mockReporter.Verify(r => r.LoadAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task MainViewModel_Error()
    {
        // Arrange
        var mockReporter = new Mock<ICrashReporter>();
        var exception = new Exception("test");
        mockReporter.Setup(x => x.LoadAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(exception);

        // Act
        var viewModel = new MainViewModel(mockReporter.Object);
        await Task.Yield();

        // Assert
        Assert.That(viewModel.Error, Is.EqualTo(exception));
        mockReporter.Verify(r => r.LoadAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
