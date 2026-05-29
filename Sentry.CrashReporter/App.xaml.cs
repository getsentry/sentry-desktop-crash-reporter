using System.Diagnostics.CodeAnalysis;
using System.Net;
using Microsoft.Extensions.Http.Resilience;
using Sentry.CrashReporter.Extensions;
using Sentry.CrashReporter.Services;
using Path = System.IO.Path;
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

    public static IServiceProvider ConfigureServices(StorageFile? file, ICacheService? cache = null)
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
        if (cache is not null)
        {
            services.AddSingleton<ICacheService>(cache);
        }
        else
        {
            services.AddSingleton<ICacheService, CacheService>();
        }
        var envelopeDir = Path.GetDirectoryName(file?.Path);
        var databaseDir = Path.GetDirectoryName(envelopeDir);
        var config = AppConfig.Load(envelopeDir, databaseDir, AppContext.BaseDirectory);
        services.AddSingleton<AppConfig>(config ?? new AppConfig());
        services.AddSingleton<IWindowService, WindowService>();
        services.AddSingleton<IClipboardService, ClipboardService>();
        services.AddSingleton<IFilePickerService, FilePickerService>();
#if __DESKTOP__
        services.AddSingleton<ISymbolDemangler, SymbolicDemangler>();
#else
        services.AddSingleton<ISymbolDemangler, NullDemangler>();
#endif
        Ioc.Default.ConfigureServices(services.BuildServiceProvider());
        return Services;
    }

    public static void ConfigureResilience(HttpStandardResilienceOptions options)
    {
        var maxRetryAttempts = 3; // default
        var retryDelay = TimeSpan.FromSeconds(2); // exponential: 2, 4, 8...
        var totalRequestTimeout = TimeSpan.FromHours(24); // max

        options.TotalRequestTimeout.Timeout = totalRequestTimeout;
        options.AttemptTimeout.Timeout = (totalRequestTimeout - retryDelay * maxRetryAttempts) / (maxRetryAttempts + 1);
        options.CircuitBreaker.SamplingDuration = totalRequestTimeout;
        options.Retry.MaxRetryAttempts = maxRetryAttempts;
        options.Retry.Delay = retryDelay;
        options.Retry.ShouldHandle = args => ValueTask.FromResult(
            args.Outcome.Exception is HttpRequestException ||
            (args.Outcome.Result?.StatusCode >= HttpStatusCode.InternalServerError) ||
            args.Outcome.Result?.StatusCode == HttpStatusCode.BadGateway ||
            args.Outcome.Result?.StatusCode == HttpStatusCode.ServiceUnavailable ||
            args.Outcome.Result?.StatusCode == HttpStatusCode.GatewayTimeout ||
            args.Outcome.Result?.StatusCode == HttpStatusCode.RequestTimeout);
    }

    public static bool CanClose =>
        (Application.Current as App)?.Resources["WindowClosable"] as bool? != false;
    public Window? MainWindow { get; private set; }
    protected IHost? Host { get; private set; }
    public static IServiceProvider Services { get; internal set; } = Ioc.Default;

    [UnconditionalSuppressMessage("Trimming", "IL2026",
        Justification = "Uno startup uses reflection-based configuration and navigation APIs; the app registers concrete routes and roots this assembly for trimmed builds.")]
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
                        .ContentSource("appsettings.json")
                        .Section<AppConfig>()
                )
                .UseNavigation(RegisterRoutes)
            );
        MainWindow = builder.Window;

        Services.GetRequiredService<AppConfig>().Apply(Resources);

        MainWindow.Title = (Resources["WindowTitle"] as string)!;
        MainWindow.Resize(900, 600);
        MainWindow.SetPreferredMinSize(600, 400);
        MainWindow.UseSystemTheme();
#if DEBUG
        MainWindow.UseStudio();
#endif
        MainWindow.SetWindowIcon();

        var windowService = Services.GetRequiredService<IWindowService>();
        windowService.Register(MainWindow);
        windowService.SetClosable(CanClose);

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
