using Sentry.CrashReporter.Controls;
using Sentry.CrashReporter.Converters;
using Sentry.CrashReporter.ViewModels;

namespace Sentry.CrashReporter.Views;

public sealed class EventView : ReactiveUserControl<EventViewModel>
{
    private static readonly JsonToBoolConverter ToBoolean = new();

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
                            .IsExpanded(x => x.Binding(() => vm.Tags).Converter(ToBoolean))
                            .IsEnabled(x => x.Binding(() => vm.Tags).Converter(ToBoolean))
                            .Content(new JsonGrid().Data(x => x.Binding(() => vm.Tags))),
                        new Expander()
                            .Header("Contexts")
                            .HorizontalAlignment(HorizontalAlignment.Stretch)
                            .HorizontalContentAlignment(HorizontalAlignment.Stretch)
                            .IsExpanded(x => x.Binding(() => vm.Contexts).Converter(ToBoolean))
                            .IsEnabled(x => x.Binding(() => vm.Contexts).Converter(ToBoolean))
                            .Content(new JsonGrid().Data(x => x.Binding(() => vm.Contexts))),
                        new Expander()
                            .Header("Additional Data")
                            .HorizontalAlignment(HorizontalAlignment.Stretch)
                            .HorizontalContentAlignment(HorizontalAlignment.Stretch)
                            .IsEnabled(x => x.Binding(() => vm.Extra).Converter(ToBoolean))
                            .Visibility(x => x.Binding(() => vm.Extra).Converter(ToBoolean))
                            .Content(new JsonGrid().Data(x => x.Binding(() => vm.Extra))),
                        new Expander()
                            .Header("SDK")
                            .HorizontalAlignment(HorizontalAlignment.Stretch)
                            .HorizontalContentAlignment(HorizontalAlignment.Stretch)
                            .IsExpanded(x => x.Binding(() => vm.Sdk).Converter(ToBoolean))
                            .IsEnabled(x => x.Binding(() => vm.Sdk).Converter(ToBoolean))
                            .Content(new JsonGrid().Data(x => x.Binding(() => vm.Sdk)))))));
    }
}
