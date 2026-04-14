using System.Text.Json.Nodes;
using Sentry.CrashReporter.Converters;
using Sentry.CrashReporter.Extensions;
using Sentry.CrashReporter.Services;

namespace Sentry.CrashReporter.Controls;

using JsonGridItem = KeyValuePair<string, JsonNode>;
using JsonGridData = IList<KeyValuePair<string, JsonNode>>;

public sealed class JsonGrid : DataGrid
{
    private static readonly JsonToStringConverter JsonToString = new ();

    public static readonly DependencyProperty CategoryProperty = DependencyProperty.Register(
        nameof(Category), typeof(string), typeof(JsonGrid), new PropertyMetadata("properties"));

    public string Category
    {
        get => (string)GetValue(CategoryProperty);
        set => SetValue(CategoryProperty, value);
    }

    public static readonly DependencyProperty DataProperty = DependencyProperty.Register(
        nameof(Data), typeof(JsonGridData), typeof(JsonGrid), new PropertyMetadata(null, OnDataChanged));

    public JsonGridData? Data
    {
        get => (JsonGridData?)GetValue(DataProperty);
        set => SetValue(DataProperty, value);
    }

    private static void OnDataChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is JsonGrid grid)
        {
            grid.ItemsSource = e.NewValue as JsonGridData;
        }
    }

    public JsonGrid()
    {
        DataContextChanged += (_, _) => TryAutoBind();
        RightTapped += OnRightTapped;

        this.AsDataTable(
            new DataAction("Copy", VirtualKey.C, CopySelection),
            new DataAction("Select All", VirtualKey.A, DoSelectAll));
        ItemsSource = Data;

        Columns.Add(new DataGridTemplateColumn
        {
            Header = "Key",
            MinWidth = 200,
            Width = DataGridLength.Auto,
            CellTemplate = new DataTemplate(() =>
                new TextBlock()
                    .WithSourceCodePro()
                    .Margin(new Thickness(8, 0))
                    .VerticalAlignment(VerticalAlignment.Center)
                    .Text(x => x.Binding("Key")))
        });

        Columns.Add(new DataGridTemplateColumn
        {
            Header = "Value",
            Width = new DataGridLength(1, DataGridLengthUnitType.Star),
            CellTemplate = new DataTemplate(() =>
                new TextBlock()
                    .WithSourceCodePro()
                    .Margin(new Thickness(8, 0))
                    .VerticalAlignment(VerticalAlignment.Center)
                    .Text(x => x.Binding("Value").Converter(JsonToString)))
        });
    }

    private void TryAutoBind()
    {
        if (ReadLocalValue(DataProperty) == DependencyProperty.UnsetValue &&
            DataContext is JsonGridData json)
        {
            Data = json;
        }
    }

    private void OnRightTapped(object sender, RightTappedRoutedEventArgs e)
    {
        if (e.OriginalSource is FrameworkElement { DataContext: JsonGridItem item } &&
            !SelectedItems.Contains(item))
        {
            SelectedItem = item;
            CurrentColumn = Columns[e.GetPosition(this).X < Columns[0].ActualWidth ? 0 : 1];
        }
    }

    internal string? GetSelectedText()
    {
        if (SelectedItems.Count > 1)
        {
            return string.Join("\n", SelectedItems.OfType<JsonGridItem>().Select(x => $"{x.Key}\t{x.Value}"));
        }

        if (SelectedItem is JsonGridItem item)
        {
            return $"{item.Key}\t{item.Value}";
        }

        return null;
    }
    
    private void DoSelectAll()
    {
        if (ItemsSource is not JsonGridData items) return;
        SelectedItems.Clear();
        foreach (var item in items)
            SelectedItems.Add(item);
    }

    private void CopySelection()
    {
        var text = GetSelectedText();
        if (!string.IsNullOrEmpty(text))
        {
            App.Services.GetRequiredService<IClipboardService>().SetText(text);
            var subtitle = SelectedItems.Count == 1
                ? text.Replace("\t", "  ")
                : $"{SelectedItems.Count} {Category}";
            _ = Toast.Show(this, "Copied to clipboard", subtitle);
        }
    }
}
