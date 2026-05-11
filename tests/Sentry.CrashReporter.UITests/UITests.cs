using System.Net;
using Sentry.CrashReporter.Models;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;
using WireMock.Settings;
using WireMock.Types;

namespace Sentry.CrashReporter.UITests;

public class UITests : TestBase
{
    [Test]
    public async Task Submit_EventOnly()
    {
        // Arrange
        App.WaitForElement(q => q.All().Class("MainPage"));
        App.WaitForElement(q => q.All().Class("FeedbackView"));
        App.WaitForElement(q => q.All().Class("FooterView"));

        using var server = WireMockServer.Start(new WireMockServerSettings
        {
            Port = 8000,
            CorsPolicyOptions = CorsPolicyOptions.AllowAll,
        });
        server.Given(Request.Create()
                .UsingPost())
                .WithPath("/api/3/envelope/")
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody("{\"id\":\"abcd1234\"}"));

        // Act
        App.ClearText(App.Marked("nameTextBox"));
        App.ClearText(App.Marked("emailTextBox"));
        App.ClearText(App.Marked("messageTextBox"));
        App.Tap(App.Marked("submitButton"));
        await WaitUntilAsync(() => server.LogEntries.Count >= 1, TimeSpan.FromSeconds(30));

        // Assert
        Assert.That(server.LogEntries, Has.Count.EqualTo(1));

