using ReactiveUI.Builder;
using ReactiveUI.Reactive.Builder;

namespace Sentry.CrashReporter.RuntimeTests.Host;

internal sealed partial class UnitTestsPage : Page
{
    public UnitTestsPage()
    {
        this.Content(new UnitTestsControl());
    }
}

public partial class App : Application
{
    public App()
    {
        InitializeComponent();
    }

    protected override async void OnLaunched(LaunchActivatedEventArgs args)
    {
        // Load WinUI Resources
        Resources.Build(r => r.Merged(
            new XamlControlsResources()));

        var builder = this.CreateBuilder(args)
            .Configure(host => host
                .UseToolkitNavigation()
                .UseLogging((context, logBuilder) =>
                {
                    logBuilder
                        .SetMinimumLevel(LogLevel.Information)
                        .CoreLogLevel(LogLevel.Warning);
                }, true)
                .UseNavigation((views, routes) =>
                {
                    views.Register(
                        new ViewMap<UnitTestsPage>()
                    );
                    routes.Register(
                        new RouteMap("Test", View: views.FindByView<UnitTestsPage>(), IsDefault: true)
                    );
                })
            );

        RxAppBuilder.CreateReactiveUIBuilder()
            .WithUno(builder.Window)
            .BuildApp();

        await builder.Window.InitializeNavigationAsync(
            () => Task.FromResult(builder.Build()),
            initialRoute: "Test");

        builder.Window.Activate();
    }
}
