using Sentry.CrashReporter.Extensions;

namespace Sentry.CrashReporter.Services;

public interface IWindowService
{
    void Register(Window window);
    void SetClosable(bool closable);
    void Close();
}

public class WindowService : IWindowService
{
    private Window? _window;
    private bool _isClosable = true;
    private bool _forceClose;

    public void Register(Window window)
    {
        _window = window;
        window.AppWindow.Closing += OnClosing;
    }

    public void SetClosable(bool closable)
    {
        _isClosable = closable;
        _window?.SetClosable(closable);
    }

    public void Close()
    {
        _forceClose = true;
        _window?.Close();
    }

    private void OnClosing(Microsoft.UI.Windowing.AppWindow sender, Microsoft.UI.Windowing.AppWindowClosingEventArgs args)
    {
        if (!_isClosable && !_forceClose)
        {
            args.Cancel = true;
        }
    }
}
