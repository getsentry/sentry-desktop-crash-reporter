namespace Sentry.CrashReporter.RuntimeTests;

[TestClass]
[RunsOnUIThread]
public class ToastTests : RuntimeTestBase
{
    [TestMethod]
    public async Task Toast_Show_CreatesAndShowsToast()
    {
        var panel = new StackPanel();
        var button = new Button();
        panel.Children.Add(button);
        await LoadTestContent(panel);

        const string title = "My Toast";
        const string subtitle = "This is a toast";

        var showTask = Toast.Show(panel, button, title, subtitle, duration: TimeSpan.FromMilliseconds(100));

        var toast = panel.Children.OfType<TeachingTip>().FirstOrDefault();
        Assert.IsNotNull(toast);
        Assert.AreSame(button, toast.Target);
        Assert.AreEqual(title, toast.Title);
        Assert.AreEqual(subtitle, toast.Subtitle);
        Assert.IsTrue(toast.IsOpen);

        await showTask;

        Assert.IsFalse(toast.IsOpen);
    }

    [TestMethod]
    public async Task Toast_Hide_ClosesOpenToast()
    {
        var panel = new StackPanel();
        var button = new Button();
        panel.Children.Add(button);
        await LoadTestContent(panel);

        var showTask = Toast.Show(panel, button, "Title", "Subtitle", duration: TimeSpan.FromSeconds(10));

        var toast = panel.Children.OfType<TeachingTip>().FirstOrDefault();
        Assert.IsNotNull(toast);
        Assert.IsTrue(toast.IsOpen);

        Toast.Hide();

        Assert.IsFalse(toast.IsOpen);

        await showTask;
    }
}
