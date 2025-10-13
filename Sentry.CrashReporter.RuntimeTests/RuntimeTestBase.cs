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
}
