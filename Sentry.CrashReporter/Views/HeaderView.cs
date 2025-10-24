using CommunityToolkit.WinUI.Converters;
using Sentry.CrashReporter.Controls;
using Sentry.CrashReporter.Extensions;
using Sentry.CrashReporter.ViewModels;

namespace Sentry.CrashReporter.Views;

public sealed class HeaderView : ReactiveUserControl<HeaderViewModel>
{
    public static readonly DependencyProperty EnvelopeProperty = DependencyProperty.Register(
        nameof(Envelope), typeof(Envelope), typeof(HeaderView), new PropertyMetadata(null));

    public Envelope? Envelope
    {
        get => (Envelope)GetValue(EnvelopeProperty);
        set => SetValue(EnvelopeProperty, value);
    }

    private static readonly StringVisibilityConverter ToVisibility = new();

    public HeaderView()
    {
        ViewModel = new HeaderViewModel();
        this.WhenActivated(d =>
        {
            this.WhenAnyValue(v => v.Envelope)
                .BindTo(ViewModel, vm => vm.Envelope)
                .DisposeWith(d);
        });

        this.Content(new Grid()
            .DataContext(ViewModel, (view, vm) => view
                .ColumnDefinitions("*,Auto")
                .Children(
                    new StackPanel()
                        .Grid(0)
                        .Orientation(Orientation.Vertical)
                        .Spacing(8)
                        .Children(
                            new TextBlock()
                                .Text("Report a Bug")
                                .Style(ThemeResource.Get<Style>("TitleTextBlockStyle")),
                            new WrapPanel()
                                .Margin(-4, 0)
                                .Orientation(Orientation.Horizontal)
                                .Children(
                                    new IconLabel(FA.Bug)
                                        .Margin(8, 4)
                                        .Name("exceptionLabel")
                                        .ToolTip(b => b.ToolTip(() => vm.Exception, e => e?.Value ?? "Exception"))
                                        .Text(x => x.Binding(() => vm.Exception).Convert(e => e?.Type ?? string.Empty))
                                        .Visibility(x => x.Binding(() => vm.Exception)
                                            .Convert(e => string.IsNullOrEmpty(e?.Type) ? Visibility.Collapsed : Visibility.Visible)),
                                    new IconLabel(FA.Globe)
                                        .Margin(8, 4)
                                        .Name("releaseLabel")
                                        .ToolTip("Release")
                                        .Text(x => x.Binding(() => vm.Release))
                                        .Visibility(x => x.Binding(() => vm.Release).Converter(ToVisibility)),
                                    new IconLabel()
                                        .Margin(8, 4)
                                        .Name("osLabel")
                                        .Brand(x => x.Binding(() => vm.OsName).Convert(ToBrand))
                                        .ToolTip("Operating System")
                                        .Text(x => x.Binding(() => vm.OsPretty))
                                        .Visibility(x => x.Binding(() => vm.OsPretty).Converter(ToVisibility)),
                                    new IconLabel(FA.Wrench)
                                        .Margin(8, 4)
                                        .Name("environmentLabel")
                                        .ToolTip("Environment")
                                        .Text(x => x.Binding(() => vm.Environment))
                                        .Visibility(x => x.Binding(() => vm.Environment).Converter(ToVisibility)))),
                    new Image()
                        .Grid(1)
                        .Source(ThemeResource.Get<ImageSource>("SentryGlyphIcon"))
                        .Width(68)
                        .Height(60))));
    }

    private static string ToBrand(string? value)
    {
        return value?.ToLower() switch
        {
            "android" => FA.Android,
            "linux" => FA.Linux,
            "windows" => FA.Windows,
            "apple" or "macos" or "ios" or "tvos" or "visionos" or "watchos" => FA.Apple,
            _ => string.Empty
        };
    }
}
