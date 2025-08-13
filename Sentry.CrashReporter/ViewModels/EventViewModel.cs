using System.Text.Json.Nodes;
using Microsoft.UI.Dispatching;
using Sentry.CrashReporter.Extensions;
using Sentry.CrashReporter.Services;

namespace Sentry.CrashReporter.ViewModels;

public record Attachment(string Filename, byte[] Data);

public partial class EventViewModel : ObservableObject
{
    [ObservableProperty] private JsonObject? _tags;
    [ObservableProperty] private JsonObject? _contexts;
    [ObservableProperty] private JsonObject? _extra;
    [ObservableProperty] private JsonObject? _sdk;
    [ObservableProperty] private List<Attachment>? _attachments;

    public EventViewModel(IEnvelopeService? service = null)
    {
        service ??= Ioc.Default.GetRequiredService<IEnvelopeService>();

        var dispatcherQueue = DispatcherQueue.GetForCurrentThread();
        Task.Run(async () =>
        {
            var envelope = await service.LoadAsync();
            dispatcherQueue.TryEnqueue(() =>
            {
                var payload = envelope?.TryGetEvent()?.TryParseAsJson();
                Tags = payload?.TryGetPropertyValue("tags", out var tags) == true
                    ? tags?.AsObject()
                    : null;
                Contexts = payload?.TryGetPropertyValue("contexts", out var contexts) == true
                    ? contexts?.AsFlatObject()
                    : null;
                Extra = payload?.TryGetPropertyValue("extra", out var extra) == true
                    ? extra?.AsFlatObject()
                    : null;
                Sdk = payload?.TryGetPropertyValue("sdk", out var sdk) == true
                    ? sdk?.AsFlatObject()
                    : null;

                Attachments = envelope?.Items
                    .Where(s => s.TryGetType() == "attachment")
                    .Select(s => new Attachment(s.Header.TryGetString("filename") ?? string.Empty, s.Payload))
                    .Where(a => !string.IsNullOrEmpty(a.Filename))
                    .ToList();
            });
        });
    }
}
