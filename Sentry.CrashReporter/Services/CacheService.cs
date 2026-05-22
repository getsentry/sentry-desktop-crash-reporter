namespace Sentry.CrashReporter.Services;

public enum CacheKeep
{
    None = 0,
    Offline = 1,
    Always = 2
}

internal static class CacheKeepExtensions
{
    public static CacheKeep Normalize(this CacheKeep value) =>
        value is CacheKeep.None or CacheKeep.Offline or CacheKeep.Always
            ? value
            : CacheKeep.Offline;
}

public interface ICacheService
{
    CacheKeep? CacheKeep { get; set; }
}

public class CacheService : ICacheService
{
    private const string Key = "cache_keep";

    private readonly IDictionary<string, object?>? _settings;
    private CacheKeep? _cacheKeep;

    public CacheService(IDictionary<string, object?>? settings = null)
    {
        _settings = settings ?? ApplicationData.Current?.LocalSettings?.Values;
        _cacheKeep = Load(_settings);
    }

    public CacheKeep? CacheKeep
    {
        get => _cacheKeep;
        set
        {
            _cacheKeep = value.HasValue ? value.Value.Normalize() : null;
            if (_settings is null)
            {
                return;
            }

            if (_cacheKeep.HasValue)
            {
                _settings[Key] = (int)_cacheKeep.Value;
            }
            else
            {
                _settings.Remove(Key);
            }
        }
    }

    private static CacheKeep? Load(IDictionary<string, object?>? settings)
    {
        if (settings is not null && settings.TryGetValue(Key, out var value) && value is int raw)
        {
            return ((CacheKeep)raw).Normalize();
        }

        return null;
    }
}

internal class MemoryCacheService(CacheKeep? cacheKeep = null) : ICacheService
{
    private CacheKeep? _cacheKeep = cacheKeep is { } value
        ? value.Normalize()
        : null;

    public CacheKeep? CacheKeep
    {
        get => _cacheKeep;
        set
        {
            _cacheKeep = value.HasValue ? value.Value.Normalize() : null;
        }
    }
}
