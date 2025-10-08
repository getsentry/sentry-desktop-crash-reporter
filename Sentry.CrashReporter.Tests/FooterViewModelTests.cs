namespace Sentry.CrashReporter.Tests;

public class FooterViewModelTests
{
    [Test]
    public void Defaults()
    {
        // Arrange
        Envelope? envelope = null;
        var mockReporter = new Mock<ICrashReporter>();
        mockReporter.Setup(x => x.LoadAsync(It.IsAny<CancellationToken>()))
            .Returns(ValueTask.FromResult(envelope));

        // Act
        var viewModel = new FooterViewModel(mockReporter.Object);

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
        mockReporter.Setup(x => x.LoadAsync(It.IsAny<CancellationToken>()))
            .Returns(ValueTask.FromResult<Envelope?>(envelope));

        // Act
        var viewModel = new FooterViewModel(mockReporter.Object);

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
            .Returns(ValueTask.FromResult(envelope));

        // Act
        var viewModel = new FooterViewModel(mockReporter.Object);
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
            .Returns(ValueTask.FromResult<Envelope?>(envelope));

        // Act
        var viewModel = new FooterViewModel(mockReporter.Object);
        var canSubmit = await viewModel.SubmitCommand.CanExecute.FirstOrDefaultAsync();

        // Assert
        Assert.That(canSubmit, Is.True);
    }

    // TODO: Submit()
}
