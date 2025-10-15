using Sentry.CrashReporter.Controls;
using Sentry.CrashReporter.Extensions;
using Sentry.CrashReporter.ViewModels;

namespace Sentry.CrashReporter.Views;

public sealed class MainPage : Page
{
    public MainPage()
    {
        this.DataContext<MainViewModel>((view, vm) => view
            .NavigationCacheMode(NavigationCacheMode.Required)
            .Background(ThemeResource.Get<Brush>("ApplicationPageBackgroundThemeBrush"))
            .Content(new LoadingView()
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
                                                .Convert(a => (a?.Count ?? 0) > 0 ? Visibility.Visible : Visibility.Collapsed))
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
                            .Grid(row: 3)))));
    }
}
