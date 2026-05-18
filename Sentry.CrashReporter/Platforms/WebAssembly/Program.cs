using Uno.UI.Hosting;
using Sentry.CrashReporter.Services;

namespace Sentry.CrashReporter;

internal class Program
{
    public static async Task Main(string[] args)
    {
        var file = await StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appx:///inproc.envelope"));
        App.ConfigureServices(file, new MemoryCacheService());

        var host = UnoPlatformHostBuilder.Create()
            .App(() => new App())
            .UseWebAssembly()
            .Build();

        await host.RunAsync();
    }
}
