using Sentry.CrashReporter.Controls;
using Sentry.CrashReporter.Converters;
using Sentry.CrashReporter.ViewModels;

namespace Sentry.CrashReporter.Views;

public sealed class HeaderView : ReactiveUserControl<HeaderViewModel>
{
    public HeaderView()
    {
        this.DataContext(new HeaderViewModel(), (view, vm) => view
            .Content(new Grid()
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
                                        .ToolTip(x => x.Binding(() => vm.Exception).Convert(e => e?.Value ?? "Exception"))
                                        .Text(x => x.Binding(() => vm.Exception).Convert(e => e?.Type ?? string.Empty))
                                        .Visibility(x => x.Binding(() => vm.Exception).Convert(e => BindingConverter.ToVisibility(e?.Type))),
                                    new IconLabel(FA.Globe)
                                        .Margin(8, 4)
                                        .Name("releaseLabel")
                                        .ToolTip("Release")
                                        .Text(x => x.Binding(() => vm.Release))
                                        .Visibility(x => x.Binding(() => vm.Release).Convert(BindingConverter.ToVisibility)),
                                    new IconLabel()
                                        .Margin(8, 4)
                                        .Name("osLabel")
                                        .Brand(x => x.Binding(() => vm.OsName).Convert(ToBrand))
                                        .ToolTip("Operating System")
                                        .Text(x => x.Binding(() => vm.OsPretty))
                                        .Visibility(x => x.Binding(() => vm.OsPretty).Convert(BindingConverter.ToVisibility)),
                                    new IconLabel(FA.Wrench)
                                        .Margin(8, 4)
                                        .Name("environmentLabel")
                                        .ToolTip("Environment")
                                        .Text(x => x.Binding(() => vm.Environment))
                                        .Visibility(x => x.Binding(() => vm.Environment).Convert(BindingConverter.ToVisibility)))),
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
