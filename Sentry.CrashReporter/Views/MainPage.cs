using CommunityToolkit.WinUI.Converters;
using Sentry.CrashReporter.Controls;
using Sentry.CrashReporter.Converters;
using Sentry.CrashReporter.Extensions;
using Sentry.CrashReporter.ViewModels;

namespace Sentry.CrashReporter.Views;

public sealed class MainPage : Page
{
    private static readonly CollectionVisibilityConverter ToVisibility = new();
    private static readonly EmptyObjectToObjectConverter ErrorToVisible = new()
    {
        EmptyValue = Visibility.Collapsed,
        NotEmptyValue = Visibility.Visible
    };
    private static readonly EmptyObjectToObjectConverter ErrorToCollapsed = new()
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
            Builder: vm => new FeedbackView().Envelope(x => x.Binding(() => vm.Envelope))
        ),
        new(
            Title: "Tags",
            Icon: FA.Tags,
            ToolTip: "Tags",
            Region: "tags",
            Visibility: (vm, x) => x.Binding(() => vm.Tags).Converter(ToVisibility),
            Builder: vm => new JsonGrid().Data(x => x.Binding(() => vm.Tags))
        ),
        new(
            Title: "Contexts",
            Icon: FA.Hashtag,
            ToolTip: "Contexts",
            Region: "contexts",
            Visibility: (vm, x) => x.Binding(() => vm.Contexts).Converter(ToVisibility),
            Builder: vm => new JsonGrid().Data(x => x.Binding(() => vm.Contexts))
        ),
        new(
            Title: "Additional Data",
            Icon: FA.Table,
            ToolTip: "Additional Data",
            Region: "extra",
            Visibility: (vm, x) => x.Binding(() => vm.Extra).Converter(ToVisibility),
            Builder: vm => new JsonGrid().Data(x => x.Binding(() => vm.Extra))
        ),
        new(
            Title: "SDK",
            Icon: FA.Cubes,
            ToolTip: "SDK",
            Region: "sdk",
            Visibility: (vm, x) => x.Binding(() => vm.Sdk).Converter(ToVisibility),
            Builder: vm => new JsonGrid().Data(x => x.Binding(() => vm.Sdk))
        ),
        new(
            Title: "User",
            Icon: FA.User,
            ToolTip: "User",
            Region: "user",
            Visibility: (vm, x) => x.Binding(() => vm.User).Converter(ToVisibility),
            Builder: vm => new JsonGrid().Data(x => x.Binding(() => vm.User))
        ),
        new(
            Title: "Attachments",
            Icon: FA.Paperclip,
            ToolTip: "Attachments",
            Region: "attachments",
            Visibility: (vm, x) => x.Binding(() => vm.Attachments).Converter(ToVisibility),
            Builder: vm => new AttachmentView().Envelope(x => x.Binding(() => vm.Envelope))
        ),
        new(
            Title: "Envelope",
            Icon: FA.Code,
            ToolTip: "Envelope",
            Region: "envelope",
            Visibility: (_, x) => x.Binding(() => Visibility.Visible),
            Builder: vm => new EnvelopeView().Envelope(x => x.Binding(() => vm.Envelope))
        )
    ];

    public MainPage()
    {
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
                            .Padding(new Thickness(16))
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
                                            .Text(x => x.Binding(() => vm.SelectedIndex).Convert(i => Views[i].Title)),
                                        new Segmented()
                                            .Grid(column: 1)
                                            .Visibility(x => x.Binding(() => vm.Error).Converter(ErrorToCollapsed))
                                            .SelectedIndex(x => x.Binding(() => vm.SelectedIndex).TwoWay())
                                            .Items(Views.Select(v => new SegmentedItem()
                                                    .Content(new FontAwesomeIcon(v.Icon))
                                                    .ToolTip(v.ToolTip)
                                                    .Navigation(request: v.Region)
                                                    .Visibility(x => v.Visibility(vm, x))).ToArray<object>())),
                                new Grid()
                                    .Grid(row: 2)
                                    .Children(
                                        new ErrorView()
                                            .Error(x => x.Binding(() => vm.Error))
                                            .Visibility(x => x.Binding(() => vm.Error).Converter(ErrorToVisible)),
                                        new Grid()
                                            .Region(attached: true, navigator: "Visibility")
                                            .Visibility(x => x.Binding(() => vm.Error).Converter(ErrorToCollapsed))
                                            .Children(Views.Select((v, i) => v.Builder(vm)
                                                .Region(name: v.Region)
                                                .Visibility(x => x.Binding(() => vm.SelectedIndex)
                                                    .Convert(s => s == i? Visibility.Visible : Visibility.Collapsed))).ToArray())),
                                new FooterView()
                                    .Grid(row: 3)
                                    .Envelope(x => x.Binding(() => vm.Envelope)))))));
    }

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
            closeAccelerator.Invoked += (_, ev) =>
            {
                ev.Handled = true;
                ((App)Application.Current)?.MainWindow?.Close();
            };
            accelerators.Add(closeAccelerator);
        }

        return accelerators.ToArray();
    }
}
