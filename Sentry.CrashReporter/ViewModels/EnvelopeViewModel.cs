using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Nodes;
using Sentry.CrashReporter.Services;
using Path = System.IO.Path;

namespace Sentry.CrashReporter.ViewModels;

public partial class EnvelopeViewModel : ReactiveObject
{
    [Reactive] private Envelope? _envelope;
    [ObservableAsProperty] private string? _eventId = string.Empty;
    [ObservableAsProperty] private FormattedEnvelope? _formatted;
    private readonly IObservable<bool> _canLaunch;

    public EnvelopeViewModel(ICrashReporter? reporter = null)
    {
        reporter ??= App.Services.GetRequiredService<ICrashReporter>();
        FilePath = reporter.FilePath;

        _eventIdHelper = this.WhenAnyValue(x => x.Envelope, e => e?.TryGetEventId())
            .ToProperty(this, x => x.EventId);

        _formattedHelper = this.WhenAnyValue(x => x.Envelope, e => e?.Format())
            .ToProperty(this, x => x.Formatted);

        _canLaunch = this.WhenAnyValue(x => x.FilePath,
            filePath => !string.IsNullOrWhiteSpace(filePath) && Uri.TryCreate(filePath, UriKind.Absolute, out _));

        Observable.FromAsync(() => reporter.LoadAsync().AsTask())
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(value => Envelope = value);
    }

    public string? FilePath { get; }
    public string? FileName => Path.GetFileName(FilePath);
    public string? Directory => Path.GetDirectoryName(FilePath);

    [ReactiveCommand(CanExecute = nameof(_canLaunch))]
    private async Task Launch() => await Launcher.LaunchUriAsync(new Uri(FilePath!, UriKind.Absolute));
}
