namespace Sentry.CrashReporter.Tests;

public class EnvelopeTests
{
    [Test]
    public async Task ParseTwoItems()
    {
        await using var file = File.OpenRead("data/two_items.envelope");
        var envelope = await Envelope.DeserializeAsync(file);

        envelope.TryGetDsn().Should().Be("https://e12d836b15bb49d7bbf99e64295d995b:@sentry.io/42");
        envelope.TryGetEventId().Should().Be("9ec79c33ec9942ab8353589fcb2e04dc");
        envelope.Items.Should().HaveCount(2);

        var attachment = envelope.Items[0];
        attachment.TryGetType().Should().Be("attachment");
        attachment.TryGetHeader("content_type").Should().Be("text/plain");
        attachment.TryGetHeader("filename").Should().Be("hello.txt");
        attachment.Payload.Length.Should().Be(10);
        attachment.TryParseAsJson()?.Should().BeNull();

        var message = envelope.Items[1];
        message.TryGetType().Should().Be("event");
        message.TryGetHeader("content_type").Should().Be("application/json");
        message.TryGetHeader("filename").Should().Be("application.log");
        message.Payload.Length.Should().Be(41);
        message.TryParseAsJson()?.ToJsonString().Should().Be("""{"message":"hello world","level":"error"}""");
    }

    [Test]
    public async Task ParseTwoEmptyAttachments()
    {
        await using var file = File.OpenRead("data/two_empty_attachments.envelope");
        var envelope = await Envelope.DeserializeAsync(file);

        envelope.TryGetDsn().Should().BeNull();
        envelope.TryGetEventId().Should().Be("9ec79c33ec9942ab8353589fcb2e04dc");
        envelope.Items.Should().HaveCount(2);

        foreach (var attachment in envelope.Items)
        {
            attachment.TryGetType().Should().Be("attachment");
            attachment.TryGetHeader("content_type").Should().BeNull();
            attachment.TryGetHeader("filename").Should().BeNull();
            attachment.Payload.Length.Should().Be(0);
            attachment.TryParseAsJson()?.Should().BeNull();
        }
    }

    [Test]
    public async Task ParseImplicitLength()
    {
        await using var file = File.OpenRead("data/implicit_length.envelope");
        var envelope = await Envelope.DeserializeAsync(file);

        envelope.TryGetDsn().Should().BeNull();
        envelope.TryGetEventId().Should().Be("9ec79c33ec9942ab8353589fcb2e04dc");
        envelope.Items.Should().HaveCount(1);

        var attachment = envelope.Items[0];
        attachment.TryGetType().Should().Be("attachment");
        attachment.TryGetHeader("content_type").Should().BeNull();
        attachment.TryGetHeader("filename").Should().BeNull();
        attachment.Payload.Length.Should().Be(10);
        attachment.TryParseAsJson()?.Should().BeNull();
    }

    [Test]
    public async Task ParseEmptyHeadersEof()
    {
        await using var file = File.OpenRead("data/empty_headers_eof.envelope");
        var envelope = await Envelope.DeserializeAsync(file);

        envelope.TryGetDsn().Should().BeNull();
        envelope.TryGetEventId().Should().BeNull();
        envelope.Items.Should().HaveCount(1);

        var session = envelope.Items[0];
        session.TryGetType().Should().Be("session");
        session.Payload.Length.Should().Be(75);
        session.TryParseAsJson()?.ToJsonString().Should()
            .Be("""{"started":"2020-02-07T14:16:00Z","attrs":{"release":"sentry-test@1.0.0"}}""");
    }

    [Test]
    public async Task ParseBinaryAttachment()
    {
        await using var file = File.OpenRead("data/binary_attachment.envelope");
        var envelope = await Envelope.DeserializeAsync(file);

        envelope.TryGetDsn().Should().BeNull();
        envelope.TryGetEventId().Should().Be("9ec79c33ec9942ab8353589fcb2e04dc");
        envelope.Items.Should().HaveCount(1);

        var binary = envelope.Items[0];
        binary.TryGetType().Should().Be("attachment");
        binary.Payload.Length.Should().Be(3);
        binary.Payload.Should().BeEquivalentTo([0xFF, 0xFE, 0xFD]);
    }

