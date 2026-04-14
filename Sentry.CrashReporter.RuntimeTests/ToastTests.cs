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

        var showTask = Toast.Show(button, title, subtitle, target: button, duration: TimeSpan.FromMilliseconds(100));

        var toast = FindToast(button);
        Assert.IsNotNull(toast);
        Assert.AreSame(button, toast.Target);
        Assert.AreEqual(title, toast.Title);
        Assert.AreEqual(subtitle, toast.Subtitle);
        await UnitTestsUIContentHelper.WaitForIdle();
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

        var showTask = Toast.Show(button, "Title", "Subtitle", target: button, duration: TimeSpan.FromSeconds(10));

        var toast = FindToast(button);
        Assert.IsNotNull(toast);
        await UnitTestsUIContentHelper.WaitForIdle();
        Assert.IsTrue(toast.IsOpen);

        Toast.Hide();

        Assert.IsFalse(toast.IsOpen);

        await showTask;
    }

    private static TeachingTip? FindToast(DependencyObject element)
    {
        DependencyObject? current = element;
        while (current is not null)
        {
            if (current is Panel panel)
            {
                var tip = panel.Children.OfType<TeachingTip>().FirstOrDefault();
                if (tip is not null) return tip;
            }
            current = VisualTreeHelper.GetParent(current);
        }
        return null;
    }
}
