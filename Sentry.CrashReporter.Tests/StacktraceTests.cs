namespace Sentry.CrashReporter.Tests;

public class StacktraceTests
{
    [Test]
    public async Task ParseCrashpad_TryGetStacktrace_ReturnsStacktrace()
    {
        // Arrange
        await using var file = File.OpenRead("data/crashpad.envelope");
        var envelope = await Envelope.DeserializeAsync(file);

        // Act
        var stacktrace = envelope.TryGetStacktrace();

        // Assert
        stacktrace.Should().NotBeNull();
        stacktrace!.Threads.Should().HaveCount(14);
        stacktrace.Threads[0].ThreadId.Should().Be(11516);
        stacktrace.Threads[0].Frames.Should().HaveCount(48);
        stacktrace.Threads[0].Frames[0].Symbol.Should().Be("memset");
        stacktrace.Threads[0].Frames[0].InstructionAddr.Should().Be(0x7FFC9BFA2766);
        stacktrace.Threads[0].Frames[1].Symbol.Should().Be("trigger_crash");
        stacktrace.Threads[0].Frames[1].InstructionAddr.Should().Be(0x7FF74BC381CF);
    }

    [Test]
    public async Task ParseCrashpad_StacktraceStream_Version()
    {
        // Arrange
        await using var file = File.OpenRead("data/crashpad.envelope");
        var envelope = await Envelope.DeserializeAsync(file);
        var minidump = envelope.TryGetMinidump();

        // Act
        var stream = minidump!.Streams.Select(s => s.Data)
            .OfType<Minidump.StacktraceStream>().FirstOrDefault();

        // Assert
        stream.Should().NotBeNull();
        stream!.Version.Should().Be(1);
    }

    [Test]
    public async Task ParseCrashpad_StacktraceStream_ThreadFrameCounts()
    {
        // Arrange
        await using var file = File.OpenRead("data/crashpad.envelope");
        var envelope = await Envelope.DeserializeAsync(file);

        // Act
        var stacktrace = envelope.TryGetStacktrace();

        // Assert
        stacktrace!.Threads.Select(t => t.Frames.Count).Should()
            .BeEquivalentTo([48, 4, 4, 4, 4, 4, 4, 4, 6, 7, 7, 8, 8, 8]);
    }

    [Test]
    public async Task ParseCrashpad_StacktraceStream_SecondThreadFirstFrame()
    {
        // Arrange
        await using var file = File.OpenRead("data/crashpad.envelope");
        var envelope = await Envelope.DeserializeAsync(file);

        // Act
        var stacktrace = envelope.TryGetStacktrace();
        var frame = stacktrace!.Threads[1].Frames[0];

        // Assert
        frame.Symbol.Should().Be("ZwWaitForWorkViaWorkerFactory");
        frame.InstructionAddr.Should().Be(0x7FFCC5BC5744);
    }

    [Test]
    public async Task ParseCrashpad_TryGetCrashedThreadId()
    {
        // Arrange
        await using var file = File.OpenRead("data/crashpad.envelope");
        var envelope = await Envelope.DeserializeAsync(file);

        // Act
        var crashedThreadId = envelope.TryGetCrashedThreadId();

        // Assert
        crashedThreadId.Should().NotBeNull();
        crashedThreadId.Should().Be(11516u);
    }

    [Test]
    public async Task ParseInproc_TryGetCrashedThreadId_ReturnsNull()
    {
        // Arrange
        await using var file = File.OpenRead("data/inproc.envelope");
        var envelope = await Envelope.DeserializeAsync(file);

        // Act
        var crashedThreadId = envelope.TryGetCrashedThreadId();

        // Assert
        crashedThreadId.Should().BeNull();
    }

    [Test]
    public async Task ParseInproc_TryGetStacktrace_ReturnsNull()
    {
        // Arrange
        await using var file = File.OpenRead("data/inproc.envelope");
        var envelope = await Envelope.DeserializeAsync(file);

        // Act
        var stacktrace = envelope.TryGetStacktrace();

        // Assert
        stacktrace.Should().BeNull();
    }

    [Test]
    public async Task ParseEmpty_TryGetStacktrace_ReturnsNull()
    {
        // Arrange
        await using var file = File.OpenRead("data/empty_headers_eof.envelope");
        var envelope = await Envelope.DeserializeAsync(file);

        // Act
        var stacktrace = envelope.TryGetStacktrace();

        // Assert
        stacktrace.Should().BeNull();
    }
}
