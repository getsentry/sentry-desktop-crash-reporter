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

    private TextBox? _messageTextBox;

    public FeedbackView()
    {
        ViewModel = new FeedbackViewModel();
        this.WhenActivated(d =>
        {
            this.WhenAnyValue(v => v.Envelope)
                .BindTo(ViewModel, vm => vm.Envelope)
                .DisposeWith(d);

            this.WhenAnyValue(vm => vm.ViewModel!.IsAvailable)
                .Where(isAvailable => isAvailable && _messageTextBox?.IsLoaded is true)
                .Take(1)
                .Subscribe(async _ => await FocusManager.TryFocusAsync(_messageTextBox!, FocusState.Keyboard))
                .DisposeWith(d);
        });

        this.Content(new ScrollViewer()
            .DataContext(ViewModel, (view, vm) => view
                .Content(new Grid()
                    .RowSpacing(16)
                    .RowDefinitions("*,Auto,Auto")
                    .Children(
                        new TextBox()
                            .Name(out _messageTextBox)
                            .Grid(row: 0)
                            .Header("Message")
                            .PlaceholderText("Tell us about your issue")
                            .AutomationProperties(automationId: "messageTextBox")
                            .MaxLength(4096)
                            .AcceptsReturn(true)
                            .TextWrapping(TextWrapping.Wrap)
                            .IsEnabled(x => x.Binding(() => vm.IsAvailable))
                            .Text(x => x.Binding(() => vm.Message).TwoWay().UpdateSourceTrigger(UpdateSourceTrigger.PropertyChanged))
                            .VerticalAlignment(VerticalAlignment.Stretch),
                        new TextBox()
                            .Grid(row: 1)
                            .Header("Name")
                            .PlaceholderText("Your name (optional)")
                            .AutomationProperties(automationId: "nameTextBox")
                            .IsEnabled(x => x.Binding(() => vm.IsEnabled))
                            .Text(x => x.Binding(() => vm.Name).TwoWay().UpdateSourceTrigger(UpdateSourceTrigger.PropertyChanged)),
                        new TextBox()
                            .Grid(row: 2)
                            .Header("Email")
                            .PlaceholderText("Contact email (optional)")
                            .AutomationProperties(automationId: "emailTextBox")
                            .IsEnabled(x => x.Binding(() => vm.IsEnabled))
                            .Text(x => x.Binding(() => vm.Email).TwoWay().UpdateSourceTrigger(UpdateSourceTrigger.PropertyChanged))))));
    }
}
