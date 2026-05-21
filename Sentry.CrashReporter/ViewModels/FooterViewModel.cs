using Sentry.CrashReporter.Services;
using Sentry.CrashReporter.Controls;
using System.Reactive;
using Path = System.IO.Path;

namespace Sentry.CrashReporter.ViewModels;

public enum FooterStatus
{
    Empty,
    Normal,
    Busy,
    Error
}

public partial class FooterViewModel : ReactiveObject
{
    private readonly ICrashReporter _reporter;
    private readonly IWindowService _window;
    private readonly ICacheService _cache;
    private readonly IClipboardService _clipboard;
    private readonly AppConfig _config;
    [Reactive] private Envelope? _envelope;
    [ObservableAsProperty] private string? _dsn = string.Empty;
    [ObservableAsProperty] private string? _eventId = string.Empty;
    [ObservableAsProperty] private string? _shortEventId = string.Empty;
    [ObservableAsProperty] private string? _statusText = string.Empty;
    [ObservableAsProperty] private string[] _statusIcons = [];
    [ObservableAsProperty] private string? _cacheDirectory = string.Empty;
    [ObservableAsProperty] private bool _canCache;
    [Reactive] private CacheKeep _cacheKeep;
    [Reactive] private bool _canResetCacheKeep;
    [Reactive] private int _cacheKeepIndex;
    [ObservableAsProperty] private bool _isSubmitting;
    private readonly IObservable<bool> _canSubmit;
    [Reactive] private string? _errorMessage;
    [ObservableAsProperty] private FooterStatus _status;

    public ReactiveCommand<Unit, string?> CopyEventIdCommand { get; }
    public ReactiveCommand<CacheKeep, Unit> SetCacheKeepCommand { get; }
    public ReactiveCommand<Unit, Unit> ResetCacheKeepCommand { get; }
    public ReactiveCommand<Unit, Unit> OpenCacheDirectoryCommand { get; }

    public FooterViewModel(
        ICrashReporter? reporter = null,
        IWindowService? windowService = null,
        ICacheService? cache = null,
        IClipboardService? clipboardService = null,
        AppConfig? config = null)
    {
        _reporter = reporter ?? App.Services.GetRequiredService<ICrashReporter>();
        _window = windowService ?? App.Services.GetRequiredService<IWindowService>();
        _cache = cache ?? App.Services.GetRequiredService<ICacheService>();
        _clipboard = clipboardService ?? App.Services.GetRequiredService<IClipboardService>();
        _config = config ?? App.Services.GetRequiredService<AppConfig>();

        CacheKeep = EffectiveCacheKeep;
        CanResetCacheKeep = _cache.CacheKeep is not null;
        CacheKeepIndex = CacheKeepToIndex(CacheKeep);

        _dsnHelper = this.WhenAnyValue(x => x.Envelope,  e => e?.TryGetDsn())
            .ToProperty(this, x => x.Dsn);

        _eventIdHelper = this.WhenAnyValue(x => x.Envelope, e => e?.TryGetEventId())
            .ToProperty(this, x => x.EventId);

        _shortEventIdHelper = this.WhenAnyValue(x => x.EventId)
            .Select(eventId => eventId?.Replace("-", string.Empty)[..8])
            .ToProperty(this, x => x.ShortEventId);

        _cacheDirectoryHelper = this.WhenAnyValue(x => x.Envelope)
            .Select(e => e?.TryGetHeader("cache_dir"))
            .ToProperty(this, x => x.CacheDirectory);

        _canCacheHelper = this.WhenAnyValue(x => x.CacheDirectory)
            .Select(cacheDirectory => !string.IsNullOrWhiteSpace(cacheDirectory))
            .ToProperty(this, x => x.CanCache);

        _statusTextHelper = this.WhenAnyValue(x => x.ShortEventId)
            .Select(eventId => eventId ?? string.Empty)
            .ToProperty(this, x => x.StatusText);

        _statusIconsHelper = this.WhenAnyValue(
                x => x.CanCache,
                x => x.CacheKeep,
                (canCache, cacheKeep) => canCache ? new[] { GetCacheIcon(cacheKeep) } : Array.Empty<string>())
            .ToProperty(this, x => x.StatusIcons);

        this.WhenAnyValue(x => x.CacheKeep)
            .Subscribe(cacheKeep =>
            {
                var cacheKeepIndex = CacheKeepToIndex(cacheKeep);
                if (CacheKeepIndex != cacheKeepIndex)
                {
                    CacheKeepIndex = cacheKeepIndex;
                }
            });

        this.WhenAnyValue(x => x.CacheKeepIndex)
            .Skip(1)
            .Where(index => index >= 0)
            .Select(IndexToCacheKeep)
            .Subscribe(SetCacheKeep);

        _canSubmit = this.WhenAnyValue(x => x.Dsn)
            .Select(dsn => !string.IsNullOrWhiteSpace(dsn));
        var canCopyEventId = this.WhenAnyValue(x => x.EventId)
            .Select(eventId => !string.IsNullOrWhiteSpace(eventId));
        var canOpenCacheDirectory = this.WhenAnyValue(x => x.CacheDirectory)
            .Select(cacheDirectory => !string.IsNullOrWhiteSpace(cacheDirectory));
        var canResetCacheKeep = this.WhenAnyValue(x => x.CanResetCacheKeep);

        CopyEventIdCommand = ReactiveCommand.Create(CopyEventId, canCopyEventId);
        SetCacheKeepCommand = ReactiveCommand.Create<CacheKeep>(SetCacheKeep);
        ResetCacheKeepCommand = ReactiveCommand.Create(ResetCacheKeep, canResetCacheKeep);
        OpenCacheDirectoryCommand = ReactiveCommand.CreateFromTask(OpenCacheDirectory, canOpenCacheDirectory);

        _isSubmittingHelper = this.WhenAnyObservable(x => x.SubmitCommand.IsExecuting)
            .ToProperty(this, x => x.IsSubmitting);

        _statusHelper = this.WhenAnyValue(
                x => x.EventId,
                x => x.IsSubmitting,
                x => x.ErrorMessage,
                (eventId, isSubmitting, errorMessage) =>
                {
                    if (isSubmitting) return FooterStatus.Busy;
                    if (!string.IsNullOrEmpty(errorMessage)) return FooterStatus.Error;
                    if (!string.IsNullOrEmpty(eventId)) return FooterStatus.Normal;
                    return FooterStatus.Empty;
                })
            .ToProperty(this, x => x.Status);
    }

