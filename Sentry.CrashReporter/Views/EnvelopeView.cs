using Sentry.CrashReporter.Controls;
using Sentry.CrashReporter.Extensions;
using Sentry.CrashReporter.ViewModels;

namespace Sentry.CrashReporter.Views;

public sealed class EnvelopeView : ReactivePage<EnvelopeViewModel>
{
    public static readonly DependencyProperty EnvelopeProperty = DependencyProperty.Register(
        nameof(Envelope), typeof(Envelope), typeof(EnvelopeView), new PropertyMetadata(null));

    public Envelope? Envelope
    {
        get => (Envelope)GetValue(EnvelopeProperty);
        set => SetValue(EnvelopeProperty, value);
    }

    public EnvelopeView()
    {
        ViewModel = new EnvelopeViewModel();
        this.WhenActivated(d =>
        {
            this.WhenAnyValue(v => v.Envelope)
                .BindTo(ViewModel, vm => vm.Envelope)
                .DisposeWith(d);
        });

        this.Background(ThemeResource.Get<Brush>("ApplicationPageBackgroundThemeBrush"))
            .Content(new Grid()
                .DataContext(ViewModel, (view, vm) => view
                .Children(
                    new ScrollViewer()
                        .Grid(row: 1)
                        .Content(new StackPanel()
                            .Children(
                                new TextBlock()
                                    .WithTextSelection()
                                    .WithSourceCodePro()
                                    .Margin(0, 0, 0, 16)
                                    .ToolTip( b => b.ToolTip(() => vm.FilePath))
                                    .Text(x => x.Binding(() => vm.FileName).Convert(fn => $"{fn}:")),
                                new Border()
                                    .Padding(ThemeResource.Get<Thickness>("ExpanderHeaderPadding"))
                                    .Background(ThemeResource.Get<Brush>("ExpanderContentBackground"))
                                    .BorderBrush(ThemeResource.Get<Brush>("ExpanderHeaderBorderBrush"))
                                    .BorderThickness(ThemeResource.Get<Thickness>("ExpanderHeaderBorderThickness"))
                                    .CornerRadius(ThemeResource.Get<CornerRadius>("ControlCornerRadius"))
                                    .Child(new TextBlock()
                                        .WithTextSelection()
                                        .WithSourceCodePro()
                                        .Text(x => x.Binding(() => vm.Formatted?.Header))),
                                new ItemsControl()
                                    .ItemsSource(x => x.Binding(() => vm.Formatted?.Items))
                                    .ItemTemplate(() =>
                                        new Expander()
                                            .HorizontalAlignment(HorizontalAlignment.Stretch)
                                            .HorizontalContentAlignment(HorizontalAlignment.Stretch)
                                            .Header(new TextBlock()
                                                .WithTextSelection()
                                                .WithSourceCodePro()
                                                .Text(x => x.Binding("Header"))
                                            )
                                            .Content(new TextBlock()
                                                .WithTextSelection()
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
