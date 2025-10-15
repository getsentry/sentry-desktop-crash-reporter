namespace Sentry.CrashReporter.Tests;

public class EventViewModelTests
{
    [Test]
    public void Defaults()
    {
        // Arrange
        var mockReporter = new Mock<ICrashReporter>();
        mockReporter.Setup(x => x.LoadAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult<Envelope?>(null));

        // Act
        var viewModel = new EventViewModel(mockReporter.Object);

        // Assert
        Assert.That(viewModel.Envelope, Is.Null.Or.Empty);
        Assert.That(viewModel.Event, Is.Null.Or.Empty);
        Assert.That(viewModel.Payload, Is.Null.Or.Empty);
        Assert.That(viewModel.Tags, Is.Null.Or.Empty);
        Assert.That(viewModel.Contexts, Is.Null.Or.Empty);
        Assert.That(viewModel.Extra, Is.Null.Or.Empty);
        Assert.That(viewModel.Sdk, Is.Null.Or.Empty);
        Assert.That(viewModel.Attachments, Is.Null.Or.Empty);
    }

    [Test]
    public void Init()
    {
        // Arrange
        var eventPayload = new JsonObject
        {
            ["tags"] = new JsonObject { ["tag_key"] = "tag_value" },
            ["contexts"] = new JsonObject { ["context_key"] = new JsonObject { ["inner_key"] = "context_value" } },
            ["extra"] = new JsonObject { ["extra_key"] = "extra_value" },
            ["sdk"] = new JsonObject { ["name"] = "Sentry.Test", ["version"] = "1.0" }
        };
        var eventItem = new EnvelopeItem(
            new JsonObject { ["type"] = "event" },
            Encoding.UTF8.GetBytes(eventPayload.ToJsonString())
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
        var viewModel = new EventViewModel(mockReporter.Object);

        // Assert
        Assert.That(viewModel.Envelope, Is.EqualTo(envelope));
        Assert.That(viewModel.Event, Is.EqualTo(eventItem));
        Assert.That(viewModel.Payload?.ToJsonString(), Is.EqualTo(eventPayload.ToJsonString()));
        Assert.That(viewModel.Tags?.ToJsonString(), Is.EqualTo("{\"tag_key\":\"tag_value\"}"));
        Assert.That(viewModel.Contexts?.ToJsonString(), Is.EqualTo("{\"context_key.inner_key\":\"context_value\"}"));
        Assert.That(viewModel.Extra?.ToJsonString(), Is.EqualTo("{\"extra_key\":\"extra_value\"}"));
        Assert.That(viewModel.Sdk?.ToJsonString(), Is.EqualTo("{\"name\":\"Sentry.Test\",\"version\":\"1.0\"}"));
        Assert.That(viewModel.Attachments, Has.Count.EqualTo(1));
        Assert.That(viewModel.Attachments![0].Filename, Is.EqualTo("test.txt"));
        Assert.That(Encoding.UTF8.GetString(viewModel.Attachments[0].Data), Is.EqualTo("attachment content"));
    }
}
