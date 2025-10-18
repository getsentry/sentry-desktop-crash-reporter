using Windows.UI.ViewManagement;

namespace Sentry.CrashReporter.Extensions;

public static class WindowExtensions
{
    private static readonly UISettings UiSettings = new();

    // https://github.com/unoplatform/uno/issues/21628
    public static void UseSystemTheme(this Window window)
    {
        UiSettings.ColorValuesChanged += (_, _) =>
            SystemThemeHelper.SetRootTheme(window?.Content?.XamlRoot, SystemThemeHelper.GetCurrentOsTheme() == ApplicationTheme.Dark);
    }
}
