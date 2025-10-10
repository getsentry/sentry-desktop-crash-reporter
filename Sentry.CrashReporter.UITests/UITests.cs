namespace Sentry.CrashReporter.UITests;

public class UITests : TestBase
{
    [Test]
    public void MainPage()
    {
        App.WaitForElement(q => q.All().Class("MainPage"));
        App.WaitForElement(q => q.All().Class("HeaderView"));
        App.WaitForElement(q => q.All().Class("EventView"));
        App.WaitForElement(q => q.All().Class("FeedbackView"));
        App.WaitForElement(q => q.All().Class("FooterView"));
        TakeScreenshot("After launch");
    }

    [Test]
    public void HeaderView()
    {
        Query headerView = q => q.All().Class("HeaderView");
        App.WaitForElement(q => headerView(q));
        App.WaitForElement(q => headerView(q).Text("SIGSEGV"));
        App.WaitForElement(q => headerView(q).Text("sentry-playground@1.2.3"));
        App.WaitForElement(q => headerView(q).Text("Linux 6.14.0"));
        App.WaitForElement(q => headerView(q).Text("development"));
    }
    
    [Test]
    public void FooterView()
    {
        Query footerView = q => q.All().Class("FooterView");
        App.WaitForElement(footerView);

        Query cancelButton = q => footerView(q).Button("Cancel");
        App.WaitForElement(cancelButton);
        Assert.That(App.Query(cancelButton).Single().Enabled, Is.True);

        Query submitButton = q => footerView(q).Button("Submit");
        App.WaitForElement(submitButton);
        Assert.That(App.Query(submitButton).Single().Enabled, Is.True);
    }
}
