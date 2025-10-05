using System.Text.Json.Nodes;
using Sentry.CrashReporter.Extensions;
using Sentry.CrashReporter.Services;

namespace Sentry.CrashReporter.ViewModels;

public partial class HeaderViewModel : ReactiveObject
{
    [Reactive] private Envelope? _envelope;
    [ObservableAsProperty] private EnvelopeItem? _event;
    [ObservableAsProperty] private JsonObject? _payload;
    [ObservableAsProperty] private string? _eventId = string.Empty;
    [ObservableAsProperty] private DateTime? _timestamp;
    [ObservableAsProperty] private string? _platform = string.Empty;
    [ObservableAsProperty] private string? _level = string.Empty;
    [ObservableAsProperty] private JsonObject? _os;
    [ObservableAsProperty] private string? _osName = string.Empty;
    [ObservableAsProperty] private string? _osVersion = string.Empty;
    [ObservableAsProperty] private string? _osPretty = string.Empty;
    [ObservableAsProperty] private string? _release = string.Empty;
    [ObservableAsProperty] private string? _environment = string.Empty;
    [ObservableAsProperty] private EnvelopeException? _exception;

    public HeaderViewModel(ICrashReporter? reporter = null)
    {
        reporter ??= Ioc.Default.GetRequiredService<ICrashReporter>();

        _eventHelper = this.WhenAnyValue(x => x.Envelope, e => e?.TryGetEvent())
            .ToProperty(this, x => x.Event);

        _payloadHelper = this.WhenAnyValue(x => x.Event, e => e?.TryParseAsJson())
            .ToProperty(this, x => x.Payload);

        _eventIdHelper = this.WhenAnyValue(x => x.Payload, p => p?.TryGetString("event_id"))
            .Select(eventId => eventId?.Replace("-", string.Empty)[..8])
            .ToProperty(this, x => x.EventId);

        _timestampHelper = this.WhenAnyValue(x => x.Payload, p => p?.TryGetDateTime("timestamp"))
            .ToProperty(this, x => x.Timestamp);

        _platformHelper = this.WhenAnyValue(x => x.Payload, p => p?.TryGetString("platform"))
            .ToProperty(this, x => x.Platform);

        _levelHelper = this.WhenAnyValue(x => x.Payload, p => p?.TryGetString("level"))
            .ToProperty(this, x => x.Level);

        _osHelper = this.WhenAnyValue(x => x.Payload, p => p?.TryGetProperty("contexts.os")?.AsObject())
            .ToProperty(this, x => x.Os);

        _osNameHelper = this.WhenAnyValue(x => x.Os, os => os?.TryGetString("name"))
            .ToProperty(this, x => x.OsName);

        _osVersionHelper = this.WhenAnyValue(x => x.Os, os => os?.TryGetString("version"))
            .ToProperty(this, x => x.OsVersion);

        _osPrettyHelper = this.WhenAnyValue(x => x.OsName, x => x.OsVersion, (name, version) => $"{name} {version}")
            .ToProperty(this, x => x.OsPretty);

        _releaseHelper = this.WhenAnyValue(x => x.Payload, p => p?.TryGetString("release"))
            .ToProperty(this, x => x.Release);

        _environmentHelper = this.WhenAnyValue(x => x.Payload, p => p?.TryGetString("environment"))
            .ToProperty(this, x => x.Environment);

        _exceptionHelper = this.WhenAnyValue(x => x.Envelope, e => e?.TryGetException())
            .ToProperty(this, x => x.Exception);

        Observable.FromAsync(() => reporter.LoadAsync().AsTask())
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(value => Envelope = value);
    }
}
