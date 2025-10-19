namespace Sentry.CrashReporter.Tests;

public class AttachmentViewModelTests
{
    [Test]
    public void Defaults()
    {
        // Act
        var viewModel = new AttachmentViewModel();

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

        // Act
        var viewModel = new AttachmentViewModel
        {
            Envelope = envelope
        };

        // Assert
        Assert.That(viewModel.Envelope, Is.EqualTo(envelope));
        Assert.That(viewModel.Attachments, Has.Count.EqualTo(1));
        Assert.That(viewModel.Attachments![0].Filename, Is.EqualTo("test.txt"));
        Assert.That(Encoding.UTF8.GetString(viewModel.Attachments[0].Data), Is.EqualTo("attachment content"));
    }
}