        using var eventStream = new MemoryStream(server.LogEntries.Single().RequestMessage.BodyAsBytes ?? []);
        var eventEnvelope = await Envelope.DeserializeAsync(eventStream);
        var eventException = eventEnvelope.TryGetException();
        Assert.That(eventException, Is.Not.Null);
        Assert.That(eventException!.Type, Is.EqualTo("SIGSEGV"));
    }

    [Test]
    public async Task Submit_EventWithFeedback()
    {
        // Arrange
        App.WaitForElement(q => q.All().Class("MainPage"));
        App.WaitForElement(q => q.All().Class("FeedbackView"));
        App.WaitForElement(q => q.All().Class("FooterView"));

        using var server = WireMockServer.Start(new WireMockServerSettings
        {
            Port = 8000,
            CorsPolicyOptions = CorsPolicyOptions.AllowAll,
        });
        server.Given(Request.Create()
                .UsingPost())
                .WithPath("/api/3/envelope/")
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody("{\"id\":\"abcd1234\"}"));

        // Act
        App.EnterText(App.Marked("messageTextBox"), "It crashed!");
        App.EnterText(App.Marked("nameTextBox"), "John Doe");
        App.EnterText(App.Marked("emailTextBox"), "john.doe@example.com");
        App.Tap(App.Marked("submitButton"));
        await WaitUntilAsync(() => server.LogEntries.Count >= 2, TimeSpan.FromSeconds(30));

        // Assert
        Assert.That(server.LogEntries, Has.Count.EqualTo(2));

        using var eventStream = new MemoryStream(server.LogEntries.First().RequestMessage.BodyAsBytes ?? []);
        var eventEnvelope = await Envelope.DeserializeAsync(eventStream);
        var eventException = eventEnvelope.TryGetException();
        Assert.That(eventException, Is.Not.Null);
        Assert.That(eventException!.Type, Is.EqualTo("SIGSEGV"));

        using var feedbackStream = new MemoryStream(server.LogEntries.Last().RequestMessage.BodyAsBytes ?? []);
        var feedbackEnvelope = await Envelope.DeserializeAsync(feedbackStream);
        var feedbackItem = feedbackEnvelope.Items.FirstOrDefault(i => i.TryGetHeader("type") == "feedback");
        Assert.That(feedbackItem, Is.Not.Null);
        var feedbackJson = feedbackItem!.TryParseAsJson()?["contexts"]?["feedback"];
        Assert.That(feedbackJson, Is.Not.Null);
        Assert.That(feedbackJson!["name"]?.ToString(), Is.EqualTo("John Doe"));
        Assert.That(feedbackJson!["contact_email"]?.ToString(), Is.EqualTo("john.doe@example.com"));
        Assert.That(feedbackJson!["message"]?.ToString(), Is.EqualTo("It crashed!"));
        Assert.That(feedbackJson!["associated_event_id"]?.ToString(), Is.EqualTo("4bf326c30e574542e355035a23d438df"));
    }

    [Test]
    public async Task Submit_Error()
    {
        // Arrange
        App.WaitForElement(q => q.All().Class("MainPage"));
        App.WaitForElement(q => q.All().Class("FeedbackView"));
        App.WaitForElement(q => q.All().Class("FooterView"));

        using var server = WireMockServer.Start(new WireMockServerSettings
        {
            Port = 8000,
            CorsPolicyOptions = CorsPolicyOptions.AllowAll,
        });
        server.Given(Request.Create()
                .UsingPost())
            .WithPath("/api/3/envelope/")
            .RespondWith(Response.Create().WithStatusCode(HttpStatusCode.NotFound));

        // Act
        App.Tap(App.Marked("submitButton"));
        await WaitUntilAsync(() => server.LogEntries.Count >= 1, TimeSpan.FromSeconds(30));

        // Assert
        Assert.That(server.LogEntries, Has.Count.EqualTo(1));
        App.WaitForElement(App.Find(q => q.WithText("Response status code does not indicate success: 404 (Not Found).")));
    }

    [Test]
    public async Task Submit_Retry_EventOnly()
    {
        // Arrange
        App.WaitForElement(q => q.All().Class("MainPage"));
        App.WaitForElement(q => q.All().Class("FeedbackView"));
        App.WaitForElement(q => q.All().Class("FooterView"));

        using var server = WireMockServer.Start(new WireMockServerSettings
        {
            Port = 8000,
            CorsPolicyOptions = CorsPolicyOptions.AllowAll,
        });

        server.Given(Request.Create().UsingPost().WithPath("/api/3/envelope/"))
            .InScenario("Submit-Retry")
            .WillSetStateTo("Failed")
            .RespondWith(Response.Create().WithStatusCode(HttpStatusCode.InternalServerError));

        server.Given(Request.Create().UsingPost().WithPath("/api/3/envelope/"))
            .InScenario("Submit-Retry")
            .WhenStateIs("Failed")
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody("{\"id\":\"abcd1234\"}"));

        // Act
        App.ClearText(App.Marked("nameTextBox"));
        App.ClearText(App.Marked("emailTextBox"));
        App.ClearText(App.Marked("messageTextBox"));
        App.Tap(App.Marked("submitButton"));
        await WaitUntilAsync(() => server.LogEntries.Count >= 2, TimeSpan.FromSeconds(30));

        // Assert
        Assert.That(server.LogEntries, Has.Count.EqualTo(2));
        foreach (var entry in server.LogEntries)
        {
            using var eventStream = new MemoryStream(entry.RequestMessage.BodyAsBytes ?? []);
            var eventEnvelope = await Envelope.DeserializeAsync(eventStream);
            var eventException = eventEnvelope.TryGetException();
            Assert.That(eventException, Is.Not.Null);
            Assert.That(eventException!.Type, Is.EqualTo("SIGSEGV"));
        }
    }

    [Test]
    public async Task Submit_Retry_WithFeedback()
    {
        // Arrange
        App.WaitForElement(q => q.All().Class("MainPage"));
        App.WaitForElement(q => q.All().Class("FeedbackView"));
        App.WaitForElement(q => q.All().Class("FooterView"));

        using var server = WireMockServer.Start(new WireMockServerSettings
        {
            Port = 8000,
            CorsPolicyOptions = CorsPolicyOptions.AllowAll,
        });

        server.Given(Request.Create().UsingPost().WithPath("/api/3/envelope/"))
            .InScenario("Submit-Retry")
            .WillSetStateTo("Failed")
            .RespondWith(Response.Create().WithStatusCode(HttpStatusCode.InternalServerError));

        server.Given(Request.Create().UsingPost().WithPath("/api/3/envelope/"))
            .InScenario("Submit-Retry")
            .WhenStateIs("Failed")
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody("{\"id\":\"abcd1234\"}"));

        // Act
        App.EnterText(App.Marked("messageTextBox"), "It crashed!");
        App.EnterText(App.Marked("nameTextBox"), "John Doe");
        App.EnterText(App.Marked("emailTextBox"), "john.doe@example.com");
        App.Tap(App.Marked("submitButton"));
        await WaitUntilAsync(() => server.LogEntries.Count >= 3, TimeSpan.FromSeconds(30));

        // Assert
        Assert.That(server.LogEntries, Has.Count.EqualTo(3));
        foreach (var entry in server.LogEntries.Take(2))
        {
            using var eventStream = new MemoryStream(entry.RequestMessage.BodyAsBytes ?? []);
            var eventEnvelope = await Envelope.DeserializeAsync(eventStream);
            var eventException = eventEnvelope.TryGetException();
            Assert.That(eventException, Is.Not.Null);
            Assert.That(eventException!.Type, Is.EqualTo("SIGSEGV"));
        }

        using var feedbackStream = new MemoryStream(server.LogEntries.Last().RequestMessage.BodyAsBytes ?? []);
        var feedbackEnvelope = await Envelope.DeserializeAsync(feedbackStream);
        var feedbackItem = feedbackEnvelope.Items.FirstOrDefault(i => i.TryGetHeader("type") == "feedback");
        Assert.That(feedbackItem, Is.Not.Null);
        var feedbackJson = feedbackItem!.TryParseAsJson()?["contexts"]?["feedback"];
        Assert.That(feedbackJson, Is.Not.Null);
        Assert.That(feedbackJson!["name"]?.ToString(), Is.EqualTo("John Doe"));
        Assert.That(feedbackJson!["contact_email"]?.ToString(), Is.EqualTo("john.doe@example.com"));
        Assert.That(feedbackJson!["message"]?.ToString(), Is.EqualTo("It crashed!"));
        Assert.That(feedbackJson!["associated_event_id"]?.ToString(), Is.EqualTo("4bf326c30e574542e355035a23d438df"));
    }
}
