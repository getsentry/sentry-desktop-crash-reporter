using Sentry.CrashReporter.Controls;
using Sentry.CrashReporter.ViewModels;

namespace Sentry.CrashReporter.Views;

public sealed class FooterView : ReactiveUserControl<FooterViewModel>
{
    public FooterView()
    {
        this.DataContext(new FooterViewModel(), (view, vm) => view
            .Content(new Grid()
                .ColumnSpacing(8)
                .ColumnDefinitions("Auto,*,Auto,Auto")
                .Children(
                    new IconLabel(FA.Copy)
                        .ToolTip("Event ID")
                        .Name("eventIdLabel")
                        .Text(x => x.Binding(() => vm.ShortEventId))
                        .Visibility(x => x.Binding(() => vm.ShortEventId).Convert(ToVisibility))
                        .Grid(0),
                    new Button { Content = "Cancel" }
                        .Grid(2)
                        .Name("cancelButton")
                        .Command(x => x.Binding(() => vm.CancelCommand))
                        .Background(Colors.Transparent),
                    new Button { Content = "Submit" }
                        .Grid(3)
                        .Name("submitButton")
                        .AutomationProperties(automationId: "submitButton")
                        .Command(x => x.Binding(() => vm.SubmitCommand))
                        .Foreground(Colors.White)
                        .Background(ThemeResource.Get<Brush>("SystemAccentColorBrush")))));
    }

    private static Visibility ToVisibility(string? obj)
    {
        return string.IsNullOrEmpty(obj) ? Visibility.Collapsed : Visibility.Visible;
    }
}
