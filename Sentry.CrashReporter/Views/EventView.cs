using Sentry.CrashReporter.Controls;
using Sentry.CrashReporter.Converters;
using Sentry.CrashReporter.ViewModels;

namespace Sentry.CrashReporter.Views;

public sealed class EventView : ReactiveUserControl<EventViewModel>
{
    public static readonly DependencyProperty EnvelopeProperty = DependencyProperty.Register(
        nameof(Envelope), typeof(Envelope), typeof(EventView), new PropertyMetadata(null));

    public Envelope? Envelope
    {
        get => (Envelope)GetValue(EnvelopeProperty);
        set => SetValue(EnvelopeProperty, value);
    }

    private static readonly JsonToBoolConverter ToBoolean = new();

    public EventView()
    {
        ViewModel = new EventViewModel();
        this.WhenActivated(d =>
        {
            this.WhenAnyValue(v => v.Envelope)
                .BindTo(ViewModel, vm => vm.Envelope)
                .DisposeWith(d);
        });

        this.Content(new ScrollViewer()
            .DataContext(ViewModel, (view, vm) => view 
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
