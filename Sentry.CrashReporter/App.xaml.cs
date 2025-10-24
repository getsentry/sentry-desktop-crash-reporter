using System.Net;
using Microsoft.Extensions.Http.Resilience;
using Sentry.CrashReporter.Extensions;
using Sentry.CrashReporter.Services;
using Sentry.CrashReporter.ViewModels;
using Sentry.CrashReporter.Views;

namespace Sentry.CrashReporter;

public partial class App : Application
{
    /// <summary>
    ///     Initializes the singleton application object. This is the first line of authored code
    ///     executed, and as such is the logical equivalent of main() or WinMain().
    /// </summary>
    public App()
    {
        InitializeComponent();
    }

    public static IServiceProvider ConfigureServices(StorageFile? file)
    {
        var services = new ServiceCollection();
        services.AddHttpClient()
            .ConfigureHttpClientDefaults(b => b.AddStandardResilienceHandler(ConfigureResilience));
        if (file is not null)
        {
            services.AddSingleton<IStorageFile>(file);
        }
        services.AddSingleton<ISentryClient, SentryClient>();
        services.AddSingleton<ICrashReporter, Services.CrashReporter>();
        services.AddSingleton<IWindowService, WindowService>();
        services.AddSingleton<IClipboardService, ClipboardService>();
        Ioc.Default.ConfigureServices(services.BuildServiceProvider());
        return Services;
    }

    public static void ConfigureResilience(HttpStandardResilienceOptions options)
    {
        options.Retry.MaxRetryAttempts = 3; // default
        options.Retry.Delay = TimeSpan.FromSeconds(2); // default
        options.Retry.ShouldHandle = args => ValueTask.FromResult(
            args.Outcome.Exception is HttpRequestException ||
            (args.Outcome.Result?.StatusCode >= HttpStatusCode.InternalServerError) ||
            args.Outcome.Result?.StatusCode == HttpStatusCode.BadGateway ||
            args.Outcome.Result?.StatusCode == HttpStatusCode.ServiceUnavailable ||
            args.Outcome.Result?.StatusCode == HttpStatusCode.GatewayTimeout ||
            args.Outcome.Result?.StatusCode == HttpStatusCode.RequestTimeout);
    }

    public Window? MainWindow { get; private set; }
    protected IHost? Host { get; private set; }
    public static IServiceProvider Services { get; internal set; } = Ioc.Default;

    protected async override void OnLaunched(LaunchActivatedEventArgs args)
    {
        // Load WinUI Resources
        Resources.Build(r => r.Merged(
            new XamlControlsResources()));

        // Load Uno.UI.Toolkit Resources
        Resources.Build(r => r.Merged(
            new ToolkitResources()));

        var builder = this.CreateBuilder(args)
            .Configure(host => host
                .UseToolkitNavigation()
#if DEBUG
                // Switch to Development environment when running in DEBUG
                .UseEnvironment(Environments.Development)
#endif
                .UseLogging((context, logBuilder) =>
                {
                    // Configure log levels for different categories of logging
                    logBuilder
                        .SetMinimumLevel(
                            context.HostingEnvironment.IsDevelopment() ? LogLevel.Information : LogLevel.Warning)

                        // Default filters for core Uno Platform namespaces
                        .CoreLogLevel(LogLevel.Warning);

                    // Uno Platform namespace filter groups
                    // Uncomment individual methods to see more detailed logging
                    //// Generic Xaml events
                    //logBuilder.XamlLogLevel(LogLevel.Debug);
                    //// Layout specific messages
                    //logBuilder.XamlLayoutLogLevel(LogLevel.Debug);
                    //// Storage messages
                    //logBuilder.StorageLogLevel(LogLevel.Debug);
                    //// Binding related messages
                    //logBuilder.XamlBindingLogLevel(LogLevel.Debug);
                    //// Binder memory references tracking
                    //logBuilder.BinderMemoryReferenceLogLevel(LogLevel.Debug);
                    //// DevServer and HotReload related
                    //logBuilder.HotReloadCoreLogLevel(LogLevel.Information);
                    //// Debug JS interop
                    //logBuilder.WebAssemblyLogLevel(LogLevel.Debug);
                }, true)
                .UseConfiguration(configure: configBuilder =>
                    configBuilder
                        .EmbeddedSource<App>()
                        .Section<AppConfig>()
                )
                .UseNavigation(RegisterRoutes)
            );
        MainWindow = builder.Window;

        MainWindow.Title = "Sentry Crash Reporter";
        MainWindow.Resize(900, 600);
        MainWindow.SetPreferredMinSize(600, 400);
        MainWindow.UseSystemTheme();
#if DEBUG
        MainWindow.UseStudio();
#endif
        MainWindow.SetWindowIcon();

        Host = await builder.NavigateAsync<ShellPage>();
    }

    private static void RegisterRoutes(IViewRegistry views, IRouteRegistry routes)
    {
        views.Register(
            new ViewMap<ShellPage>(),
            new ViewMap<MainPage, MainViewModel>()
        );
        routes.Register(
            new RouteMap("", View: views.FindByView<ShellPage>(),
                Nested:
                [
                    new ("Main", View: views.FindByView<MainPage>(), IsDefault: true),
                ]
            )
        );
    }
}
