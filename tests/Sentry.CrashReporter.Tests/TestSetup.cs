using System.Reactive.Concurrency;
using ReactiveUI.Reactive;

namespace Sentry.CrashReporter.Tests;

[SetUpFixture]
public class TestSetup
{
    [OneTimeSetUp]
    public void SetUp()
    {
        RxSchedulers.MainThreadScheduler = Scheduler.Immediate;
    }
}
