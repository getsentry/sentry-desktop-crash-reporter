using Sentry.CrashReporter.Controls;
using Sentry.CrashReporter.Extensions;
using Sentry.CrashReporter.ViewModels;

namespace Sentry.CrashReporter.Views;

public sealed class EnvelopeView : ReactivePage<EnvelopeViewModel>
{
    public EnvelopeView()
    {
        this.DataContext(new EnvelopeViewModel(), (view, vm) => view
            .Background(ThemeResource.Get<Brush>("ApplicationPageBackgroundThemeBrush"))
            .Content(new Grid()
                .Children(
                    new ScrollViewer()
                        .Grid(row: 1)
                        .Content(new StackPanel()
                            .Children(
                                new SelectableTextBlock()
                                    .WithSourceCodePro()
                                    .Text(x => x.Binding(() => vm.Formatted?.Header)),
                                new ItemsControl()
                                    .ItemsSource(x => x.Binding(() => vm.Formatted?.Items))
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
                                                .Text(x => x.Binding("Payload")))))),
                    new Button()
                        .Grid(2)
                        .ToolTip("Open")
                        .VerticalAlignment(VerticalAlignment.Top)
                        .HorizontalAlignment(HorizontalAlignment.Right)
                        .Content(new FontAwesomeIcon(FA.Share))
                        .Command(() => vm.LaunchCommand))));
    }
}
