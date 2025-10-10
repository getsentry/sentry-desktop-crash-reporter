namespace Sentry.CrashReporter.UITests;

public class FooterViewTests : TestBase
{
    [Test]
    public void Structure()
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
