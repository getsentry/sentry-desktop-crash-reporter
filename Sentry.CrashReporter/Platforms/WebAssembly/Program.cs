using Uno.UI.Hosting;
using Sentry.CrashReporter.Services;

namespace Sentry.CrashReporter;

internal class Program
{
    public static async Task Main(string[] args)
    {
        var file = await StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appx:///inproc.envelope"));
        App.ConfigureServices(file, new MemoryCacheService());

#if USE_UITESTS
        // UI tests query the semantic DOM, which otherwise only exists once a user enables accessibility
        Uno.UI.FeatureConfiguration.AutomationPeer.AutoEnableAccessibility = true;
#endif

        var host = UnoPlatformHostBuilder.Create()
            .App(() => new App())
            .UseWebAssembly()
            .Build();

        await host.RunAsync();
    }
}
