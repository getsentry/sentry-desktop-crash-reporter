using Sentry.CrashReporter.Services;
using Path = System.IO.Path;

namespace Sentry.CrashReporter.ViewModels;

public partial class AttachmentViewModel : ReactiveObject
{
    private readonly IFilePickerService? _filePickerOverride;
    [Reactive] private Envelope? _envelope;
    [ObservableAsProperty] private List<Attachment>? _attachments;
    private readonly IObservable<bool> _canAdd;

    public AttachmentViewModel(IFilePickerService? filePicker = null)
    {
        _filePickerOverride = filePicker;

        _attachmentsHelper = this.WhenAnyValue(x => x.Envelope)
            .Select(env => env is null
                ? Observable.Return<Envelope?>(null)
                : Observable.FromEventPattern(
                        h => env.ItemsChanged += h,
                        h => env.ItemsChanged -= h)
                    .Select(_ => (Envelope?)env)
                    .StartWith(env))
            .Switch()
            .Select(env => env?.TryGetAttachments())
            .ToProperty(this, x => x.Attachments);

        _canAdd = this.WhenAnyValue(x => x.Envelope).Select(env => env is not null);
    }

    public async Task Launch(Attachment attachment)
    {
        string filePath = Path.Combine(Path.GetTempPath(), attachment.Filename);
        await File.WriteAllBytesAsync(filePath, attachment.Data);
        await Launcher.LaunchUriAsync(new Uri(filePath, UriKind.Absolute));
    }

    public void Remove(Attachment attachment)
    {
        if (attachment.IsMinidump || attachment.Source is null)
        {
            return;
        }
        Envelope?.RemoveItem(attachment.Source);
    }

    [ReactiveCommand(CanExecute = nameof(_canAdd))]
    private async Task Add()
    {
        if (Envelope is null)
        {
            return;
        }
        var picker = _filePickerOverride ?? App.Services.GetService<IFilePickerService>();
        if (picker is null)
        {
            return;
        }
        var files = await picker.PickFilesAsync();
        foreach (var (name, bytes) in files)
        {
            Envelope.AddItem(EnvelopeItem.CreateAttachment(name, bytes));
        }
    }
}
