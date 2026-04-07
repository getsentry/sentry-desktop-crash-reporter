using Windows.ApplicationModel.DataTransfer;

namespace Sentry.CrashReporter.Extensions;

internal static class ClipboardExtensions
{
    internal static void SetText(string text)
    {
        var dataPackage = new DataPackage();
        dataPackage.SetText(text);
        Clipboard.SetContent(dataPackage);
    }
}
