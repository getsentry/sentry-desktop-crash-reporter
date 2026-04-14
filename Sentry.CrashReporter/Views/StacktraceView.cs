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
    private static readonly StringVisibilityConverter StringToVisibility = new();
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

        var frameGrid = new StacktraceFrameGrid()
            .Name("frameGrid");

        this.WithDataActions(frameGrid,
            new DataAction("Copy", VirtualKey.C, frameGrid.CopySelection),
            new DataAction("Select All", VirtualKey.A, frameGrid.DoSelectAll));

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
                                                    .Convert<string>(id => $"Thread {id}")),
                                            new TextBlock()
                                                .WithSourceCodePro()
                                                .VerticalAlignment(VerticalAlignment.Center)
                                                .Text(x => x.Binding("Name")
                                                    .Convert<string>(name => name is not null ? $"\u2014 {name}" : ""))
                                                .Visibility(x => x.Binding("Name")
                                                    .Converter(StringToVisibility))))),
                    frameGrid
                        .Grid(row: 1)
                        .Data(x => x.Binding(() => vm.Frames)),
                    new Button()
                        .Grid(row: 1)
                        .Name("copyAllButton")
                        .Content(new FontAwesomeIcon(FA.Copy).FontSize(12))
                        .Command(new RelayCommand(frameGrid.CopyAll))
                        .HorizontalAlignment(HorizontalAlignment.Right)
                        .VerticalAlignment(VerticalAlignment.Top)
                        .Margin(4, 4)
                        .Padding(8, 4)
                        .Opacity(0.6)
                        .ToolTip("Copy stacktrace"))));
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
        this.AsDataTable();
        RightTapped += OnRightTapped;

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
            Width = DataGridLength.Auto,
            CellTemplate = new DataTemplate(() =>
                new TextBlock()
                    .WithSourceCodePro()
                    .Margin(new Thickness(8, 0))
                    .VerticalAlignment(VerticalAlignment.Center)
                    .Text(x => x.Binding("Symbol")))
        });
    }

    private void OnRightTapped(object sender, RightTappedRoutedEventArgs e)
    {
        if (e.OriginalSource is FrameworkElement { DataContext: StacktraceFrameItem item } &&
            ItemsSource is List<StacktraceFrameItem> items &&
            !SelectedItems.Contains(item))
        {
            SelectedIndex = items.IndexOf(item);
            CurrentColumn = Columns[e.GetPosition(this).X < Columns[0].ActualWidth ? 0 : 1];
        }
    }

    internal void DoSelectAll()
    {
        if (ItemsSource is not List<StacktraceFrameItem> items) return;
        SelectedItems.Clear();
        foreach (var item in items)
            SelectedItems.Add(item);
    }

    internal void CopySelection()
    {
        var text = GetSelectedText();
        if (!string.IsNullOrEmpty(text))
        {
            App.Services.GetRequiredService<IClipboardService>().SetText(text);
            var subtitle = SelectedItems.Count == 1
                ? text.Replace("\t", "  ")
                : $"{SelectedItems.Count} frames";
            _ = Toast.Show(this, "Copied to clipboard", subtitle);
        }
    }

    internal void CopyAll()
    {
        var text = GetAllText();
        if (!string.IsNullOrEmpty(text))
        {
            App.Services.GetRequiredService<IClipboardService>().SetText(text);
            var count = ((List<StacktraceFrameItem>)ItemsSource!).Count;
            _ = Toast.Show(this, "Copied to clipboard",$"{count} frames");
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
            return $"{frame.Address}\t{frame.Symbol}";
        }

        return null;
    }

    internal string? GetAllText()
    {
        if (ItemsSource is not List<StacktraceFrameItem> items || items.Count == 0)
            return null;

        return string.Join("\n", items.Select(x => $"{x.Address}\t{x.Symbol}"));
    }
}
