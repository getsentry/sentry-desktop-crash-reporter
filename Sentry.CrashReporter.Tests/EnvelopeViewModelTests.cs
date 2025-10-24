namespace Sentry.CrashReporter.Tests;

public class EnvelopeViewModelTests
{
    [Test]
    public void Defaults()
    {
        // Act
        var viewModel = new EnvelopeViewModel();

        // Assert
        Assert.That(viewModel.Envelope, Is.Null);
    }

    [Test]
    public void Init()
    {
        // Arrange
        var envelope = new Envelope(new JsonObject(), new List<EnvelopeItem>());

        // Act
        var viewModel = new EnvelopeViewModel
        {
            Envelope = envelope
        };

        // Assert
        Assert.That(viewModel.Envelope, Is.EqualTo(envelope));
    }

    [Test]
    [TestCase("")]
    [TestCase("foo.envelope")]
    [TestCase("/path/to/foo.envelope")]
    public void FileNamePath(string filePath)
    {
        // Arrange
        var envelope = new Envelope(new JsonObject(), [])
        {
            FilePath = filePath
        };

        // Act
        var viewModel = new EnvelopeViewModel
        {
            Envelope = envelope
        };

        // Assert
        Assert.That(viewModel.FilePath, Is.EqualTo(filePath));
        Assert.That(viewModel.FileName, Is.EqualTo(System.IO.Path.GetFileName(filePath)));
    }

    [Test]
    [TestCase("", false)]
    [TestCase("foo.envelope", true)]
    public async Task CanLaunch(string filePath, bool expectedCanLaunch)
    {
        // Arrange
        var envelope = new Envelope(new JsonObject(), [])
        {
            FilePath = filePath
        };

        // Act
        var viewModel = new EnvelopeViewModel
        {
            Envelope = envelope
        };
        var canLaunch = await viewModel.LaunchCommand.CanExecute.FirstOrDefaultAsync();

        // Assert
        Assert.That(canLaunch, Is.EqualTo(expectedCanLaunch));
    }

    // TODO: Launch()
}
