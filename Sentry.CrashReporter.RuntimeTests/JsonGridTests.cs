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
}
