namespace Sentry.CrashReporter.RuntimeTests;

[TestClass]
[RunsOnUIThread]
public class MainPageTests : RuntimeTestBase
{
    [TestMethod]
    public async Task MainPage_IsLoaded()
    {
        // Arrange
        var mockReporter = new Mock<ICrashReporter>();
        var services = new ServiceCollection();
        services.AddSingleton<ICrashReporter>(sp => mockReporter.Object);
        App.Services = services.BuildServiceProvider();

        // Act
        var page = new MainPage();
        UnitTestsUIContentHelper.EmbeddedTestRoot.SetContent(page);
        await UnitTestsUIContentHelper.WaitForLoaded(page);
        await UnitTestsUIContentHelper.WaitForIdle();
        var loadingView = page.FindFirstDescendant<LoadingView>();
        var progressRing = loadingView?.FindFirstDescendant<ProgressRing>();
        var headerView = loadingView?.FindFirstDescendant<HeaderView>();
        var eventView = loadingView?.FindFirstDescendant<EventView>();
        var feedbackView = loadingView?.FindFirstDescendant<FeedbackView>();
        var footerView = loadingView?.FindFirstDescendant<FooterView>();

        // Assert
        Assert.IsNotNull(loadingView);
        Assert.IsNotNull(progressRing);
        Assert.IsNotNull(headerView);
        Assert.IsNotNull(eventView);
        Assert.IsNotNull(feedbackView);
        Assert.IsNotNull(footerView);
    }
}
