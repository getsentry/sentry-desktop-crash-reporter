namespace Sentry.CrashReporter.Tests;

public class AppInfoTests
{
    [Test]
    public void AppInfoCreation()
    {
        var appInfo = new AppConfig { Environment = "Test" };

        appInfo.Should().NotBeNull();
        appInfo.Environment.Should().Be("Test");
    }
}
