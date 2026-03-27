using CommunityToolkit.WinUI.Converters;
using CommunityToolkit.Mvvm.Input;
using Sentry.CrashReporter.Controls;
using Sentry.CrashReporter.Extensions;
using Sentry.CrashReporter.Services;
using Sentry.CrashReporter.ViewModels;

namespace Sentry.CrashReporter.Views;

public class StacktraceView : ReactiveUserControl<StacktraceViewModel>
{
    private static readonly BoolToVisibilityConverter BoolToVisibility = new();
    public static readonly DependencyProperty EnvelopeProperty = DependencyProperty.Register(
        nameof(Envelope), typeof(Envelope), typeof(StacktraceView), new PropertyMetadata(null));

    public Envelope? Envelope
    {
        get => (Envelope)GetValue(EnvelopeProperty);
        set => SetValue(EnvelopeProperty, value);
    }

    public StacktraceView()
    {
        ViewModel = new StacktraceViewModel();
        this.WhenActivated(d =>
        {
            this.WhenAnyValue(v => v.Envelope)
                .BindTo(ViewModel, vm => vm.Envelope)
                .DisposeWith(d);
        });

        this.Content(new Grid()
            .DataContext(ViewModel, (view, vm) => view
                .RowSpacing(8)
                .RowDefinitions("Auto,*")
                .Children(
                    new Grid()
                        .Grid(row: 0)
                        .ColumnSpacing(4)
                        .ColumnDefinitions("Auto,Auto,*")
                        .Visibility(x => x.Binding(() => vm.Threads)
                            .Convert(threads => threads is { Count: > 0 }
                                && threads.Any(t => t.ThreadId.Length > 0)
                                ? Visibility.Visible : Visibility.Collapsed))
                        .Children(
                            new Button()
                                .Grid(column: 0)
                                .Name("previousThreadButton")
                                .Content(new FontAwesomeIcon(FA.ChevronLeft).FontSize(12))
                                .Command(x => x.Binding(() => vm.PreviousThread))
                                .Padding(8, 4),
                            new Button()
                                .Grid(column: 1)
                                .Name("nextThreadButton")
                                .Content(new FontAwesomeIcon(FA.ChevronRight).FontSize(12))
                                .Command(x => x.Binding(() => vm.NextThread))
                                .Padding(8, 4),
                            new ComboBox()
                                .Grid(column: 2)
                                .Name("threadComboBox")
                                .HorizontalAlignment(HorizontalAlignment.Stretch)
                                .IsEnabled(x => x.Binding(() => vm.HasMultipleThreads))
                                .ItemsSource(x => x.Binding(() => vm.Threads))
                                .SelectedIndex(x => x.Binding(() => vm.SelectedThreadIndex).TwoWay())
                                .ItemTemplate(() =>
                                    new StackPanel()
                                        .Orientation(Orientation.Horizontal)
                                        .Spacing(6)
                                        .Children(
                                            new FontAwesomeIcon(FA.Bug)
                                                .FontSize(12)
                                                .VerticalAlignment(VerticalAlignment.Center)
                                                .Foreground(ThemeResource.Get<Brush>("SystemFillColorCriticalBrush"))
                                                .Visibility(x => x.Binding("Crashed")
                                                    .Converter(BoolToVisibility)),
                                            new TextBlock()
                                                .WithSourceCodePro()
                                                .VerticalAlignment(VerticalAlignment.Center)
                                                .Text(x => x.Binding("ThreadId")
                                                    .Convert<string>(id => $"Thread {id}"))))),
                    new StacktraceFrameGrid()
                        .Grid(row: 1)
                        .Name("frameGrid")
                        .Data(x => x.Binding(() => vm.Frames)))));
    }
}

internal class StacktraceFrameGrid : DataGrid
{
    public static readonly DependencyProperty DataProperty = DependencyProperty.Register(
        nameof(Data), typeof(List<StacktraceFrameItem>), typeof(StacktraceFrameGrid),
        new PropertyMetadata(null, OnDataChanged));

    public List<StacktraceFrameItem>? Data
    {
        get => (List<StacktraceFrameItem>?)GetValue(DataProperty);
        set => SetValue(DataProperty, value);
    }

    private static void OnDataChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is StacktraceFrameGrid grid)
        {
            grid.ItemsSource = e.NewValue as List<StacktraceFrameItem>;
        }
    }

    public StacktraceFrameGrid()
    {
        ActualThemeChanged += (_, _) => UpdateAlternatingRowBackground();
        RightTapped += OnRightTapped;

        IsReadOnly = true;
        AutoGenerateColumns = false;
        GridLinesVisibility = DataGridGridLinesVisibility.None;
        HeadersVisibility = DataGridHeadersVisibility.Column;
        SelectionMode = DataGridSelectionMode.Extended;

        UpdateAlternatingRowBackground();

        ContextFlyout = new MenuFlyout
        {
            Items =
            {
                new MenuFlyoutItem
                {
                    Text = "Copy",
                    Command = new RelayCommand(CopySelection),
                }
            }
        };

        Columns.Add(new DataGridTemplateColumn
        {
            Header = "Address",
            Width = DataGridLength.Auto,
            CellTemplate = new DataTemplate(() =>
                new TextBlock()
                    .WithSourceCodePro()
                    .Margin(new Thickness(8, 0))
                    .VerticalAlignment(VerticalAlignment.Center)
                    .Text(x => x.Binding("Address")))
        });

        Columns.Add(new DataGridTemplateColumn
        {
            Header = "Symbol",
            Width = new DataGridLength(1, DataGridLengthUnitType.Star),
            CellTemplate = new DataTemplate(() =>
                new TextBlock()
                    .WithSourceCodePro()
                    .TextWrapping(TextWrapping.Wrap)
                    .Margin(new Thickness(8, 0))
                    .VerticalAlignment(VerticalAlignment.Center)
                    .Text(x => x.Binding("Symbol")))
        });
    }

    private void UpdateAlternatingRowBackground()
    {
        if (Application.Current.Resources.TryGetValue("SystemControlBackgroundListLowBrush", out var brush))
        {
            AlternatingRowBackground = (Brush)brush;
        }
    }

    private void OnRightTapped(object sender, RightTappedRoutedEventArgs e)
    {
        if (e.OriginalSource is FrameworkElement { DataContext: StacktraceFrameItem item } &&
            ItemsSource is List<StacktraceFrameItem> items)
        {
            SelectedIndex = items.IndexOf(item);
            CurrentColumn = Columns[e.GetPosition(this).X < Columns[0].ActualWidth ? 0 : 1];
        }
    }

    private void CopySelection()
    {
        var text = GetSelectedText();
        if (!string.IsNullOrEmpty(text))
        {
            App.Services.GetRequiredService<IClipboardService>().SetText(text);
        }
    }

    internal string? GetSelectedText()
    {
        if (SelectedItems.Count > 1)
        {
            return string.Join("\n", SelectedItems.OfType<StacktraceFrameItem>()
                .Select(x => $"{x.Address}\t{x.Symbol}"));
        }

        if (SelectedItem is StacktraceFrameItem frame)
        {
            return CurrentColumn?.DisplayIndex switch
            {
                0 => frame.Address,
                1 => frame.Symbol,
                _ => null
            };
        }

        return null;
    }
}
