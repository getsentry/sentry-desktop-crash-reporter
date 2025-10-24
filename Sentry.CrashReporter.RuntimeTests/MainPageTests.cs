using CommunityToolkit.WinUI.Controls;
using Sentry.CrashReporter.ViewModels;

namespace Sentry.CrashReporter.RuntimeTests;

[TestClass]
[RunsOnUIThread]
public class MainPageTests : RuntimeTestBase
{
    [TestMethod]
    public async Task MainPage_IsLoaded()
    {
        // Arrange
        _ = MockRuntime();

        // Act
        var page = new MainPage();
        await LoadTestContent(page);

        var loadingView = page.FindFirstDescendant<LoadingView>();
        var progressRing = loadingView?.FindFirstDescendant<ProgressRing>();
        var headerView = loadingView?.FindFirstDescendant<HeaderView>();
        var segmented = page.FindFirstDescendant<Segmented>();
        var feedbackView = loadingView?.FindFirstDescendant<FeedbackView>();
        var footerView = loadingView?.FindFirstDescendant<FooterView>();
        var errorView = loadingView?.FindFirstDescendant<ErrorView>();

        // Assert
        Assert.IsNotNull(loadingView);
        Assert.IsNotNull(progressRing);
        Assert.IsNotNull(headerView);
        Assert.IsNotNull(segmented);
        Assert.IsNotNull(feedbackView);
        Assert.IsNotNull(footerView);
        Assert.IsNotNull(errorView);
    }

    [TestMethod]
    public async Task MainPage_Normal()
    {
        // Arrange
        var mockRuntime = MockRuntime();
        var viewModel = new MainViewModel(mockRuntime.Reporter.Object);

        // Act
        var page = new MainPage { DataContext = viewModel };
        await LoadTestContent(page);

        // Assert
        var errorView = page.FindFirstDescendant<ErrorView>();
        Assert.IsNotNull(errorView);
        Assert.AreEqual(Visibility.Collapsed, errorView.Visibility);
    }

    [TestMethod]
    public async Task MainPage_Error()
    {
        // Arrange
        var mockRuntime = MockRuntime();
        var viewModel = new MainViewModel(mockRuntime.Reporter.Object)
        {
            Error = new Exception("Something went wrong")
        };

        // Act
        var page = new MainPage { DataContext = viewModel };
        await LoadTestContent(page);

        // Assert
        var errorView = page.FindFirstDescendant<ErrorView>();
        Assert.IsNotNull(errorView);
        Assert.AreEqual(Visibility.Visible, errorView.Visibility);
    }

    [TestMethod]
    public async Task MainPage_Tags()
    {
        // Arrange
        var envelope = new Envelope(new JsonObject(), [
            new EnvelopeItem(new JsonObject { ["type"] = "event" },
                System.Text.Encoding.UTF8.GetBytes(new JsonObject
                {
                    ["tags"] = new JsonObject { ["tag_key"] = "tag_value" },
                }.ToJsonString()))
        ]);
        var mockRuntime = MockRuntime(envelope);
        var viewModel = new MainViewModel(mockRuntime.Reporter.Object)
        {
            SelectedIndex = Array.FindIndex(MainPage.Views, v => v.Region == "tags")
        };

        // Act
        var page = new MainPage { DataContext = viewModel };
        await LoadTestContent(page);

        // Assert
        Assert.IsNotNull(page.FindFirstDescendant<TextBlock>(tb => tb.Text == "tag_key"));
        Assert.IsNotNull(page.FindFirstDescendant<TextBlock>(tb => tb.Text == "tag_value"));
    }

    [TestMethod]
    public async Task MainPage_Contexts()
    {
        // Arrange
        var envelope = new Envelope(new JsonObject(), [
            new EnvelopeItem(new JsonObject { ["type"] = "event" },
                System.Text.Encoding.UTF8.GetBytes(new JsonObject
                {
                    ["contexts"] = new JsonObject { ["context_key"] = "context_value" },
                }.ToJsonString()))
        ]);
        var mockRuntime = MockRuntime(envelope);
        var viewModel = new MainViewModel(mockRuntime.Reporter.Object)
        {
            SelectedIndex = Array.FindIndex(MainPage.Views, v => v.Region == "contexts")
        };

        // Act
        var page = new MainPage { DataContext = viewModel };
        await LoadTestContent(page);

        // Assert
        Assert.IsNotNull(page.FindFirstDescendant<TextBlock>(tb => tb.Text == "context_key"));
        Assert.IsNotNull(page.FindFirstDescendant<TextBlock>(tb => tb.Text == "context_value"));
    }

