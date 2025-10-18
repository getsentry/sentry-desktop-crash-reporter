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
        Assert.AreEqual(2, grid.ColumnDefinitions.Count);
        Assert.AreEqual(0, grid.RowDefinitions.Count);
        Assert.AreEqual(0, grid.Children.Count);
    }

    [TestMethod]
    public void JsonGrid_UpdatesWithData()
    {
        // Arrange
        var grid = new JsonGrid();
        var json = JsonNode.Parse("""{"key":"value","another_key":123}""")!;
        var data = json.AsObject().ToList();

        // Act
        grid.Data = data!;

        // Assert
        Assert.AreEqual(2, grid.RowDefinitions.Count);
        Assert.AreEqual(4, grid.Children.Count);

        var borders = grid.Children.OfType<Border>().ToList();
        Assert.AreEqual(4, borders.Count);

        var textBlocks = borders.Select(b => b.Child).OfType<TextBlock>().ToList();
        Assert.AreEqual(4, textBlocks.Count);

        Assert.AreEqual("key", textBlocks[0].Text);
        Assert.AreEqual("value", textBlocks[1].Text);
        Assert.AreEqual("another_key", textBlocks[2].Text);
        Assert.AreEqual("123", textBlocks[3].Text);
    }

    [TestMethod]
    public void JsonGrid_HandlesNullJsonValue()
    {
        // Arrange
        var grid = new JsonGrid();
        var json = JsonNode.Parse("""{"key":null}""")!;
        var data = json.AsObject().ToList();

        // Act
        grid.Data = data!;

        // Assert
        Assert.AreEqual(1, grid.RowDefinitions.Count);
        Assert.AreEqual(2, grid.Children.Count);

        var textBlocks = grid.Children.OfType<Border>().Select(b => b.Child).OfType<TextBlock>().ToList();
        Assert.AreEqual(2, textBlocks.Count);

        Assert.AreEqual("key", textBlocks[0].Text);
        Assert.AreEqual(string.Empty, textBlocks[1].Text);
    }

    [TestMethod]
    public void JsonGrid_ClearsWithNullData()
    {
        // Arrange
        var grid = new JsonGrid();
        var json = JsonNode.Parse("""{"key":"value","another_key":123}""")!;
        var data = json.AsObject().ToList();
        grid.Data = data!;
        Assert.AreEqual(2, grid.RowDefinitions.Count);
        Assert.AreEqual(4, grid.Children.Count);

        // Act
        grid.Data = null;

        // Assert
        Assert.AreEqual(0, grid.RowDefinitions.Count);
        Assert.AreEqual(0, grid.Children.Count);
    }

    [TestMethod]
    public void JsonGrid_BindsToDataContext()
    {
        // Arrange
        var grid = new JsonGrid();
        var json = JsonNode.Parse("""{"key":"value","another_key":123}""")!;
        var data = json.AsObject().ToList();

        // Act
        grid.DataContext = data;

        // Assert
        Assert.AreSame(data!, grid.Data);
        Assert.AreEqual(2, grid.RowDefinitions.Count);
        Assert.AreEqual(4, grid.Children.Count);
    }
}
