namespace Sentry.CrashReporter.Tests;

using Microsoft.Extensions.DependencyInjection;

public class CacheServiceTests
{
    [Test]
    public void CacheKeep_IsNullWhenUnset()
    {
        // Act
        var service = new CacheService(new Dictionary<string, object?>());

        // Assert
        service.CacheKeep.Should().BeNull();
    }

    [Test]
    public void CacheKeep_IsNullWhenConstructedByDependencyInjection()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton<ICacheService, CacheService>();
        var serviceProvider = services.BuildServiceProvider();

        // Act
        var service = serviceProvider.GetRequiredService<ICacheService>();

        // Assert
        service.CacheKeep.Should().BeNull();
    }

    [Test]
    public void CacheKeep_LoadsPersistedValue()
    {
        // Arrange
        var settings = new Dictionary<string, object?> { ["cache_keep"] = (int)CacheKeep.None };

        // Act
        var service = new CacheService(settings);

        // Assert
        service.CacheKeep.Should().Be(CacheKeep.None);
    }

    [Test]
    public void CacheKeep_LoadsPersistedDefaultValue()
    {
        // Arrange
        var settings = new Dictionary<string, object?> { ["cache_keep"] = (int)CacheKeep.Always };

        // Act
        var service = new CacheService(settings);

        // Assert
        service.CacheKeep.Should().Be(CacheKeep.Always);
        settings["cache_keep"].Should().Be((int)CacheKeep.Always);
    }

    [Test]
    public void CacheKeep_SavesChanges()
    {
        // Arrange
        var settings = new Dictionary<string, object?>();
        var service = new CacheService(settings);

        // Act
        service.CacheKeep = CacheKeep.Always;

        // Assert
        settings["cache_keep"].Should().Be((int)CacheKeep.Always);
    }

    [Test]
    public void CacheKeep_SavesOffline()
    {
        // Arrange
        var settings = new Dictionary<string, object?> { ["cache_keep"] = (int)CacheKeep.Always };
        var service = new CacheService(settings);

        // Act
        service.CacheKeep = CacheKeep.Offline;

        // Assert
        service.CacheKeep.Should().Be(CacheKeep.Offline);
        settings["cache_keep"].Should().Be((int)CacheKeep.Offline);
    }

    [Test]
    public void CacheKeep_SettingNullRemovesPersistedValue()
    {
        // Arrange
        var settings = new Dictionary<string, object?> { ["cache_keep"] = (int)CacheKeep.Always };
        var service = new CacheService(settings);

        // Act
        service.CacheKeep = null;

        // Assert
        service.CacheKeep.Should().BeNull();
        settings.Should().NotContainKey("cache_keep");
    }

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
