using CommunityToolkit.WinUI.Converters;
using Sentry.CrashReporter.Controls;
using Sentry.CrashReporter.Converters;
using Sentry.CrashReporter.Extensions;
using Sentry.CrashReporter.Services;
using Sentry.CrashReporter.ViewModels;

namespace Sentry.CrashReporter.Views;

public sealed class MainPage : Page
{
    internal const double ContentPadding = 16;

    // Converters are dispatcher-bound, so create them on use rather than in the static
    // constructor, which also runs when non-UI code reads the Views table.
    private static CollectionVisibilityConverter ToVisibility => new();
    private static EmptyObjectToObjectConverter ErrorToVisible => new()
    {
        EmptyValue = Visibility.Collapsed,
        NotEmptyValue = Visibility.Visible
    };
    private static EmptyObjectToObjectConverter ErrorToCollapsed => new()
    {
        EmptyValue = Visibility.Visible,
        NotEmptyValue = Visibility.Collapsed
    };

    internal record ViewItem(
        string Title,
        string Icon,
        string ToolTip,
        string Region,
        Func<MainViewModel, IDependencyPropertyBuilder<Visibility>, IBindingBuilder> Visibility,
        Func<MainViewModel, bool> IsAvailable,
        Func<MainViewModel, UIElement> Builder
    );

    internal static readonly ViewItem[] Views =
    [
        new(
            Title: "Feedback (optional)",
            Icon: FA.CommentDots,
            ToolTip: "Feedback",
            Region: "feedback",
            Visibility: (_, x) => x.Binding(() => Visibility.Visible),
            IsAvailable: _ => true,
            Builder: vm => new FeedbackView().Envelope(x => x.Binding(() => vm.Envelope))
        ),
        new(
            Title: "Tags",
            Icon: FA.Tags,
            ToolTip: "Tags",
            Region: "tags",
            Visibility: (vm, x) => x.Binding(() => vm.Tags).Converter(ToVisibility),
            IsAvailable: vm => vm.Tags is { Count: > 0 },
            Builder: vm => new JsonGrid().Category("tags").Data(x => x.Binding(() => vm.Tags))
        ),
        new(
            Title: "Contexts",
            Icon: FA.Hashtag,
            ToolTip: "Contexts",
            Region: "contexts",
            Visibility: (vm, x) => x.Binding(() => vm.Contexts).Converter(ToVisibility),
            IsAvailable: vm => vm.Contexts is { Count: > 0 },
            Builder: vm => new JsonGrid().Category("contexts").Data(x => x.Binding(() => vm.Contexts))
        ),
        new(
            Title: "Additional Data",
            Icon: FA.Table,
            ToolTip: "Additional Data",
            Region: "extra",
            Visibility: (vm, x) => x.Binding(() => vm.Extra).Converter(ToVisibility),
            IsAvailable: vm => vm.Extra is { Count: > 0 },
            Builder: vm => new JsonGrid().Category("entries").Data(x => x.Binding(() => vm.Extra))
        ),
        new(
            Title: "SDK",
            Icon: FA.Cubes,
            ToolTip: "SDK",
            Region: "sdk",
            Visibility: (vm, x) => x.Binding(() => vm.Sdk).Converter(ToVisibility),
            IsAvailable: vm => vm.Sdk is { Count: > 0 },
            Builder: vm => new JsonGrid().Data(x => x.Binding(() => vm.Sdk))
        ),
        new(
            Title: "User",
            Icon: FA.User,
            ToolTip: "User",
            Region: "user",
            Visibility: (vm, x) => x.Binding(() => vm.User).Converter(ToVisibility),
            IsAvailable: vm => vm.User is { Count: > 0 },
            Builder: vm => new JsonGrid().Data(x => x.Binding(() => vm.User))
        ),
        new(
            Title: "Attachments",
            Icon: FA.Paperclip,
            ToolTip: "Attachments",
            Region: "attachments",
            Visibility: (vm, x) => x.Binding(() => vm.Attachments).Converter(ToVisibility),
            IsAvailable: vm => vm.Attachments is { Count: > 0 },
            Builder: vm => new AttachmentView().Envelope(x => x.Binding(() => vm.Envelope))
        ),
        new(
            Title: "Stacktrace",
            Icon: FA.List,
            ToolTip: "Stacktrace",
            Region: "stacktrace",
            Visibility: (vm, x) => x.Binding(() => vm.HasStacktrace)
                .Convert(has => has ? Visibility.Visible : Visibility.Collapsed),
            IsAvailable: vm => vm.HasStacktrace,
            Builder: vm => new StacktraceView().Envelope(x => x.Binding(() => vm.Envelope))
        ),
        new(
            Title: "Envelope",
            Icon: FA.Code,
            ToolTip: "Envelope",
            Region: "envelope",
            Visibility: (_, x) => x.Binding(() => Visibility.Visible),
            IsAvailable: _ => true,
            Builder: vm => new EnvelopeView().Envelope(x => x.Binding(() => vm.Envelope))
        )
    ];

