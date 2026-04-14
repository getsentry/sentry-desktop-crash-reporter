using System.Reactive;
using System.Text.Json.Nodes;
using Sentry.CrashReporter.Extensions;

namespace Sentry.CrashReporter.ViewModels;

public class StacktraceFrameItem(string Address, string Symbol)
{
    public string Address { get; } = Address;
    public string Symbol { get; } = Symbol;
    public override string ToString() => $"{Address}  {Symbol}";
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

    public ReactiveCommand<Unit, Unit> PreviousThread { get; }
    public ReactiveCommand<Unit, Unit> NextThread { get; }

    public StacktraceViewModel()
    {
        _threadsHelper = this.WhenAnyValue(x => x.Envelope)
            .Select(envelope =>
            {
                var stacktrace = envelope?.TryGetStacktrace();
                if (stacktrace is not null)
                {
                    var crashedThreadId = envelope?.TryGetCrashedThreadId();
                    return stacktrace.Threads
                        .Select(t => new StacktraceThreadItem(
                            $"0x{t.ThreadId:X}",
                            null,
                            t.ThreadId == crashedThreadId,
                            t.Frames.Select(f => new StacktraceFrameItem(
                                $"0x{f.InstructionAddr:X}",
                                f.Symbol)).ToList()))
                        .ToList();
                }

                return TryGetThreadsFromEvent(envelope);
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

    private static List<StacktraceThreadItem>? TryGetThreadsFromEvent(Envelope? envelope)
    {
        var payload = envelope?.TryGetEvent()?.TryParseAsJson();
        if (payload is null) return null;

        var exceptions = payload.TryGetProperty("exception.values") as JsonArray;
        var threads = payload.TryGetProperty("threads.values") as JsonArray;

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
                    return new StacktraceThreadItem(id, name, crashed, ParseFrames(frames));
                })
                .Where(t => t.Frames.Count > 0)
                .ToList();
            return threadItems.Count > 0 ? threadItems : null;
        }

        // No threads interface — create entries from exceptions with stacktraces
        var result = exceptions!.OfType<JsonObject>()
            .Select(ex => new StacktraceThreadItem(
                "", null, true, ParseFrames(ex.TryGetProperty("stacktrace.frames"))))
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

    private static List<StacktraceFrameItem> ParseFrames(JsonNode? framesNode)
    {
        if (framesNode is not JsonArray frames) return [];
        return frames.OfType<JsonObject>()
            .Reverse()
            .Select(f => new StacktraceFrameItem(
                f.TryGetString("instruction_addr") ?? "",
                f.TryGetString("function") ?? ""))
            .ToList();
    }
}
