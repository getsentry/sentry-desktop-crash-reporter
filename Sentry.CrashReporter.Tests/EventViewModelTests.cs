namespace Sentry.CrashReporter.Tests;

public class EventViewModelTests
{
    [Test]
    public void Defaults()
    {
        // Act
        var viewModel = new EventViewModel();

        // Assert
        Assert.That(viewModel.Envelope, Is.Null.Or.Empty);
        Assert.That(viewModel.Event, Is.Null.Or.Empty);
        Assert.That(viewModel.Payload, Is.Null.Or.Empty);
        Assert.That(viewModel.Tags, Is.Null.Or.Empty);
        Assert.That(viewModel.Contexts, Is.Null.Or.Empty);
        Assert.That(viewModel.Extra, Is.Null.Or.Empty);
        Assert.That(viewModel.Sdk, Is.Null.Or.Empty);
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
        var envelope = new Envelope(
            new JsonObject(),
            new List<EnvelopeItem> { eventItem }
        );

        // Act
        var viewModel = new EventViewModel
        {
            Envelope = envelope
        };

        // Assert
        Assert.That(viewModel.Envelope, Is.EqualTo(envelope));
        Assert.That(viewModel.Event, Is.EqualTo(eventItem));
        Assert.That(viewModel.Payload?.ToJsonString(), Is.EqualTo(eventPayload.ToJsonString()));
        Assert.That(viewModel.Tags?.ToJsonString(), Is.EqualTo("{\"tag_key\":\"tag_value\"}"));
        Assert.That(viewModel.Contexts?.ToJsonString(), Is.EqualTo("{\"context_key.inner_key\":\"context_value\"}"));
        Assert.That(viewModel.Extra?.ToJsonString(), Is.EqualTo("{\"extra_key\":\"extra_value\"}"));
        Assert.That(viewModel.Sdk?.ToJsonString(), Is.EqualTo("{\"name\":\"Sentry.Test\",\"version\":\"1.0\"}"));
    }
}
