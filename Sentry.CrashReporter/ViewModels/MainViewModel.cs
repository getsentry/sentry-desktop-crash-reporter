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
    [ObservableAsProperty] private string? _fileName = string.Empty;

    public event EventHandler? IsExecutingChanged;

    public MainViewModel(ICrashReporter? reporter = null)
    {
        reporter ??= App.Services.GetRequiredService<ICrashReporter>();

        this.WhenAnyValue(x => x.IsExecuting)
            .Subscribe(x => IsExecutingChanged?.Invoke(this, EventArgs.Empty));

        _subtitleHelper = this.WhenAnyValue(x => x.SelectedIndex, y => y.FileName, ResolveSubtitle)
            .ToProperty(this, x => x.Subtitle);

        _attachmentsHelper = this.WhenAnyValue(x => x.Envelope)
            .Select(envelope => envelope?.TryGetAttachments())
            .ToProperty(this, x => x.Attachments);

        _fileNameHelper = this.WhenAnyValue(x => x.Envelope)
            .Select(envelope => Path.GetFileName(envelope?.FilePath))
            .ToProperty(this, x => x.FileName);

        IsExecuting = true;

        Observable.FromAsync(() => reporter.LoadAsync().AsTask())
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(value =>
            {
                Envelope = value;
                IsExecuting = false;
            });
    }

    private static string ResolveSubtitle(int index, string? fileName)
    {
        return index switch
        {
            0 => "Feedback (optional)",
            1 => "Event",
            2 => "Attachments",
            _ => string.IsNullOrEmpty(fileName) ? "Envelope" : $"Envelope ({fileName})",
        };
    }
}
