namespace Sentry.CrashReporter.RuntimeTests;

[TestClass]
[RunsOnUIThread]
public class FooterViewTests : RuntimeTestBase
{
    [TestMethod]
    public async Task FooterView_Empty()
    {
        // Arrange
        _ = MockCrashReporter();

        // Act
        var view = new FooterView();
        await LoadTestContent(view);
    
        // Assert
        var statusLabel = view.FindFirstDescendant<FrameworkElement>("statusLabel");
        Assert.IsNotNull(statusLabel);
        Assert.AreEqual(Visibility.Collapsed, statusLabel.Visibility);
    
        var cancelButton = view.FindFirstDescendant<Button>("cancelButton");
        Assert.IsNotNull(cancelButton);
        Assert.IsTrue(cancelButton.IsEnabled);
    
        var submitButton = view.FindFirstDescendant<Button>("submitButton");
        Assert.IsNotNull(submitButton);
        Assert.IsFalse(submitButton.IsEnabled);
    }

    [TestMethod]
    public async Task FooterView_Normal()
    {
        // Arrange
        var envelope = new Envelope(new JsonObject { { "dsn", "https://foo@bar.com/123" }, { "event_id" , "12345678901234567890123456789012" } }, []);
        _ = MockCrashReporter(envelope);

        // Act
        var view = new FooterView().Envelope(envelope);
        await LoadTestContent(view);

        // Assert
        var statusLabel = view.FindFirstDescendant<FrameworkElement>("statusLabel");
        Assert.IsNotNull(statusLabel);
        Assert.AreEqual(Visibility.Visible, statusLabel.Visibility);
    
        var eventIdLabel = statusLabel.FindFirstDescendant<TextBlock>(tb => tb.Text.StartsWith("123456"));
        Assert.IsNotNull(eventIdLabel);
        Assert.AreEqual(Visibility.Visible, eventIdLabel.Visibility);

        var cancelButton = view.FindFirstDescendant<Button>("cancelButton");
        Assert.IsNotNull(cancelButton);
        Assert.IsTrue(cancelButton.IsEnabled);

        var submitButton = view.FindFirstDescendant<Button>("submitButton");
        Assert.IsNotNull(submitButton);
        Assert.IsTrue(submitButton.IsEnabled);
    }

    [TestMethod]
    public async Task FooterView_Submitting()
    {
        // Arrange
        var envelope = new Envelope(new JsonObject { { "dsn", "https://foo@bar.com/123" }, { "event_id" , "12345678901234567890123456789012" } }, []);
        var (mockReporter, _) = MockCrashReporter(envelope);
        var tcs = new TaskCompletionSource();
        mockReporter.Setup(r => r.SubmitAsync(It.IsAny<CancellationToken>())).Returns(tcs.Task);

        // Act
        var view = new FooterView().Envelope(envelope);
        await LoadTestContent(view);
        view.FindFirstDescendant<Button>("submitButton")?.Command.Execute(null);
        await Task.Yield();

        // Assert
        var statusLabel = view.FindFirstDescendant<FrameworkElement>("statusLabel");
        Assert.IsNotNull(statusLabel);
        Assert.AreEqual(Visibility.Visible, statusLabel.Visibility);

        var progressRing = statusLabel.FindFirstDescendant<ProgressRing>();
        Assert.IsNotNull(progressRing);
        Assert.IsTrue(progressRing.IsActive);

        var cancelButton = view.FindFirstDescendant<Button>("cancelButton");
        Assert.IsNotNull(cancelButton);
        Assert.IsTrue(cancelButton.IsEnabled);

        var submitButton = view.FindFirstDescendant<Button>("submitButton");
        Assert.IsNotNull(submitButton);
        Assert.IsFalse(submitButton.IsEnabled);

        // Cleanup
        tcs.SetResult();
    }

    [TestMethod]
    public async Task FooterView_Error()
    {
        // Arrange
        var envelope = new Envelope(new JsonObject { { "dsn", "https://foo@bar.com/123" }, { "event_id" , "12345678901234567890123456789012" } }, []);
        var (mockReporter, _) = MockCrashReporter(envelope);
        mockReporter.Setup(r => r.SubmitAsync(It.IsAny<CancellationToken>())).ThrowsAsync(new Exception("Something went wrong"));

        // Act
        var view = new FooterView().Envelope(envelope);
        await LoadTestContent(view);
        view.FindFirstDescendant<Button>("submitButton")?.Command.Execute(null);
        await UnitTestsUIContentHelper.WaitForIdle();

        // Assert
        var statusLabel = view.FindFirstDescendant<FrameworkElement>("statusLabel");
        Assert.IsNotNull(statusLabel);

        var errorLabel = statusLabel.FindFirstDescendant<TextBlock>(tb => tb.Text == "Something went wrong");
        Assert.IsNotNull(errorLabel);
        Assert.AreEqual(Visibility.Visible, statusLabel.Visibility);

        var cancelButton = view.FindFirstDescendant<Button>("cancelButton");
        Assert.IsNotNull(cancelButton);
        Assert.IsTrue(cancelButton.IsEnabled);

        var submitButton = view.FindFirstDescendant<Button>("submitButton");
        Assert.IsNotNull(submitButton);
        Assert.IsTrue(submitButton.IsEnabled);
    }

    [TestMethod]
    public void FooterView_Submit_AndCloseWindow()
    {
        // Arrange
        var envelope = new Envelope(new JsonObject { { "dsn", "https://foo@bar.com/123" }, { "event_id", "12345678901234567890123456789012" } }, []);
        var (mockReporter, mockWindow) = MockCrashReporter(envelope);

        // Act
        var view = new FooterView().Envelope(envelope);
        var submitButton = view.FindFirstDescendant<Button>("submitButton");
        submitButton?.Command.Execute(null);

        // Assert
        mockReporter.Verify(x => x.SubmitAsync(It.IsAny<CancellationToken>()), Times.Once);
        mockWindow.Verify(x => x.Close(), Times.Once);
    }

    [TestMethod]
    public void FooterView_Cancel_ClosesWindow()
    {
        // Arrange
        var (_, mockWindow) = MockCrashReporter();

        // Act
        var view = new FooterView();
        var cancelButton = view.FindFirstDescendant<Button>("cancelButton");
        cancelButton?.Command.Execute(null);

        // Assert
        mockWindow.Verify(x => x.Close(), Times.Once);
    }
}
