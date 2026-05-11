using System.Text.Json.Nodes;
using Sentry.CrashReporter.ViewModels;

namespace Sentry.CrashReporter.RuntimeTests;

[TestClass]
[RunsOnUIThread]
public class StacktraceViewTests : RuntimeTestBase
{
    [TestMethod]
    public async Task StacktraceView_CanBeCreated()
    {
        // Arrange
        _ = MockRuntime();

        // Act
        var view = new StacktraceView();
        await LoadTestContent(view);

        // Assert
        Assert.IsNotNull(view);
    }

    [TestMethod]
    public async Task StacktraceView_HasNavButtons()
    {
        // Arrange
        await using var file = OpenTestFile("data/crashpad.envelope");
        var envelope = await Envelope.FromFileStreamAsync(file);
        _ = MockRuntime(envelope);

        // Act
        var view = new StacktraceView().Envelope(envelope);
        await LoadTestContent(view);

        // Assert
        var prevButton = view.FindFirstDescendant<Button>(b => b.Name == "previousThreadButton");
        var nextButton = view.FindFirstDescendant<Button>(b => b.Name == "nextThreadButton");
        Assert.IsNotNull(prevButton);
        Assert.IsNotNull(nextButton);
    }

    [TestMethod]
    public async Task StacktraceView_HasThreadComboBox()
    {
        // Arrange
        await using var file = OpenTestFile("data/crashpad.envelope");
        var envelope = await Envelope.FromFileStreamAsync(file);
        _ = MockRuntime(envelope);

        // Act
        var view = new StacktraceView().Envelope(envelope);
        await LoadTestContent(view);

        // Assert
        var comboBox = view.FindFirstDescendant<ComboBox>(cb => cb.Name == "threadComboBox");
        Assert.IsNotNull(comboBox);
        Assert.AreEqual(14, comboBox.Items.Count);
    }

    [TestMethod]
    public async Task StacktraceView_DefaultsToCrashedThread()
    {
        // Arrange
        await using var file = OpenTestFile("data/crashpad.envelope");
        var envelope = await Envelope.FromFileStreamAsync(file);
        _ = MockRuntime(envelope);

        // Act
        var view = new StacktraceView().Envelope(envelope);
        await LoadTestContent(view);

        // Assert
        var comboBox = view.FindFirstDescendant<ComboBox>(cb => cb.Name == "threadComboBox");
        Assert.IsNotNull(comboBox);
        Assert.AreEqual(0, comboBox.SelectedIndex);
        var selected = comboBox.SelectedItem as StacktraceThreadItem;
        Assert.IsNotNull(selected);
        Assert.IsTrue(selected.Crashed);
    }

    [TestMethod]
    public async Task StacktraceView_DisplaysFrameAddress()
    {
        // Arrange
        await using var file = OpenTestFile("data/crashpad.envelope");
        var envelope = await Envelope.FromFileStreamAsync(file);
        _ = MockRuntime(envelope);

        // Act
        var view = new StacktraceView().Envelope(envelope);
        await LoadTestContent(view);

        // Assert
        var address = view.FindFirstDescendant<TextBlock>(tb => tb.Text == "0x7FFC9BFA2766");
        Assert.IsNotNull(address);
    }

    [TestMethod]
    public async Task StacktraceView_DisplaysFrameSymbol()
    {
        // Arrange
        await using var file = OpenTestFile("data/crashpad.envelope");
        var envelope = await Envelope.FromFileStreamAsync(file);
        _ = MockRuntime(envelope);

        // Act
        var view = new StacktraceView().Envelope(envelope);
        await LoadTestContent(view);

        // Assert
        var symbol = view.FindFirstDescendant<TextBlock>(tb => tb.Text == "memset");
        Assert.IsNotNull(symbol);
    }

    [TestMethod]
    public async Task StacktraceView_CrashedThreadHasBugIcon()
    {
        // Arrange
        await using var file = OpenTestFile("data/crashpad.envelope");
        var envelope = await Envelope.FromFileStreamAsync(file);
        _ = MockRuntime(envelope);

        // Act
        var view = new StacktraceView().Envelope(envelope);
        await LoadTestContent(view);

        // Assert
        var bugIcon = view.FindFirstDescendant<FontAwesomeIcon>(icon => icon.Solid == FA.Bug);
        Assert.IsNotNull(bugIcon);
    }