    public MainPage()
    {
        var config = App.Services.GetRequiredService<AppConfig>();
        foreach (var name in (config.Views ?? []).Where(name => !IsKnownView(name)))
        {
            this.Log().LogWarning("Ignoring unknown view in AppConfig.Views: {View}", name);
        }
        var views = GetViews(config);

        this.DataContext<MainViewModel>((view, vm) => view
            .NavigationCacheMode(NavigationCacheMode.Required)
            .Background(ThemeResource.Get<Brush>("ApplicationPageBackgroundThemeBrush"))
            .Content(new Grid()
                .Children(new Control()
                        .IsHitTestVisible(false)
                        .KeyboardAccelerators(GetKeyboardAccelerators()),
                    new LoadingView()
                        .Source(x => x.Binding(() => vm))
                        .LoadingContent(new ProgressRing()
                            .Width(72)
                            .Height(72))
                        .Content(new Grid()
                            .Region(attached: true)
                            .Padding(new Thickness(ContentPadding))
                            .RowSpacing(16)
                            .RowDefinitions("Auto,Auto,*,Auto")
                            .Children(
                                new HeaderView()
                                    .Grid(row: 0)
                                    .Envelope(x => x.Binding(() => vm.Envelope)),
                                new Grid()
                                    .Grid(row: 1)
                                    .ColumnDefinitions("*,Auto")
                                    .Children(
                                        new TextBlock()
                                            .Grid(column: 0)
                                            .Style(ThemeResource.Get<Style>("SubtitleTextBlockStyle"))
                                            .Visibility(x => x.Binding(() => vm.Error).Converter(ErrorToVisible))
                                            .Text("Something went wrong"),
                                        new TextBlock()
                                            .Grid(column: 0)
                                            .Style(ThemeResource.Get<Style>("SubtitleTextBlockStyle"))
                                            .Visibility(x => x.Binding(() => vm.Error).Converter(ErrorToCollapsed))
                                            .Text(x => x.Binding(() => vm.SelectedIndex).Convert(i => views[i].Title)),
                                        new Segmented()
                                            .Grid(column: 1)
                                            .Visibility(x => x.Binding(() => vm.Error).Converter(ErrorToCollapsed))
                                            .SelectedIndex(x => x.Binding(() => vm.SelectedIndex).TwoWay())
                                            .Items(views.Select((v, i) => IsViewEnabled(config, v.Region)
                                                ? CreateSegmentedItem(v).Visibility(x => v.Visibility(vm, x))
                                                // A disabled view only shows up as the fallback selection
                                                : CreateSegmentedItem(v).Visibility(x => x.Binding(() => vm.SelectedIndex)
                                                    .Convert(s => s == i ? Visibility.Visible : Visibility.Collapsed))).ToArray<object>())),
                                new Grid()
                                    .Grid(row: 2)
                                    .Children(
                                        new ErrorView()
                                            .Error(x => x.Binding(() => vm.Error))
                                            .Visibility(x => x.Binding(() => vm.Error).Converter(ErrorToVisible)),
                                        new Grid()
                                            .Region(attached: true, navigator: "Visibility")
                                            .Visibility(x => x.Binding(() => vm.Error).Converter(ErrorToCollapsed))
                                            .Children(views.Select((v, i) => v.Builder(vm)
                                                .Region(name: v.Region)
                                                .Visibility(x => x.Binding(() => vm.SelectedIndex)
                                                    .Convert(s => s == i? Visibility.Visible : Visibility.Collapsed))).ToArray())),
                                new FooterView()
                                    .Grid(row: 3)
                                    .Envelope(x => x.Binding(() => vm.Envelope)))))));
    }

    internal static bool IsKnownView(string name) =>
        Views.Any(v => string.Equals(v.Region, name, StringComparison.OrdinalIgnoreCase));

    // Unknown names are ignored, and a list without any known name enables every view
    internal static bool IsViewEnabled(AppConfig config, string region)
    {
        var known = (config.Views ?? []).Where(IsKnownView).ToArray();
        return known.Length == 0 || known.Contains(region, StringComparer.OrdinalIgnoreCase);
    }

    // Configured views first in the configured order, then the remaining views in the default order
    internal static ViewItem[] GetViews(AppConfig config)
    {
        var configured = (config.Views ?? [])
            .Select(name => Views.FirstOrDefault(v => string.Equals(v.Region, name, StringComparison.OrdinalIgnoreCase)))
            .OfType<ViewItem>()
            .Distinct()
            .ToArray();
        return [.. configured, .. Views.Except(configured)];
    }

    private static SegmentedItem CreateSegmentedItem(ViewItem view) =>
        new SegmentedItem()
            .Content(new FontAwesomeIcon(view.Icon))
            .ToolTip(view.ToolTip)
            .Navigation(request: view.Region);

    private static KeyboardAccelerator[] GetKeyboardAccelerators()
    {
        var accelerators = new List<KeyboardAccelerator>();

        // https://github.com/unoplatform/uno/issues/20332
        if (OperatingSystem.IsMacOS())
        {
            var closeAccelerator = new KeyboardAccelerator
            {
                Key = VirtualKey.Q,
                Modifiers = VirtualKeyModifiers.Windows
            };
            closeAccelerator.Invoked += async (_, ev) =>
            {
                ev.Handled = true;
                if (App.CanClose)
                {
                    await App.Services.GetRequiredService<IWindowService>().RequestCloseAsync();
                }
            };
            accelerators.Add(closeAccelerator);
        }

        return accelerators.ToArray();
    }
}
