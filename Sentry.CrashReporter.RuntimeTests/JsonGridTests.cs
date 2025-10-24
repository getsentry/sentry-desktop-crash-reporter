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
    public async Task JsonGrid_HasCopyAccelerator()
    {
        // Arrange
        var json = JsonNode.Parse("""{"key":"value"}""")!.AsObject();

        // Act
        var grid = new JsonGrid().Data(json!);
        await LoadTestContent(grid);

        // Assert
        Assert.HasCount(1, grid.KeyboardAccelerators);
        var accelerator = grid.KeyboardAccelerators[0];
        Assert.AreEqual(Windows.System.VirtualKey.C, accelerator.Key);
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
        Assert.HasCount(1, menu.Items);
        Assert.IsInstanceOfType<MenuFlyoutItem>(menu.Items[0]);
        var copyItem = (MenuFlyoutItem)menu.Items[0];
        Assert.AreEqual("Copy", copyItem.Text);
    }

    [TestMethod]
    public async Task JsonGrid_GetSelectedText_ReturnsKey()
    {
        // Arrange
        var json = JsonNode.Parse("""{"mykey":"myvalue"}""")!.AsObject();

        // Act
        var grid = new JsonGrid().Data(json!);
        await LoadTestContent(grid);
        grid.SelectedIndex = 0;
        grid.CurrentColumn = grid.Columns[0]; // Select key column
        await UnitTestsUIContentHelper.WaitForIdle();

        // Assert
        var selectedText = grid.GetSelectedText();
        Assert.AreEqual("mykey", selectedText);
    }

    [TestMethod]
    public async Task JsonGrid_GetSelectedText_ReturnsValue()
    {
        // Arrange
        var json = JsonNode.Parse("""{"mykey":"myvalue"}""")!.AsObject();

        // Act
        var grid = new JsonGrid().Data(json!);
        await LoadTestContent(grid);
        grid.SelectedIndex = 0;
        grid.CurrentColumn = grid.Columns[1]; // Select value column
        await UnitTestsUIContentHelper.WaitForIdle();

        // Assert
        var selectedText = grid.GetSelectedText();
        Assert.AreEqual("myvalue", selectedText);
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
}
