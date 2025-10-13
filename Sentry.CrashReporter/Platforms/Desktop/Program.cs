using Uno.UI.Hosting;
#if INTEGRATION_TEST
using Sentry.CrashReporter.Services;
#endif

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

#if INTEGRATION_TEST
        var reporter = App.Services.GetRequiredService<ICrashReporter>();
        await reporter.LoadAsync();
        await reporter.SubmitAsync();
#else
        var host = UnoPlatformHostBuilder.Create()
            .App(() => new App())
            .UseX11()
            .UseLinuxFrameBuffer()
            .UseMacOS()
            .UseWin32()
            .Build();

        await host.RunAsync();
#endif
    }
}
