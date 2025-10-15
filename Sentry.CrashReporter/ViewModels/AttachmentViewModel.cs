using Sentry.CrashReporter.Services;

namespace Sentry.CrashReporter.ViewModels;

public partial class AttachmentViewModel : ReactiveObject
{
    [Reactive] private Envelope? _envelope;
    [ObservableAsProperty] private List<Attachment>? _attachments;

    public AttachmentViewModel(ICrashReporter? reporter = null)
    {
        reporter ??= App.Services.GetRequiredService<ICrashReporter>();

        _attachmentsHelper = this.WhenAnyValue(x => x.Envelope)
            .Select(envelope => envelope?.TryGetAttachments())
            .ToProperty(this, x => x.Attachments);

        Observable.FromAsync(() => reporter.LoadAsync().AsTask())
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(envelope => Envelope = envelope);
    }
}
