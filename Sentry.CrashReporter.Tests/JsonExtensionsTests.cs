namespace Sentry.CrashReporter.Tests;

public class JsonExtensionsTests
{
    [Test]
    public void TryGetProperty_ExistingProperty_ReturnsNode()
    {
        // Arrange
        var json = new JsonObject { ["a"] = new JsonObject { ["b"] = "c" } };

        // Act
        var node = json.TryGetProperty("a.b");

        // Assert
        Assert.That(node, Is.Not.Null);
        Assert.That(node!.GetValue<string>(), Is.EqualTo("c"));
    }

    [Test]
    public void TryGetProperty_NonExistingProperty_ReturnsNull()
    {
        // Arrange
        var json = new JsonObject { ["a"] = new JsonObject { ["b"] = "c" } };

        // Act
        var node = json.TryGetProperty("a.d");

        // Assert
        Assert.That(node, Is.Null);
    }

    [Test]
    public void TryGetString_ExistingString_ReturnsValue()
    {
        // Arrange
        var json = new JsonObject { ["a"] = "b" };

        // Act
        var value = json.TryGetString("a");

        // Assert
        Assert.That(value, Is.EqualTo("b"));
    }

    [Test]
    public void TryGetString_NonExistingString_ReturnsNull()
    {
        // Arrange
        var json = new JsonObject { ["a"] = 1 };

        // Act
        var value = json.TryGetString("b");

        // Assert
        Assert.That(value, Is.Null);
    }

    [Test]
    public void TryGetDateTime_ExistingDateTime_ReturnsValue()
    {
        // Arrange
        var expected = DateTime.UtcNow;
        var json = new JsonObject { ["a"] = expected.ToString("O") };

        // Act
        var value = json.TryGetDateTime("a");

        // Assert
        Assert.That(value?.ToString("O"), Is.EqualTo(expected.ToString("O")));
    }

    [Test]
    public void TryGetDateTime_NonExistingDateTime_ReturnsNull()
    {
        // Arrange
        var json = new JsonObject { ["a"] = "not a date" };

        // Act
        var value = json.TryGetDateTime("a");

        // Assert
        Assert.That(value, Is.Null);
    }

    [Test]
    public void AsFlatObject_SimpleObject_Flattens()
    {
        // Arrange
        var json = new JsonObject
        {
            ["a"] = "b",
            ["c"] = 1
        };

        // Act
        var flat = json.AsFlatObject();

        // Assert
        Assert.That(flat["a"]!.GetValue<string>(), Is.EqualTo("b"));
        Assert.That(flat["c"]!.GetValue<int>(), Is.EqualTo(1));
    }

    [Test]
    public void AsFlatObject_NestedObject_Flattens()
    {
        // Arrange
        var json = new JsonObject
        {
            ["a"] = new JsonObject
            {
                ["b"] = "c"
            }
        };

        // Act
        var flat = json.AsFlatObject();

        // Assert
        Assert.That(flat["a.b"]!.GetValue<string>(), Is.EqualTo("c"));
    }

    [Test]
    public void AsFlatObject_ArrayOfObjects_Flattens()
    {
        // Arrange
        var json = new JsonObject
        {
            ["a"] = new JsonArray
            {
                new JsonObject { ["b"] = "c" },
                new JsonObject { ["d"] = "e" }
            }
        };

        // Act
        var flat = json.AsFlatObject();

        // Assert
        Assert.That(flat["a[0].b"]!.GetValue<string>(), Is.EqualTo("c"));
        Assert.That(flat["a[1].d"]!.GetValue<string>(), Is.EqualTo("e"));
    }

    [Test]
    public void AsFlatObject_ArrayOfValues_Joins()
    {
        // Arrange
        var json = new JsonObject
        {
            ["a"] = new JsonArray { "b", "c", "d" }
        };

        // Act
        var flat = json.AsFlatObject();

        // Assert
        Assert.That(flat["a"]!.GetValue<string>(), Is.EqualTo("b, c, d"));
    }

    [Test]
    public void AsFlatObject_NullPropertyValue_FlattensToNull()
    {
        // Arrange
        var json = new JsonObject { ["a"] = null };

        // Act
        var flat = json.AsFlatObject();

        // Assert
        Assert.That(flat.Count, Is.EqualTo(1));
        Assert.That(flat.ContainsKey("a"), Is.True);
        Assert.That(flat["a"], Is.Null);
    }

    [Test]
    public void AsFlatObject_Empty_ReturnsEmpty()
    {
        // Arrange
        var json = new JsonObject();

        // Act
        var flat = json.AsFlatObject();

        // Assert
        Assert.That(flat.Count, Is.EqualTo(0));
    }

    [Test]
    public void AsFlatObject_ArrayOfValuesWithDifferentTypes_Joins()
    {
        // Arrange
        var json = new JsonObject
        {
            ["a"] = new JsonArray { null, true, 123L, 4.56, "str" }
        };

        // Act
        var flat = json.AsFlatObject();

        // Assert
        Assert.That(flat["a"]!.GetValue<string>(), Is.EqualTo("null, true, 123, 4.56, str"));
    }

    [Test]
    public void AsFlatObject_NestedEmptyObject_IsEmpty()
    {
        // Arrange
        var json = new JsonObject
        {
            ["a"] = new JsonObject()
        };

        // Act
        var flat = json.AsFlatObject();

        // Assert
        Assert.That(flat.Count, Is.EqualTo(0));
    }
}
