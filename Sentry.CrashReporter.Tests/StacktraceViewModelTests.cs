namespace Sentry.CrashReporter.Tests;

public class StacktraceViewModelTests
{
    [Test]
    public void Defaults()
    {
        // Act
        var viewModel = new StacktraceViewModel();

        // Assert
        Assert.That(viewModel.Envelope, Is.Null);
        Assert.That(viewModel.Threads, Is.Null);
        Assert.That(viewModel.SelectedThreadIndex, Is.EqualTo(0));
        Assert.That(viewModel.SelectedThread, Is.Null);
        Assert.That(viewModel.Frames, Is.Null);
    }

    [Test]
    public async Task Init_WithStacktrace()
    {
        // Arrange
        await using var file = File.OpenRead("data/crashpad.envelope");
        var envelope = await Envelope.FromFileStreamAsync(file);

        // Act
        var viewModel = new StacktraceViewModel { Envelope = envelope };

        // Assert
        Assert.That(viewModel.Threads, Is.Not.Null);
        Assert.That(viewModel.Threads, Has.Count.EqualTo(14));
    }

    [Test]
    public async Task Init_ThreadIdIsHex()
    {
        // Arrange
        await using var file = File.OpenRead("data/crashpad.envelope");
        var envelope = await Envelope.FromFileStreamAsync(file);

        // Act
        var viewModel = new StacktraceViewModel { Envelope = envelope };

        // Assert
        Assert.That(viewModel.Threads![0].ThreadId, Is.EqualTo("0x2CFC"));
    }

    [Test]
    public async Task Init_CrashedThread()
    {
        // Arrange
        await using var file = File.OpenRead("data/crashpad.envelope");
        var envelope = await Envelope.FromFileStreamAsync(file);

        // Act
        var viewModel = new StacktraceViewModel { Envelope = envelope };

        // Assert
        Assert.That(viewModel.Threads![0].Crashed, Is.True);
        Assert.That(viewModel.Threads.Skip(1).All(t => !t.Crashed), Is.True);
    }

    [Test]
    public async Task Init_DefaultsToFirstCrashedThread()
    {
        // Arrange
        await using var file = File.OpenRead("data/crashpad.envelope");
        var envelope = await Envelope.FromFileStreamAsync(file);

        // Act
        var viewModel = new StacktraceViewModel { Envelope = envelope };

        // Assert
        Assert.That(viewModel.SelectedThreadIndex, Is.EqualTo(0));
        Assert.That(viewModel.SelectedThread, Is.Not.Null);
        Assert.That(viewModel.SelectedThread!.Crashed, Is.True);
    }

    [Test]
    public async Task Init_SelectedThreadHasFrames()
    {
        // Arrange
        await using var file = File.OpenRead("data/crashpad.envelope");
        var envelope = await Envelope.FromFileStreamAsync(file);

        // Act
        var viewModel = new StacktraceViewModel { Envelope = envelope };

        // Assert
        Assert.That(viewModel.Frames, Is.Not.Null);
        Assert.That(viewModel.Frames, Has.Count.EqualTo(48));
        Assert.That(viewModel.Frames![0].Address, Is.EqualTo("0x7FFC9BFA2766"));
        Assert.That(viewModel.Frames[0].Symbol, Is.EqualTo("memset"));
    }

    [Test]
    public async Task NextThread_AdvancesIndex()
    {
        // Arrange
        await using var file = File.OpenRead("data/crashpad.envelope");
        var envelope = await Envelope.FromFileStreamAsync(file);
        var viewModel = new StacktraceViewModel { Envelope = envelope };

        // Act
        viewModel.NextThread.Execute().Subscribe();

        // Assert
        Assert.That(viewModel.SelectedThreadIndex, Is.EqualTo(1));
        Assert.That(viewModel.SelectedThread!.ThreadId, Is.EqualTo(viewModel.Threads![1].ThreadId));
        Assert.That(viewModel.Frames, Is.EqualTo(viewModel.Threads[1].Frames));
    }

