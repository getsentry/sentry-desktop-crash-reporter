namespace Sentry.CrashReporter.Services;

public enum CacheKeep
{
    None = 0,
    Offline = 1,
    Always = 2
}

public interface ICacheService
{
    CacheKeep CacheKeep { get; set; }
}

public class CacheService : ICacheService
{
    private const string Key = "cache_keep";
    private CacheKeep _cacheKeep;

    public CacheService()
    {
        _cacheKeep = Load();
    }

    public CacheKeep CacheKeep
    {
        get => _cacheKeep;
        set
        {
            value = Normalize(value);
            if (_cacheKeep == value)
            {
                return;
            }

            _cacheKeep = value;
            Save(value);
        }
    }

    private static CacheKeep Load()
    {
        var values = ApplicationData.Current.LocalSettings.Values;
        if (values.TryGetValue(Key, out var value) && value is int raw)
        {
            return Normalize((CacheKeep)raw);
        }

        return CacheKeep.Offline;
    }

    private static void Save(CacheKeep value)
    {
        ApplicationData.Current.LocalSettings.Values[Key] = (int)value;
    }

    internal static CacheKeep Normalize(CacheKeep value) =>
        value is CacheKeep.None or CacheKeep.Offline or CacheKeep.Always
            ? value
            : CacheKeep.Offline;
}

internal class MemoryCacheService(CacheKeep cacheKeep = CacheKeep.Offline) : ICacheService
{
    private CacheKeep _cacheKeep = CacheService.Normalize(cacheKeep);

    public CacheKeep CacheKeep
    {
        get => _cacheKeep;
        set => _cacheKeep = CacheService.Normalize(value);
    }
}
