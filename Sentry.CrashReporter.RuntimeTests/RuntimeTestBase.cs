namespace Sentry.CrashReporter.RuntimeTests;

public record MockRuntime(Mock<ICrashReporter> Reporter, Mock<IWindowService> Window, Mock<IClipboardService> Clipboard);

public class RuntimeTestBase
{
    [TestInitialize]
    public void Initialize()
    {
        RxApp.MainThreadScheduler = Scheduler.Immediate;
    }

    [TestCleanup]
    public async Task Cleanup()
    {
        UnitTestsUIContentHelper.EmbeddedTestRoot.SetContent(null);
        await UnitTestsUIContentHelper.WaitForIdle();
    }

    protected static async Task LoadTestContent(FrameworkElement element)
    {
        UnitTestsUIContentHelper.EmbeddedTestRoot.SetContent(element);
        await UnitTestsUIContentHelper.WaitForLoaded(element);
        await UnitTestsUIContentHelper.WaitForIdle();
    }

    protected static MockRuntime MockRuntime(Envelope? envelope = null)
    {
        var mockReporter = new Mock<ICrashReporter>();
        mockReporter.Setup(x => x.LoadAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(envelope));
        var mockWindow = new Mock<IWindowService>();
        var mockClipboard = new Mock<IClipboardService>();

        var services = new ServiceCollection();
        services.AddSingleton(mockReporter.Object);
        services.AddSingleton(mockWindow.Object);
        services.AddSingleton(mockClipboard.Object);
        App.Services = services.BuildServiceProvider();

        return new MockRuntime(mockReporter, mockWindow, mockClipboard);
    }
}
