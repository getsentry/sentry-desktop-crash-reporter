using Sentry.CrashReporter.Extensions;
using Sentry.CrashReporter.Services;
using Path = System.IO.Path;

namespace Sentry.CrashReporter.ViewModels;

public partial class MainViewModel : ReactiveObject, ILoadable
{
    [Reactive] private Envelope? _envelope;
    [Reactive] private bool _isExecuting;
    [Reactive] private int _selectedIndex;
    [ObservableAsProperty] private string _subtitle = string.Empty;
    [ObservableAsProperty] private List<Attachment>? _attachments;
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

        _attachmentsHelper = this.WhenAnyValue(x => x.Envelope)
            .Select(envelope => envelope?.TryGetAttachments())
            .ToProperty(this, x => x.Attachments);

        IsExecuting = true;

        Observable.FromAsync(() => reporter.LoadAsync().AsTask())
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(value =>
            {
                Envelope = value;
                IsExecuting = false;
            });
    }

    private string ResolveSubtitle(int index)
    {
        return index switch
        {
            0 => "Feedback (optional)",
            1 => "Event",
            2 => "Attachments",
            _ => string.IsNullOrEmpty(_fileName) ? "Envelope" : $"Envelope ({_fileName})",
        };
    }
}
