using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Nodes;
using Sentry.CrashReporter.Services;
using Path = System.IO.Path;

namespace Sentry.CrashReporter.ViewModels;

public partial class EnvelopeViewModel : ReactiveObject
{
    [Reactive] private Envelope? _envelope;
    [ObservableAsProperty] private string? _eventId = string.Empty;
    [ObservableAsProperty] private FormattedEnvelope? _formatted;
    private readonly IObservable<bool> _canLaunch;

    public EnvelopeViewModel(IEnvelopeService? service = null)
    {
        service ??= Ioc.Default.GetRequiredService<IEnvelopeService>();
        FilePath = service.FilePath;

        _eventIdHelper = this.WhenAnyValue(x => x.Envelope, e => e?.TryGetEventId())
            .ToProperty(this, x => x.EventId);

        _formattedHelper = this.WhenAnyValue(x => x.Envelope, e => e?.Format())
            .ToProperty(this, x => x.Formatted);

        _canLaunch = this.WhenAnyValue(x => x.FilePath, filePath => !string.IsNullOrWhiteSpace(filePath));

        Observable.FromAsync(() => service.LoadAsync().AsTask())
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(value => Envelope = value);
    }

    public string? FilePath { get; }
    public string? FileName => Path.GetFileName(FilePath);
    public string? Directory => Path.GetDirectoryName(FilePath);

    [ReactiveCommand(CanExecute = nameof(_canLaunch))]
    private void Launch()
    {
        var launched = false;
        try
        {
            var process = Process.Start(new ProcessStartInfo
            {
                FileName = FilePath,
                UseShellExecute = true
            });
            if (process?.WaitForExit(TimeSpan.FromSeconds(3)) == true)
            {
                launched = process.ExitCode == 0;
            }
        }
        catch (Exception)
        {
            launched = false;
        }

        if (!launched)
        {
            if (OperatingSystem.IsMacOS())
            {
                // reveal in Finder
                Process.Start("open", ["-R", FilePath!]);
            }
            else
            {
                // open directory
                Process.Start(new ProcessStartInfo
                {
                    FileName = Directory,
                    UseShellExecute = true
                });
            }
        }
    }
}
