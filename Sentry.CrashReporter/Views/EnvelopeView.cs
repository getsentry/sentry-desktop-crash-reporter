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
                        .Margin(4, 2)
                        .Content(new FontAwesomeIcon(FA.ArrowUpRightFromSquare).FontSize(12))
                        .Background(Colors.Transparent)
                        .BorderBrush(Colors.Transparent)
                        .Resources(r => r.Add("ButtonBackgroundPointerOver", new SolidColorBrush(Colors.Transparent))
                            .Add("ButtonBackgroundPressed", new SolidColorBrush(Colors.Transparent))
                            .Add("ButtonBackgroundDisabled", new SolidColorBrush(Colors.Transparent))
                            .Add("ButtonBorderBrushPointerOver", new SolidColorBrush(Colors.Transparent))
                            .Add("ButtonBorderBrushPressed", new SolidColorBrush(Colors.Transparent))
                            .Add("ButtonBorderBrushDisabled", new SolidColorBrush(Colors.Transparent)))
                        .Command(() => vm.LaunchCommand))));
    }
}
