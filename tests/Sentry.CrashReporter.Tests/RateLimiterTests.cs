namespace Sentry.CrashReporter.Tests;

public class RateLimiterTests
{
    private TestTimeProvider _time = null!;
    private RateLimiter _rateLimiter = null!;

    [SetUp]
    public void Setup()
    {
        _time = new TestTimeProvider();
        _rateLimiter = new RateLimiter(_time);
    }

    private static EnvelopeItem Item(string type) =>
        new(new JsonObject { ["type"] = type }, Array.Empty<byte>());

    private static HttpResponseMessage Response(HttpStatusCode status, params (string Name, string Value)[] headers)
    {
        var response = new HttpResponseMessage(status);
        foreach (var (name, value) in headers)
        {
            response.Headers.TryAddWithoutValidation(name, value);
        }

        return response;
    }

    [Test]
    public void Fresh_Limiter_Disables_Nothing()
    {
        Assert.That(_rateLimiter.IsDisabled(RateLimitCategory.Error), Is.False);
        Assert.That(_rateLimiter.IsDisabled(RateLimitCategory.Session), Is.False);
    }

    [TestCase("event", RateLimitCategory.Error)]
    [TestCase("attachment", RateLimitCategory.Error)]
    [TestCase("feedback", RateLimitCategory.Error)]
    [TestCase("session", RateLimitCategory.Session)]
    [TestCase("transaction", RateLimitCategory.Transaction)]
    [TestCase("replay_video", RateLimitCategory.Replay)]
    public void GetCategory_Maps_Item_Type(string type, RateLimitCategory expected)
    {
        Assert.That(RateLimiter.GetCategory(Item(type)), Is.EqualTo(expected));
    }

    [Test]
    public void GetCategory_ClientReport_Bypasses_RateLimiting()
    {
        Assert.That(RateLimiter.GetCategory(Item("client_report")), Is.Null);
    }

    [Test]
    public void Header_Disables_Only_Named_Category()
    {
        _rateLimiter.Update(Response(HttpStatusCode.TooManyRequests, ("X-Sentry-Rate-Limits", "60:error")));

        Assert.That(_rateLimiter.IsDisabled(RateLimitCategory.Error), Is.True);
        Assert.That(_rateLimiter.IsDisabled(RateLimitCategory.Session), Is.False);
    }

    [Test]
    public void Header_Supports_Multiple_Categories_And_Limits()
    {
        _rateLimiter.Update(Response(HttpStatusCode.TooManyRequests,
            ("X-Sentry-Rate-Limits", "60:error;transaction:organization, 120:session:project")));

        Assert.That(_rateLimiter.IsDisabled(RateLimitCategory.Error), Is.True);
        Assert.That(_rateLimiter.IsDisabled(RateLimitCategory.Transaction), Is.True);
        Assert.That(_rateLimiter.IsDisabled(RateLimitCategory.Session), Is.True);
        Assert.That(_rateLimiter.IsDisabled(RateLimitCategory.Replay), Is.False);
    }

    [Test]
    public void Header_With_Empty_Categories_Disables_Everything()
    {
        _rateLimiter.Update(Response(HttpStatusCode.TooManyRequests, ("X-Sentry-Rate-Limits", "60::organization")));

        Assert.That(_rateLimiter.IsDisabled(RateLimitCategory.Error), Is.True);
        Assert.That(_rateLimiter.IsDisabled(RateLimitCategory.Session), Is.True);
        Assert.That(_rateLimiter.IsDisabled(RateLimitCategory.Replay), Is.True);
    }

    [Test]
    public void Header_Ignores_Unknown_Categories()
    {
        _rateLimiter.Update(Response(HttpStatusCode.TooManyRequests, ("X-Sentry-Rate-Limits", "60:metric_bucket")));

        Assert.That(_rateLimiter.IsDisabled(RateLimitCategory.Error), Is.False);
        Assert.That(_rateLimiter.IsDisabled(RateLimitCategory.Session), Is.False);
    }

    [Test]
    public void RetryAfter_Header_Disables_Everything()
    {
        _rateLimiter.Update(Response(HttpStatusCode.TooManyRequests, ("Retry-After", "30")));

        Assert.That(_rateLimiter.IsDisabled(RateLimitCategory.Error), Is.True);
        Assert.That(_rateLimiter.IsDisabled(RateLimitCategory.Session), Is.True);
    }

    [Test]
    public void Bare_429_Disables_Everything()
    {
        _rateLimiter.Update(Response(HttpStatusCode.TooManyRequests));

        Assert.That(_rateLimiter.IsDisabled(RateLimitCategory.Error), Is.True);
        Assert.That(_rateLimiter.IsDisabled(RateLimitCategory.Session), Is.True);
    }

    [Test]
    public void Header_Preferred_Over_Bare_429()
    {
        // A 429 that carries a per-category header must only limit that category.
        _rateLimiter.Update(Response(HttpStatusCode.TooManyRequests, ("X-Sentry-Rate-Limits", "60:session")));

        Assert.That(_rateLimiter.IsDisabled(RateLimitCategory.Session), Is.True);
        Assert.That(_rateLimiter.IsDisabled(RateLimitCategory.Error), Is.False);
    }

    [Test]
    public void Header_On_Success_Response_Still_Updates()
    {
        // Sentry can inform limits preemptively on a 200.
        _rateLimiter.Update(Response(HttpStatusCode.OK, ("X-Sentry-Rate-Limits", "60:error")));

        Assert.That(_rateLimiter.IsDisabled(RateLimitCategory.Error), Is.True);
    }

    [Test]
    public void Limit_Expires_After_Duration()
    {
        _rateLimiter.Update(Response(HttpStatusCode.TooManyRequests, ("X-Sentry-Rate-Limits", "60:error")));
        Assert.That(_rateLimiter.IsDisabled(RateLimitCategory.Error), Is.True);

        _time.Now += TimeSpan.FromSeconds(59);
        Assert.That(_rateLimiter.IsDisabled(RateLimitCategory.Error), Is.True);

        _time.Now += TimeSpan.FromSeconds(2);
        Assert.That(_rateLimiter.IsDisabled(RateLimitCategory.Error), Is.False);
    }

    [Test]
    public void Zero_Duration_Does_Not_Disable()
    {
        _rateLimiter.Update(Response(HttpStatusCode.TooManyRequests, ("X-Sentry-Rate-Limits", "0:error")));

        Assert.That(_rateLimiter.IsDisabled(RateLimitCategory.Error), Is.False);
    }

    private sealed class TestTimeProvider : TimeProvider
    {
        public DateTimeOffset Now { get; set; } = DateTimeOffset.UnixEpoch;
        public override DateTimeOffset GetUtcNow() => Now;
    }
}