    [TestMethod]
    public async Task MainPage_Extra()
    {
        // Arrange
        var envelope = new Envelope(new JsonObject(), [
            new EnvelopeItem(new JsonObject { ["type"] = "event" },
                System.Text.Encoding.UTF8.GetBytes(new JsonObject
                {
                    ["extra"] = new JsonObject { ["extra_key"] = "extra_value" },
                }.ToJsonString()))
        ]);
        var mockRuntime = MockRuntime(envelope);
        var viewModel = new MainViewModel(mockRuntime.Reporter.Object)
        {
            SelectedIndex = Array.FindIndex(MainPage.Views, v => v.Region == "extra")
        };

        // Act
        var page = new MainPage { DataContext = viewModel };
        await LoadTestContent(page);

        // Assert
        Assert.IsNotNull(page.FindFirstDescendant<TextBlock>(tb => tb.Text == "extra_key"));
        Assert.IsNotNull(page.FindFirstDescendant<TextBlock>(tb => tb.Text == "extra_value"));
    }

    [TestMethod]
    public async Task MainPage_Sdk()
    {
        // Arrange
        var envelope = new Envelope(new JsonObject(), [
            new EnvelopeItem(new JsonObject { ["type"] = "event" },
                System.Text.Encoding.UTF8.GetBytes(new JsonObject
                {
                    ["sdk"] = new JsonObject { ["name"] = "sentry.test", ["version"] = "1.0" }
                }.ToJsonString()))
        ]);
        var mockRuntime = MockRuntime(envelope);
        var viewModel = new MainViewModel(mockRuntime.Reporter.Object)
        {
            SelectedIndex = Array.FindIndex(MainPage.Views, v => v.Region == "sdk")
        };

        // Act
        var page = new MainPage { DataContext = viewModel };
        await LoadTestContent(page);

        // Assert
        Assert.IsNotNull(page.FindFirstDescendant<TextBlock>(tb => tb.Text == "name"));
        Assert.IsNotNull(page.FindFirstDescendant<TextBlock>(tb => tb.Text == "sentry.test"));
        Assert.IsNotNull(page.FindFirstDescendant<TextBlock>(tb => tb.Text == "version"));
        Assert.IsNotNull(page.FindFirstDescendant<TextBlock>(tb => tb.Text == "1.0"));
    }

    [TestMethod]
    public async Task MainPage_User()
    {
        // Arrange
        var envelope = new Envelope(new JsonObject(), [
            new EnvelopeItem(new JsonObject { ["type"] = "event" },
                System.Text.Encoding.UTF8.GetBytes(new JsonObject
                {
                    ["user"] = new JsonObject { ["username"] = "nobody" }
                }.ToJsonString()))
        ]);
        var mockRuntime = MockRuntime(envelope);
        var viewModel = new MainViewModel(mockRuntime.Reporter.Object)
        {
            SelectedIndex = Array.FindIndex(MainPage.Views, v => v.Region == "user")
        };

        // Act
        var page = new MainPage { DataContext = viewModel };
        await LoadTestContent(page);

        // Assert
        Assert.IsNotNull(page.FindFirstDescendant<TextBlock>(tb => tb.Text == "username"));
        Assert.IsNotNull(page.FindFirstDescendant<TextBlock>(tb => tb.Text == "nobody"));
    }

    [TestMethod]
    public async Task MainPage_Attachments()
    {
        // Arrange
        var envelope = new Envelope(new JsonObject(), [
            new EnvelopeItem(new JsonObject { ["type"] = "event" }, [])
        ]);
        var mockRuntime = MockRuntime(envelope);
        var viewModel = new MainViewModel(mockRuntime.Reporter.Object)
        {
            SelectedIndex = Array.FindIndex(MainPage.Views, v => v.Region == "attachments")
        };

        // Act
        var page = new MainPage { DataContext = viewModel };
        await LoadTestContent(page);

        // Assert
        var view = page.FindFirstDescendant<AttachmentView>();
        Assert.IsNotNull(view);
        Assert.AreEqual(Visibility.Visible, view.Visibility);
    }

    [TestMethod]
    public async Task MainPage_Envelope()
    {
        // Arrange
        var envelope = new Envelope(new JsonObject(), [
            new EnvelopeItem(new JsonObject { ["type"] = "event" }, [])
        ]);
        var mockRuntime = MockRuntime(envelope);
        var viewModel = new MainViewModel(mockRuntime.Reporter.Object)
        {
            SelectedIndex = Array.FindIndex(MainPage.Views, v => v.Region == "envelope")
        };

        // Act
        var page = new MainPage { DataContext = viewModel };
        await LoadTestContent(page);

        // Assert
        var view = page.FindFirstDescendant<EnvelopeView>();
        Assert.IsNotNull(view);
        Assert.AreEqual(Visibility.Visible, view.Visibility);
    }
}
