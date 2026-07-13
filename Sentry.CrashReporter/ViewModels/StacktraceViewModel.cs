using System.Reactive;
using System.Globalization;
using System.Text.Json.Nodes;
using Sentry.CrashReporter.Extensions;
using Sentry.CrashReporter.Services;

namespace Sentry.CrashReporter.ViewModels;

public class StacktraceFrameItem(string Image, string Address, string Symbol)
{
    public string Image { get; } = Image;
    public string Address { get; } = Address;
    public string Symbol { get; } = Symbol;
    public override string ToString() => $"{Image}  {Address}  {Symbol}";
}
public record StacktraceThreadItem(string ThreadId, string? Name, bool Crashed, List<StacktraceFrameItem> Frames);

public partial class StacktraceViewModel : ReactiveObject
{
    [Reactive] private Envelope? _envelope;
    [Reactive] private int _selectedThreadIndex;
    [ObservableAsProperty] private List<StacktraceThreadItem>? _threads;
    [ObservableAsProperty] private StacktraceThreadItem? _selectedThread;
    [ObservableAsProperty] private List<StacktraceFrameItem>? _frames;
    [ObservableAsProperty] private bool _hasMultipleThreads;
    private readonly ISymbolDemangler _demangler;

    public ReactiveCommand<Unit, Unit> PreviousThread { get; }
    public ReactiveCommand<Unit, Unit> NextThread { get; }

    public StacktraceViewModel(ISymbolDemangler? demangler = null)
    {
        _demangler = demangler ?? App.Services.GetRequiredService<ISymbolDemangler>();

        _threadsHelper = this.WhenAnyValue(x => x.Envelope)
            .Select(envelope =>
            {
                var stacktrace = envelope?.TryGetStacktrace();
                if (stacktrace is not null)
                {
                    var images = ParseImages(envelope);
                    var crashedThreadId = envelope?.TryGetCrashedThreadId();
                    return stacktrace.Threads
                        .Select(t => new StacktraceThreadItem(
                            $"0x{t.ThreadId:X}",
                            null,
                            t.ThreadId == crashedThreadId,
                            t.Frames.Select(f => new StacktraceFrameItem(
                                FindImageName(images, f.InstructionAddr),
                                $"0x{f.InstructionAddr:X}",
                                _demangler.Demangle(f.Symbol))).ToList()))
                        .ToList();
                }

                return TryGetThreadsFromEvent(envelope, _demangler);
            })
            .ToProperty(this, x => x.Threads);

        this.WhenAnyValue(x => x.Threads)
            .Subscribe(threads =>
            {
                if (threads is null) return;
                var crashedIndex = threads.FindIndex(t => t.Crashed);
                SelectedThreadIndex = crashedIndex >= 0 ? crashedIndex : 0;
            });

        _selectedThreadHelper = this.WhenAnyValue(x => x.Threads, x => x.SelectedThreadIndex)
            .Select(((List<StacktraceThreadItem>? threads, int index) t) =>
                t.threads is { Count: > 0 } && t.index >= 0 && t.index < t.threads.Count
                    ? t.threads[t.index]
                    : null)
            .ToProperty(this, x => x.SelectedThread);

        _framesHelper = this.WhenAnyValue(x => x.SelectedThread)
            .Select(thread => thread?.Frames)
            .ToProperty(this, x => x.Frames);

        _hasMultipleThreadsHelper = this.WhenAnyValue(x => x.Threads)
            .Select(threads => threads is { Count: > 1 })
            .ToProperty(this, x => x.HasMultipleThreads);

        var canGoPrevious = this.WhenAnyValue(x => x.SelectedThreadIndex)
            .Select(i => i > 0);
        PreviousThread = ReactiveCommand.Create(() => { SelectedThreadIndex--; }, canGoPrevious);

        var canGoNext = this.WhenAnyValue(x => x.Threads, x => x.SelectedThreadIndex)
            .Select(((List<StacktraceThreadItem>? threads, int index) t) =>
                t.threads is not null && t.index < t.threads.Count - 1);
        NextThread = ReactiveCommand.Create(() => { SelectedThreadIndex++; }, canGoNext);
    }

    private static List<StacktraceThreadItem>? TryGetThreadsFromEvent(
        Envelope? envelope,
        ISymbolDemangler demangler)
    {
        var payload = envelope?.TryGetEvent()?.TryParseAsJson();
        if (payload is null) return null;

        var exceptions = payload.TryGetProperty("exception.values") as JsonArray;
        var threads = payload.TryGetProperty("threads.values") as JsonArray;
        var images = ParseImages(envelope);

        if (threads is not { Count: > 0 } && exceptions is null) return null;

        // Index exception stacktraces by thread_id
        var exceptionFrames = new Dictionary<string, JsonNode>();
        JsonNode? unmatchedExceptionFrames = null;
        if (exceptions is not null)
        {
            foreach (var ex in exceptions.OfType<JsonObject>())
            {
                var threadId = NodeToString(ex.TryGetProperty("thread_id"));
                if (ex.TryGetProperty("stacktrace.frames") is { } frames)
                {
                    if (threadId is not null)
                        exceptionFrames[threadId] = frames;
                    else
                        unmatchedExceptionFrames ??= frames;
                }
            }
        }

        if (threads is { Count: > 0 })
        {
            var threadItems = threads.OfType<JsonObject>()
                .Select(t =>
                {
                    var id = NodeToString(t.TryGetProperty("id")) ?? "";
                    var name = t.TryGetString("name");
                    var crashed = NodeToBool(t.TryGetProperty("crashed"));
                    var frames = t.TryGetProperty("stacktrace.frames");
                    if (frames is null && !exceptionFrames.TryGetValue(id, out frames) && crashed)
                        frames = unmatchedExceptionFrames;
                    return new StacktraceThreadItem(id, name, crashed, ParseFrames(frames, demangler, images));
                })
                .Where(t => t.Frames.Count > 0)
                .ToList();
            return threadItems.Count > 0 ? threadItems : null;
        }

        // No threads interface — create entries from exceptions with stacktraces
        var result = exceptions!.OfType<JsonObject>()
            .Select(ex => new StacktraceThreadItem(
                "", null, true, ParseFrames(ex.TryGetProperty("stacktrace.frames"), demangler, images)))
            .Where(t => t.Frames.Count > 0)
            .ToList();
        return result.Count > 0 ? result : null;
    }

