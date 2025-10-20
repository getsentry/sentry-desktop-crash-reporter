namespace Sentry.CrashReporter.RuntimeTests;

[TestClass]
[RunsOnUIThread]
public class EventViewTests : RuntimeTestBase
{
    [TestMethod]
    public async Task EventView_CanBeCreated()
    {
        // Arrange
        _ = MockCrashReporter();

        // Act
        var view = new EventView();
        await LoadTestContent(view);

        var tagsExpander = view.FindFirstDescendant<Expander>(e => e.Header.ToString() == "Tags");
        var contextsExpander = view.FindFirstDescendant<Expander>(e => e.Header.ToString() == "Contexts");
        var extraExpander = view.FindFirstDescendant<Expander>(e => e.Header.ToString() == "Additional Data");
        var sdkExpander = view.FindFirstDescendant<Expander>(e => e.Header.ToString() == "SDK");

        // Assert
        Assert.IsNotNull(tagsExpander);
        Assert.IsFalse(tagsExpander.IsEnabled);
        Assert.IsFalse(tagsExpander.IsExpanded);
        Assert.AreEqual(Visibility.Visible, tagsExpander.Visibility);

        Assert.IsNotNull(contextsExpander);
        Assert.IsFalse(contextsExpander.IsEnabled);
        Assert.IsFalse(contextsExpander.IsExpanded);
        Assert.AreEqual(Visibility.Visible, contextsExpander.Visibility);

        Assert.IsNotNull(extraExpander);
        Assert.IsFalse(extraExpander.IsEnabled);
        Assert.IsFalse(extraExpander.IsExpanded);
        Assert.AreEqual(Visibility.Collapsed, extraExpander.Visibility);

        Assert.IsNotNull(sdkExpander);
        Assert.IsFalse(sdkExpander.IsEnabled);
        Assert.IsFalse(sdkExpander.IsExpanded);
        Assert.AreEqual(Visibility.Visible, sdkExpander.Visibility);
    }

    [TestMethod]
    public async Task EventView_Empty()
    {
        // Arrange
        var envelope = new Envelope(new JsonObject(), [
            new EnvelopeItem(
                new JsonObject { { "type", "event" } },
                System.Text.Encoding.UTF8.GetBytes(new JsonObject
                {
                    { "tags", new JsonObject() },
                    { "contexts", new JsonObject() },
                    { "extra", new JsonObject() },
                    { "sdk", new JsonObject() }
                }.ToJsonString())),
        ]);
        _ = MockCrashReporter(envelope);

        // Act
        var view = new EventView().Envelope(envelope);
        await LoadTestContent(view);

        var tagsExpander = view.FindFirstDescendant<Expander>(e => e.Header.ToString() == "Tags");
        var contextsExpander = view.FindFirstDescendant<Expander>(e => e.Header.ToString() == "Contexts");
        var extraExpander = view.FindFirstDescendant<Expander>(e => e.Header.ToString() == "Additional Data");
        var sdkExpander = view.FindFirstDescendant<Expander>(e => e.Header.ToString() == "SDK");

        // Assert
        Assert.IsNotNull(tagsExpander);
        Assert.IsFalse(tagsExpander.IsEnabled);
        Assert.IsFalse(tagsExpander.IsExpanded);
        Assert.AreEqual(Visibility.Visible, tagsExpander.Visibility);

        Assert.IsNotNull(contextsExpander);
        Assert.IsFalse(contextsExpander.IsEnabled);
        Assert.IsFalse(contextsExpander.IsExpanded);
        Assert.AreEqual(Visibility.Visible, contextsExpander.Visibility);

        Assert.IsNotNull(extraExpander);
        Assert.IsFalse(extraExpander.IsEnabled);
        Assert.IsFalse(extraExpander.IsExpanded);
        Assert.AreEqual(Visibility.Collapsed, extraExpander.Visibility);

        Assert.IsNotNull(sdkExpander);
        Assert.IsFalse(sdkExpander.IsEnabled);
        Assert.IsFalse(sdkExpander.IsExpanded);
        Assert.AreEqual(Visibility.Visible, sdkExpander.Visibility);
    }

