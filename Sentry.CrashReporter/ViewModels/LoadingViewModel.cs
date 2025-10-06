using Microsoft.UI.Dispatching;
using Sentry.CrashReporter.Services;

namespace Sentry.CrashReporter.ViewModels;

public class LoadingViewModel : ILoadable
{
    private bool _isExecuting;

    public bool IsExecuting
    {
        get => _isExecuting;
        private set
        {
            _isExecuting = value;
            IsExecutingChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public event EventHandler? IsExecutingChanged;

    public LoadingViewModel(ICrashReporter? reporter = null)
    {
        reporter ??= Ioc.Default.GetRequiredService<ICrashReporter>();

        IsExecuting = true;
        var dispatcherQueue = DispatcherQueue.GetForCurrentThread();
        Task.Run(async () =>
        {
            await reporter.LoadAsync();
            dispatcherQueue.TryEnqueue(() => IsExecuting = false);
        });
    }
}
