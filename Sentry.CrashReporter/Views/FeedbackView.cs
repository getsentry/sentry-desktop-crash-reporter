using Sentry.CrashReporter.ViewModels;

namespace Sentry.CrashReporter.Views;

public sealed class FeedbackView : ReactiveUserControl<FeedbackViewModel>
{
    public static readonly DependencyProperty EnvelopeProperty = DependencyProperty.Register(
        nameof(Envelope), typeof(Envelope), typeof(FeedbackView), new PropertyMetadata(null));

    public Envelope? Envelope
    {
        get => (Envelope)GetValue(EnvelopeProperty);
        set => SetValue(EnvelopeProperty, value);
    }

    public FeedbackView()
    {
        ViewModel = new FeedbackViewModel();
        this.WhenActivated(d =>
        {
            this.WhenAnyValue(v => v.Envelope)
                .BindTo(ViewModel, vm => vm.Envelope)
                .DisposeWith(d);
        });

        this.Content(new ScrollViewer()
            .DataContext(ViewModel, (view, vm) => view
                .Content(new Grid()
                    .RowSpacing(8)
                    .RowDefinitions("Auto,Auto,Auto,*")
                    .Children(
                        new TextBox()
                            .PlaceholderText("Name")
                            .AutomationProperties(automationId: "nameTextBox")
                            .IsEnabled(x => x.Binding(() => vm.IsEnabled))
                            .Text(x => x.Binding(() => vm.Name).TwoWay())
                            .Grid(row: 1),
                        new TextBox()
                            .PlaceholderText("Email")
                            .AutomationProperties(automationId: "emailTextBox")
                            .IsEnabled(x => x.Binding(() => vm.IsEnabled))
                            .Text(x => x.Binding(() => vm.Email).TwoWay())
                            .Grid(row: 2),
                        new TextBox()
                            .PlaceholderText("Message")
                            .AutomationProperties(automationId: "messageTextBox")
                            .AcceptsReturn(true)
                            .TextWrapping(TextWrapping.Wrap)
                            .Text(x => x.Binding(() => vm.Message).TwoWay())
                            .IsEnabled(x => x.Binding(() => vm.IsEnabled))
                            .VerticalAlignment(VerticalAlignment.Stretch)
                            .Grid(row: 3)))));
    }
}
