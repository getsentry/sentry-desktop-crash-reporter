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
                                    .Children(new TextBlock()
                                            .Grid(column: 0)
                                            .Style(ThemeResource.Get<Style>("SubtitleTextBlockStyle"))
                                            .Text(x => x.Binding(() => vm.Subtitle)),
                                        new Segmented()
                                            .Name(out var segmented)
                                            .Grid(column: 1)
                                            .Visibility(x => x.Binding(() => vm.Error).Converter(ErrorToCollapsed))
                                            .SelectedIndex(x => x.Binding(() => vm.SelectedIndex).TwoWay())
                                            .Items(
                                                new SegmentedItem()
                                                    .Content(new FontAwesomeIcon(FA.CommentDots))
                                                    .ToolTip("Feedback")
                                                    .Navigation(request: "feedback"),
                                                new SegmentedItem()
                                                    .Content(new FontAwesomeIcon(FA.Tags))
                                                    .ToolTip("Tags")
                                                    .Visibility(x => x.Binding(() => vm.Tags).Converter(ToVisibility))
                                                    .Navigation(request: "tags"),
                                                new SegmentedItem()
                                                    .Content(new FontAwesomeIcon(FA.Hashtag))
                                                    .ToolTip("Contexts")
                                                    .Visibility(x => x.Binding(() => vm.Contexts).Converter(ToVisibility))
                                                    .Navigation(request: "contexts"),
                                                new SegmentedItem()
                                                    .Content(new FontAwesomeIcon(FA.Table))
                                                    .ToolTip("Additional Data")
                                                    .Visibility(x => x.Binding(() => vm.Extra).Converter(ToVisibility))
                                                    .Navigation(request: "extra"),
                                                new SegmentedItem()
                                                    .Content(new FontAwesomeIcon(FA.Cubes))
                                                    .ToolTip("SDK")
                                                    .Visibility(x => x.Binding(() => vm.Sdk).Converter(ToVisibility))
                                                    .Navigation(request: "sdk"),
                                                new SegmentedItem()
                                                    .Content(new FontAwesomeIcon(FA.User))
                                                    .ToolTip("User")
                                                    .Visibility(x => x.Binding(() => vm.User).Converter(ToVisibility))
                                                    .Navigation(request: "user"),
                                                new SegmentedItem()
                                                    .Content(new FontAwesomeIcon(FA.Paperclip))
                                                    .ToolTip("Attachments")
                                                    .Visibility(x => x.Binding(() => vm.Attachments).Converter(ToVisibility))
                                                    .Navigation(request: "attachment"),
                                                new SegmentedItem()
                                                    .Content(new FontAwesomeIcon(FA.Code))
                                                    .ToolTip("Envelope")
                                                    .Navigation(request: "envelope"))),
                                new Grid()
                                    .Grid(row: 2)
                                    .Children(
                                        new ErrorView()
                                            .Error(x => x.Binding(() => vm.Error))
                                            .Visibility(x => x.Binding(() => vm.Error).Converter(ErrorToVisible)),
                                        new Grid()
                                            .Region(attached: true, navigator: "Visibility")
                                            .Visibility(x => x.Binding(() => vm.Error).Converter(ErrorToCollapsed))
                                            .Children(
                                                new FeedbackView()
                                                    .Region(name: "feedback")
                                                    .Visibility(Visibility.Visible)
                                                    .Envelope(x => x.Binding(() => vm.Envelope)),
                                                new JsonGrid()
                                                    .Region(name: "tags")
                                                    .Visibility(Visibility.Collapsed)
                                                    .Data(x => x.Binding(() => vm.Tags)),
                                                new JsonGrid()
                                                    .Region(name: "contexts")
                                                    .Visibility(Visibility.Collapsed)
                                                    .Data(x => x.Binding(() => vm.Contexts)),
                                                new JsonGrid()
                                                    .Region(name: "extra")
                                                    .Visibility(Visibility.Collapsed)
                                                    .Data(x => x.Binding(() => vm.Extra)),
                                                new JsonGrid()
                                                    .Region(name: "sdk")
                                                    .Visibility(Visibility.Collapsed)
                                                    .Data(x => x.Binding(() => vm.Sdk)),
                                                new JsonGrid()
                                                    .Region(name: "user")
                                                    .Visibility(Visibility.Collapsed)
                                                    .Data(x => x.Binding(() => vm.User)),
                                                new AttachmentView()
                                                    .Region(name: "attachment")
                                                    .Visibility(Visibility.Collapsed)
                                                    .Envelope(x => x.Binding(() => vm.Envelope)),
                                                new EnvelopeView()
                                                    .Region(name: "envelope")
                                                    .Visibility(Visibility.Collapsed)
                                                    .Envelope(x => x.Binding(() => vm.Envelope)))),
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
