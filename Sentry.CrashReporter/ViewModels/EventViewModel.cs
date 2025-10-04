using System.Text.Json.Nodes;
using Sentry.CrashReporter.Extensions;
using Sentry.CrashReporter.Services;

namespace Sentry.CrashReporter.ViewModels;

public record Attachment(string Filename, byte[] Data);

public partial class EventViewModel : ReactiveObject
{
    [Reactive] private Envelope? _envelope;
    [ObservableAsProperty] private EnvelopeItem? _event;
    [ObservableAsProperty] private JsonObject? _payload;
    [ObservableAsProperty] private JsonObject? _tags;
    [ObservableAsProperty] private JsonObject? _contexts;
    [ObservableAsProperty] private JsonObject? _extra;
    [ObservableAsProperty] private JsonObject? _sdk;
    [ObservableAsProperty] private List<Attachment>? _attachments;

    public EventViewModel(IEnvelopeService? service = null)
    {
        service ??= Ioc.Default.GetRequiredService<IEnvelopeService>();

        _eventHelper = this.WhenAnyValue(x => x.Envelope)
            .Select(envelope => envelope?.TryGetEvent())
            .ToProperty(this, x => x.Event);

        _payloadHelper = this.WhenAnyValue(x => x.Event)
            .Select(ev => ev?.TryParseAsJson())
            .ToProperty(this, x => x.Payload);

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
            .Select(envelope => envelope?.Items
                .Where(s => s.TryGetType() == "attachment")
                .Select(s => new Attachment(s.Header.TryGetString("filename") ?? string.Empty, s.Payload))
                .Where(a => !string.IsNullOrEmpty(a.Filename))
                .ToList())
            .ToProperty(this, x => x.Attachments);

        Observable.FromAsync(() => service.LoadAsync().AsTask())
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(envelope => Envelope = envelope);
    }
}
