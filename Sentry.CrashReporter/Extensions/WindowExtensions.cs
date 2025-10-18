using Windows.Foundation;
using Windows.Graphics;
using Windows.Graphics.Display;
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

    public static void Resize(this Window window, int width, int height)
    {
#if !__WASM__
        var scale = DisplayInformation.GetForCurrentView().RawPixelsPerViewPixel;
        window.AppWindow.Resize(new SizeInt32
            { Width = (int)Math.Round(width * scale), Height = (int)Math.Round(height * scale) });
#endif
    }

    public static void SetPreferredMinSize(this Window window, int width, int height)
    {
#if !__WASM__
        var view = ApplicationView.GetForCurrentView();
        view.SetPreferredMinSize(new Size(width, height));
#endif
    }
}
