namespace Sentry.CrashReporter.Tests;

public class CacheServiceTests
{
    [Test]
    public void MemoryCacheKeep_RoundTrips()
    {
        // Arrange
        var service = new MemoryCacheService();

        // Act
        service.CacheKeep = CacheKeep.Always;

        // Assert
        service.CacheKeep.Should().Be(CacheKeep.Always);
    }

    [Test]
    public void CacheKeep_NormalizesUnknownValues()
    {
        // Arrange
        var service = new MemoryCacheService();

        // Act
        service.CacheKeep = (CacheKeep)255;

        // Assert
        service.CacheKeep.Should().Be(CacheKeep.Offline);
    }
}
