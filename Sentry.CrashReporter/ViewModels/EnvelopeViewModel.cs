namespace Sentry.CrashReporter.ViewModels;

public partial class EnvelopeViewModel : ReactiveObject
{
    [Reactive] private Envelope? _envelope;
    [ObservableAsProperty] private string? _eventId = string.Empty;
    [ObservableAsProperty] private string? _filePath = string.Empty;
    [ObservableAsProperty] private FormattedEnvelope? _formatted;
    private readonly IObservable<bool> _canLaunch;

    public EnvelopeViewModel()
    {
        _eventIdHelper = this.WhenAnyValue(x => x.Envelope, e => e?.TryGetEventId())
            .ToProperty(this, x => x.EventId);

        _filePathHelper = this.WhenAnyValue(x => x.Envelope)
            .Select(e => e?.FilePath)
            .ToProperty(this, x => x.FilePath);

        _formattedHelper = this.WhenAnyValue(x => x.Envelope, e => e?.Format())
            .ToProperty(this, x => x.Formatted);

        _canLaunch = this.WhenAnyValue(x => x.FilePath,
            filePath => !string.IsNullOrWhiteSpace(filePath) && Uri.TryCreate(filePath, UriKind.RelativeOrAbsolute, out _));
    }

    [ReactiveCommand(CanExecute = nameof(_canLaunch))]
    private async Task Launch() => await Launcher.LaunchUriAsync(new Uri(FilePath!, UriKind.Absolute));
}
