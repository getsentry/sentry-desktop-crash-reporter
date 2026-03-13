using System.Runtime.InteropServices;

namespace Sentry.CrashReporter.Extensions;

internal static class Win32Extensions
{
    private const uint SC_CLOSE = 0xF060;
    private const uint MF_GRAYED = 0x00000001;
    private const uint MF_ENABLED = 0x00000000;

    internal static void SetClosable(object nativeWindow, bool closable)
    {
        if (nativeWindow is not Uno.UI.NativeElementHosting.Win32NativeWindow win32Window)
        {
            return;
        }

        var hMenu = GetSystemMenu(win32Window.Hwnd, false);
        if (hMenu != IntPtr.Zero)
        {
            EnableMenuItem(hMenu, SC_CLOSE, closable ? MF_ENABLED : MF_GRAYED);
        }
    }

    [DllImport("user32.dll")]
    private static extern IntPtr GetSystemMenu(IntPtr hWnd, bool bRevert);

    [DllImport("user32.dll")]
    private static extern bool EnableMenuItem(IntPtr hMenu, uint uIDEnableItem, uint uEnable);
}
