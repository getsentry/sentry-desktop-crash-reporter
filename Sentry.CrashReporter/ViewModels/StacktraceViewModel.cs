using System.Reactive;

namespace Sentry.CrashReporter.ViewModels;

public record StacktraceFrameItem(string Address, string Symbol);
public record StacktraceThreadItem(string ThreadId, bool Crashed, List<StacktraceFrameItem> Frames);

public partial class StacktraceViewModel : ReactiveObject
{
    [Reactive] private Envelope? _envelope;
    [Reactive] private int _selectedThreadIndex;
    [ObservableAsProperty] private List<StacktraceThreadItem>? _threads;
    [ObservableAsProperty] private StacktraceThreadItem? _selectedThread;
    [ObservableAsProperty] private List<StacktraceFrameItem>? _frames;

    public ReactiveCommand<Unit, Unit> PreviousThread { get; }
    public ReactiveCommand<Unit, Unit> NextThread { get; }

    public StacktraceViewModel()
    {
        _threadsHelper = this.WhenAnyValue(x => x.Envelope)
            .Select(envelope =>
            {
                var stacktrace = envelope?.TryGetStacktrace();
                if (stacktrace is null) return null;

                var crashedThreadId = envelope?.TryGetCrashedThreadId();
                return stacktrace.Threads
                    .Select(t => new StacktraceThreadItem(
                        $"0x{t.ThreadId:X}",
                        t.ThreadId == crashedThreadId,
                        t.Frames.Select(f => new StacktraceFrameItem(
                            $"0x{f.InstructionAddr:X}",
                            f.Symbol)).ToList()))
                    .ToList();
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

        var canGoPrevious = this.WhenAnyValue(x => x.SelectedThreadIndex)
            .Select(i => i > 0);
        PreviousThread = ReactiveCommand.Create(() => { SelectedThreadIndex--; }, canGoPrevious);

        var canGoNext = this.WhenAnyValue(x => x.Threads, x => x.SelectedThreadIndex)
            .Select(((List<StacktraceThreadItem>? threads, int index) t) =>
                t.threads is not null && t.index < t.threads.Count - 1);
        NextThread = ReactiveCommand.Create(() => { SelectedThreadIndex++; }, canGoNext);
    }
}