    [TestMethod]
    public async Task StacktraceView_FrameGridHasContextMenu()
    {
        // Arrange
        await using var file = OpenTestFile("data/crashpad.envelope");
        var envelope = await Envelope.FromFileStreamAsync(file);
        _ = MockRuntime(envelope);

        // Act
        var view = new StacktraceView().Envelope(envelope);
        await LoadTestContent(view);

        // Assert
        var grid = view.FindFirstDescendant<StacktraceFrameGrid>();
        Assert.IsNotNull(grid);
        Assert.IsNotNull(grid.ContextFlyout);
        Assert.IsInstanceOfType<MenuFlyout>(grid.ContextFlyout);
        var menu = (MenuFlyout)grid.ContextFlyout;
        Assert.HasCount(2, menu.Items);
        Assert.AreEqual("Copy", ((MenuFlyoutItem)menu.Items[0]).Text);
        Assert.AreEqual("Select All", ((MenuFlyoutItem)menu.Items[1]).Text);
    }

    [TestMethod]
    public async Task StacktraceView_HasKeyboardAccelerators()
    {
        // Arrange
        await using var file = OpenTestFile("data/crashpad.envelope");
        var envelope = await Envelope.FromFileStreamAsync(file);
        _ = MockRuntime(envelope);

        // Act
        var view = new StacktraceView().Envelope(envelope);
        await LoadTestContent(view);

        // Assert
        var accelerators = view.KeyboardAccelerators;
        Assert.AreEqual(3, accelerators.Count);
        Assert.IsNotNull(accelerators.FirstOrDefault(a => a.Key == Windows.System.VirtualKey.C));
        Assert.IsNotNull(accelerators.FirstOrDefault(a => a.Key == Windows.System.VirtualKey.A));
        Assert.IsNotNull(accelerators.FirstOrDefault(a => a.Key == Windows.System.VirtualKey.Escape));
    }

    [TestMethod]
    public async Task StacktraceView_FrameGridHasTwoColumns()
    {
        // Arrange
        await using var file = OpenTestFile("data/crashpad.envelope");
        var envelope = await Envelope.FromFileStreamAsync(file);
        _ = MockRuntime(envelope);

        // Act
        var view = new StacktraceView().Envelope(envelope);
        await LoadTestContent(view);

        // Assert
        var grid = view.FindFirstDescendant<StacktraceFrameGrid>();
        Assert.IsNotNull(grid);
        Assert.AreEqual(2, grid.Columns.Count);
    }

    [TestMethod]
    public async Task StacktraceView_GetSelectedText_ReturnsRow()
    {
        // Arrange
        await using var file = OpenTestFile("data/crashpad.envelope");
        var envelope = await Envelope.FromFileStreamAsync(file);
        _ = MockRuntime(envelope);

        // Act
        var view = new StacktraceView().Envelope(envelope);
        await LoadTestContent(view);

        var grid = view.FindFirstDescendant<StacktraceFrameGrid>()!;
        grid.SelectedIndex = 0;
        await UnitTestsUIContentHelper.WaitForIdle();

        // Assert
        var selectedText = grid.GetSelectedText();
        Assert.AreEqual("0x7FFC9BFA2766\tmemset", selectedText);
    }

    [TestMethod]
    public async Task StacktraceView_GetSelectedText_ReturnsMultipleRows()
    {
        // Arrange
        await using var file = OpenTestFile("data/crashpad.envelope");
        var envelope = await Envelope.FromFileStreamAsync(file);
        _ = MockRuntime(envelope);

        // Act
        var view = new StacktraceView().Envelope(envelope);
        await LoadTestContent(view);

        var grid = view.FindFirstDescendant<StacktraceFrameGrid>()!;
        var items = (List<StacktraceFrameItem>)grid.ItemsSource!;
        grid.SelectedItems.Add(items[0]);
        grid.SelectedItems.Add(items[1]);
        await UnitTestsUIContentHelper.WaitForIdle();

        // Assert
        var selectedText = grid.GetSelectedText();
        Assert.IsNotNull(selectedText);
        var lines = selectedText.Split('\n');
        Assert.AreEqual(2, lines.Length);
    }

    [TestMethod]
    public async Task StacktraceView_GetAllText_ReturnsAllFrames()
    {
        // Arrange
        await using var file = OpenTestFile("data/crashpad.envelope");
        var envelope = await Envelope.FromFileStreamAsync(file);
        _ = MockRuntime(envelope);

        // Act
        var view = new StacktraceView().Envelope(envelope);
        await LoadTestContent(view);

        var grid = view.FindFirstDescendant<StacktraceFrameGrid>()!;

        // Assert
        var allText = grid.GetAllText();
        Assert.IsNotNull(allText);
        var lines = allText.Split('\n');
        Assert.IsTrue(lines.Length > 1);
        Assert.IsTrue(lines[0].Contains('\t'));
    }

