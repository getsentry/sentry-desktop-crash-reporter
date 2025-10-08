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

    [Test]
    public void FormatNode_Null_ReturnsNullString()
    {
        // Arrange
        JsonNode? node = null;

        // Act
        var result = node.FormatNode();

        // Assert
        Assert.That(result, Is.EqualTo("null"));
    }

    [TestCase(true, "true")]
    [TestCase(false, "false")]
    public void FormatNode_Bool_ReturnsString(bool value, string expected)
    {
        // Arrange
        var node = JsonValue.Create(value);

        // Act
        var result = node.FormatNode();

        // Assert
        Assert.That(result, Is.EqualTo(expected));
    }

    [Test]
    public void FormatNode_Int_ReturnsString()
    {
        // Arrange
        var node = JsonValue.Create(123);

        // Act
        var result = node.FormatNode();

        // Assert
        Assert.That(result, Is.EqualTo("123"));
    }

    [Test]
    public void FormatNode_Long_ReturnsString()
    {
        // Arrange
        var node = JsonValue.Create(123456L);

        // Act
        var result = node.FormatNode();

        // Assert
        Assert.That(result, Is.EqualTo("123456"));
    }

    [Test]
    public void FormatNode_Float_ReturnsString()
    {
        // Arrange
        var node = JsonValue.Create(1.23f);

        // Act
        var result = node.FormatNode();

        // Assert
        Assert.That(result, Is.EqualTo("1.23"));
    }

    [Test]
    public void FormatNode_Double_ReturnsString()
    {
        // Arrange
        var node = JsonValue.Create(1.23456d);

        // Act
        var result = node.FormatNode();

        // Assert
        Assert.That(result, Is.EqualTo("1.23456"));
    }

    [Test]
    public void FormatNode_String_ReturnsString()
    {
        // Arrange
        var node = JsonValue.Create("a");

        // Act
        var result = node.FormatNode();

        // Assert
        Assert.That(result, Is.EqualTo("a"));
    }

    [Test]
    public void FormatNode_Object_ReturnsJsonString()
    {
        // Arrange
        var node = new JsonObject { ["a"] = "b" };

        // Act
        var result = node.FormatNode();

        // Assert
        Assert.That(result, Is.EqualTo("{\"a\":\"b\"}"));
    }

    [Test]
    public void FormatNode_Array_ReturnsJsonString()
    {
        // Arrange
        var node = new JsonArray { "a", "b" };

        // Act
        var result = node.FormatNode();

        // Assert
        Assert.That(result, Is.EqualTo("[\"a\",\"b\"]"));
    }
}
