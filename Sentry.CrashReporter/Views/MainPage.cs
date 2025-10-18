using CommunityToolkit.WinUI.Converters;
using Sentry.CrashReporter.Controls;
using Sentry.CrashReporter.Extensions;
using Sentry.CrashReporter.ViewModels;

namespace Sentry.CrashReporter.Views;

public sealed class MainPage : Page
{
    private static readonly CollectionVisibilityConverter ToVisibility = new();

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
                                    .Grid(row: 0),
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
                                            .SelectedIndex(x => x.Binding(() => vm.SelectedIndex).TwoWay())
                                            .Items(
                                                new SegmentedItem()
                                                    .Content(new FontAwesomeIcon(FA.CommentDots))
                                                    .ToolTip("Feedback")
                                                    .Navigation(request: "feedback"),
                                                new SegmentedItem()
                                                    .Content(new FontAwesomeIcon(FA.Bug))
                                                    .ToolTip("Event")
                                                    .Navigation(request: "event"),
                                                new SegmentedItem()
                                                    .Content(new FontAwesomeIcon(FA.Paperclip))
                                                    .ToolTip("Attachments")
                                                    .Visibility(x => x.Binding(() => vm.Attachments)
                                                        .Converter(ToVisibility))
                                                    .Navigation(request: "attachment"),
                                                new SegmentedItem()
                                                    .Content(new FontAwesomeIcon(FA.Code))
                                                    .ToolTip("Envelope")
                                                    .Navigation(request: "envelope"))),
                                new Grid()
                                    .Region(attached: true, navigator: "Visibility")
                                    .Grid(row: 2)
                                    .Children(
                                        new FeedbackView()
                                            .Region(name: "feedback")
                                            .Visibility(Visibility.Visible),
                                        new EventView()
                                            .Region(name: "event")
                                            .Visibility(Visibility.Collapsed),
                                        new AttachmentView()
                                            .Region(name: "attachment")
                                            .Visibility(Visibility.Collapsed),
                                        new EnvelopeView()
                                            .Region(name: "envelope")
                                            .Visibility(Visibility.Collapsed)),
                                new FooterView()
                                    .Grid(row: 3))))));
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
