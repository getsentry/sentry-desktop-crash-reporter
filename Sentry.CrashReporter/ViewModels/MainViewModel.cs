using Sentry.CrashReporter.Services;
using Path = System.IO.Path;

namespace Sentry.CrashReporter.ViewModels;

public partial class MainViewModel : ReactiveObject, ILoadable
{
    [Reactive] private bool _isExecuting;
    [Reactive] private int _selectedIndex;
    [ObservableAsProperty] private string _subtitle = string.Empty;
    private readonly string? _fileName;

    public event EventHandler? IsExecutingChanged;

    public MainViewModel(ICrashReporter? reporter = null)
    {
        reporter ??= App.Services.GetRequiredService<ICrashReporter>();
        _fileName = Path.GetFileName(reporter.FilePath);

        this.WhenAnyValue(x => x.IsExecuting)
            .Subscribe(x => IsExecutingChanged?.Invoke(this, EventArgs.Empty));

        _subtitleHelper = this.WhenAnyValue(x => x.SelectedIndex, ResolveSubtitle)
            .ToProperty(this, x => x.Subtitle);

        IsExecuting = true;

        Observable.FromAsync(() => reporter.LoadAsync().AsTask())
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(value => IsExecuting = false);
    }

    private string ResolveSubtitle(int index)
    {
        return index switch
        {
            0 => "Feedback (Optional)",
            1 => "Event",
            _ => string.IsNullOrEmpty(_fileName) ? "Envelope" : $"Envelope ({_fileName})",
        };
    }
}
