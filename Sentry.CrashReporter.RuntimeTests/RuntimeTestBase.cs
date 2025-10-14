namespace Sentry.CrashReporter.RuntimeTests;

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

    protected static (Mock<ICrashReporter>, Mock<IWindowService>) MockCrashReporter(Envelope? envelope = null)
    {
        var mockReporter = new Mock<ICrashReporter>();
        mockReporter.Setup(x => x.LoadAsync(It.IsAny<CancellationToken>()))
            .Returns<CancellationToken>(ct => new ValueTask<Envelope?>(envelope));
        var mockWindow = new Mock<IWindowService>();

        var services = new ServiceCollection();
        services.AddSingleton<ICrashReporter>(sp => mockReporter.Object);
        services.AddSingleton<IWindowService>(sp => mockWindow.Object);
        App.Services = services.BuildServiceProvider();

        return (mockReporter, mockWindow);
    }
}
