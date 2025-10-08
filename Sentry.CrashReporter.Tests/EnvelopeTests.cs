using System.Text;

namespace Sentry.CrashReporter.Tests;

public class EnvelopeTests
{
    [Test]
    public void ParseTwoItems()
    {
        using var file = File.OpenRead("Envelopes/two_items.envelope");
        var envelope = Envelope.DeserializeAsync(file).GetAwaiter().GetResult();

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
    public void ParseTwoEmptyAttachments()
    {
        using var file = File.OpenRead("Envelopes/two_empty_attachments.envelope");
        var envelope = Envelope.DeserializeAsync(file).GetAwaiter().GetResult();

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
    public void ParseImplicitLength()
    {
        using var file = File.OpenRead("Envelopes/implicit_length.envelope");
        var envelope = Envelope.DeserializeAsync(file).GetAwaiter().GetResult();

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
    public void ParseEmptyHeadersEof()
    {
        using var file = File.OpenRead("Envelopes/empty_headers_eof.envelope");
        var envelope = Envelope.DeserializeAsync(file).GetAwaiter().GetResult();

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
    public void ParseBinaryAttachment()
    {
        using var file = File.OpenRead("Envelopes/binary_attachment.envelope");
        var envelope = Envelope.DeserializeAsync(file).GetAwaiter().GetResult();

        envelope.TryGetDsn().Should().BeNull();
        envelope.TryGetEventId().Should().Be("9ec79c33ec9942ab8353589fcb2e04dc");
        envelope.Items.Should().HaveCount(1);

        var binary = envelope.Items[0];
        binary.TryGetType().Should().Be("attachment");
        binary.Payload.Length.Should().Be(3);
        binary.Payload.Should().BeEquivalentTo([0xFF, 0xFE, 0xFD]);
    }

    [Test]
    [TestCase("Envelopes/two_items.envelope")]
    [TestCase("Envelopes/two_empty_attachments.envelope")]
    [TestCase("Envelopes/implicit_length.envelope")]
    [TestCase("Envelopes/empty_headers_eof.envelope")]
    [TestCase("Envelopes/binary_attachment.envelope")]
    public void Serialize(string filePath)
    {
        using var file = File.OpenRead(filePath);
        var envelope = Envelope.DeserializeAsync(file).GetAwaiter().GetResult();

        using var stream = new MemoryStream();
        envelope.SerializeAsync(stream).GetAwaiter().GetResult();
        stream.FlushAsync().GetAwaiter().GetResult();
        stream.Seek(0, SeekOrigin.Begin);

        const byte newLine = (byte)'\n';
        var bytes = stream.ReadBytesAsync(CancellationToken.None).GetAwaiter().GetResult();
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
