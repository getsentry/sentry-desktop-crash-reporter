using Uno.UI.Hosting;

namespace Sentry.CrashReporter;

internal class Program
{
    [STAThread]
    public static async Task Main(string[] args)
    {
        StorageFile? file = null;
        if (args.Length == 1)
        {
            file = await StorageFile.GetFileFromPathAsync(args[0]);
        }
        App.ConfigureServices(file);

        var host = UnoPlatformHostBuilder.Create()
            .App(() => new App())
            .UseX11()
            .UseLinuxFrameBuffer()
            .UseMacOS()
            .UseWin32()
            .Build();

        await host.RunAsync();
    }
}
