namespace Sentry.CrashReporter.RuntimeTests;

[TestClass]
[RunsOnUIThread]
public class FooterViewTests : RuntimeTestBase
{
    [TestMethod]
    public async Task FooterView_Empty()
    {
        // Arrange
        _ = MockRuntime();

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
        _ = MockRuntime(envelope);

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
        var mockRuntime = MockRuntime();
        var tcs = new TaskCompletionSource();
        mockRuntime.Reporter.Setup(r => r.SubmitAsync(envelope, It.IsAny<CancellationToken>())).Returns(tcs.Task);

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
        var mockRuntime = MockRuntime();
        mockRuntime.Reporter.Setup(r => r.SubmitAsync(envelope, It.IsAny<CancellationToken>())).ThrowsAsync(new Exception("Something went wrong"));

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
    public async Task FooterView_Submit_AndCloseWindow()
    {
        // Arrange
        var envelope = new Envelope(new JsonObject { { "dsn", "https://foo@bar.com/123" }, { "event_id", "12345678901234567890123456789012" } }, []);
        var mockRuntime = MockRuntime();

        // Act
        var view = new FooterView().Envelope(envelope);
        await LoadTestContent(view);

        var submitButton = view.FindFirstDescendant<Button>("submitButton");
        submitButton?.Command.Execute(null);

        // Assert
        mockRuntime.Reporter.Verify(x => x.SubmitAsync(envelope, It.IsAny<CancellationToken>()), Times.Once);
        mockRuntime.Window.Verify(x => x.Close(), Times.Once);
    }

    [TestMethod]
    public async Task FooterView_EmptyCancelLabel_HidesCancelButton()
    {
        // Arrange
        _ = MockRuntime();
        Application.Current.Resources["CancelButton"] = "";

        // Act
        var view = new FooterView();
        await LoadTestContent(view);

        // Assert
        var cancelButton = view.FindFirstDescendant<Button>("cancelButton");
        Assert.IsNotNull(cancelButton);
        Assert.AreEqual(Visibility.Collapsed, cancelButton.Visibility);

        // Cleanup
        Application.Current.Resources["CancelButton"] = "Cancel";
    }

    [TestMethod]
    public async Task FooterView_CustomButtonLabels()
    {
        // Arrange
        _ = MockRuntime();
        Application.Current.Resources["CancelButton"] = "Dismiss";
        Application.Current.Resources["SubmitButton"] = "Send";

        // Act
        var view = new FooterView();
        await LoadTestContent(view);

        // Assert
        var cancelButton = view.FindFirstDescendant<Button>("cancelButton");
        Assert.IsNotNull(cancelButton);
        Assert.AreEqual("Dismiss", cancelButton.Content);

        var submitButton = view.FindFirstDescendant<Button>("submitButton");
        Assert.IsNotNull(submitButton);
        Assert.AreEqual("Send", submitButton.Content);

        // Cleanup
        Application.Current.Resources["CancelButton"] = "Cancel";
        Application.Current.Resources["SubmitButton"] = "Submit";
    }

    [TestMethod]
    public async Task FooterView_Cancel_ClosesWindow()
    {
        // Arrange
        var mockRuntime = MockRuntime();

        // Act
        var view = new FooterView();
        await LoadTestContent(view);

        var cancelButton = view.FindFirstDescendant<Button>("cancelButton");
        cancelButton?.Command.Execute(null);

        // Assert
        mockRuntime.Window.Verify(x => x.Close(), Times.Once);
    }
}
