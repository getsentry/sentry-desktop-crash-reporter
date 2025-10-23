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
        
        var messageTextBox = view.FindFirstDescendant<TextBox>(tb => tb.Header.ToString() == "Message");
        var nameTextBox = view.FindFirstDescendant<TextBox>(tb => tb.Header.ToString() == "Name");
        var emailTextBox = view.FindFirstDescendant<TextBox>(tb => tb.Header.ToString() == "Email");

        // Assert
        Assert.IsNotNull(messageTextBox);
        Assert.IsFalse(messageTextBox.IsEnabled);

        Assert.IsNotNull(nameTextBox);
        Assert.IsFalse(nameTextBox.IsEnabled);

        Assert.IsNotNull(emailTextBox);
        Assert.IsFalse(emailTextBox.IsEnabled);
    }

    [TestMethod]
    public async Task FeedbackView_TextBoxesUpdate()
    {
        // Arrange
        var (mockReporter, _) = MockCrashReporter();

        var view = new FeedbackView();
        await LoadTestContent(view);

        var messageTextBox = view.FindFirstDescendant<TextBox>(tb => tb.Header.ToString() == "Message")!;
        var nameTextBox = view.FindFirstDescendant<TextBox>(tb => tb.Header.ToString() == "Name")!;
        var emailTextBox = view.FindFirstDescendant<TextBox>(tb => tb.Header.ToString() == "Email")!;

        // Act
        messageTextBox.Text = "This is a test message.";
        nameTextBox.Text = "John Doe";
        emailTextBox.Text = "john.doe@example.com";

        // Assert
        mockReporter.Verify(r => r.UpdateFeedback(It.Is<Feedback>(f => f.Name == "John Doe" && f.Email == "john.doe@example.com" && f.Message == "This is a test message.")));
    }

    [TestMethod]
    public async Task FeedbackView_IsAvailable_True()
    {
        // Arrange
        var envelope = new Envelope(new JsonObject { ["dsn"] = "https://foo@bar.com/123", ["event_id"] = "12345678901234567890123456789012" }, []);
        _ = MockCrashReporter(envelope);

        var view = new FeedbackView().Envelope(envelope);
        await LoadTestContent(view);

        var messageTextBox = view.FindFirstDescendant<TextBox>(tb => tb.Header.ToString() == "Message")!;
        var nameTextBox = view.FindFirstDescendant<TextBox>(tb => tb.Header.ToString() == "Name")!;
        var emailTextBox = view.FindFirstDescendant<TextBox>(tb => tb.Header.ToString() == "Email")!;

        // Assert
        Assert.IsTrue(messageTextBox.IsEnabled);
        Assert.IsFalse(nameTextBox.IsEnabled);
        Assert.IsFalse(emailTextBox.IsEnabled);
    }

    [TestMethod]
    public async Task FeedbackView_IsAvailable_False()
    {
        // Arrange
        var envelope = new Envelope(new JsonObject(), []);
        _ = MockCrashReporter(envelope);

        var view = new FeedbackView().Envelope(envelope);
        await LoadTestContent(view);
        
        var messageTextBox = view.FindFirstDescendant<TextBox>(tb => tb.Header.ToString() == "Message")!;
        var nameTextBox = view.FindFirstDescendant<TextBox>(tb => tb.Header.ToString() == "Name")!;
        var emailTextBox = view.FindFirstDescendant<TextBox>(tb => tb.Header.ToString() == "Email")!;

        // Assert
        Assert.IsFalse(messageTextBox.IsEnabled);
        Assert.IsFalse(nameTextBox.IsEnabled);
        Assert.IsFalse(emailTextBox.IsEnabled);
    }

    [TestMethod]
    public async Task FeedbackView_IsEnabled_True()
    {
        // Arrange
        var envelope = new Envelope(new JsonObject { ["dsn"] = "https://foo@bar.com/123", ["event_id"] = "12345678901234567890123456789012" }, []);
        _ = MockCrashReporter(envelope);

        var view = new FeedbackView().Envelope(envelope);
        await LoadTestContent(view);

        var messageTextBox = view.FindFirstDescendant<TextBox>(tb => tb.Header.ToString() == "Message")!;
        var nameTextBox = view.FindFirstDescendant<TextBox>(tb => tb.Header.ToString() == "Name")!;
        var emailTextBox = view.FindFirstDescendant<TextBox>(tb => tb.Header.ToString() == "Email")!;

        messageTextBox.Text = "This is a test message.";

        // Assert
        Assert.IsTrue(messageTextBox.IsEnabled);
        Assert.IsTrue(nameTextBox.IsEnabled);
        Assert.IsTrue(emailTextBox.IsEnabled);
    }
}
