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
        Assert.That(exceptionText?.GetDependencyPropertyValue("Text"), Is.EqualTo("SIGSEGV"));

        var releaseText = App.Marked("releaseText");
        Assert.That(releaseText?.GetDependencyPropertyValue("Text"), Is.EqualTo("sentry-playground@1.2.3"));

        var osText = App.Marked("osText");
        Assert.That(osText?.GetDependencyPropertyValue("Text"), Is.EqualTo("Linux 6.14.0"));

        var environmentText = App.Marked("environmentText");
        Assert.That(environmentText?.GetDependencyPropertyValue("Text"), Is.EqualTo("development"));
    }

    [Test]
    public void EventView()
    {
        App.WaitForElement(q => q.All().Class("EventView"));

        var tagsExpander = App.Marked("tagsExpander");
        Assert.That(tagsExpander?.GetDependencyPropertyValue("Visibility"), Is.EqualTo("Visible"));

        var contextsExpander = App.Marked("contextsExpander");
        Assert.That(contextsExpander?.GetDependencyPropertyValue("Visibility"), Is.EqualTo("Visible"));

        var extraExpander = App.Marked("extraExpander");
        Assert.That(extraExpander?.GetDependencyPropertyValue("Visibility"), Is.EqualTo("Collapsed"));

        var sdkExpander = App.Marked("sdkExpander");
        Assert.That(sdkExpander?.GetDependencyPropertyValue("Visibility"), Is.EqualTo("Visible"));

        var attachmentsExpander = App.Marked("attachmentsExpander");
        Assert.That(attachmentsExpander?.GetDependencyPropertyValue("Visibility"), Is.EqualTo("Collapsed"));
    }

    [Test]
    public void FeedbackView()
    {
        App.WaitForElement(q => q.All().Class("FeedbackView"));

        var nameTextBox = App.Marked("nameTextBox");
        Assert.That(nameTextBox?.GetDependencyPropertyValue("IsEnabled"), Is.EqualTo("True"));

        var emailTextBox = App.Marked("emailTextBox");
        Assert.That(emailTextBox?.GetDependencyPropertyValue("IsEnabled"), Is.EqualTo("True"));

        var messageTextBox = App.Marked("messageTextBox");
        Assert.That(messageTextBox?.GetDependencyPropertyValue("IsEnabled"), Is.EqualTo("True"));
    }

    [Test]
    public void FooterView()
    {
        App.WaitForElement(q => q.All().Class("FooterView"));

        var eventIdText = App.Marked("eventIdText");
        Assert.That(eventIdText?.GetDependencyPropertyValue("Text"), Is.EqualTo("4bf326c3"));

        var cancelButton = App.Marked("cancelButton");
        Assert.That(cancelButton?.GetDependencyPropertyValue("IsEnabled"), Is.EqualTo("True"));

        var submitButton = App.Marked("submitButton");
        Assert.That(submitButton?.GetDependencyPropertyValue("IsEnabled"), Is.EqualTo("True"));
    }
}
