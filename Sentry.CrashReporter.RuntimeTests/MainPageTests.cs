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
        _ = MockCrashReporter();

        // Act
        var page = new MainPage();
        await LoadTestContent(page);

        var loadingView = page.FindFirstDescendant<LoadingView>();
        var progressRing = loadingView?.FindFirstDescendant<ProgressRing>();
        var headerView = loadingView?.FindFirstDescendant<HeaderView>();
        var eventView = loadingView?.FindFirstDescendant<EventView>();
        var feedbackView = loadingView?.FindFirstDescendant<FeedbackView>();
        var footerView = loadingView?.FindFirstDescendant<FooterView>();
        var errorView = loadingView?.FindFirstDescendant<ErrorView>();

        // Assert
        Assert.IsNotNull(loadingView);
        Assert.IsNotNull(progressRing);
        Assert.IsNotNull(headerView);
        Assert.IsNotNull(eventView);
        Assert.IsNotNull(feedbackView);
        Assert.IsNotNull(footerView);
        Assert.IsNotNull(errorView);
    }

    [TestMethod]
    public async Task MainPage_Normal()
    {
        // Arrange
        var (mockReporter, _) = MockCrashReporter();
        var viewModel = new MainViewModel(mockReporter.Object);

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
        var (mockReporter, _) = MockCrashReporter();
        var viewModel = new MainViewModel(mockReporter.Object)
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
}
