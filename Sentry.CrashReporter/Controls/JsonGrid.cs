using System.Text.Json.Nodes;
using CommunityToolkit.Mvvm.Input;
using Sentry.CrashReporter.Converters;
using Sentry.CrashReporter.Extensions;
using Sentry.CrashReporter.Services;

namespace Sentry.CrashReporter.Controls;

using JsonGridItem = KeyValuePair<string, JsonNode>;
using JsonGridData = IList<KeyValuePair<string, JsonNode>>;

public sealed class JsonGrid : DataGrid
{
    private static readonly JsonToStringConverter JsonToString = new ();

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
        ActualThemeChanged += OnThemeChanged;
        RightTapped += OnRightTapped;

        IsReadOnly = true;
        AutoGenerateColumns = false;
        GridLinesVisibility = DataGridGridLinesVisibility.None;
        HeadersVisibility = DataGridHeadersVisibility.Column;
        SelectionMode = DataGridSelectionMode.Extended;
        ItemsSource = Data;

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
                    .TextWrapping(TextWrapping.Wrap)
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

    private void OnThemeChanged(FrameworkElement sender, object args)
    {
        UpdateAlternatingRowBackground();
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
        if (e.OriginalSource is FrameworkElement { DataContext: JsonGridItem item } &&
            (SelectedItem is not JsonGridItem selected || selected.Key == item.Key))
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
            return CurrentColumn?.DisplayIndex switch
            {
                0 => item.Key,
                1 => item.Value.ToString(),
                _ => null
            };
        }

        return null;
    }
    
    private void CopySelection()
    {
        var text = GetSelectedText();
        if (!string.IsNullOrEmpty(text))
        {
            App.Services.GetRequiredService<IClipboardService>().SetText(text);
        }
    }
}
