using System.Diagnostics;
using System.Text.Json;
using Microsoft.UI.Dispatching;
using Sentry.CrashReporter.Services;
using Path = System.IO.Path;

namespace Sentry.CrashReporter.ViewModels;

public record FormattedEnvelopeItem(string Header, string Payload);

public partial class EnvelopeViewModel : ObservableObject
{
    private Envelope? _envelope;
    [ObservableProperty] private string? _eventId;
    [ObservableProperty] private string? _header;
    [ObservableProperty] private List<FormattedEnvelopeItem>? _items;

    public EnvelopeViewModel(IEnvelopeService? service = null)
    {
        service ??= Ioc.Default.GetRequiredService<IEnvelopeService>();
        FilePath = service.FilePath;

        var dispatcherQueue = DispatcherQueue.GetForCurrentThread();
        Task.Run(async () =>
        {
            var envelope = await service.LoadAsync();
            dispatcherQueue.TryEnqueue(() => Envelope = envelope);
        });
    }

    public string? FilePath { get; }
    public string? FileName => Path.GetFileName(FilePath);
    public string? Directory => Path.GetDirectoryName(FilePath);

    public Envelope? Envelope
    {
        get => _envelope;
        private set
        {
            SetProperty(ref _envelope, value);
            EventId = value?.TryGetEventId();

            var options = new JsonSerializerOptions { WriteIndented = true };
            Header = JsonSerializer.Serialize(value?.Header, options);

            var items = new List<FormattedEnvelopeItem>();
            foreach (var item in Envelope?.Items ?? [])
            {
                var header = JsonSerializer.Serialize(item.Header, options);
                try
                {
                    var json = JsonDocument.Parse(item.Payload)?.RootElement;
                    var payload = JsonSerializer.Serialize(json, options);
                    items.Add(new FormattedEnvelopeItem(header, payload));
                }
                catch (JsonException)
                {
                    const int maxLen = 32;
                    var hex = BitConverter.ToString(item.Payload.Take(maxLen).ToArray()).Replace("-", " ");
                    if (item.Payload.Length > maxLen)
                    {
                        hex += "...";
                    }

                    items.Add(new FormattedEnvelopeItem(header, hex));
                }
            }

            Items = items;
        }
    }

    private bool CanLaunch()
    {
        return !string.IsNullOrWhiteSpace(FilePath);
    }

    [RelayCommand(CanExecute = nameof(CanLaunch))]
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
