using Uno.UI.Hosting;
using Sentry.CrashReporter.Services;

namespace Sentry.CrashReporter;

internal class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        var services = new ServiceCollection();
        services.AddSingleton<HttpClient>();
        services.AddSingleton<ISentryClient, SentryClient>();
        services.AddSingleton<ICrashReporter>(sp => new Services.CrashReporter(args.SingleOrDefault() ?? string.Empty));
        Ioc.Default.ConfigureServices(services.BuildServiceProvider());

#if INTEGRATION_TEST
        var reporter = Ioc.Default.GetRequiredService<ICrashReporter>();
        var envelope = reporter.LoadAsync().GetAwaiter().GetResult();
        reporter.SubmitAsync().GetAwaiter().GetResult();
#else
        var host = UnoPlatformHostBuilder.Create()
            .App(() => new App())
            .UseX11()
            .UseLinuxFrameBuffer()
            .UseMacOS()
            .UseWin32()
            .Build();

        host.Run();
#endif
    }
}
