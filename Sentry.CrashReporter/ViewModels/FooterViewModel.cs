using Sentry.CrashReporter.Services;

namespace Sentry.CrashReporter.ViewModels;

public partial class FooterViewModel : ReactiveObject
{
    private readonly ICrashReporter _reporter;
    [Reactive] private Envelope? _envelope;
    [ObservableAsProperty] private string? _dsn = string.Empty;
    [ObservableAsProperty] private string? _eventId = string.Empty;
    [ObservableAsProperty] private string? _shortEventId = string.Empty;
    private readonly IObservable<bool> _canSubmit;

    public FooterViewModel(ICrashReporter? reporter = null)
    {
        _reporter = reporter ?? Ioc.Default.GetRequiredService<ICrashReporter>();

        _dsnHelper = this.WhenAnyValue(x => x.Envelope,  e => e?.TryGetDsn())
            .ToProperty(this, x => x.Dsn);

        _eventIdHelper = this.WhenAnyValue(x => x.Envelope, e => e?.TryGetEventId())
            .ToProperty(this, x => x.EventId);

        _shortEventIdHelper = this.WhenAnyValue(x => x.EventId)
            .Select(eventId => eventId?.Replace("-", string.Empty)[..8])
            .ToProperty(this, x => x.ShortEventId);

        _canSubmit = this.WhenAnyValue(x => x.Dsn, dsn => !string.IsNullOrWhiteSpace(dsn));

        Observable.FromAsync(() => _reporter.LoadAsync().AsTask())
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(value => Envelope = value);
    }

    [ReactiveCommand(CanExecute = nameof(_canSubmit))]
    private async Task Submit()
    {
        await _reporter.SubmitAsync();

        (Application.Current as App)?.MainWindow?.Close(); // TODO: cleanup
    }

    [ReactiveCommand]
    private void Cancel()
    {
        (Application.Current as App)?.MainWindow?.Close(); // TODO: cleanup
    }
}
