using Windows.ApplicationModel.DataTransfer;

namespace Sentry.CrashReporter.Services;

public interface IClipboardService
{
    void SetText(string text);
}

public class ClipboardService : IClipboardService
{
    public void SetText(string text)
    {
        var dataPackage = new DataPackage();
        dataPackage.SetText(text);
        Clipboard.SetContent(dataPackage);
    }
}