    [TestMethod]
    public async Task StacktraceView_GetSelectedText_ReturnsNull_WhenNoSelection()
    {
        // Arrange
        await using var file = OpenTestFile("data/crashpad.envelope");
        var envelope = await Envelope.FromFileStreamAsync(file);
        _ = MockRuntime(envelope);

        // Act
        var view = new StacktraceView().Envelope(envelope);
        await LoadTestContent(view);

        var grid = view.FindFirstDescendant<StacktraceFrameGrid>()!;

        // Assert
        Assert.IsNull(grid.GetSelectedText());
    }

    [TestMethod]
    public async Task StacktraceView_EventStacktrace_DisplaysFrames()
    {
        // Arrange
        await using var file = OpenTestFile("data/stacktrace.envelope");
        var envelope = await Envelope.FromFileStreamAsync(file);
        _ = MockRuntime(envelope);

        // Act
        var view = new StacktraceView().Envelope(envelope);
        await LoadTestContent(view);

        // Assert
        var address = view.FindFirstDescendant<TextBlock>(tb => tb.Text == "0x400500");
        Assert.IsNotNull(address);
        var symbol = view.FindFirstDescendant<TextBlock>(tb => tb.Text == "crash_here");
        Assert.IsNotNull(symbol);
    }

    [TestMethod]
    public async Task StacktraceView_HidesNavWhenSingleThread()
    {
        // Arrange
        var envelope = new Envelope(new JsonObject(), [
            new EnvelopeItem(new JsonObject { ["type"] = "event" },
                System.Text.Encoding.UTF8.GetBytes(new JsonObject
                {
                    ["exception"] = new JsonObject
                    {
                        ["values"] = new JsonArray(
                            new JsonObject
                            {
                                ["stacktrace"] = new JsonObject
                                {
                                    ["frames"] = new JsonArray(
                                        new JsonObject { ["instruction_addr"] = "0x1234", ["function"] = "crash" })
                                }
                            })
                    }
                }.ToJsonString()))
        ]);
        _ = MockRuntime(envelope);

        // Act
        var view = new StacktraceView().Envelope(envelope);
        await LoadTestContent(view);

        // Assert
        var prevButton = view.FindFirstDescendant<Button>(b => b.Name == "previousThreadButton");
        var nextButton = view.FindFirstDescendant<Button>(b => b.Name == "nextThreadButton");
        var comboBox = view.FindFirstDescendant<ComboBox>(cb => cb.Name == "threadComboBox");
        Assert.IsNotNull(prevButton);
        Assert.IsNotNull(nextButton);
        Assert.IsNotNull(comboBox);
        Assert.AreEqual(Visibility.Collapsed, prevButton.FindFirstAncestor<Grid>()!.Visibility);
        Assert.AreEqual(Visibility.Collapsed, nextButton.FindFirstAncestor<Grid>()!.Visibility);
        Assert.AreEqual(Visibility.Collapsed, comboBox.FindFirstAncestor<Grid>()!.Visibility);
    }

    [TestMethod]
    public async Task StacktraceView_CopySelection()
    {
        // Arrange
        await using var file = OpenTestFile("data/crashpad.envelope");
        var envelope = await Envelope.FromFileStreamAsync(file);
        var mockRuntime = MockRuntime(envelope);

        // Act
        var view = new StacktraceView().Envelope(envelope);
        await LoadTestContent(view);

        var grid = view.FindFirstDescendant<StacktraceFrameGrid>()!;
        grid.SelectedIndex = 0;
        await UnitTestsUIContentHelper.WaitForIdle();

        var menu = (MenuFlyout)grid.ContextFlyout!;
        var copyItem = (MenuFlyoutItem)menu.Items[0];
        copyItem.Command?.Execute(null);
        await UnitTestsUIContentHelper.WaitForIdle();

        // Assert
        mockRuntime.Clipboard.Verify(c => c.SetText("0x7FFC9BFA2766\tmemset"), Times.Once);
    }

    [TestMethod]
    public async Task StacktraceView_CopyAll()
    {
        // Arrange
        await using var file = OpenTestFile("data/crashpad.envelope");
        var envelope = await Envelope.FromFileStreamAsync(file);
        var mockRuntime = MockRuntime(envelope);

        // Act
        var view = new StacktraceView().Envelope(envelope);
        await LoadTestContent(view);

        var copyAllButton = view.FindFirstDescendant<Button>(b => b.Name == "copyAllButton")!;
        copyAllButton.Command?.Execute(null);
        await UnitTestsUIContentHelper.WaitForIdle();

        // Assert
        var grid = view.FindFirstDescendant<StacktraceFrameGrid>()!;
        var allText = grid.GetAllText();
        Assert.IsNotNull(allText);
        mockRuntime.Clipboard.Verify(c => c.SetText(allText), Times.Once);
    }
}
