using System.Text.Json.Nodes;
using Sentry.CrashReporter.Extensions;
using Sentry.CrashReporter.Services;
using Path = System.IO.Path;

namespace Sentry.CrashReporter.ViewModels;

public partial class MainViewModel : ReactiveObject, ILoadable
{
    [Reactive] private Envelope? _envelope;
    [Reactive] private bool _isExecuting;
    [Reactive] private int _selectedIndex;
    [Reactive] private Exception? _error;
    [ObservableAsProperty] private string _subtitle = string.Empty;
    [ObservableAsProperty] private EnvelopeItem? _event;
    [ObservableAsProperty] private JsonObject? _payload;
    [ObservableAsProperty] private JsonObject? _user;
    [ObservableAsProperty] private JsonObject? _tags;
    [ObservableAsProperty] private JsonObject? _contexts;
    [ObservableAsProperty] private JsonObject? _extra;
    [ObservableAsProperty] private JsonObject? _sdk;
    [ObservableAsProperty] private List<Attachment>? _attachments;
    [ObservableAsProperty] private string? _fileName = string.Empty;

    public event EventHandler? IsExecutingChanged;

    public MainViewModel(ICrashReporter? reporter = null)
    {
        reporter ??= App.Services.GetRequiredService<ICrashReporter>();

        this.WhenAnyValue(x => x.IsExecuting)
            .Subscribe(x => IsExecutingChanged?.Invoke(this, EventArgs.Empty));

        _subtitleHelper = this.WhenAnyValue(x => x.SelectedIndex, x => x.FileName, x => x.Error, ResolveSubtitle)
            .ToProperty(this, x => x.Subtitle);

        _eventHelper = this.WhenAnyValue(x => x.Envelope)
            .Select(envelope => envelope?.TryGetEvent())
            .ToProperty(this, x => x.Event);

        _payloadHelper = this.WhenAnyValue(x => x.Event)
            .Select(ev => ev?.TryParseAsJson())
            .ToProperty(this, x => x.Payload);

        _userHelper = this.WhenAnyValue(x => x.Payload)
            .Select(payload => payload?.TryGetProperty("user")?.AsObject())
            .ToProperty(this, x => x.User);

        _tagsHelper = this.WhenAnyValue(x => x.Payload)
            .Select(payload => payload?.TryGetProperty("tags")?.AsObject())
            .ToProperty(this, x => x.Tags);

        _contextsHelper = this.WhenAnyValue(x => x.Payload)
            .Select(payload => payload?.TryGetProperty("contexts")?.AsFlatObject())
            .ToProperty(this, x => x.Contexts);

        _extraHelper = this.WhenAnyValue(x => x.Payload)
            .Select(payload => payload?.TryGetProperty("extra")?.AsFlatObject())
            .ToProperty(this, x => x.Extra);

        _sdkHelper = this.WhenAnyValue(x => x.Payload)
            .Select(payload => payload?.TryGetProperty("sdk")?.AsFlatObject())
            .ToProperty(this, x => x.Sdk);

        _attachmentsHelper = this.WhenAnyValue(x => x.Envelope)
            .Select(envelope => envelope?.TryGetAttachments())
            .ToProperty(this, x => x.Attachments);

        _fileNameHelper = this.WhenAnyValue(x => x.Envelope)
            .Select(envelope => Path.GetFileName(envelope?.FilePath))
            .ToProperty(this, x => x.FileName);

        IsExecuting = true;

        Observable.FromAsync(() => reporter.LoadAsync())
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(
                onNext: value =>
                {
                    Envelope = value;
                    Error = null;
                    IsExecuting = false;
                },
                onError: ex =>
                {
                    Error = ex;
                    IsExecuting = false;
                });
    }

    private static string ResolveSubtitle(int index, string? fileName, Exception? error)
    {
        return (error, index) switch
        {
            // TODO: clean up
            (not null, _) => "Something went wrong",
            (_, 0) => "Feedback (optional)",
            (_, 1) => "Tags",
            (_, 2) => "Contexts",
            (_, 3) => "Additional Data",
            (_, 4) => "SDK",
            (_, 5) => "User",
            (_, 6) => "Attachments",
            _ => string.IsNullOrEmpty(fileName) ? "Envelope" : $"Envelope ({fileName})",
        };
    }
}