    private static bool NodeToBool(JsonNode? node)
    {
        if (node is not JsonValue v) return false;
        if (v.TryGetValue(out bool b)) return b;
        if (v.TryGetValue(out long l)) return l != 0;
        return v.TryGetValue(out string? s) && bool.TryParse(s, out var parsed) && parsed;
    }

    private static string? NodeToString(JsonNode? node) => node switch
    {
        JsonValue v when v.TryGetValue(out string? s) => s,
        JsonValue v when v.TryGetValue(out long l) => l.ToString(),
        _ => null
    };

    private static List<StacktraceFrameItem> ParseFrames(
        JsonNode? framesNode,
        ISymbolDemangler demangler,
        List<StacktraceImageItem> images)
    {
        if (framesNode is not JsonArray frames) return [];
        return frames.OfType<JsonObject>()
            .Reverse()
            .Select(f => new StacktraceFrameItem(
                GetFrameImageName(f, images),
                f.TryGetString("instruction_addr") ?? "",
                demangler.Demangle(f.TryGetString("function") ?? "")))
            .ToList();
    }

    private static string GetFrameImageName(JsonObject frame, List<StacktraceImageItem> images)
    {
        var image = frame.TryGetString("package")
                    ?? frame.TryGetString("module")
                    ?? frame.TryGetString("filename")
                    ?? frame.TryGetString("abs_path");
        if (!string.IsNullOrEmpty(image))
        {
            return GetFileName(image);
        }

        return TryParseAddress(frame.TryGetString("instruction_addr"), out var address)
            ? FindImageName(images, address)
            : "";
    }

    private static List<StacktraceImageItem> ParseImages(JsonObject? payload)
    {
        if (payload?.TryGetProperty("debug_meta.images") is not JsonArray images)
        {
            return [];
        }

        return images.OfType<JsonObject>()
            .Select(image =>
            {
                var file = image.TryGetString("code_file")
                           ?? image.TryGetString("debug_file")
                           ?? image.TryGetString("name");

                if (string.IsNullOrEmpty(file)
                    || !TryParseAddress(image.TryGetString("image_addr"), out var address)
                    || !TryGetUlong(image.TryGetProperty("image_size"), out var size)
                    || size == 0)
                {
                    return null;
                }

                return new StacktraceImageItem(address, size, GetFileName(file));
            })
            .OfType<StacktraceImageItem>()
            .OrderBy(image => image.Address)
            .ToList();
    }

    private static List<StacktraceImageItem> ParseImages(Envelope? envelope)
    {
        return ParseImages(envelope?.TryGetEvent()?.TryParseAsJson())
            .Concat(ParseImages(envelope?.TryGetModuleList()))
            .OrderBy(image => image.Address)
            .ToList();
    }

    private static List<StacktraceImageItem> ParseImages(Minidump.ModuleList? moduleList)
    {
        if (moduleList is null) return [];

        var images = new List<StacktraceImageItem>();
        foreach (var module in moduleList.Modules.Where(module => module.SizeOfImage > 0))
        {
            try
            {
                if (!string.IsNullOrEmpty(module.Name))
                {
                    images.Add(new StacktraceImageItem(
                        module.BaseOfImage,
                        module.SizeOfImage,
                        GetFileName(module.Name)));
                }
            }
            catch
            {
            }
        }

        return images.OrderBy(image => image.Address).ToList();
    }

    private static string FindImageName(List<StacktraceImageItem> images, ulong address)
    {
        var image = images.LastOrDefault(image => image.Contains(address));
        return image?.Name ?? "";
    }

    private static string GetFileName(string path)
    {
        var normalized = path.TrimEnd('/', '\\');
        var separator = normalized.LastIndexOfAny(['/', '\\']);
        return separator >= 0 ? normalized[(separator + 1)..] : normalized;
    }

    private static bool TryParseAddress(string? value, out ulong address)
    {
        address = 0;
        if (string.IsNullOrWhiteSpace(value)) return false;

        value = value.Trim();
        if (value.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
        {
            return ulong.TryParse(value[2..], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out address);
        }

        return ulong.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out address);
    }

    private static bool TryGetUlong(JsonNode? node, out ulong value)
    {
        value = 0;
        if (node is not JsonValue jsonValue) return false;

        if (jsonValue.TryGetValue(out ulong ulongValue))
        {
            value = ulongValue;
            return true;
        }

        if (jsonValue.TryGetValue(out long longValue) && longValue >= 0)
        {
            value = (ulong)longValue;
            return true;
        }

        if (jsonValue.TryGetValue(out string? stringValue))
        {
            return TryParseAddress(stringValue, out value);
        }

        return false;
    }

    private sealed record StacktraceImageItem(ulong Address, ulong Size, string Name)
    {
        public bool Contains(ulong address) => address >= Address && address - Address < Size;
    }
}
