using Sentry.CrashReporter.Services;

namespace Sentry.CrashReporter.ViewModels;

public partial class MainViewModel : ReactiveObject, ILoadable
{
    [Reactive] private bool _isExecuting;
    [Reactive] private int _selectedIndex;

    public event EventHandler? IsExecutingChanged;

    public MainViewModel(ICrashReporter? reporter = null)
    {
        reporter ??= App.Services.GetRequiredService<ICrashReporter>();

        this.WhenAnyValue(x => x.IsExecuting)
            .Subscribe(x => IsExecutingChanged?.Invoke(this, EventArgs.Empty));

        IsExecuting = true;

        Observable.FromAsync(() => reporter.LoadAsync().AsTask())
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(value => IsExecuting = false);
    }
}
