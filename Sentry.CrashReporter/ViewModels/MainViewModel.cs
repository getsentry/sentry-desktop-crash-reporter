using System.Text.Json.Nodes;
using Sentry.CrashReporter.Extensions;
using Sentry.CrashReporter.Services;

namespace Sentry.CrashReporter.ViewModels;

public partial class MainViewModel : ReactiveObject, ILoadable
{
    [Reactive] private Envelope? _envelope;
    [Reactive] private bool _isExecuting;
    [Reactive] private int _selectedIndex;
    [Reactive] private Exception? _error;
    [ObservableAsProperty] private EnvelopeItem? _event;
    [ObservableAsProperty] private JsonObject? _payload;
    [ObservableAsProperty] private JsonObject? _user;
    [ObservableAsProperty] private JsonObject? _tags;
    [ObservableAsProperty] private JsonObject? _contexts;
    [ObservableAsProperty] private JsonObject? _extra;
    [ObservableAsProperty] private JsonObject? _sdk;
    [ObservableAsProperty] private List<Attachment>? _attachments;
    [ObservableAsProperty] private bool _hasStacktrace;

    public event EventHandler? IsExecutingChanged;

    public MainViewModel(ICrashReporter? reporter = null, IWindowService? windowService = null)
    {
        reporter ??= App.Services.GetRequiredService<ICrashReporter>();
        windowService ??= App.Services.GetRequiredService<IWindowService>();
        windowService.Closing += async () =>
        {
            if (Envelope is not null)
            {
                await reporter.CacheAsync(Envelope);
            }
        };

        this.WhenAnyValue(x => x.IsExecuting)
            .Subscribe(x => IsExecutingChanged?.Invoke(this, EventArgs.Empty));

        _eventHelper = this.WhenAnyValue(x => x.Envelope)
            .Select(envelope => envelope?.TryGetEvent())
            .ToProperty(this, x => x.Event);

        _payloadHelper = this.WhenAnyValue(x => x.Event)
            .Select(ev => ev?.TryParseAsJson())
            .ToProperty(this, x => x.Payload);

        _userHelper = this.WhenAnyValue(x => x.Payload)
            .Select(payload => payload?.TryGetProperty("user")?.AsObject())
            .ToProperty(this, x => x.User);

        _tagsHelper = this.WhenAnyValue(x => x.Payload)
            .Select(payload => payload?.TryGetProperty("tags")?.AsObject())
            .ToProperty(this, x => x.Tags);

        _contextsHelper = this.WhenAnyValue(x => x.Payload)
            .Select(payload => payload?.TryGetProperty("contexts")?.AsFlatObject())
            .ToProperty(this, x => x.Contexts);

        _extraHelper = this.WhenAnyValue(x => x.Payload)
            .Select(payload => payload?.TryGetProperty("extra")?.AsFlatObject())
            .ToProperty(this, x => x.Extra);

        _sdkHelper = this.WhenAnyValue(x => x.Payload)
            .Select(payload => payload?.TryGetProperty("sdk")?.AsFlatObject())
            .ToProperty(this, x => x.Sdk);

        _attachmentsHelper = this.WhenAnyValue(x => x.Envelope)
            .Select(envelope => envelope?.TryGetAttachments())
            .ToProperty(this, x => x.Attachments);

        _hasStacktraceHelper = this.WhenAnyValue(x => x.Envelope)
            .Select(envelope =>
            {
                if (envelope?.TryGetStacktrace() is not null) return true;
                var payload = envelope?.TryGetEvent()?.TryParseAsJson();
                if (payload?.TryGetProperty("threads.values") is JsonArray { Count: > 0 }) return true;
                return payload?.TryGetProperty("exception.values") is JsonArray exceptions &&
                       exceptions.OfType<JsonObject>().Any(ex =>
                           ex.TryGetProperty("stacktrace.frames") is JsonArray { Count: > 0 });
            })
            .ToProperty(this, x => x.HasStacktrace);

        IsExecuting = true;

        Observable.FromAsync(() => reporter.LoadAsync())
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(
                onNext: value =>
                {
                    Envelope = value;
                    Error = null;
                    IsExecuting = false;
                },
                onError: ex =>
                {
                    Error = ex;
                    IsExecuting = false;
                });
    }
}
