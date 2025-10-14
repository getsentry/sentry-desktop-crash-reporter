namespace Sentry.CrashReporter.Tests;

public class EnvelopeViewModelTests
{
    [Test]
    public void Defaults()
    {
        // Arrange
        var mockReporter = new Mock<ICrashReporter>();
        mockReporter.Setup(x => x.LoadAsync(It.IsAny<CancellationToken>()))
            .Returns(ValueTask.FromResult<Envelope?>(null));

        // Act
        var viewModel = new EnvelopeViewModel(mockReporter.Object);

        // Assert
        Assert.That(viewModel.Envelope, Is.Null);
    }

    [Test]
    public void Init()
    {
        // Arrange
        var envelope = new Envelope(new JsonObject(), new List<EnvelopeItem>());
        var mockReporter = new Mock<ICrashReporter>();
        mockReporter.Setup(x => x.LoadAsync(It.IsAny<CancellationToken>()))
            .Returns(ValueTask.FromResult<Envelope?>(envelope));

        // Act
        var viewModel = new EnvelopeViewModel(mockReporter.Object);

        // Assert
        Assert.That(viewModel.Envelope, Is.EqualTo(envelope));
    }

    [Test]
    [TestCase("", false)]
    [TestCase("foo.envelope", true)]
    public async Task CanLaunch(string filePath, bool expectedCanLaunch)
    {
        // Arrange
        Envelope? envelope = null;
        var mockReporter = new Mock<ICrashReporter>();
        mockReporter.Setup(x => x.FilePath).Returns(filePath);
        mockReporter.Setup(x => x.LoadAsync(It.IsAny<CancellationToken>()))
            .Returns(ValueTask.FromResult(envelope));

        // Act
        var viewModel = new EnvelopeViewModel(mockReporter.Object);
        var canLaunch = await viewModel.LaunchCommand.CanExecute.FirstOrDefaultAsync();

        // Assert
        Assert.That(canLaunch, Is.EqualTo(expectedCanLaunch));
    }

    // TODO: Launch()
}
