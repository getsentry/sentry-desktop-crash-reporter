using Microsoft.Extensions.DependencyInjection;

namespace Sentry.CrashReporter.Tests;

public class AttachmentViewModelTests
{
    [SetUp]
    public void SetUp()
    {
        var services = new ServiceCollection();
        services.AddSingleton(Mock.Of<IFilePickerService>());
        App.Services = services.BuildServiceProvider();
    }

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

    [Test]
    public void AddItem_UpdatesAttachments()
    {
        // Arrange
        var envelope = new Envelope(new JsonObject(), Array.Empty<EnvelopeItem>());
        var viewModel = new AttachmentViewModel { Envelope = envelope };
        Assert.That(viewModel.Attachments, Is.Null.Or.Empty);

        // Act
        envelope.AddItem(EnvelopeItem.CreateAttachment("new.bin", [0x01, 0x02]));

        // Assert
        Assert.That(viewModel.Attachments, Has.Count.EqualTo(1));
        Assert.That(viewModel.Attachments![0].Filename, Is.EqualTo("new.bin"));
        Assert.That(viewModel.Attachments[0].Source, Is.Not.Null);
        Assert.That(viewModel.Attachments[0].IsMinidump, Is.False);
    }

    [Test]
    public void Remove_DropsUnderlyingItem()
    {
        // Arrange
        var minidumpItem = new EnvelopeItem(
            new JsonObject
            {
                ["type"] = "attachment",
                ["filename"] = "crash.dmp",
                ["attachment_type"] = "event.minidump"
            },
            [0xFF]);
        var regularItem = new EnvelopeItem(
            new JsonObject { ["type"] = "attachment", ["filename"] = "note.txt" },
            Encoding.UTF8.GetBytes("hi"));
        var envelope = new Envelope(new JsonObject(), new List<EnvelopeItem> { minidumpItem, regularItem });
        var viewModel = new AttachmentViewModel { Envelope = envelope };
        Assert.That(viewModel.Attachments, Has.Count.EqualTo(2));

        // Act
        var regular = viewModel.Attachments!.First(a => a.Filename == "note.txt");
        viewModel.Remove(regular);

        // Assert
        Assert.That(viewModel.Attachments, Has.Count.EqualTo(1));
        Assert.That(envelope.Items, Does.Not.Contain(regularItem));
        Assert.That(envelope.Items, Contains.Item(minidumpItem));
    }

    [Test]
    public void Remove_IgnoresMinidump()
    {
        // Arrange
        var minidumpItem = new EnvelopeItem(
            new JsonObject
            {
                ["type"] = "attachment",
                ["filename"] = "crash.dmp",
                ["attachment_type"] = "event.minidump"
            },
            [0xFF]);
        var envelope = new Envelope(new JsonObject(), new List<EnvelopeItem> { minidumpItem });
        var viewModel = new AttachmentViewModel { Envelope = envelope };
        var md = viewModel.Attachments!.Single();
        Assert.That(md.IsMinidump, Is.True);

        // Act
        viewModel.Remove(md);

        // Assert
        Assert.That(envelope.Items, Contains.Item(minidumpItem));
    }
}
