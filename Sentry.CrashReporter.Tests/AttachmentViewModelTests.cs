namespace Sentry.CrashReporter.Tests;

public class AttachmentViewModelTests
{
    [Test]
    public void Defaults()
    {
        // Arrange
        var mockReporter = new Mock<ICrashReporter>();
        mockReporter.Setup(x => x.LoadAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult<Envelope?>(null));

        // Act
        var viewModel = new AttachmentViewModel(mockReporter.Object);

        // Assert
        Assert.That(viewModel.Envelope, Is.Null.Or.Empty);
        Assert.That(viewModel.Attachments, Is.Null.Or.Empty);
    }

    [Test]
    public void Init()
    {
        // Arrange
        var eventItem = new EnvelopeItem(
            new JsonObject { ["type"] = "event" }, []
        );
        var attachmentItem = new EnvelopeItem(
            new JsonObject { ["type"] = "attachment", ["filename"] = "test.txt" },
            Encoding.UTF8.GetBytes("attachment content")
        );
        var envelope = new Envelope(
            new JsonObject(),
            new List<EnvelopeItem> { eventItem, attachmentItem }
        );
        var mockReporter = new Mock<ICrashReporter>();
        mockReporter.Setup(x => x.LoadAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult<Envelope?>(envelope));

        // Act
        var viewModel = new AttachmentViewModel(mockReporter.Object);

        // Assert
        Assert.That(viewModel.Envelope, Is.EqualTo(envelope));
        Assert.That(viewModel.Attachments, Has.Count.EqualTo(1));
        Assert.That(viewModel.Attachments![0].Filename, Is.EqualTo("test.txt"));
        Assert.That(Encoding.UTF8.GetString(viewModel.Attachments[0].Data), Is.EqualTo("attachment content"));
    }
}
