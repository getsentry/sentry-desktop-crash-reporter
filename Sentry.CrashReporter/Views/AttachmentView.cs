using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Storage.Streams;
using Microsoft.UI.Xaml.Media.Imaging;
using Sentry.CrashReporter.Controls;
using Sentry.CrashReporter.Extensions;
using Sentry.CrashReporter.ViewModels;

namespace Sentry.CrashReporter.Views;

public class AttachmentView : ReactiveUserControl<AttachmentViewModel>
{
    public AttachmentView()
    {
        ViewModel = new AttachmentViewModel();
        this.DataContext(ViewModel, (view, vm) => view
            .Content(new ScrollViewer()
                .Content(new StackPanel()
                    .Orientation(Orientation.Vertical)
                    .Spacing(8)
                    .Children(new AttachmentGrid()
                        .Data(x => x.Binding(() => vm.Attachments))
                        .OnLaunch(a => _ = view.ViewModel?.Launch(a))))));
    }
}

internal class AttachmentGrid : Grid
{
    public AttachmentGrid()
    {
        ColumnDefinitions.Add(new ColumnDefinition().Width(new GridLength(1, GridUnitType.Star)));
        ColumnDefinitions.Add(new ColumnDefinition().Width(GridLength.Auto));
        ColumnDefinitions.Add(new ColumnDefinition().Width(GridLength.Auto));

        DataContextChanged += (_, _) => TryAutoBind();
    }

    public static readonly DependencyProperty DataProperty =
        DependencyProperty.Register(
            nameof(Data),
            typeof(List<Attachment>),
            typeof(AttachmentGrid),
            new PropertyMetadata(null, (d, e) =>
            {
                if (d is AttachmentGrid grid)
                {
                    grid.UpdateGrid(e.NewValue as List<Attachment>);
                }
            }));

    public List<Attachment>? Data
    {
        get => (List<Attachment>?)GetValue(DataProperty);
        set => SetValue(DataProperty, value);
    }

    public event Action<Attachment>? Launch;

    public AttachmentGrid OnLaunch(Action<Attachment> handler)
    {
        Launch += handler;
        return this;
    }

    private void TryAutoBind()
    {
        if (ReadLocalValue(DataProperty) == DependencyProperty.UnsetValue &&
            DataContext is List<Attachment> data)
        {
            Data = data;
        }
    }

    private void UpdateGrid(List<Attachment>? data)
    {
        Children.Clear();
        RowDefinitions.Clear();

        if (data is null)
        {
            return;
        }

        var row = 0;
        var evenBrush = ThemeResource.Get<Brush>("SystemControlTransparentBrush");
        var oddBrush = ThemeResource.Get<Brush>("SystemControlBackgroundListLowBrush");

        foreach (var item in data)
        {
            RowDefinitions.Add(new RowDefinition().Height(GridLength.Auto));

            Children.Add(new Border()
                .Grid(row: row, column: 0)
                .Background(row % 2 == 0 ? evenBrush : oddBrush)
                .CornerRadius(new CornerRadius(2, 0, 0, 2))
                .Padding(new Thickness(4, 2, 8, 2))
                .Child(new SelectableTextBlock()
                    .WithSourceCodePro()
                    .Text(item.Filename)));

            Children.Add(new Border()
                .Grid(row: row, column: 1)
                .Background(row % 2 == 0 ? evenBrush : oddBrush)
                .CornerRadius(new CornerRadius(2, 0, 0, 2))
                .Padding(new Thickness(4, 2, 8, 2))
                .Child(new SelectableTextBlock()
                    .WithSourceCodePro()
                    .Text(ToHumanReadableSize(item.Data.Length))));

            Children.Add(new Border()
                .Grid(row: row, column: 2)
                .Background(row % 2 == 0 ? evenBrush : oddBrush)
                .CornerRadius(new CornerRadius(0, 2, 2, 0))
                .Padding(new Thickness(8, 2, 4, 2))
                .Child(new Button()
                    .Content(new FontAwesomeIcon(FA.ArrowUpRightFromSquare).FontSize(12))
                    .Background(Colors.Transparent)
                    .BorderBrush(Colors.Transparent)
                    .Resources(r => r.Add("ButtonBackgroundPointerOver", new SolidColorBrush(Colors.Transparent))
                        .Add("ButtonBackgroundPressed", new SolidColorBrush(Colors.Transparent))
                        .Add("ButtonBackgroundDisabled", new SolidColorBrush(Colors.Transparent))
                        .Add("ButtonBorderBrushPointerOver", new SolidColorBrush(Colors.Transparent))
                        .Add("ButtonBorderBrushPressed", new SolidColorBrush(Colors.Transparent))
                        .Add("ButtonBorderBrushDisabled", new SolidColorBrush(Colors.Transparent)))
                    .Command(ReactiveCommand.Create(() => Launch?.Invoke(item)))));

            row++;
        }
    }

    private static string ToHumanReadableSize(long bytes)
    {
        string[] sizes = { "B", "KiB", "MiB", "GiB", "TiB" };
        double len = bytes;
        var order = 0;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len /= 1024;
        }

        return $"{len:0.#} {sizes[order]}";
    }
}
