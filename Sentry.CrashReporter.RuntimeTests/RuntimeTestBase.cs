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

    public static Mock<ICrashReporter> MockCrashReporter(Envelope? envelope = null)
    {
        var mockReporter = new Mock<ICrashReporter>();
        mockReporter.Setup(x => x.LoadAsync(It.IsAny<CancellationToken>()))
            .Returns<CancellationToken>(ct => new ValueTask<Envelope?>(envelope));

        var services = new ServiceCollection();
        services.AddSingleton<ICrashReporter>(sp => mockReporter.Object);
        App.Services = services.BuildServiceProvider();

        return mockReporter;
    }
}