    [Test]
    public async Task PreviousThread_DecrementsIndex()
    {
        // Arrange
        await using var file = File.OpenRead("data/crashpad.envelope");
        var envelope = await Envelope.FromFileStreamAsync(file);
        var viewModel = new StacktraceViewModel { Envelope = envelope };
        viewModel.SelectedThreadIndex = 2;

        // Act
        viewModel.PreviousThread.Execute().Subscribe();

        // Assert
        Assert.That(viewModel.SelectedThreadIndex, Is.EqualTo(1));
    }

    [Test]
    public async Task NextThread_DisabledAtEnd()
    {
        // Arrange
        await using var file = File.OpenRead("data/crashpad.envelope");
        var envelope = await Envelope.FromFileStreamAsync(file);
        var viewModel = new StacktraceViewModel { Envelope = envelope };

        // Act
        viewModel.SelectedThreadIndex = viewModel.Threads!.Count - 1;

        // Assert
        bool canExecute = false;
        viewModel.NextThread.CanExecute.Subscribe(x => canExecute = x);
        Assert.That(canExecute, Is.False);
    }

    [Test]
    public async Task PreviousThread_DisabledAtStart()
    {
        // Arrange
        await using var file = File.OpenRead("data/crashpad.envelope");
        var envelope = await Envelope.FromFileStreamAsync(file);
        var viewModel = new StacktraceViewModel { Envelope = envelope };

        // Assert
        bool canExecute = false;
        viewModel.PreviousThread.CanExecute.Subscribe(x => canExecute = x);
        Assert.That(viewModel.SelectedThreadIndex, Is.EqualTo(0));
        Assert.That(canExecute, Is.False);
    }

    [Test]
    public async Task SelectThreadIndex_UpdatesFrames()
    {
        // Arrange
        await using var file = File.OpenRead("data/crashpad.envelope");
        var envelope = await Envelope.FromFileStreamAsync(file);
        var viewModel = new StacktraceViewModel { Envelope = envelope };

        // Act
        viewModel.SelectedThreadIndex = 1;

        // Assert
        Assert.That(viewModel.SelectedThread, Is.EqualTo(viewModel.Threads![1]));
        Assert.That(viewModel.Frames, Is.EqualTo(viewModel.Threads[1].Frames));
    }

    [Test]
    public async Task Init_WithoutStacktrace()
    {
        // Arrange
        await using var file = File.OpenRead("data/inproc.envelope");
        var envelope = await Envelope.FromFileStreamAsync(file);

        // Act
        var viewModel = new StacktraceViewModel { Envelope = envelope };

        // Assert
        Assert.That(viewModel.Threads, Is.Null);
        Assert.That(viewModel.SelectedThread, Is.Null);
        Assert.That(viewModel.Frames, Is.Null);
    }

    [Test]
    public async Task Init_WithEventStacktrace()
    {
        // Arrange
        await using var file = File.OpenRead("data/stacktrace.envelope");
        var envelope = await Envelope.FromFileStreamAsync(file);

        // Act
        var viewModel = new StacktraceViewModel { Envelope = envelope };

        // Assert
        Assert.That(viewModel.Threads, Has.Count.EqualTo(3));
    }

    [Test]
    public async Task Init_EventStacktrace_CrashedThread()
    {
        // Arrange
        await using var file = File.OpenRead("data/stacktrace.envelope");
        var envelope = await Envelope.FromFileStreamAsync(file);

        // Act
        var viewModel = new StacktraceViewModel { Envelope = envelope };

        // Assert — crashed thread gets frames from exception via thread_id
        Assert.That(viewModel.Threads![0].ThreadId, Is.EqualTo("1000"));
        Assert.That(viewModel.Threads[0].Crashed, Is.True);
        Assert.That(viewModel.Threads[0].Frames, Has.Count.EqualTo(7));
        Assert.That(viewModel.Threads[0].Frames[0].Address, Is.EqualTo("0x400500"));
        Assert.That(viewModel.Threads[0].Frames[0].Symbol, Is.EqualTo("crash_here"));
        Assert.That(viewModel.SelectedThreadIndex, Is.EqualTo(0));
    }

