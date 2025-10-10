using Uno.UITest.Helpers;

namespace Sentry.CrashReporter.UITests;

public class UITests : TestBase
{
    [Test]
    public void MainPage()
    {
        App.WaitForElement(q => q.All().Class("MainPage"));
    }

    [Test]
    public void HeaderView()
    {
        App.WaitForElement(q => q.All().Class("HeaderView"));

        var exceptionText = App.Marked("exceptionText");
        Assert.That(exceptionText?.GetText(), Is.EqualTo("SIGSEGV"));

        var releaseText = App.Marked("releaseText");
        Assert.That(releaseText?.GetText(), Is.EqualTo("sentry-playground@1.2.3"));

        var osText = App.Marked("osText");
        Assert.That(osText?.GetText(), Is.EqualTo("Linux 6.14.0"));

        var environmentText = App.Marked("environmentText");
        Assert.That(environmentText?.GetText(), Is.EqualTo("development"));
    }

    [Test]
    public void EventView()
    {
        App.WaitForElement(q => q.All().Class("EventView"));

        var tagsExpander = App.Marked("tagsExpander");
        Assert.That(tagsExpander, Is.Not.Null);
        Assert.That(tagsExpander!.IsVisible(), Is.True);
        tagsExpander.Tap();
        App.WaitForElement(q => tagsExpander.Unwrap(q).Text("backend"));
        App.WaitForElement(q => tagsExpander.Unwrap(q).Text("inproc"));

        var contextsExpander = App.Marked("contextsExpander");
        Assert.That(contextsExpander, Is.Not.Null);
        Assert.That(contextsExpander!.IsVisible(), Is.True);
        contextsExpander.Tap();
        App.WaitForElement(q => contextsExpander.Unwrap(q).Text("os.name"));
        App.WaitForElement(q => contextsExpander.Unwrap(q).Text("Linux"));
        App.WaitForElement(q => contextsExpander.Unwrap(q).Text("os.version"));
        App.WaitForElement(q => contextsExpander.Unwrap(q).Text("6.14.0"));

        var extraExpander = App.Marked("extraExpander");
        Assert.That(extraExpander, Is.Not.Null);
        Assert.That(extraExpander!.IsCollapsed(), Is.True);

        var sdkExpander = App.Marked("sdkExpander");
        Assert.That(sdkExpander, Is.Not.Null);
        Assert.That(sdkExpander!.IsVisible(), Is.True);
        sdkExpander.Tap();
        App.WaitForElement(q => sdkExpander.Unwrap(q).Text("name"));
        App.WaitForElement(q => sdkExpander.Unwrap(q).Text("sentry.native"));
        App.WaitForElement(q => sdkExpander.Unwrap(q).Text("packages[0].name"));
        App.WaitForElement(q => sdkExpander.Unwrap(q).Text("github:getsentry/sentry-native"));

        var attachmentsExpander = App.Marked("attachmentsExpander");
        Assert.That(attachmentsExpander, Is.Not.Null);
        Assert.That(attachmentsExpander!.IsCollapsed(), Is.True);
    }

    [Test]
    public void FeedbackView()
    {
        App.WaitForElement(q => q.All().Class("FeedbackView"));

        var nameTextBox = App.Marked("nameTextBox");
        Assert.That(nameTextBox?.IsEnabled(), Is.True);
        App.EnterText(nameTextBox, "John Doe");

        var emailTextBox = App.Marked("emailTextBox");
        Assert.That(emailTextBox?.IsEnabled(), Is.True);
        App.EnterText(emailTextBox, "john.doe@example.com");

        var messageTextBox = App.Marked("messageTextBox");
        Assert.That(messageTextBox?.IsEnabled(), Is.True);
        App.EnterText(messageTextBox, "It crashed!");

        // TODO: submit
    }

    [Test]
    public void FooterView()
    {
        App.WaitForElement(q => q.All().Class("FooterView"));

        var eventIdText = App.Marked("eventIdText");
        Assert.That(eventIdText?.GetText(), Is.EqualTo("4bf326c3"));

        var cancelButton = App.Marked("cancelButton");
        Assert.That(cancelButton?.IsEnabled(), Is.True);

        var submitButton = App.Marked("submitButton");
        Assert.That(submitButton?.IsEnabled(), Is.True);
    }
}

internal static class QueryExtensions
{
    public static string? GetText(this QueryEx query) => query.GetDependencyPropertyValue("Text")?.ToString();
    public static bool IsEnabled(this QueryEx query) => query.GetDependencyPropertyValue("IsEnabled")?.ToString() == "True";
    public static bool IsVisible(this QueryEx query) => query.GetDependencyPropertyValue("Visibility")?.ToString() == "Visible";
    public static bool IsCollapsed(this QueryEx query) => query.GetDependencyPropertyValue("Visibility")?.ToString() == "Collapsed";
}
