using Microsoft.UI.Dispatching;
using Sentry.CrashReporter.Services;

namespace Sentry.CrashReporter.ViewModels;

public partial class FeedbackViewModel : ObservableObject
{
    private readonly ISentryClient _client;
    private Envelope? _envelope;
    [ObservableProperty] private string? _dsn;
    [ObservableProperty] private string? _eventId;
    [ObservableProperty] private string _description = string.Empty;
    [ObservableProperty] private string _email = string.Empty;
    [ObservableProperty] private string _name = string.Empty;
    [ObservableProperty] private bool _isEnabled;

    partial void OnNameChanged(string value)
    {
        UpdateFeedback();
    }

    partial void OnEmailChanged(string value)
    {
        UpdateFeedback();
    }

    partial void OnDescriptionChanged(string value)
    {
        UpdateFeedback();
    }

    private void UpdateFeedback()
    {
        _client.UpdateFeedback(new Feedback(Name, Email, Description));
    }

    public FeedbackViewModel(IEnvelopeService? service = null, ISentryClient? client = null)
    {
        service ??= Ioc.Default.GetRequiredService<IEnvelopeService>();
        _client = client ?? Ioc.Default.GetRequiredService<ISentryClient>();

        var dispatcherQueue = DispatcherQueue.GetForCurrentThread();
        Task.Run(async () =>
        {
            var envelope = await service.LoadAsync();
            dispatcherQueue.TryEnqueue(() => Envelope = envelope);

            // TODO: do we want to pre-fill the user information?
            // var user = envelope.TryGetEvent()?.TryGetPayload("user");
            // Name = (user?.TryGetProperty("username", out var value) == true ? value.GetString() : null) ?? string.Empty;
            // Email = (user?.TryGetProperty("email", out value) == true ? value.GetString() : null) ?? string.Empty;
        });
    }

    private Envelope? Envelope
    {
        get => _envelope;
        set
        {
            Dsn = value?.TryGetDsn();
            EventId = value?.TryGetEventId();
            IsEnabled = EventId is not null && !string.IsNullOrWhiteSpace(Dsn);
            SetProperty(ref _envelope, value);
        }
    }
}
