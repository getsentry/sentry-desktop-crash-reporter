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
        Assert.That(viewModel.Threads, Has.Count.EqualTo(12));
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
        Assert.That(viewModel.Threads![0].ThreadId, Is.EqualTo("0x1E17A7"));
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
        Assert.That(viewModel.Frames, Has.Count.EqualTo(35));
        Assert.That(viewModel.Frames![0].Address, Is.EqualTo("0x10469E538"));
        Assert.That(viewModel.Frames[0].Symbol, Is.EqualTo("_ZL13trigger_crashv"));
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
}
