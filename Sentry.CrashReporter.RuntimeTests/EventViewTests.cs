namespace Sentry.CrashReporter.RuntimeTests;

[TestClass]
[RunsOnUIThread]
public class EventViewTests : RuntimeTestBase
{
    [TestMethod]
    public void EventView_CanBeCreated()
    {
        // Arrange
        _ = MockCrashReporter();

        // Act
        var view = new EventView();
        var tagsExpander = view.FindFirstDescendant<Expander>(e => e.Header.ToString() == "Tags");
        var contextsExpander = view.FindFirstDescendant<Expander>(e => e.Header.ToString() == "Contexts");
        var extraExpander = view.FindFirstDescendant<Expander>(e => e.Header.ToString() == "Additional Data");
        var sdkExpander = view.FindFirstDescendant<Expander>(e => e.Header.ToString() == "SDK");
        var attachmentsExpander = view.FindFirstDescendant<Expander>(e => e.Header.ToString() == "Attachments");

        // Assert
        Assert.IsNotNull(view);

        Assert.IsNotNull(tagsExpander);
        Assert.IsFalse(tagsExpander.IsEnabled);
        Assert.AreEqual(Visibility.Visible, tagsExpander.Visibility);

        Assert.IsNotNull(contextsExpander);
        Assert.IsFalse(contextsExpander.IsEnabled);
        Assert.AreEqual(Visibility.Visible, contextsExpander.Visibility);

        Assert.IsNotNull(extraExpander);
        Assert.IsFalse(extraExpander.IsEnabled);
        Assert.AreEqual(Visibility.Collapsed, extraExpander.Visibility);

        Assert.IsNotNull(sdkExpander);
        Assert.IsFalse(sdkExpander.IsEnabled);
        Assert.AreEqual(Visibility.Visible, sdkExpander.Visibility);

        Assert.IsNotNull(attachmentsExpander);
        Assert.IsFalse(attachmentsExpander.IsEnabled);
        Assert.AreEqual(Visibility.Collapsed, attachmentsExpander.Visibility);
    }

    [TestMethod]
    public void EventView_ExpanderVisibilityAndEnabledState()
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
            new EnvelopeItem(
                new JsonObject { { "type", "attachment" }, { "filename", "test.txt" } },
                [0x01, 0x02, 0x03])
        ]);
        _ = MockCrashReporter(envelope);

        // Act
        var view = new EventView();

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

        var attachmentsExpander = view.FindFirstDescendant<Expander>(e => e.Header.ToString() == "Attachments");

        // Assert
        Assert.IsNotNull(tagsExpander);
        Assert.IsTrue(tagsExpander.IsEnabled);
        Assert.AreEqual(Visibility.Visible, tagsExpander.Visibility);
        Assert.IsNotNull(tagKey);
        Assert.IsNotNull(tagValue);

        Assert.IsNotNull(contextsExpander);
        Assert.IsTrue(contextsExpander.IsEnabled);
        Assert.AreEqual(Visibility.Visible, contextsExpander.Visibility);
        Assert.IsNotNull(contextKey);
        Assert.IsNotNull(contextValue);

        Assert.IsNotNull(extraExpander);
        Assert.IsTrue(extraExpander.IsEnabled);
        Assert.AreEqual(Visibility.Visible, extraExpander.Visibility);
        Assert.IsNotNull(extraKey);
        Assert.IsNotNull(extraValue);

        Assert.IsNotNull(sdkExpander);
        Assert.IsTrue(sdkExpander.IsEnabled);
        Assert.AreEqual(Visibility.Visible, sdkExpander.Visibility);
        Assert.IsNotNull(sdkKey);
        Assert.IsNotNull(sdkValue);

        Assert.IsNotNull(attachmentsExpander);
        Assert.IsTrue(attachmentsExpander.IsEnabled);
        Assert.AreEqual(Visibility.Visible, attachmentsExpander.Visibility);
    }
}
