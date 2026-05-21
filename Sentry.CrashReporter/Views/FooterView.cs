using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Sentry.CrashReporter.Controls;
using Sentry.CrashReporter.Extensions;
using Sentry.CrashReporter.Services;
using Sentry.CrashReporter.ViewModels;

namespace Sentry.CrashReporter.Views;

public sealed class FooterView : ReactiveUserControl<FooterViewModel>
{
    public static readonly DependencyProperty EnvelopeProperty = DependencyProperty.Register(
        nameof(Envelope), typeof(Envelope), typeof(FooterView), new PropertyMetadata(null));

    public Envelope? Envelope
    {
        get => (Envelope)GetValue(EnvelopeProperty);
        set => SetValue(EnvelopeProperty, value);
    }

    public FooterView()
    {
        ViewModel = new FooterViewModel();
        this.WhenActivated(d =>
        {
            this.WhenAnyValue(v => v.Envelope)
                .BindTo(ViewModel, vm => vm.Envelope)
                .DisposeWith(d);
        });

        this.Content(new UserControl()
            .DataContext(ViewModel, (view, vm) => view
                .Content(BuildFooterGrid(vm))));
    }

    private static bool CanCancel =>
        App.CanClose
        && !string.IsNullOrEmpty(Application.Current.Resources["CancelButton"]?.ToString());

    private static Grid BuildFooterGrid(FooterViewModel vm)
    {
        return new Grid()
            .ColumnSpacing(8)
            .ColumnDefinitions("*,Auto,Auto")
            .Children(
                new ContentControl()
                    .Grid(0)
                    .Content(x => x.Binding(() => vm.Status).Convert(status => BuildStatusLabelSafe(vm, status))),
                new Button()
                    .Grid(1)
                    .Content(x => x.StaticResource("CancelButton"))
                    .Name("cancelButton")
                    .Visibility(CanCancel ? Visibility.Visible : Visibility.Collapsed)
                    .Command(x => x.Binding(() => vm.CancelCommand))
                    .Background(Colors.Transparent),
                new Button()
                    .Grid(2)
                    .Content(x => x.StaticResource("SubmitButton"))
                    .Name("submitButton")
                    .AutomationProperties(automationId: "submitButton")
                    .Command(x => x.Binding(() => vm.SubmitCommand))
                    .Style(StaticResource.Get<Style>("AccentButtonStyle"))
                    .CornerRadius(ThemeResource.Get<CornerRadius>("ControlCornerRadius")));
    }

    private static UIElement BuildStatusPopupContent(
        FooterViewModel vm,
        Action close,
        FrameworkElement toastTarget)
    {
        var cacheSection = new StackPanel()
            .Spacing(8)
            .Visibility(x => x.Binding(() => vm.CanCache)
                .Convert(canCache => canCache ? Visibility.Visible : Visibility.Collapsed));
        cacheSection.Children.Add(BuildCacheSectionHeader(vm, close));
        cacheSection.Children.Add(BuildCacheSegmented(vm, close));

        return new StackPanel()
            .MaxWidth(360)
            .Spacing(12)
            .Children(
                BuildSectionHeader("Event ID", BuildCopyEventIdButton(vm, close, toastTarget)),
                BuildEventIdText(vm),
                cacheSection);
    }

    private static FrameworkElement BuildSectionHeader(string title, Button action)
    {
        return new Grid()
            .ColumnSpacing(8)
            .ColumnDefinitions("*,Auto")
            .Children(
                new TextBlock()
                    .Text(title)
                    .FontWeight(Microsoft.UI.Text.FontWeights.SemiBold)
                    .VerticalAlignment(VerticalAlignment.Center)
                    .Grid(0),
                action.Grid(1));
    }

    private static FrameworkElement BuildCacheSectionHeader(FooterViewModel vm, Action close)
    {
        return new Grid()
            .ColumnSpacing(6)
            .ColumnDefinitions("Auto,Auto,*,Auto")
            .Children(
                new TextBlock()
                    .Text("Cache")
                    .FontWeight(Microsoft.UI.Text.FontWeights.SemiBold)
                    .VerticalAlignment(VerticalAlignment.Center)
                    .Grid(0),
                BuildResetCacheKeepButton(vm)
                    .Grid(1)
                    .Visibility(x => x.Binding(() => vm.CanResetCacheKeep).Convert(ToVisibility)),
                BuildOpenCacheDirectoryButton(vm, close)
                    .Grid(3));
    }

    private static FrameworkElement BuildEventIdText(FooterViewModel vm)
    {
        return new TextBlock()
            .TextWrapping(TextWrapping.Wrap)
            .IsTextSelectionEnabled(true)
            .Text(x => x.Binding(() => vm.EventId));
    }

    private static Flyout BuildReportStatusFlyout(FooterViewModel vm, FrameworkElement toastTarget)
    {
        var flyout = new Flyout()
            .Placement(FlyoutPlacementMode.TopEdgeAlignedLeft);
        void Close() => flyout.Hide();
        flyout.Opening += (_, _) =>
        {
            try
            {
                flyout.Content = BuildStatusPopupContent(vm, Close, toastTarget);
            }
            catch
            {
                flyout.Content = new TextBlock()
                    .Text("Status details unavailable")
                    .TextWrapping(TextWrapping.Wrap);
            }
        };
        return flyout;
    }

