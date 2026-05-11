using CommunityToolkit.WinUI;

namespace Sentry.CrashReporter.RuntimeTests;

[TestClass]
[RunsOnUIThread]
public class JsonGridTests : RuntimeTestBase
{
    [TestMethod]
    public void JsonGrid_CanBeCreated()
    {
        // Act
        var grid = new JsonGrid();

        // Assert
        Assert.IsNotNull(grid);
        Assert.AreEqual(2, grid.Columns.Count);
    }

    [TestMethod]
    public async Task JsonGrid_UpdatesWithData()
    {
        // Arrange
        var json = JsonNode.Parse("""{"key":"value","another_key":123}""")!.AsObject();

        // Act
        var grid = new JsonGrid().Data(json!);
        await LoadTestContent(grid);

        // Assert
        Assert.IsNotNull(grid.FindDescendant<TextBlock>(tb => tb.Text == "key"));
        Assert.IsNotNull(grid.FindDescendant<TextBlock>(tb => tb.Text == "value"));
        Assert.IsNotNull(grid.FindDescendant<TextBlock>(tb => tb.Text == "another_key"));
        Assert.IsNotNull(grid.FindDescendant<TextBlock>(tb => tb.Text == "123"));
    }

    [TestMethod]
    public async Task JsonGrid_HandlesNullJsonValue()
    {
        // Arrange
        var json = JsonNode.Parse("""{"key":null}""")!.AsObject();

        // Act
        var grid = new JsonGrid().Data(json!);
        await LoadTestContent(grid);

        // Assert
        Assert.IsNotNull(grid.FindDescendant<TextBlock>(tb => tb.Text == "key"));
        Assert.IsNotNull(grid.FindDescendant<TextBlock>(tb => tb.Text == string.Empty));
    }

    [TestMethod]
    public async Task JsonGrid_ClearsWithNullData()
    {
        // Arrange
        var json = JsonNode.Parse("""{"key":"value"}""")!.AsObject();

        // Act
        var grid = new JsonGrid().Data(json!);
        await LoadTestContent(grid);
        grid.Data = null;
        await UnitTestsUIContentHelper.WaitForIdle();

        // Assert
        Assert.IsNull(grid.FindDescendant<TextBlock>(tb => tb.Text == "key"));
        Assert.IsNull(grid.FindDescendant<TextBlock>(tb => tb.Text == "value"));
    }

    [TestMethod]
    public async Task JsonGrid_BindsToDataContext()
    {
        // Arrange
        var json = JsonNode.Parse("""{"key":"value","another_key":123}""")!.AsObject();

        // Act
        var grid = new JsonGrid().DataContext(json);
        await LoadTestContent(grid);

        // Assert
        Assert.AreSame(json!, grid.Data);
        Assert.IsNotNull(grid.FindDescendant<TextBlock>(tb => tb.Text == "key"));
        Assert.IsNotNull(grid.FindDescendant<TextBlock>(tb => tb.Text == "value"));
        Assert.IsNotNull(grid.FindDescendant<TextBlock>(tb => tb.Text == "another_key"));
        Assert.IsNotNull(grid.FindDescendant<TextBlock>(tb => tb.Text == "123"));
    }

    [TestMethod]
    public async Task JsonGrid_SelectsItemOnSelection()
    {
        // Arrange
        var json = JsonNode.Parse("""{"first":"value1","second":"value2"}""")!.AsObject();

        // Act
        var grid = new JsonGrid().Data(json!);
        await LoadTestContent(grid);
        grid.SelectedIndex = 1;
        await UnitTestsUIContentHelper.WaitForIdle();

        // Assert
        Assert.IsNotNull(grid.SelectedItem);
        var selectedItem = (KeyValuePair<string, JsonNode>)grid.SelectedItem;
        Assert.AreEqual("second", selectedItem.Key);
        Assert.AreEqual("value2", selectedItem.Value?.ToString());
    }

