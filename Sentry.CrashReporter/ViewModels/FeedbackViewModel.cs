using Sentry.CrashReporter.Services;

namespace Sentry.CrashReporter.ViewModels;

public partial class FeedbackViewModel : ReactiveObject
{
    [Reactive] private Envelope? _envelope;
    [ObservableAsProperty] private string? _dsn;
    [ObservableAsProperty] private string? _eventId;
    [ObservableAsProperty] private bool _isEnabled;
    [Reactive] private string _description = string.Empty;
    [Reactive] private string? _email;
    [Reactive] private string? _name;

    public FeedbackViewModel(ICrashReporter? reporter = null)
    {
        reporter ??= Ioc.Default.GetRequiredService<ICrashReporter>();

        _dsnHelper = this.WhenAnyValue(x => x.Envelope, e => e?.TryGetDsn())
            .ToProperty(this, x => x.Dsn);

        _eventIdHelper = this.WhenAnyValue(x => x.Envelope, e => e?.TryGetEventId())
            .ToProperty(this, x => x.EventId);

        _isEnabledHelper = this.WhenAnyValue(x => x.Dsn, y => y.EventId, (x, y) => !string.IsNullOrWhiteSpace(x) && !string.IsNullOrWhiteSpace(y))
            .ToProperty(this, x => x.IsEnabled);

        this.WhenAnyValue(x => x.Name, x => x.Email, x => x.Description)
            .Subscribe(_ => reporter.UpdateFeedback(new Feedback(Name, Email, Description)));

        // TODO: do we want to pre-fill the user information?
        // var user = envelope.TryGetEvent()?.TryGetPayload("user");
        // Name = (user?.TryGetProperty("username", out var value) == true ? value.GetString() : null) ?? string.Empty;
        // Email = (user?.TryGetProperty("email", out value) == true ? value.GetString() : null) ?? string.Empty;

        Observable.FromAsync(() => reporter.LoadAsync().AsTask())
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(value => Envelope = value);
    }
}
