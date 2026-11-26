using Path = System.IO.Path;

namespace Sentry.CrashReporter.ViewModels;

public partial class EnvelopeViewModel : ReactiveObject
{
    [Reactive] private Envelope? _envelope;
    [ObservableAsProperty] public partial string? EventId { get; }
    [ObservableAsProperty] public partial string? FilePath { get; }
    [ObservableAsProperty] public partial string? FileName { get; }
    [ObservableAsProperty] public partial FormattedEnvelope? Formatted { get; }
    private readonly IObservable<bool> _canLaunch;

    public EnvelopeViewModel()
    {
        _eventIdHelper = this.WhenAnyValue(x => x.Envelope, e => e?.TryGetEventId())
            .ToProperty(this, x => x.EventId);

        _filePathHelper = this.WhenAnyValue(x => x.Envelope)
            .Select(e => e?.FilePath)
            .ToProperty(this, x => x.FilePath);

        _fileNameHelper = this.WhenAnyValue(x => x.FilePath)
            .Select(Path.GetFileName)
            .ToProperty(this, x => x.FileName);

        _formattedHelper = this.WhenAnyValue(x => x.Envelope, e => e?.Format())
            .ToProperty(this, x => x.Formatted);

        _canLaunch = this.WhenAnyValue(x => x.FilePath,
            filePath => !string.IsNullOrWhiteSpace(filePath) && Uri.TryCreate(filePath, UriKind.RelativeOrAbsolute, out _));
    }

    [ReactiveCommand(CanExecute = nameof(_canLaunch))]
    private async Task Launch() => await Launcher.LaunchUriAsync(new Uri(FilePath!, UriKind.Absolute));
}
