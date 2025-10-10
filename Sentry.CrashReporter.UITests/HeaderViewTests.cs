namespace Sentry.CrashReporter.UITests;

public class HeaderViewTests : TestBase
{
    [Test]
    public void Structure()
    {
        Query headerView = q => q.All().Class("HeaderView");
        App.WaitForElement(q => headerView(q));
        App.WaitForElement(q => headerView(q).Text("SIGSEGV"));
        App.WaitForElement(q => headerView(q).Text("sentry-playground@1.2.3"));
        App.WaitForElement(q => headerView(q).Text("Linux 6.14.0"));
        App.WaitForElement(q => headerView(q).Text("development"));
    }
}
