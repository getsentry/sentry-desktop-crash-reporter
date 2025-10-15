using System.Runtime.InteropServices.WindowsRuntime;
using System.Text.Json.Nodes;
using Windows.Storage.Streams;
using Microsoft.UI.Xaml.Media.Imaging;
using Sentry.CrashReporter.Controls;
using Sentry.CrashReporter.Extensions;
using Sentry.CrashReporter.ViewModels;

namespace Sentry.CrashReporter.Views;

public sealed class EventView : ReactiveUserControl<EventViewModel>
{
    public EventView()
    {
        this.DataContext(new EventViewModel(), (view, vm) => view
            .Content(new ScrollViewer()
                .Content(new StackPanel()
                    .Orientation(Orientation.Vertical)
                    .Spacing(8)
                    .Children(
                        new Expander()
                            .Header("Tags")
                            .HorizontalAlignment(HorizontalAlignment.Stretch)
                            .HorizontalContentAlignment(HorizontalAlignment.Stretch)
                            .IsExpanded(x => x.Binding(() => vm.Tags).Convert(IsNotNullOrEmpty))
                            .IsEnabled(x => x.Binding(() => vm.Tags).Convert(IsNotNullOrEmpty))
                            .Content(new JsonGrid()
                                .Data(x => x.Binding(() => vm.Tags))),
                        new Expander()
                            .Header("Contexts")
                            .HorizontalAlignment(HorizontalAlignment.Stretch)
                            .HorizontalContentAlignment(HorizontalAlignment.Stretch)
                            .IsExpanded(x => x.Binding(() => vm.Contexts).Convert(IsNotNullOrEmpty))
                            .IsEnabled(x => x.Binding(() => vm.Contexts).Convert(IsNotNullOrEmpty))
                            .Content(new JsonGrid()
                                .Data(x => x.Binding(() => vm.Contexts))),
                        new Expander()
                            .Header("Additional Data")
                            .HorizontalAlignment(HorizontalAlignment.Stretch)
                            .HorizontalContentAlignment(HorizontalAlignment.Stretch)
                            .IsEnabled(x => x.Binding(() => vm.Extra).Convert(IsNotNullOrEmpty))
                            .Visibility(x => x.Binding(() => vm.Extra).Convert(ToVisibility))
                            .Content(new JsonGrid()
                                .Data(x => x.Binding(() => vm.Extra))),
                        new Expander()
                            .Header("SDK")
                            .HorizontalAlignment(HorizontalAlignment.Stretch)
                            .HorizontalContentAlignment(HorizontalAlignment.Stretch)
                            .IsExpanded(x => x.Binding(() => vm.Sdk).Convert(IsNotNullOrEmpty))
                            .IsEnabled(x => x.Binding(() => vm.Sdk).Convert(IsNotNullOrEmpty))
                            .Content(new JsonGrid()
                                .Data(x => x.Binding(() => vm.Sdk)))))));
    }

    private static Visibility ToVisibility(object? obj)
    {
        return IsNotNullOrEmpty(obj) ? Visibility.Visible : Visibility.Collapsed;
    }

    private static bool IsNotNullOrEmpty(object? obj)
    {
        return obj switch
        {
            JsonObject json => json.Count > 0,
            _ => obj is not null
        };
    }
}
