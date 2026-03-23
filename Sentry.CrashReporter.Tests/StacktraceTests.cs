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
        stacktrace!.Threads.Should().HaveCount(12);
        stacktrace.Threads[0].ThreadId.Should().Be(1972135);
        stacktrace.Threads[0].Frames.Should().HaveCount(35);
        stacktrace.Threads[0].Frames[0].Symbol.Should().Be("_ZL13trigger_crashv");
        stacktrace.Threads[0].Frames[0].InstructionAddr.Should().Be(0x10469E538);
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
            .BeEquivalentTo([35, 1, 3, 3, 5, 9, 3, 10, 8, 8, 8, 8]);
    }

    [Test]
    public async Task ParseCrashpad_StacktraceStream_EmptySymbol()
    {
        // Arrange
        await using var file = File.OpenRead("data/crashpad.envelope");
        var envelope = await Envelope.DeserializeAsync(file);

        // Act
        var stacktrace = envelope.TryGetStacktrace();
        var frame = stacktrace!.Threads[1].Frames[0];

        // Assert
        frame.Symbol.Should().BeEmpty();
        frame.InstructionAddr.Should().Be(0x18CDDEB94);
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
        crashedThreadId.Should().Be(1972135u);
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