    [TestMethod]
    public async Task EventView_ExpanderVisibilityAndEnabledState()
    {
        // Arrange
        var envelope = new Envelope(new JsonObject(), [
            new EnvelopeItem(
                new JsonObject { { "type", "event" } },
                System.Text.Encoding.UTF8.GetBytes(new JsonObject
                {
                    { "tags", new JsonObject { { "t1", "tag1" } } },
                    { "contexts", new JsonObject { { "c1", "context1" } } },
                    { "extra", new JsonObject { { "e1", "extra1" } } },
                    { "sdk", new JsonObject { { "s1", "sdk1" } } }
                }.ToJsonString())),
        ]);
        _ = MockCrashReporter(envelope);

        // Act
        var view = new EventView().Envelope(envelope);
        await LoadTestContent(view);

        var tagsExpander = view.FindFirstDescendant<Expander>(e => e.Header.ToString() == "Tags");
        var tagKey = tagsExpander?.FindFirstDescendant<TextBlock>(tb => tb.Text == "t1");
        var tagValue = tagsExpander?.FindFirstDescendant<TextBlock>(tb => tb.Text == "tag1");

        var contextsExpander = view.FindFirstDescendant<Expander>(e => e.Header.ToString() == "Contexts");
        var contextKey = contextsExpander?.FindFirstDescendant<TextBlock>(tb => tb.Text == "c1");
        var contextValue = contextsExpander?.FindFirstDescendant<TextBlock>(tb => tb.Text == "context1");

        var extraExpander = view.FindFirstDescendant<Expander>(e => e.Header.ToString() == "Additional Data");
        var extraKey = extraExpander?.FindFirstDescendant<TextBlock>(tb => tb.Text == "e1");
        var extraValue = extraExpander?.FindFirstDescendant<TextBlock>(tb => tb.Text == "extra1");

        var sdkExpander = view.FindFirstDescendant<Expander>(e => e.Header.ToString() == "SDK");
        var sdkKey = sdkExpander?.FindFirstDescendant<TextBlock>(tb => tb.Text == "s1");
        var sdkValue = sdkExpander?.FindFirstDescendant<TextBlock>(tb => tb.Text == "sdk1");

        // Assert
        Assert.IsNotNull(tagsExpander);
        Assert.IsTrue(tagsExpander.IsEnabled);
        Assert.IsTrue(tagsExpander.IsExpanded);
        Assert.AreEqual(Visibility.Visible, tagsExpander.Visibility);
        Assert.IsNotNull(tagKey);
        Assert.IsNotNull(tagValue);

        Assert.IsNotNull(contextsExpander);
        Assert.IsTrue(contextsExpander.IsEnabled);
        Assert.IsTrue(contextsExpander.IsExpanded);
        Assert.AreEqual(Visibility.Visible, contextsExpander.Visibility);
        Assert.IsNotNull(contextKey);
        Assert.IsNotNull(contextValue);

        Assert.IsNotNull(extraExpander);
        Assert.IsTrue(extraExpander.IsEnabled);
        Assert.IsFalse(extraExpander.IsExpanded);
        Assert.AreEqual(Visibility.Visible, extraExpander.Visibility);
        Assert.IsNotNull(extraKey);
        Assert.IsNotNull(extraValue);

        Assert.IsNotNull(sdkExpander);
        Assert.IsTrue(sdkExpander.IsEnabled);
        Assert.IsTrue(sdkExpander.IsExpanded);
        Assert.AreEqual(Visibility.Visible, sdkExpander.Visibility);
        Assert.IsNotNull(sdkKey);
        Assert.IsNotNull(sdkValue);
    }
}
