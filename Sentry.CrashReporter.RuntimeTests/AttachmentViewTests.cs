namespace Sentry.CrashReporter.RuntimeTests;

[TestClass]
[RunsOnUIThread]
public class AttachmentViewTests : RuntimeTestBase
{
    [TestMethod]
    public async Task AttachmentView_CanBeCreated()
    {
        // Arrange
        _ = MockCrashReporter();

        // Act
        var view = new AttachmentView();
        await LoadTestContent(view);

        // Assert
        Assert.IsNotNull(view);
    }

    [TestMethod]
    public async Task AttachmentView_HasAttachments()
    {
        // Arrange
        var envelope = new Envelope(new JsonObject(), [
            new EnvelopeItem(new JsonObject { { "type", "event" } }, []),
            new EnvelopeItem(new JsonObject { { "type", "attachment" }, { "filename", "a.bin" } }, [0x01, 0x02, 0x03]),
            new EnvelopeItem(new JsonObject { { "type", "attachment" }, { "filename", "b.bin" } }, [0x04, 0x05, 0x06, 0x7, 0x8, 0x9])
        ]);
        _ = MockCrashReporter(envelope);

        // Act
        var view = new AttachmentView();
        await LoadTestContent(view);

        var filenameA = view.FindFirstDescendant<TextBlock>(tb => tb.Text == "a.bin");
        var sizeA = view.FindFirstDescendant<TextBlock>(tb => tb.Text == "3 B");

        var filenameB = view.FindFirstDescendant<TextBlock>(tb => tb.Text == "b.bin");
        var sizeB = view.FindFirstDescendant<TextBlock>(tb => tb.Text == "6 B");
        
        // Assert
        Assert.IsNotNull(filenameA);
        Assert.IsNotNull(sizeA);

        Assert.IsNotNull(filenameB);
        Assert.IsNotNull(sizeB);
    }
}
