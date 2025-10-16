using System.Runtime.InteropServices.WindowsRuntime;
using System.Text.Json.Nodes;
using Windows.Storage.Streams;
using Microsoft.UI.Xaml.Media.Imaging;
using Sentry.CrashReporter.Controls;
using Sentry.CrashReporter.Converters;
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
                            .IsExpanded(x => x.Binding(() => vm.Tags).Convert(BindingConverter.ToEnabled))
                            .IsEnabled(x => x.Binding(() => vm.Tags).Convert(BindingConverter.ToEnabled))
                            .Content(new JsonGrid()
                                .Data(x => x.Binding(() => vm.Tags))),
                        new Expander()
                            .Header("Contexts")
                            .HorizontalAlignment(HorizontalAlignment.Stretch)
                            .HorizontalContentAlignment(HorizontalAlignment.Stretch)
                            .IsExpanded(x => x.Binding(() => vm.Contexts).Convert(BindingConverter.ToEnabled))
                            .IsEnabled(x => x.Binding(() => vm.Contexts).Convert(BindingConverter.ToEnabled))
                            .Content(new JsonGrid()
                                .Data(x => x.Binding(() => vm.Contexts))),
                        new Expander()
                            .Header("Additional Data")
                            .HorizontalAlignment(HorizontalAlignment.Stretch)
                            .HorizontalContentAlignment(HorizontalAlignment.Stretch)
                            .IsEnabled(x => x.Binding(() => vm.Extra).Convert(BindingConverter.ToEnabled))
                            .Visibility(x => x.Binding(() => vm.Extra).Convert(BindingConverter.ToVisibility))
                            .Content(new JsonGrid()
                                .Data(x => x.Binding(() => vm.Extra))),
                        new Expander()
                            .Header("SDK")
                            .HorizontalAlignment(HorizontalAlignment.Stretch)
                            .HorizontalContentAlignment(HorizontalAlignment.Stretch)
                            .IsExpanded(x => x.Binding(() => vm.Sdk).Convert(BindingConverter.ToEnabled))
                            .IsEnabled(x => x.Binding(() => vm.Sdk).Convert(BindingConverter.ToEnabled))
                            .Content(new JsonGrid()
                                .Data(x => x.Binding(() => vm.Sdk)))))));
    }
}
