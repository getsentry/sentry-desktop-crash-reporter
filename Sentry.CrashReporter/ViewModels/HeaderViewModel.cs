using Microsoft.UI.Dispatching;
using Sentry.CrashReporter.Extensions;
using Sentry.CrashReporter.Services;

namespace Sentry.CrashReporter.ViewModels;

public partial class HeaderViewModel : ObservableObject
{
    [ObservableProperty] private EnvelopeItem? _event;
    [ObservableProperty] private string? _eventId = string.Empty;
    [ObservableProperty] private DateTime? _timestamp;
    [ObservableProperty] private string? _platform = string.Empty;
    [ObservableProperty] private string? _level = string.Empty;
    [ObservableProperty] private string? _os = string.Empty;
    [ObservableProperty] private string? _osName = string.Empty;
    [ObservableProperty] private string? _osVersion = string.Empty;
    [ObservableProperty] private string? _release = string.Empty;
    [ObservableProperty] private string? _environment = string.Empty;
    [ObservableProperty] private string? _exceptionType = string.Empty;
    [ObservableProperty] private string? _exceptionValue = string.Empty;

    public HeaderViewModel(IEnvelopeService? service = null)
    {
        service ??= Ioc.Default.GetRequiredService<IEnvelopeService>();

        var dispatcherQueue = DispatcherQueue.GetForCurrentThread();
        Task.Run(async () =>
        {
            var envelope = await service.LoadAsync();
            dispatcherQueue.TryEnqueue(() => UpdateEvent(envelope));
        });
    }

    private void UpdateEvent(Envelope? envelope)
    {
        var ev = envelope?.TryGetEvent();
        var payload = ev?.TryParseAsJson();
        EventId = payload?.TryGetString("event_id")?.Replace("-", string.Empty)[..8];
        Timestamp = payload?.TryGetDateTime("timestamp");
        Platform = payload?.TryGetString("platform");
        Level = payload?.TryGetString("level");
        var os = payload?.TryGetProperty("contexts.os")?.AsObject();
        OsName = os?.TryGetString("name");
        OsVersion = os?.TryGetString("version");
        Os = $"{OsName} {OsVersion}";
        Release = payload?.TryGetString("release");
        Environment = payload?.TryGetString("environment");

        if (payload?.TryGetProperty("exception.values")?.AsArray().FirstOrDefault()?.AsObject() is { } inproc)
        {
            ExceptionType = inproc.TryGetString("type");
            ExceptionValue = inproc.TryGetString("value") ?? "Exception";
        }
        else if (envelope?.TryGetMinidump()?.Streams.Select(s => s.Data)
                     .OfType<Minidump.ExceptionStream>()
                     .FirstOrDefault() is { } minidump)
        {
            var code = minidump.ExceptionRec.Code.AsExceptionCode(OsName ?? string.Empty);
            ExceptionType = code?.Type;
            ExceptionValue = code?.Value ?? "Exception";
        }

        Event = ev;
    }
}
