using Sentry.CrashReporter.ViewModels;

namespace Sentry.CrashReporter.Views;

public sealed class FeedbackView : ReactiveUserControl<FeedbackViewModel>
{
    public FeedbackView()
    {
        this.DataContext(new FeedbackViewModel(), (view, vm) => view
            .Content(new Grid()
                .RowSpacing(8)
                .RowDefinitions("Auto,Auto,Auto,*")
                .Children(
                    new TextBlock()
                        .Text("Feedback (optional)")
                        .Style(ThemeResource.Get<Style>("SubtitleTextBlockStyle")),
                    new TextBox()
                        .PlaceholderText("Name")
                        .IsEnabled(x => x.Binding(() => vm.IsEnabled))
                        .Text(x => x.Binding(() => vm.Name).TwoWay())
                        .Grid(row: 1),
                    new TextBox()
                        .PlaceholderText("Email")
                        .IsEnabled(x => x.Binding(() => vm.IsEnabled))
                        .Text(x => x.Binding(() => vm.Email).TwoWay())
                        .Grid(row: 2),
                    new TextBox()
                        .PlaceholderText("Description")
                        .AcceptsReturn(true)
                        .TextWrapping(TextWrapping.Wrap)
                        .Text(x => x.Binding(() => vm.Description).TwoWay())
                        .IsEnabled(x => x.Binding(() => vm.IsEnabled))
                        .VerticalAlignment(VerticalAlignment.Stretch)
                        .Grid(row: 3))));
    }
}
