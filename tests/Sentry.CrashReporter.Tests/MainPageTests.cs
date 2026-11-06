using Sentry.CrashReporter.Views;

namespace Sentry.CrashReporter.Tests;

public class MainPageTests
{
    [TestCase(null, "tags", true)]
    [TestCase(new string[0], "tags", true)]
    [TestCase(new[] { "stacktrace", "feedback" }, "feedback", true)]
    [TestCase(new[] { "stacktrace", "feedback" }, "tags", false)]
    [TestCase(new[] { "StackTrace" }, "stacktrace", true)]
    [TestCase(new[] { "unknown", "stacktrace" }, "stacktrace", true)]
    [TestCase(new[] { "unknown", "stacktrace" }, "tags", false)]
    [TestCase(new[] { "unknown" }, "tags", true)]
    public void IsViewEnabled(string[]? views, string region, bool expected)
    {
        var config = new AppConfig { Views = views };

        Assert.That(MainPage.IsViewEnabled(config, region), Is.EqualTo(expected));
    }

    private static readonly string[] DefaultOrder =
        ["feedback", "tags", "contexts", "extra", "sdk", "user", "attachments", "stacktrace", "envelope"];

    [Test]
    public void GetViews_Defaults()
    {
        Assert.That(MainPage.Views.Select(v => v.Region), Is.EqualTo(DefaultOrder));
        Assert.That(MainPage.GetViews(new AppConfig()), Is.EqualTo(MainPage.Views));
        Assert.That(MainPage.GetViews(new AppConfig { Views = [] }), Is.EqualTo(MainPage.Views));
        Assert.That(MainPage.GetViews(new AppConfig { Views = ["unknown"] }), Is.EqualTo(MainPage.Views));
    }

    [TestCase(new[] { "stacktrace", "feedback" },
        new[] { "stacktrace", "feedback", "tags", "contexts", "extra", "sdk", "user", "attachments", "envelope" })]
    [TestCase(new[] { "envelope", "StackTrace" },
        new[] { "envelope", "stacktrace", "feedback", "tags", "contexts", "extra", "sdk", "user", "attachments" })]
    [TestCase(new[] { "unknown", "user", "stacktrace" },
        new[] { "user", "stacktrace", "feedback", "tags", "contexts", "extra", "sdk", "attachments", "envelope" })]
    [TestCase(new[] { "stacktrace", "feedback", "stacktrace" },
        new[] { "stacktrace", "feedback", "tags", "contexts", "extra", "sdk", "user", "attachments", "envelope" })]
    public void GetViews_Order(string[] views, string[] expected)
    {
        var config = new AppConfig { Views = views };

        Assert.That(MainPage.GetViews(config).Select(v => v.Region), Is.EqualTo(expected));
    }

    [TestCase("stacktrace", true)]
    [TestCase("StackTrace", true)]
    [TestCase("unknown", false)]
    public void IsKnownView(string name, bool expected)
    {
        Assert.That(MainPage.IsKnownView(name), Is.EqualTo(expected));
    }
}
