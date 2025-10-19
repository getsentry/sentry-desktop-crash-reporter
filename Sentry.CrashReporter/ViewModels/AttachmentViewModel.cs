using Sentry.CrashReporter.Services;
using Path = System.IO.Path;

namespace Sentry.CrashReporter.ViewModels;

public partial class AttachmentViewModel : ReactiveObject
{
    [Reactive] private Envelope? _envelope;
    [ObservableAsProperty] private List<Attachment>? _attachments;

    public AttachmentViewModel()
    {
        _attachmentsHelper = this.WhenAnyValue(x => x.Envelope)
            .Select(envelope => envelope?.TryGetAttachments())
            .ToProperty(this, x => x.Attachments);
    }

    public async Task Launch(Attachment attachment)
    {
        string filePath = Path.Combine(Path.GetTempPath(), attachment.Filename);
        await File.WriteAllBytesAsync(filePath, attachment.Data);
        await Launcher.LaunchUriAsync(new Uri(filePath, UriKind.Absolute));
    }
}
