using Sentry.CrashReporter.Services;

namespace Sentry.CrashReporter.ViewModels;

public partial class FeedbackViewModel : ReactiveObject
{
    [Reactive] private Envelope? _envelope;
    [ObservableAsProperty] private string? _dsn;
    [ObservableAsProperty] private string? _eventId;
    [ObservableAsProperty] private bool _isEnabled;
    [Reactive] private string _message = string.Empty;
    [Reactive] private string? _email;
    [Reactive] private string? _name;

    public FeedbackViewModel(ICrashReporter? reporter = null)
    {
        reporter ??= App.Services.GetRequiredService<ICrashReporter>();
        if (reporter.Feedback != null)
        {
            Name = reporter.Feedback.Name;
            Email = reporter.Feedback.Email;
            Message = reporter.Feedback.Message;
        }

        _dsnHelper = this.WhenAnyValue(x => x.Envelope, e => e?.TryGetDsn())
            .ToProperty(this, x => x.Dsn);

        _eventIdHelper = this.WhenAnyValue(x => x.Envelope, e => e?.TryGetEventId())
            .ToProperty(this, x => x.EventId);

        _isEnabledHelper = this.WhenAnyValue(x => x.Dsn, y => y.EventId, (x, y) => !string.IsNullOrWhiteSpace(x) && !string.IsNullOrWhiteSpace(y))
            .ToProperty(this, x => x.IsEnabled);

        this.WhenAnyValue(x => x.Name, x => x.Email, x => x.Message)
            .Subscribe(_ => reporter.UpdateFeedback(new Feedback(Name, Email, Message)));

        Observable.FromAsync(() => reporter.LoadAsync().AsTask())
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(value => Envelope = value);
    }
}
