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
        Assert.AreEqual(12, comboBox.Items.Count);
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
        var address = view.FindFirstDescendant<TextBlock>(tb => tb.Text == "0x10469E538");
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
        var symbol = view.FindFirstDescendant<TextBlock>(tb => tb.Text == "_ZL13trigger_crashv");
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
        Assert.HasCount(1, menu.Items);
        var copyItem = (MenuFlyoutItem)menu.Items[0];
        Assert.AreEqual("Copy", copyItem.Text);
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
    public async Task StacktraceView_GetSelectedText_ReturnsAddress()
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
        grid.CurrentColumn = grid.Columns[0];
        await UnitTestsUIContentHelper.WaitForIdle();

        // Assert
        var selectedText = grid.GetSelectedText();
        Assert.AreEqual("0x10469E538", selectedText);
    }

    [TestMethod]
    public async Task StacktraceView_GetSelectedText_ReturnsSymbol()
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
        grid.CurrentColumn = grid.Columns[1];
        await UnitTestsUIContentHelper.WaitForIdle();

        // Assert
        var selectedText = grid.GetSelectedText();
        Assert.AreEqual("_ZL13trigger_crashv", selectedText);
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
        grid.CurrentColumn = grid.Columns[0];
        await UnitTestsUIContentHelper.WaitForIdle();

        var menu = (MenuFlyout)grid.ContextFlyout!;
        var copyItem = (MenuFlyoutItem)menu.Items[0];
        copyItem.Command?.Execute(null);
        await UnitTestsUIContentHelper.WaitForIdle();

        // Assert
        mockRuntime.Clipboard.Verify(c => c.SetText("0x10469E538"), Times.Once);
    }
}