    [Test]
    public async Task Init_EventStacktrace_WorkerThread()
    {
        // Arrange
        await using var file = File.OpenRead("data/stacktrace.envelope");
        var envelope = await Envelope.FromFileStreamAsync(file);

        // Act
        var viewModel = new StacktraceViewModel { Envelope = envelope };

        // Assert
        Assert.That(viewModel.Threads![1].ThreadId, Is.EqualTo("1001"));
        Assert.That(viewModel.Threads[1].Crashed, Is.False);
        Assert.That(viewModel.Threads[1].Frames, Has.Count.EqualTo(4));
        Assert.That(viewModel.Threads[1].Frames[0].Symbol, Is.EqualTo("futex_wait"));
    }

    [Test]
    public void Init_ExceptionOnlyStacktrace()
    {
        // Arrange — no threads interface, only exception with stacktrace
        var envelope = new Envelope(new JsonObject(), [
            new EnvelopeItem(new JsonObject { ["type"] = "event" },
                Encoding.UTF8.GetBytes(new JsonObject
                {
                    ["exception"] = new JsonObject
                    {
                        ["values"] = new JsonArray(
                            new JsonObject
                            {
                                ["type"] = "SIGSEGV",
                                ["stacktrace"] = new JsonObject
                                {
                                    ["frames"] = new JsonArray(
                                        new JsonObject { ["instruction_addr"] = "0x1234", ["function"] = "segfault" })
                                }
                            })
                    }
                }.ToJsonString()))
        ]);

        // Act
        var viewModel = new StacktraceViewModel { Envelope = envelope };

        // Assert
        Assert.That(viewModel.Threads, Has.Count.EqualTo(1));
        Assert.That(viewModel.Threads![0].Crashed, Is.True);
        Assert.That(viewModel.Threads[0].Frames, Has.Count.EqualTo(1));
        Assert.That(viewModel.Threads[0].Frames[0].Symbol, Is.EqualTo("segfault"));
    }

    [Test]
    public async Task Init_NativeWithoutMinidump_CrashedThreadHasExceptionFrames()
    {
        // Arrange — crashed thread has no stacktrace, exception has no thread_id
        await using var file = File.OpenRead("data/native.envelope");
        var envelope = await Envelope.FromFileStreamAsync(file);

        // Act
        var viewModel = new StacktraceViewModel { Envelope = envelope };

        // Assert
        Assert.That(viewModel.Threads, Has.Count.EqualTo(15));
        Assert.That(viewModel.Threads![0].ThreadId, Is.EqualTo("18732"));
        Assert.That(viewModel.Threads[0].Crashed, Is.True);
        Assert.That(viewModel.Threads[0].Frames, Has.Count.EqualTo(28));
        Assert.That(viewModel.Threads[0].Frames[0].Symbol, Is.EqualTo("trigger_crash"));
    }

    [Test]
    public async Task Init_NativeThreadNames()
    {
        // Arrange
        await using var file = File.OpenRead("data/native.envelope");
        var envelope = await Envelope.FromFileStreamAsync(file);

        // Act
        var viewModel = new StacktraceViewModel { Envelope = envelope };

        // Assert — named threads
        Assert.That(viewModel.Threads![0].Name, Is.EqualTo("main"));
        Assert.That(viewModel.Threads[1].Name, Is.EqualTo("sentry-http"));
        Assert.That(viewModel.Threads[2].Name, Is.EqualTo("ThreadPoolWorker"));
        // Assert — unnamed threads
        Assert.That(viewModel.Threads[3].Name, Is.Null);
        Assert.That(viewModel.Threads[6].Name, Is.Null);
        Assert.That(viewModel.Threads[11].Name, Is.Null);
    }

    [Test]
    public void Init_EmptyThreadsNoExceptions()
    {
        // Arrange
        var envelope = new Envelope(new JsonObject(), [
            new EnvelopeItem(new JsonObject { ["type"] = "event" },
                Encoding.UTF8.GetBytes(new JsonObject
                {
                    ["threads"] = new JsonObject
                    {
                        ["values"] = new JsonArray()
                    }
                }.ToJsonString()))
        ]);

        // Act
        var viewModel = new StacktraceViewModel { Envelope = envelope };

        // Assert
        Assert.That(viewModel.Threads, Is.Null);
    }

}
