using Sentry.CrashReporter.Services;

namespace Sentry.CrashReporter.ViewModels;

public partial class LoadingViewModel : ReactiveObject, ILoadable
{
    [Reactive] private bool _isExecuting;

    public event EventHandler? IsExecutingChanged;

    public LoadingViewModel(ICrashReporter? reporter = null)
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
