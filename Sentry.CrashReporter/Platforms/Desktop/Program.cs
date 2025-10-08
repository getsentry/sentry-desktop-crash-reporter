using Uno.UI.Hosting;
using Sentry.CrashReporter.Services;

namespace Sentry.CrashReporter;

internal class Program
{
    [STAThread]
    public static async Task Main(string[] args)
    {
        var services = new ServiceCollection();
        services.AddSingleton<HttpClient>();
        services.AddSingleton<ISentryClient, SentryClient>();
        services.AddSingleton<ICrashReporter>(sp => new Services.CrashReporter(args.SingleOrDefault() ?? string.Empty));
        Ioc.Default.ConfigureServices(services.BuildServiceProvider());

#if INTEGRATION_TEST
        var reporter = Ioc.Default.GetRequiredService<ICrashReporter>();
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