    [Test]
    public async Task ParseBreakpad_TryGetMinidump_ReturnsMinidump()
    {
        // Arrange
        await using var file = File.OpenRead("data/breakpad.envelope");
        var envelope = await Envelope.DeserializeAsync(file);

        // Act
        var minidump = envelope.TryGetMinidump();

        // Assert
        minidump.Should().NotBeNull();
        minidump!.Streams.Should().NotBeEmpty();
        minidump.Streams.Select(s => s.Data).OfType<Minidump.ExceptionStream>().Should().HaveCount(1);
    }

    [Test]
    public async Task ParseInproc_TryGetMinidump_ReturnsNull()
    {
        // Arrange
        await using var file = File.OpenRead("data/inproc.envelope");
        var envelope = await Envelope.DeserializeAsync(file);

        // Act
        var minidump = envelope.TryGetMinidump();

        // Assert
        minidump.Should().BeNull();
    }

    [Test]
    public void Format_WithJsonPayload_IsPrettyPrinted()
    {
        // Arrange
        var envelope = Envelope.FromJson(
            new JsonObject { ["event_id"] = "dc6e5422-dd17-4467-921c-cf11121a373c" },
            [
                (
                    new JsonObject { ["type"] = "event" },
                    new JsonObject { ["message"] = "test" }
                )
            ]);

        // Act
        var formatted = envelope.Format();

        // Assert
        formatted.Header.Should().Be("""
                                     {
                                       "event_id": "dc6e5422-dd17-4467-921c-cf11121a373c"
                                     }
                                     """);
        formatted.Items.Single().Payload.Should().Be("""
                                                     {
                                                       "message": "test"
                                                     }
                                                     """);
    }

    [Test]
    public void Format_WithBinaryPayload_IsHexFormatted()
    {
        // Arrange
        var header = new JsonObject { ["event_id"] = "dc6e5422-dd17-4467-921c-cf11121a373c" };
        var itemHeader = new JsonObject { ["type"] = "attachment" };
        var payload = Enumerable.Range(0, 100).Select(i => (byte)i).ToArray();
        var envelope = new Envelope(header, [new EnvelopeItem(itemHeader, payload)]);

        // Act
        var formatted = envelope.Format();

        // Assert
        formatted.Header.Should().Be("""
                                     {
                                       "event_id": "dc6e5422-dd17-4467-921c-cf11121a373c"
                                     }
                                     """);
        formatted.Items.Single().Payload.Should().Be("00 01 02 03 04 05 06 07 08 09 0A 0B 0C 0D 0E 0F 10 11 12 13 14 15 16 17 18 19 1A 1B 1C 1D 1E 1F...");
    }

    [Test]
    [TestCase("data/two_items.envelope")]
    [TestCase("data/two_empty_attachments.envelope")]
    [TestCase("data/implicit_length.envelope")]
    [TestCase("data/empty_headers_eof.envelope")]
    [TestCase("data/binary_attachment.envelope")]
    [TestCase("data/breakpad.envelope")]
    [TestCase("data/inproc.envelope")]
    public async Task Serialize(string filePath)
    {
        await using var file = File.OpenRead(filePath);
        var envelope = await Envelope.DeserializeAsync(file);

        using var stream = new MemoryStream();
        await envelope.SerializeAsync(stream);
        await stream.FlushAsync();
        stream.Seek(0, SeekOrigin.Begin);

        const byte newLine = (byte)'\n';
        var bytes = await stream.ReadBytesAsync(CancellationToken.None);
        bytes.Should().StartWith(Encoding.UTF8.GetBytes(envelope.Header.ToJsonString()).Append(newLine));
        bytes.Should().EndWith(
            envelope.Items
                .SelectMany(i =>
                    Encoding.UTF8.GetBytes(i.Header.ToJsonString())
                        .Append(newLine)
                        .Concat(i.Payload)
                        .Append(newLine)
                )
                .ToArray()
        );
    }
}
