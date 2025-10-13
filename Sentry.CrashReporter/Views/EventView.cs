using System.Runtime.InteropServices.WindowsRuntime;
using System.Text.Json.Nodes;
using Windows.Storage.Streams;
using Microsoft.UI.Xaml.Media.Imaging;
using Sentry.CrashReporter.Controls;
using Sentry.CrashReporter.Extensions;
using Sentry.CrashReporter.ViewModels;

namespace Sentry.CrashReporter.Views;

public sealed class EventView : ReactiveUserControl<EventViewModel>
{
    public EventView() : this(null)
    {
    }

    internal EventView(EventViewModel? dataContext)
    {
        this.DataContext(dataContext ?? new EventViewModel(), (view, vm) => view
            .Content(new StackPanel()
                .Orientation(Orientation.Vertical)
                .Spacing(8)
                .Children(
                    new TextBlock()
                        .Text("Event")
                        .Style(ThemeResource.Get<Style>("SubtitleTextBlockStyle")),
                    new Expander()
                        .Header("Tags")
                        .HorizontalAlignment(HorizontalAlignment.Stretch)
                        .HorizontalContentAlignment(HorizontalAlignment.Stretch)
                        .IsEnabled(x => x.Binding(() => vm.Tags).Convert(IsNotNullOrEmpty))
                        .Content(new JsonGrid()
                            .Data(x => x.Binding(() => vm.Tags))),
                    new Expander()
                        .Header("Contexts")
                        .HorizontalAlignment(HorizontalAlignment.Stretch)
                        .HorizontalContentAlignment(HorizontalAlignment.Stretch)
                        .IsEnabled(x => x.Binding(() => vm.Contexts).Convert(IsNotNullOrEmpty))
                        .Content(new JsonGrid()
                            .Data(x => x.Binding(() => vm.Contexts))),
                    new Expander()
                        .Header("Additional Data")
                        .HorizontalAlignment(HorizontalAlignment.Stretch)
                        .HorizontalContentAlignment(HorizontalAlignment.Stretch)
                        .IsEnabled(x => x.Binding(() => vm.Extra).Convert(IsNotNullOrEmpty))
                        .Visibility(x => x.Binding(() => vm.Extra).Convert(ToVisibility))
                        .Content(new JsonGrid()
                            .Data(x => x.Binding(() => vm.Extra))),
                    new Expander()
                        .Header("SDK")
                        .HorizontalAlignment(HorizontalAlignment.Stretch)
                        .HorizontalContentAlignment(HorizontalAlignment.Stretch)
                        .IsEnabled(x => x.Binding(() => vm.Sdk).Convert(IsNotNullOrEmpty))
                        .Content(new JsonGrid()
                            .Data(x => x.Binding(() => vm.Sdk))),
                    new Expander()
                        .Header("Attachments")
                        .HorizontalAlignment(HorizontalAlignment.Stretch)
                        .HorizontalContentAlignment(HorizontalAlignment.Stretch)
                        .IsEnabled(x => x.Binding(() => vm.Attachments).Convert(IsNotNullOrEmpty))
                        .Visibility(x => x.Binding(() => vm.Attachments).Convert(ToVisibility))
                        .Content(new AttachmentGrid()
                            .Data(x => x.Binding(() => vm.Attachments))
                            .OnPreview(a => _ = ShowPreview(a))))));
    }

    private static Visibility ToVisibility(object? obj)
    {
        return IsNotNullOrEmpty(obj) ? Visibility.Visible : Visibility.Collapsed;
    }

    private static bool IsNotNullOrEmpty(object? obj)
    {
        return obj switch
        {
            JsonObject json => json.Count > 0,
            List<EnvelopeItem> list => list.Count > 0,
            _ => obj is not null
        };
    }

    private async Task ShowPreview(Attachment attachment)
    {
        var bitmap = new BitmapImage();
        using (var randomAccessStream = new InMemoryRandomAccessStream())
        {
            await randomAccessStream.WriteAsync(attachment.Data.AsBuffer());
            randomAccessStream.Seek(0);

            await bitmap.SetSourceAsync(randomAccessStream);
        }

        var dialog = new ContentDialog
        {
            Title = attachment.Filename,
            Content = new Image { Source = bitmap, Stretch = Stretch.Uniform },
            CloseButtonText = "Close",
            XamlRoot = XamlRoot
        };

        await dialog.ShowAsync();
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

    public event Action<Attachment>? Preview;

    public AttachmentGrid OnPreview(Action<Attachment> handler)
    {
        Preview += handler;
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
        if (!DispatcherQueue.HasThreadAccess)
        {
            DispatcherQueue.TryEnqueue(() => UpdateGrid(data));
            return;
        }

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
                    .Content(new FontAwesomeIcon(FA.Eye).FontSize(12))
                    .Background(Colors.Transparent)
                    .BorderBrush(Colors.Transparent)
                    .Resources(r => r
                        .Add("ButtonBackgroundPointerOver", new SolidColorBrush(Colors.Transparent))
                        .Add("ButtonBackgroundPressed", new SolidColorBrush(Colors.Transparent))
                        .Add("ButtonBackgroundDisabled", new SolidColorBrush(Colors.Transparent))
                        .Add("ButtonBorderBrushPointerOver",
                            new SolidColorBrush(Colors.Transparent))
                        .Add("ButtonBorderBrushPressed", new SolidColorBrush(Colors.Transparent))
                        .Add("ButtonBorderBrushDisabled", new SolidColorBrush(Colors.Transparent)))
                    .Command(ReactiveCommand.Create(() => Preview?.Invoke(item),
                        Observable.Return(item.Filename.EndsWith(".png") || item.Filename.EndsWith(".jpg"))))));

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