    [TestMethod]
    public async Task JsonGrid_HasContextMenu()
    {
        // Arrange
        var json = JsonNode.Parse("""{"key":"value"}""")!.AsObject();

        // Act
        var grid = new JsonGrid().Data(json!);
        await LoadTestContent(grid);

        // Assert
        Assert.IsNotNull(grid.ContextFlyout);
        Assert.IsInstanceOfType<MenuFlyout>(grid.ContextFlyout);
        var menu = (MenuFlyout)grid.ContextFlyout;
        Assert.HasCount(2, menu.Items);
        Assert.AreEqual("Copy", ((MenuFlyoutItem)menu.Items[0]).Text);
        Assert.AreEqual("Select All", ((MenuFlyoutItem)menu.Items[1]).Text);
    }

    [TestMethod]
    public async Task JsonGrid_GetSelectedText_ReturnsRow()
    {
        // Arrange
        var json = JsonNode.Parse("""{"mykey":"myvalue"}""")!.AsObject();

        // Act
        var grid = new JsonGrid().Data(json!);
        await LoadTestContent(grid);
        grid.SelectedIndex = 0;
        await UnitTestsUIContentHelper.WaitForIdle();

        // Assert
        var selectedText = grid.GetSelectedText();
        Assert.AreEqual("mykey\tmyvalue", selectedText);
    }

    [TestMethod]
    public async Task JsonGrid_HasKeyboardAccelerators()
    {
        // Act
        var grid = new JsonGrid();
        await LoadTestContent(grid);

        // Assert
        var accelerators = grid.KeyboardAccelerators;
        Assert.AreEqual(3, accelerators.Count);
        Assert.IsNotNull(accelerators.FirstOrDefault(a => a.Key == Windows.System.VirtualKey.C));
        Assert.IsNotNull(accelerators.FirstOrDefault(a => a.Key == Windows.System.VirtualKey.A));
        Assert.IsNotNull(accelerators.FirstOrDefault(a => a.Key == Windows.System.VirtualKey.Escape));
    }

    [TestMethod]
    public async Task JsonGrid_GetSelectedText_ReturnsNull_WhenNoSelection()
    {
        // Arrange
        var json = JsonNode.Parse("""{"mykey":"myvalue"}""")!.AsObject();

        // Act
        var grid = new JsonGrid().Data(json!);
        await LoadTestContent(grid);

        // Assert
        var selectedText = grid.GetSelectedText();
        Assert.IsNull(selectedText);
    }

    [TestMethod]
    public async Task JsonGrid_GetSelectedText_ReturnsMultipleRows()
    {
        // Arrange
        var json = JsonNode.Parse("""{"first":"value1","second":"value2","third":"value3"}""")!.AsObject();

        // Act
        var grid = new JsonGrid().Data(json!);
        await LoadTestContent(grid);
        grid.SelectedItems.Add(grid.Data![0]);
        grid.SelectedItems.Add(grid.Data![1]);
        grid.SelectedItems.Add(grid.Data![2]);
        await UnitTestsUIContentHelper.WaitForIdle();

        // Assert
        var selectedText = grid.GetSelectedText();
        Assert.AreEqual("first\tvalue1\nsecond\tvalue2\nthird\tvalue3", selectedText);
    }

    [TestMethod]
    public async Task JsonGrid_CopySelection_CopiesToClipboard()
    {
        // Arrange
        var json = JsonNode.Parse("""{"testkey":"testvalue"}""")!.AsObject();
        var mockRuntime = MockRuntime();

        // Act
        var grid = new JsonGrid().Data(json!);
        await LoadTestContent(grid);
        grid.SelectedIndex = 0;
        await UnitTestsUIContentHelper.WaitForIdle();

        var menu = (MenuFlyout)grid.ContextFlyout!;
        var copyItem = (MenuFlyoutItem)menu.Items[0];
        copyItem.Command?.Execute(null);
        await UnitTestsUIContentHelper.WaitForIdle();

        // Assert
        mockRuntime.Clipboard.Verify(c => c.SetText("testkey\ttestvalue"), Times.Once);
    }
}