    private static string GetCacheIcon(CacheKeep cacheKeep) =>
        cacheKeep switch
        {
            CacheKeep.None => FA.FileCircleXmark,
            CacheKeep.Always => FA.FileCircleCheck,
            _ => FA.FileCircleExclamation
        };

    private static int CacheKeepToIndex(CacheKeep cacheKeep) =>
        cacheKeep switch
        {
            CacheKeep.None => 0,
            CacheKeep.Always => 2,
            _ => 1
        };

    private static CacheKeep IndexToCacheKeep(int index) =>
        index switch
        {
            0 => CacheKeep.None,
            2 => CacheKeep.Always,
            _ => CacheKeep.Offline
        };

    private CacheKeep EffectiveCacheKeep =>
        (_cache.CacheKeep ?? _config.CacheKeep ?? Sentry.CrashReporter.Services.CacheKeep.Offline).Normalize();

    private void SetCacheKeep(CacheKeep cacheKeep)
    {
        var hasOverride = _cache.CacheKeep is not null || cacheKeep != EffectiveCacheKeep;
        _cache.CacheKeep = hasOverride ? cacheKeep : null;
        CanResetCacheKeep = hasOverride;
        CacheKeep = cacheKeep;
    }

    private void ResetCacheKeep()
    {
        _cache.CacheKeep = null;
        CanResetCacheKeep = _cache.CacheKeep is not null;
        CacheKeep = EffectiveCacheKeep;
    }

    private string? CopyEventId()
    {
        var eventId = EventId;
        if (string.IsNullOrWhiteSpace(eventId))
        {
            return null;
        }

        _clipboard.SetText(eventId);
        return eventId;
    }

    private async Task OpenCacheDirectory()
    {
        var cacheDirectory = CacheDirectory;
        if (string.IsNullOrWhiteSpace(cacheDirectory))
        {
            return;
        }

        var fullPath = Path.GetFullPath(cacheDirectory);
        System.IO.Directory.CreateDirectory(fullPath);
        await Launcher.LaunchUriAsync(new Uri(fullPath, UriKind.Absolute));
    }

    [ReactiveCommand(CanExecute = nameof(_canSubmit))]
    private async Task Submit()
    {
        ErrorMessage = null;
        _window.SetClosable(false);
        try
        {
            await _reporter.SubmitAsync(_envelope!);
            _window.Close();
        }
        catch (Exception e)
        {
            ErrorMessage = e.Message;
            _window.SetClosable(App.CanClose);
        }
    }

    [ReactiveCommand]
    private async Task Cancel()
    {
        if (IsSubmitting)
        {
            return;
        }

        if (_envelope is not null)
        {
            await _reporter.CacheAsync(_envelope);
        }

        _window.Close();
    }
}
