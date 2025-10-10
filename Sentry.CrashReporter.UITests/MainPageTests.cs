namespace Sentry.CrashReporter.UITests;

public class MainPageTests : TestBase
{
    [Test]
    public void Structure()
    {
        App.WaitForElement(q => q.All().Class("MainPage"));
        App.WaitForElement(q => q.All().Class("HeaderView"));
        App.WaitForElement(q => q.All().Class("EventView"));
        App.WaitForElement(q => q.All().Class("FeedbackView"));
        App.WaitForElement(q => q.All().Class("FooterView"));
        TakeScreenshot("After launch");
    }
}
