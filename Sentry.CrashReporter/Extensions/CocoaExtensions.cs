using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace Sentry.CrashReporter.Extensions;

internal static class CocoaExtensions
{
    private const nint NSWindowStyleMaskClosable = 2;

    [UnconditionalSuppressMessage("Trimming", "IL2075", Justification = "MacOSWindowNative is internal; reflection is required")]
    internal static void SetClosable(object nativeWindow, bool closable)
    {
        // TODO: Adapt to XxxNativeWindow (https://platform.uno/docs/articles/features/using-skia-hosting-native-controls.html)
        var handleProp = nativeWindow.GetType().GetProperty("Handle",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
        if (handleProp?.GetValue(nativeWindow) is not nint handle || handle == 0)
        {
            return;
        }

        var styleMaskSel = sel_registerName("styleMask");
        var setStyleMaskSel = sel_registerName("setStyleMask:");

        var currentMask = objc_msgSend_nint(handle, styleMaskSel);
        var newMask = closable
            ? currentMask | NSWindowStyleMaskClosable
            : currentMask & ~NSWindowStyleMaskClosable;
        objc_msgSend_void_nint(handle, setStyleMaskSel, newMask);
    }

    [DllImport("/usr/lib/libobjc.dylib", EntryPoint = "objc_msgSend")]
    private static extern nint objc_msgSend_nint(nint receiver, nint selector);

    [DllImport("/usr/lib/libobjc.dylib", EntryPoint = "objc_msgSend")]
    private static extern void objc_msgSend_void_nint(nint receiver, nint selector, nint arg);

    [DllImport("/usr/lib/libobjc.dylib")]
    private static extern nint sel_registerName(string name);
}