    private static FrameworkElement BuildReportStatus(FooterViewModel vm)
    {
        return new StatusLabel()
            .HorizontalAlignment(HorizontalAlignment.Left)
            .VerticalAlignment(VerticalAlignment.Center)
            .MinHeight(24)
            .Text(x => x.Binding(() => vm.StatusText))
            .Icons(x => x.Binding(() => vm.StatusIcons));
    }

    private static Button BuildReportStatusButton(FooterViewModel vm)
    {
        var button = new Button()
            .Content(BuildReportStatus(vm))
            .HorizontalAlignment(HorizontalAlignment.Left)
            .VerticalAlignment(VerticalAlignment.Center)
            .MinWidth(0)
            .MinHeight(0)
            .Padding(new Thickness(8, 4))
            .BorderThickness(new Thickness(0))
            .Background(Colors.Transparent)
            .BorderBrush(Colors.Transparent);
        button.Flyout = BuildReportStatusFlyout(vm, button);
        return button;
    }

    private static Button BuildCopyEventIdButton(FooterViewModel vm, Action close, FrameworkElement toastTarget)
    {
        var button = new Button()
            .Content(new FontAwesomeIcon(FA.Copy).FontSize(12))
            .HorizontalAlignment(HorizontalAlignment.Left)
            .Width(28)
            .Height(28)
            .MinWidth(0)
            .MinHeight(0)
            .IsTabStop(false)
            .Padding(0)
            .Background(Colors.Transparent)
            .Tag(x => x.Binding(() => vm.EventId))
            .Command(x => x.Binding(() => vm.CopyEventIdCommand))
            .ToolTip("Copy");
        button.Click += (_, _) =>
        {
            if (button.Tag is string eventId && !string.IsNullOrWhiteSpace(eventId))
            {
                _ = Toast.Show(toastTarget, "Copied to clipboard", eventId);
            }
            close();
        };
        return button;
    }

    private static Button BuildOpenCacheDirectoryButton(FooterViewModel vm, Action close)
    {
        var button = new Button()
            .Content(new FontAwesomeIcon(FA.ArrowUpRightFromSquare).FontSize(12))
            .HorizontalAlignment(HorizontalAlignment.Left)
            .Width(28)
            .Height(28)
            .MinWidth(0)
            .MinHeight(0)
            .IsTabStop(false)
            .Padding(0)
            .Background(Colors.Transparent)
            .Command(x => x.Binding(() => vm.OpenCacheDirectoryCommand))
            .ToolTip("Open");
        button.Click += (_, _) => close();
        return button;
    }

    private static Segmented BuildCacheSegmented(FooterViewModel vm, Action close)
    {
        var segmented = new Segmented()
            .Width(280)
            .HorizontalAlignment(HorizontalAlignment.Left)
            .SelectedIndex(x => x.Binding(() => vm.CacheKeepIndex).TwoWay())
            .Items(new object[]
            {
                new SegmentedItem().Content("None"),
                new SegmentedItem().Content("Offline"),
                new SegmentedItem().Content("Always")
            });
        segmented.AddHandler(UIElement.TappedEvent, new TappedEventHandler((_, _) => close()), true);
        return segmented;
    }

    private static Button BuildResetCacheKeepButton(FooterViewModel vm)
    {
        var button = new Button()
            .Content(new FontAwesomeIcon(FA.ArrowRotateLeft).FontSize(12))
            .Width(28)
            .Height(28)
            .MinWidth(0)
            .MinHeight(0)
            .IsTabStop(false)
            .Padding(0)
            .Background(Colors.Transparent)
            .BorderBrush(Colors.Transparent)
            .Command(x => x.Binding(() => vm.ResetCacheKeepCommand))
            .ToolTip("Reset");
        return button;
    }

    private static Visibility ToVisibility(bool visible) =>
        visible ? Visibility.Visible : Visibility.Collapsed;

    private static FrameworkElement BuildStatusLabelSafe(FooterViewModel vm, FooterStatus status)
    {
        try
        {
            return BuildStatusLabel(vm, status).Name("statusLabel");
        }
        catch
        {
            return new TextBlock()
                .Text("Status unavailable")
                .TextWrapping(TextWrapping.Wrap)
                .VerticalAlignment(VerticalAlignment.Center)
                .Foreground(ThemeResource.Get<Brush>("SystemErrorTextColor"))
                .Name("statusLabel");
        }
    }

    private static FrameworkElement BuildStatusLabel(FooterViewModel vm, FooterStatus status)
    {
        return status switch
        {
            FooterStatus.Normal => BuildReportStatusButton(vm),
            FooterStatus.Busy => new IconLabel()
                .Icon(new ProgressRing()
                    .IsActive(true)
                    .Width(20)
                    .Height(20))
                .IsTextSelectionEnabled(false)
                .Text("Please wait. Submitting the report..."),
            FooterStatus.Error => new IconLabel(FA.CircleExclamation)
                .TextWrapping(TextWrapping.Wrap)
                .VerticalAlignment(VerticalAlignment.Center)
                .Text(x => x.Binding(() => vm.ErrorMessage))
                .Foreground(ThemeResource.Get<Brush>("SystemErrorTextColor")),
            _ => new Control()
                .Visibility(Visibility.Collapsed),
        };
    }
}
