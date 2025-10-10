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
        Assert.That(tagsExpander?.IsVisible(), Is.True);
        tagsExpander!.SetExpanded(false);
        App.Tap(tagsExpander);
        App.WaitForElement(App.FindWithin(q => q.WithText("backend"), tagsExpander));
        App.WaitForElement(App.FindWithin(q => q.WithText("inproc"), tagsExpander));

        var contextsExpander = App.Marked("contextsExpander");
        Assert.That(contextsExpander?.IsVisible(), Is.True);
        contextsExpander!.SetExpanded(false);
        App.Tap(contextsExpander);
        App.WaitForElement(App.FindWithin(q => q.WithText("os.name"), contextsExpander));
        App.WaitForElement(App.FindWithin(q => q.WithText("Linux"), contextsExpander));

        var extraExpander = App.Marked("extraExpander");
        Assert.That(extraExpander?.IsCollapsed(), Is.True);

        var sdkExpander = App.Marked("sdkExpander");
        Assert.That(sdkExpander?.IsVisible(), Is.True);
        sdkExpander!.SetExpanded(false);
        App.Tap(sdkExpander);
        App.WaitForElement(App.FindWithin(q => q.WithText("name"), sdkExpander));
        App.WaitForElement(App.FindWithin(q => q.WithText("sentry.native"), sdkExpander));

        var attachmentsExpander = App.Marked("attachmentsExpander");
        Assert.That(attachmentsExpander?.IsCollapsed(), Is.True);
    }

    [Test]
    public void FeedbackView()
    {
        App.WaitForElement(q => q.All().Class("FeedbackView"));

        var nameTextBox = App.Marked("nameTextBox");
        Assert.That(nameTextBox?.IsEnabled(), Is.True);

        var emailTextBox = App.Marked("emailTextBox");
        Assert.That(emailTextBox?.IsEnabled(), Is.True);

        var messageTextBox = App.Marked("messageTextBox");
        Assert.That(messageTextBox?.IsEnabled(), Is.True);
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
    public static void SetExpanded(this QueryEx query, bool value) => query.SetDependencyPropertyValue("IsExpanded", value ? "True" : "False");
}
