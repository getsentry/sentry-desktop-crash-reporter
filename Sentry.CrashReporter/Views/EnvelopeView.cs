using Sentry.CrashReporter.Controls;
using Sentry.CrashReporter.Extensions;
using Sentry.CrashReporter.ViewModels;

namespace Sentry.CrashReporter.Views;

public sealed class EnvelopeView : Page
{
    public EnvelopeView()
    {
        this.DataContext(new EnvelopeViewModel(), (view, vm) => view
            .Background(ThemeResource.Get<Brush>("ApplicationPageBackgroundThemeBrush"))
            .Content(new Grid()
                .RowDefinitions("Auto,*")
                .Children(
                    new Grid()
                        .Grid(row: 0)
                        .ColumnDefinitions("Auto,*,Auto")
                        .ColumnSpacing(16)
                        .Children(
                            new AppBarButton()
                                .Grid(0)
                                .ToolTip("Back")
                                .Icon(new FontAwesomeIcon(FA.ArrowLeft))
                                .LabelPosition(CommandBarLabelPosition.Collapsed)
                                .Command(new RelayCommand(() => (Window.Current?.Content as Frame)?.GoBack())),
                            new StackPanel()
                                .Grid(1)
                                .VerticalAlignment(VerticalAlignment.Center)
                                .Children(
                                    new SelectableTextBlock()
                                        .Style(ThemeResource.Get<Style>("SubtitleTextBlockStyle"))
                                        .Text(x => x.Binding(() => vm.FileName)),
                                    new SelectableTextBlock()
                                        .Style(ThemeResource.Get<Style>("CaptionTextBlockStyle"))
                                        .Text(x => x.Binding(() => vm.Directory))),
                            new AppBarButton()
                                .Grid(2)
                                .ToolTip("Open")
                                .Icon(new FontAwesomeIcon(FA.Share))
                                .LabelPosition(CommandBarLabelPosition.Collapsed)
                                .Command(() => vm.LaunchCommand)),
                    new ScrollViewer()
                        .Grid(row: 1)
                        .Content(new StackPanel()
                            .Children(
                                new SelectableTextBlock()
                                    .WithSourceCodePro()
                                    .Text(x => x.Binding(() => vm.Header)),
                                new ItemsControl()
                                    .ItemsSource(x => x.Binding(() => vm.Items))
                                    .ItemTemplate(() =>
                                        new Expander()
                                            .HorizontalAlignment(HorizontalAlignment.Stretch)
                                            .HorizontalContentAlignment(HorizontalAlignment.Stretch)
                                            .Header(new SelectableTextBlock()
                                                .WithSourceCodePro()
                                                .Text(x => x.Binding("Header"))
                                            )
                                            .Content(new SelectableTextBlock()
                                                .WithSourceCodePro()
                                                .Text(x => x.Binding("Payload")))))))));
    }
}
