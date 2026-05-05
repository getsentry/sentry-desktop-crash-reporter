using Sentry.CrashReporter.Extensions;

namespace Sentry.CrashReporter.Services;

public interface IWindowService
{
    event Func<Task>? Closing;
    void Register(Window window);
    void SetClosable(bool closable);
    Task RequestCloseAsync();
    void Close();
}

public class WindowService : IWindowService
{
    private Window? _window;
    private bool _isClosable = true;
    private bool _forceClose;
    private bool _isClosing;

    public event Func<Task>? Closing;

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

    public async Task RequestCloseAsync()
    {
        if (!_isClosable || _forceClose || _isClosing)
        {
            return;
        }
        _isClosing = true;

        try
        {
            await NotifyClosingAsync();
            Close();
        }
        finally
        {
            if (!_forceClose)
            {
                _isClosing = false;
            }
        }
    }

    public void Close()
    {
        _forceClose = true;
        _window?.Close();
    }

    private async void OnClosing(Microsoft.UI.Windowing.AppWindow sender, Microsoft.UI.Windowing.AppWindowClosingEventArgs args)
    {
        if (_forceClose)
        {
            return;
        }

        args.Cancel = true;
        await RequestCloseAsync();
    }

    private async Task NotifyClosingAsync()
    {
        foreach (Func<Task> handler in Closing?.GetInvocationList() ?? [])
        {
            try
            {
                await handler();
            }
            catch (Exception e)
            {
                this.Log().LogWarning(e, "Window closing handler failed.");
            }
        }
    }
}
