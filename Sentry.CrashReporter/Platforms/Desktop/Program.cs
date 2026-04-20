using Uno.UI.Hosting;

namespace Sentry.CrashReporter;

internal class Program
{
    // Main must return void (not Task) so that [STAThread] is honored by the CLR — otherwise
    // the C# compiler emits a synthetic sync entry point without [STAThread] and Uno's Win32
    // message loop ends up on an MTA thread, which hangs IFileOpenDialog on the file picker.
    // See https://github.com/unoplatform/uno/issues/23070.
    [STAThread]
    public static void Main(string[] args)
    {
        StorageFile? file = null;
        if (args.Length == 1)
        {
            file = StorageFile.GetFileFromPathAsync(args[0]).AsTask().GetAwaiter().GetResult();
        }
        App.ConfigureServices(file);

        var host = UnoPlatformHostBuilder.Create()
            .App(() => new App())
            .UseX11()
            .UseLinuxFrameBuffer()
            .UseMacOS()
            .UseWin32()
            .Build();

        host.RunAsync().GetAwaiter().GetResult();
    }
}
