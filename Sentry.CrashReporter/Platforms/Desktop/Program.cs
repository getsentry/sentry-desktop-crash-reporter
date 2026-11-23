using Uno.UI.Hosting;

namespace Sentry.CrashReporter;

internal class Program
{
    // Main must return void (not Task) so that [STAThread] is honored by the CLR — otherwise
    // the C# compiler emits a synthetic sync entry point without [STAThread] and Uno's Win32
    // message loop ends up on an MTA thread, which hangs IFileOpenDialog on the file picker.
    // See https://github.com/unoplatform/uno/issues/23070.
    [STAThread]
    public static void Main()
    {
        var args = CommandLineArgs.Get();

        StorageFile? file = null;
        if (args.Length == 1)
        {
            file = StorageFile.GetFileFromPathAsync(args[0]).AsTask().GetAwaiter().GetResult();
        }
        App.ConfigureServices(file);

        var host = UnoPlatformHostBuilder.Create()
            .App(() => new App())
#if GOLDEN_TEST
            // TODO: remove when Uno's X11 host stops rendering before the process exits. Mesa's llvmpipe
            // otherwise tears down under the X11 render thread at exit and crashes cold golden runs.
            .UseX11(x11 => x11.ForceRenderingBackend(X11RenderingBackend.Software))
#else
            .UseX11()
#endif
            .UseLinuxFrameBuffer()
            .UseMacOS()
            .UseWin32()
            .Build();

        host.RunAsync().GetAwaiter().GetResult();
    }
}
