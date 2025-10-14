namespace Sentry.CrashReporter.Services;

public interface IWindowService
{
    void Close();
}

public class WindowService : IWindowService
{
    public void Close()
    {
        (Application.Current as App)?.MainWindow?.Close();
    }
}
