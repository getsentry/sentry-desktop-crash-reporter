namespace Sentry.CrashReporter.Tests;

public class FeedbackViewModelTests
{
    [Test]
    public void Defaults()
    {
        // Arrange
        Feedback? feedback = null;
        var mockReporter = new Mock<ICrashReporter>();
        mockReporter.Setup(x => x.Feedback).Returns(feedback);

        // Act
        var viewModel = new FeedbackViewModel(mockReporter.Object);

        // Assert
        Assert.That(viewModel.Name, Is.Null.Or.Empty);
        Assert.That(viewModel.Email, Is.Null.Or.Empty);
        Assert.That(viewModel.Message, Is.Null.Or.Empty);
    }

    [Test]
    public void Init()
    {
        // Arrange
        var feedback = new Feedback("John Doe", "john.doe@example.com", "It crashed!");
        var mockReporter = new Mock<ICrashReporter>();
        mockReporter.Setup(x => x.Feedback).Returns(feedback);

        // Act
        var viewModel = new FeedbackViewModel(mockReporter.Object);

        // Assert
        Assert.That(viewModel.Name, Is.EqualTo(feedback.Name));
        Assert.That(viewModel.Email, Is.EqualTo(feedback.Email));
        Assert.That(viewModel.Message, Is.EqualTo(feedback.Message));
    }

    [Test]
    [TestCase("https://foo@bar.com/123", "", false)]
    [TestCase("", "12345678901234567890123456789012", false)]
    [TestCase("https://foo@bar.com/123", "12345678901234567890123456789012", true)]
    public void IsAvailable(string dsn, string eventId, bool expectedAvailable)
    {
        // Arrange
        var envelope = new Envelope(
            new JsonObject { ["dsn"] = dsn, ["event_id"] = eventId },
            new List<EnvelopeItem>()
        );
        var mockReporter = new Mock<ICrashReporter>();

        // Act
        var viewModel = new FeedbackViewModel(mockReporter.Object)
        {
            Envelope = envelope
        };

        // Assert
        Assert.That(viewModel.IsEnabled, Is.False);
        Assert.That(viewModel.IsAvailable, Is.EqualTo(expectedAvailable));
    }

    [Test]
    public void UpdateFeedback_Name()
    {
        // Arrange
        var mockReporter = new Mock<ICrashReporter>();
        var viewModel = new FeedbackViewModel(mockReporter.Object);

         // Act
        viewModel.Name = "John Doe";

         // Assert
         Assert.That(viewModel.IsEnabled, Is.False);
         mockReporter.Verify(r => r.UpdateFeedback(It.Is<Feedback>(
            f => f.Name == "John Doe" && f.Email == null && f.Message == string.Empty)), Times.Once);
    }

    [Test]
    public void UpdateFeedback_Email()
    {
        // Arrange
        var mockReporter = new Mock<ICrashReporter>();
        var viewModel = new FeedbackViewModel(mockReporter.Object);

        // Act
        viewModel.Email = "john.doe@example.com";

        // Assert
        Assert.That(viewModel.IsEnabled, Is.False);
        mockReporter.Verify(r => r.UpdateFeedback(It.Is<Feedback>(
            f => f.Name == null && f.Email == "john.doe@example.com" && f.Message == string.Empty)), Times.Once);
    }

    [Test]
    public void UpdateFeedback_Message()
    {
        // Arrange
        var envelope = new Envelope(
            new JsonObject { ["dsn"] = "https://foo@bar.com/123", ["event_id"] = "12345678901234567890123456789012" },
            new List<EnvelopeItem>()
        );
        var mockReporter = new Mock<ICrashReporter>();
        var viewModel = new FeedbackViewModel(mockReporter.Object)
        {
            Envelope = envelope
        };

        // Act
        viewModel.Message = "It crashed!";

        // Assert
        Assert.That(viewModel.IsEnabled, Is.True);
        mockReporter.Verify(r => r.UpdateFeedback(It.Is<Feedback>(
            f => f.Name == null && f.Email == null && f.Message == "It crashed!")), Times.Once);
    }
}
