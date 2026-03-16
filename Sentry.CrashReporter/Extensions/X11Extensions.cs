using System.Runtime.InteropServices;

namespace Sentry.CrashReporter.Extensions;

internal static class X11Extensions
{
    private const nint MWM_HINTS_FUNCTIONS = 0x01;
    private const nint MWM_HINTS_DECORATIONS = 0x02;
    private const nint MWM_FUNC_ALL = 0x01;
    private const nint MWM_FUNC_CLOSE = 0x20;
    private const nint MWM_DECOR_ALL = 0x01;
    private const nint MWM_DECOR_MENU = 0x10;
    private const int PropModeReplace = 0;

    internal static void SetClosable(object nativeWindow, bool closable)
    {
        if (nativeWindow is not Uno.UI.NativeElementHosting.X11NativeWindow x11Window)
        {
            return;
        }

        var windowId = x11Window.WindowId;
        var display = XOpenDisplay(IntPtr.Zero);
        if (display == IntPtr.Zero)
        {
            return;
        }

        try
        {
            var atom = XInternAtom(display, "_MOTIF_WM_HINTS", false);
            // X11 format-32 uses C long (nint) per element, not int
            var hints = new nint[5];

            if (XGetWindowProperty(display, windowId, atom, 0, 5, false, IntPtr.Zero,
                    out _, out var format, out var nItems, out _, out var propPtr) == 0
                && propPtr != IntPtr.Zero)
            {
                if (format == 32 && nItems >= 5)
                {
                    for (var i = 0; i < 5; i++)
                    {
                        hints[i] = Marshal.ReadIntPtr(propPtr, i * IntPtr.Size);
                    }
                }
                XFree(propPtr);
            }

            hints[0] |= MWM_HINTS_FUNCTIONS | MWM_HINTS_DECORATIONS;
            if (closable)
            {
                hints[1] = MWM_FUNC_ALL;
                hints[2] = MWM_DECOR_ALL;
            }
            else
            {
                hints[1] = MWM_FUNC_ALL | MWM_FUNC_CLOSE;
                hints[2] = MWM_DECOR_ALL | MWM_DECOR_MENU;
            }

            XChangeProperty(display, windowId, atom, atom, 32, PropModeReplace, hints, 5);
            XFlush(display);
        }
        finally
        {
            XCloseDisplay(display);
        }
    }

    [DllImport("libX11.so.6")]
    private static extern IntPtr XOpenDisplay(IntPtr display);

    [DllImport("libX11.so.6")]
    private static extern int XCloseDisplay(IntPtr display);

    [DllImport("libX11.so.6")]
    private static extern IntPtr XInternAtom(IntPtr display, string atomName, bool onlyIfExists);

    [DllImport("libX11.so.6")]
    private static extern int XGetWindowProperty(
        IntPtr display, IntPtr window, IntPtr property,
        nint longOffset, nint longLength, bool delete, IntPtr reqType,
        out IntPtr actualType, out int actualFormat, out nint nItems,
        out nint bytesAfter, out IntPtr prop);

    [DllImport("libX11.so.6")]
    private static extern int XChangeProperty(
        IntPtr display, IntPtr window, IntPtr property,
        IntPtr type, int format, int mode, nint[] data, int nelements);

    [DllImport("libX11.so.6")]
    private static extern int XFlush(IntPtr display);

    [DllImport("libX11.so.6")]
    private static extern int XFree(IntPtr data);
}
