namespace Sentry.CrashReporter.Tests;

public class FooterViewModelTests
{
    [Test]
    public void Defaults()
    {
        // Arrange
        Envelope? envelope = null;
        var mockReporter = new Mock<ICrashReporter>();
        var mockWindow = new Mock<IWindowService>();
        mockReporter.Setup(x => x.LoadAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(envelope));

        // Act
        var viewModel = new FooterViewModel(mockReporter.Object, mockWindow.Object);

        // Assert
        Assert.That(viewModel.Dsn, Is.Null.Or.Empty);
        Assert.That(viewModel.EventId, Is.Null.Or.Empty);
        Assert.That(viewModel.ShortEventId, Is.Null.Or.Empty);
    }

    [Test]
    public void Init()
    {
        // Arrange
        var envelope = new Envelope(
            new JsonObject
            {
                ["dsn"] = "https://foo@bar.com/123",
                ["event_id"] = "12345678-90ab-cdef-1234-567890abcdef"
            },
            new List<EnvelopeItem>()
        );
        var mockReporter = new Mock<ICrashReporter>();
        var mockWindow = new Mock<IWindowService>();
        mockReporter.Setup(x => x.LoadAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult<Envelope?>(envelope));

        // Act
        var viewModel = new FooterViewModel(mockReporter.Object, mockWindow.Object);

        // Assert
        Assert.That(viewModel.Dsn, Is.EqualTo("https://foo@bar.com/123"));
        Assert.That(viewModel.EventId, Is.EqualTo("12345678-90ab-cdef-1234-567890abcdef"));
        Assert.That(viewModel.ShortEventId, Is.EqualTo("12345678"));
    }

    [Test]
    public async Task CannotSubmit()
    {
        // Arrange
        Envelope? envelope = null;
        var mockReporter = new Mock<ICrashReporter>();
        mockReporter.Setup(x => x.LoadAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(envelope));
        var mockWindow = new Mock<IWindowService>();

        // Act
        var viewModel = new FooterViewModel(mockReporter.Object, mockWindow.Object);
        var canSubmit = await viewModel.SubmitCommand.CanExecute.FirstOrDefaultAsync();

        // Assert
        Assert.That(canSubmit, Is.False);
    }

    [Test]
    public async Task CanSubmit()
    {
        // Arrange
        var envelope = new Envelope(
            new JsonObject { ["dsn"] = "https://foo@bar.com/123" },
            new List<EnvelopeItem>()
        );
        var mockReporter = new Mock<ICrashReporter>();
        mockReporter.Setup(x => x.LoadAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult<Envelope?>(envelope));
        var mockWindow = new Mock<IWindowService>();

        // Act
        var viewModel = new FooterViewModel(mockReporter.Object, mockWindow.Object);
        var canSubmit = await viewModel.SubmitCommand.CanExecute.FirstOrDefaultAsync();

        // Assert
        Assert.That(canSubmit, Is.True);
    }

    [Test]
    public async Task Submit_ClosesWindow()
    {
        // Arrange
        var envelope = new Envelope(
            new JsonObject { ["dsn"] = "https://foo@bar.com/123" },
            new List<EnvelopeItem>()
        );
        var mockReporter = new Mock<ICrashReporter>();
        mockReporter.Setup(x => x.LoadAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult<Envelope?>(envelope));
        var mockWindow = new Mock<IWindowService>();

        var viewModel = new FooterViewModel(mockReporter.Object, mockWindow.Object);
        await viewModel.SubmitCommand.CanExecute.FirstAsync();

        // Act
        await viewModel.SubmitCommand.Execute();

        // Assert
        mockReporter.Verify(x => x.SubmitAsync(It.IsAny<CancellationToken>()), Times.Once);
        mockWindow.Verify(x => x.Close(), Times.Once);
    }

    [Test]
    public async Task Cancel_ClosesWindow()
    {
        // Arrange
        var mockReporter = new Mock<ICrashReporter>();
        var mockWindow = new Mock<IWindowService>();
        var viewModel = new FooterViewModel(mockReporter.Object, mockWindow.Object);

        // Act
        await viewModel.CancelCommand.Execute();

        // Assert
        mockReporter.Verify(x => x.SubmitAsync(It.IsAny<CancellationToken>()), Times.Never);
        mockWindow.Verify(x => x.Close(), Times.Once);
    }
}
