namespace Sentry.CrashReporter.RuntimeTests;

[TestClass]
[RunsOnUIThread]
public class FeedbackViewTests : RuntimeTestBase
{
    [TestMethod]
    public void FeedbackView_CanBeCreated()
    {
        // Arrange
        var mockReporter = new Mock<ICrashReporter>();
        var viewModel = new FeedbackViewModel(mockReporter.Object);

        // Act
        var view = new FeedbackView(viewModel);
        var nameTextBox = view.FindFirstDescendant<TextBox>(tb => tb.PlaceholderText == "Name");
        var emailTextBox = view.FindFirstDescendant<TextBox>(tb => tb.PlaceholderText == "Email");
        var messageTextBox = view.FindFirstDescendant<TextBox>(tb => tb.PlaceholderText == "Message");

        // Assert
        Assert.IsNotNull(view);
        Assert.IsFalse(viewModel.IsEnabled);

        Assert.IsNotNull(nameTextBox);
        Assert.IsFalse(nameTextBox.IsEnabled);

        Assert.IsNotNull(emailTextBox);
        Assert.IsFalse(emailTextBox.IsEnabled);

        Assert.IsNotNull(messageTextBox);
        Assert.IsFalse(messageTextBox.IsEnabled);
    }

    [TestMethod]
    public void FeedbackView_TextBoxesUpdateViewModel()
    {
        // Arrange
        var mockReporter = new Mock<ICrashReporter>();
        var viewModel = new FeedbackViewModel(mockReporter.Object);
    
        var view = new FeedbackView(viewModel);
        var nameTextBox = view.FindFirstDescendant<TextBox>(tb => tb.PlaceholderText == "Name")!;
        var emailTextBox = view.FindFirstDescendant<TextBox>(tb => tb.PlaceholderText == "Email")!;
        var messageTextBox = view.FindFirstDescendant<TextBox>(tb => tb.PlaceholderText == "Message")!;

        // Act
        nameTextBox.Text = "John Doe";
        emailTextBox.Text = "john.doe@example.com";
        messageTextBox.Text = "This is a test message.";

        // Assert
        Assert.AreEqual("John Doe", viewModel.Name);
        Assert.AreEqual("john.doe@example.com", viewModel.Email);
        Assert.AreEqual("This is a test message.", viewModel.Message);
        mockReporter.Verify(r => r.UpdateFeedback(It.Is<Feedback>(f => f.Name == "John Doe" && f.Email == "john.doe@example.com" && f.Message == "This is a test message.")));
    }

    [TestMethod]
    public void FeedbackView_IsEnabled_TrueWhenViewModelIsEnabled()
    {
        // Arrange
        var envelope = new Envelope(new JsonObject { ["dsn"] = "https://foo@bar.com/123", ["event_id"] = "12345678901234567890123456789012" }, []);
        var mockReporter = new Mock<ICrashReporter>();
        mockReporter.Setup(x => x.LoadAsync(It.IsAny<CancellationToken>())).Returns<CancellationToken>(ct => new ValueTask<Envelope?>(envelope));
        var viewModel = new FeedbackViewModel(mockReporter.Object);

        var view = new FeedbackView(viewModel);
        var nameTextBox = view.FindFirstDescendant<TextBox>(tb => tb.PlaceholderText == "Name")!;
        var emailTextBox = view.FindFirstDescendant<TextBox>(tb => tb.PlaceholderText == "Email")!;
        var messageTextBox = view.FindFirstDescendant<TextBox>(tb => tb.PlaceholderText == "Message")!;

        // Assert
        Assert.IsTrue(viewModel.IsEnabled);
        Assert.IsTrue(nameTextBox.IsEnabled);
        Assert.IsTrue(emailTextBox.IsEnabled);
        Assert.IsTrue(messageTextBox.IsEnabled);
    }

    [TestMethod]
    public void FeedbackView_IsEnabled_FalseWhenViewModelIsDisabled()
    {
        // Arrange
        var envelope = new Envelope(new JsonObject(), []);
        var mockReporter = new Mock<ICrashReporter>();
        mockReporter.Setup(x => x.LoadAsync(It.IsAny<CancellationToken>())).Returns<CancellationToken>(ct => new ValueTask<Envelope?>(envelope));
        var viewModel = new FeedbackViewModel(mockReporter.Object);

        var view = new FeedbackView(viewModel);
        var nameTextBox = view.FindFirstDescendant<TextBox>(tb => tb.PlaceholderText == "Name")!;
        var emailTextBox = view.FindFirstDescendant<TextBox>(tb => tb.PlaceholderText == "Email")!;
        var messageTextBox = view.FindFirstDescendant<TextBox>(tb => tb.PlaceholderText == "Message")!;

        // Assert
        Assert.IsFalse(viewModel.IsEnabled);
        Assert.IsFalse(nameTextBox.IsEnabled);
        Assert.IsFalse(emailTextBox.IsEnabled);
        Assert.IsFalse(messageTextBox.IsEnabled);
    }
}
