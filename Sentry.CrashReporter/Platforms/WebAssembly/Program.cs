using Uno.UI.Hosting;

namespace Sentry.CrashReporter;

internal class Program
{
    public static async Task Main(string[] args)
    {
        var file = await StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appx:///inproc.envelope"));
        App.ConfigureServices(file);

        var host = UnoPlatformHostBuilder.Create()
            .App(() => new App())
            .UseWebAssembly()
            .Build();

        await host.RunAsync();
    }
}
