using Sentry.CrashReporter.Services;

namespace Sentry.CrashReporter.ViewModels;

public enum FooterStatus
{
    Empty,
    Normal,
    Busy,
    Error
}

public partial class FooterViewModel : ReactiveObject
{
    private readonly ICrashReporter _reporter;
    private readonly IWindowService _window;
    [Reactive] private Envelope? _envelope;
    [ObservableAsProperty] private string? _dsn = string.Empty;
    [ObservableAsProperty] private string? _eventId = string.Empty;
    [ObservableAsProperty] private string? _shortEventId = string.Empty;
    [ObservableAsProperty] private bool _isSubmitting;
    private readonly IObservable<bool> _canSubmit;
    [Reactive] private string? _errorMessage;
    [ObservableAsProperty] private FooterStatus _status;

    public FooterViewModel(ICrashReporter? reporter = null, IWindowService? windowService = null)
    {
        _reporter = reporter ?? App.Services.GetRequiredService<ICrashReporter>();
        _window = windowService ?? App.Services.GetRequiredService<IWindowService>();

        _dsnHelper = this.WhenAnyValue(x => x.Envelope,  e => e?.TryGetDsn())
            .ToProperty(this, x => x.Dsn);

        _eventIdHelper = this.WhenAnyValue(x => x.Envelope, e => e?.TryGetEventId())
            .ToProperty(this, x => x.EventId);

        _shortEventIdHelper = this.WhenAnyValue(x => x.EventId)
            .Select(eventId => eventId?.Replace("-", string.Empty)[..8])
            .ToProperty(this, x => x.ShortEventId);

        _canSubmit = this.WhenAnyValue(x => x.Dsn, dsn => !string.IsNullOrWhiteSpace(dsn));

        _isSubmittingHelper = this.WhenAnyObservable(x => x.SubmitCommand.IsExecuting)
            .ToProperty(this, x => x.IsSubmitting);

        _statusHelper = this.WhenAnyValue(
                x => x.EventId,
                x => x.IsSubmitting,
                x => x.ErrorMessage,
                (eventId, isSubmitting, errorMessage) =>
                {
                    if (isSubmitting) return FooterStatus.Busy;
                    if (!string.IsNullOrEmpty(errorMessage)) return FooterStatus.Error;
                    if (!string.IsNullOrEmpty(eventId)) return FooterStatus.Normal;
                    return FooterStatus.Empty;
                })
            .ToProperty(this, x => x.Status);
    }

    [ReactiveCommand(CanExecute = nameof(_canSubmit))]
    private async Task Submit()
    {
        ErrorMessage = null;
        try
        {
            await _reporter.SubmitAsync();
            _window.Close();
        }
        catch (Exception e)
        {
            ErrorMessage = e.Message;
        }
    }

    [ReactiveCommand]
    private void Cancel() => _window.Close();
}
