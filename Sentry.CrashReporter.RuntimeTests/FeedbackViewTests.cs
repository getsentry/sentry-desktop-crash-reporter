namespace Sentry.CrashReporter.RuntimeTests;

[TestClass]
[RunsOnUIThread]
public class FeedbackViewTests : RuntimeTestBase
{
    [TestMethod]
    public async Task FeedbackView_CanBeCreated()
    {
        // Arrange
        _ = MockCrashReporter();

        // Act
        var view = new FeedbackView();
        await LoadTestContent(view);
        
        var nameTextBox = view.FindFirstDescendant<TextBox>(tb => tb.PlaceholderText == "Name");
        var emailTextBox = view.FindFirstDescendant<TextBox>(tb => tb.PlaceholderText == "Email");
        var messageTextBox = view.FindFirstDescendant<TextBox>(tb => tb.PlaceholderText == "Message");

        // Assert
        Assert.IsNotNull(nameTextBox);
        Assert.IsFalse(nameTextBox.IsEnabled);

        Assert.IsNotNull(emailTextBox);
        Assert.IsFalse(emailTextBox.IsEnabled);

        Assert.IsNotNull(messageTextBox);
        Assert.IsFalse(messageTextBox.IsEnabled);
    }

    [TestMethod]
    public async Task FeedbackView_TextBoxesUpdate()
    {
        // Arrange
        var (mockReporter, _) = MockCrashReporter();

        var view = new FeedbackView();
        await LoadTestContent(view);

        var nameTextBox = view.FindFirstDescendant<TextBox>(tb => tb.PlaceholderText == "Name")!;
        var emailTextBox = view.FindFirstDescendant<TextBox>(tb => tb.PlaceholderText == "Email")!;
        var messageTextBox = view.FindFirstDescendant<TextBox>(tb => tb.PlaceholderText == "Message")!;

        // Act
        nameTextBox.Text = "John Doe";
        emailTextBox.Text = "john.doe@example.com";
        messageTextBox.Text = "This is a test message.";

        // Assert
        mockReporter.Verify(r => r.UpdateFeedback(It.Is<Feedback>(f => f.Name == "John Doe" && f.Email == "john.doe@example.com" && f.Message == "This is a test message.")));
    }

    [TestMethod]
    public async Task FeedbackView_IsEnabled_True()
    {
        // Arrange
        var envelope = new Envelope(new JsonObject { ["dsn"] = "https://foo@bar.com/123", ["event_id"] = "12345678901234567890123456789012" }, []);
        _ = MockCrashReporter(envelope);

        var view = new FeedbackView();
        await LoadTestContent(view);

        var nameTextBox = view.FindFirstDescendant<TextBox>(tb => tb.PlaceholderText == "Name")!;
        var emailTextBox = view.FindFirstDescendant<TextBox>(tb => tb.PlaceholderText == "Email")!;
        var messageTextBox = view.FindFirstDescendant<TextBox>(tb => tb.PlaceholderText == "Message")!;

        // Assert
        Assert.IsTrue(nameTextBox.IsEnabled);
        Assert.IsTrue(emailTextBox.IsEnabled);
        Assert.IsTrue(messageTextBox.IsEnabled);
    }

    [TestMethod]
    public async Task FeedbackView_IsEnabled_False()
    {
        // Arrange
        var envelope = new Envelope(new JsonObject(), []);
        _ = MockCrashReporter(envelope);

        var view = new FeedbackView();
        await LoadTestContent(view);
        
        var nameTextBox = view.FindFirstDescendant<TextBox>(tb => tb.PlaceholderText == "Name")!;
        var emailTextBox = view.FindFirstDescendant<TextBox>(tb => tb.PlaceholderText == "Email")!;
        var messageTextBox = view.FindFirstDescendant<TextBox>(tb => tb.PlaceholderText == "Message")!;

        // Assert
        Assert.IsFalse(nameTextBox.IsEnabled);
        Assert.IsFalse(emailTextBox.IsEnabled);
        Assert.IsFalse(messageTextBox.IsEnabled);
    }
}
