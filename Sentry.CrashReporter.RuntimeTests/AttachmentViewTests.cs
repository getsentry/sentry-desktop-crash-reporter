using CommunityToolkit.WinUI.UI.Controls;
using Windows.System;

namespace Sentry.CrashReporter.RuntimeTests;

[TestClass]
[RunsOnUIThread]
public class AttachmentViewTests : RuntimeTestBase
{
    [TestMethod]
    public async Task AttachmentView_CanBeCreated()
    {
        // Arrange
        _ = MockRuntime();

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
        _ = MockRuntime(envelope);

        // Act
        var view = new AttachmentView().Envelope(envelope);
        await LoadTestContent(view);

        var filenameA = view.FindFirstDescendant<TextBlock>(tb => tb.Text == "a.bin");
        var sizeA = view.FindFirstDescendant<TextBlock>(tb => tb.Text == "3 bytes");

        var filenameB = view.FindFirstDescendant<TextBlock>(tb => tb.Text == "b.bin");
        var sizeB = view.FindFirstDescendant<TextBlock>(tb => tb.Text == "6 bytes");

        // Assert
        Assert.IsNotNull(filenameA);
        Assert.IsNotNull(sizeA);

        Assert.IsNotNull(filenameB);
        Assert.IsNotNull(sizeB);
    }

    [TestMethod]
    public async Task AttachmentView_AddButton_InvokesPickerAndAddsItems()
    {
        // Arrange
        var envelope = new Envelope(new JsonObject(), Array.Empty<EnvelopeItem>());
        var mockRuntime = MockRuntime(envelope);
        mockRuntime.FilePicker.Setup(p => p.PickFilesAsync())
            .ReturnsAsync(new List<(string Name, byte[] Data)>
            {
                ("picked.txt", System.Text.Encoding.UTF8.GetBytes("hello")),
            });

        // Act
        var view = new AttachmentView().Envelope(envelope);
        await LoadTestContent(view);
        var addButton = view.FindFirstDescendant<Button>("addButton");
        Assert.IsNotNull(addButton);
        addButton.Command.Execute(null);
        await UnitTestsUIContentHelper.WaitForIdle();

        // Assert
        mockRuntime.FilePicker.Verify(p => p.PickFilesAsync(), Times.Once);
        Assert.AreEqual(1, envelope.Items.Count);
        Assert.AreEqual("attachment", envelope.Items[0].TryGetType());
        Assert.AreEqual("picked.txt", envelope.Items[0].TryGetHeader("filename"));

        var row = view.FindFirstDescendant<TextBlock>(tb => tb.Text == "picked.txt");
        Assert.IsNotNull(row);
    }

    [TestMethod]
    public async Task AttachmentView_Remove_DropsUnderlyingItem()
    {
        // Arrange
        var regularItem = new EnvelopeItem(
            new JsonObject { { "type", "attachment" }, { "filename", "note.txt" } },
            System.Text.Encoding.UTF8.GetBytes("hi"));
        var envelope = new Envelope(new JsonObject(), new List<EnvelopeItem> { regularItem });
        _ = MockRuntime(envelope);

        // Act
        var view = new AttachmentView().Envelope(envelope);
        await LoadTestContent(view);
        Assert.IsNotNull(view.FindFirstDescendant<TextBlock>(tb => tb.Text == "note.txt"));

        var attachment = view.ViewModel!.Attachments!.Single();
        view.ViewModel!.Remove(attachment);
        await UnitTestsUIContentHelper.WaitForIdle();

        // Assert
        Assert.AreEqual(0, envelope.Items.Count);
        Assert.IsNull(view.FindFirstDescendant<TextBlock>(tb => tb.Text == "note.txt"));
    }

    [TestMethod]
    public async Task AttachmentView_Grid_HasDeleteAndBackspaceAccelerators()
    {
        // Arrange
        var envelope = new Envelope(new JsonObject(), [
            new EnvelopeItem(new JsonObject { { "type", "attachment" }, { "filename", "note.txt" } }, [0x01])
        ]);
        _ = MockRuntime(envelope);

        // Act
        var view = new AttachmentView().Envelope(envelope);
        await LoadTestContent(view);
        var grid = view.FindFirstDescendant<DataGrid>();

        // Assert
        Assert.IsNotNull(grid);
        var keys = grid.KeyboardAccelerators.Select(a => a.Key).ToList();
        Assert.IsTrue(keys.Contains(VirtualKey.Delete));
        Assert.IsTrue(keys.Contains(VirtualKey.Back));
    }

    [TestMethod]
    public async Task AttachmentView_Remove_IgnoresMinidump()
    {
        // Arrange
        var minidumpItem = new EnvelopeItem(
            new JsonObject
            {
                { "type", "attachment" },
                { "filename", "crash.dmp" },
                { "attachment_type", "event.minidump" }
            },
            [0xFF]);
        var envelope = new Envelope(new JsonObject(), new List<EnvelopeItem> { minidumpItem });
        _ = MockRuntime(envelope);

        // Act
        var view = new AttachmentView().Envelope(envelope);
        await LoadTestContent(view);
        var attachment = view.ViewModel!.Attachments!.Single();
        Assert.IsTrue(attachment.IsMinidump);
        view.ViewModel!.Remove(attachment);
        await UnitTestsUIContentHelper.WaitForIdle();

        // Assert
        Assert.AreEqual(1, envelope.Items.Count);
        Assert.IsNotNull(view.FindFirstDescendant<TextBlock>(tb => tb.Text == "crash.dmp"));
    }
}
