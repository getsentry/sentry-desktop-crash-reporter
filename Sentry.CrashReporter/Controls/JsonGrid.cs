using System.Text.Json.Nodes;

namespace Sentry.CrashReporter.Controls;

public class JsonGrid : Grid
{
    public JsonGrid()
    {
        ColumnDefinitions.Add(new ColumnDefinition().Width(GridLength.Auto));
        ColumnDefinitions.Add(new ColumnDefinition().Width(new GridLength(1, GridUnitType.Star)));

        DataContextChanged += (_, _) => TryAutoBind();
    }

    public static readonly DependencyProperty DataProperty =
        DependencyProperty.Register(
            nameof(Data),
            typeof(JsonObject),
            typeof(JsonGrid),
            new PropertyMetadata(null, (d, e) =>
            {
                if (d is JsonGrid grid)
                    grid.UpdateGrid(e.NewValue as JsonObject);
            }));

    public JsonObject? Data
    {
        get => (JsonObject?)GetValue(DataProperty);
        set => SetValue(DataProperty, value);
    }

    private void TryAutoBind()
    {
        if (ReadLocalValue(DataProperty) == DependencyProperty.UnsetValue &&
            DataContext is JsonObject json)
        {
            Data = json;
        }
    }

    private void UpdateGrid(JsonObject? json)
    {
        if (!DispatcherQueue.HasThreadAccess)
        {
            DispatcherQueue.TryEnqueue(() => UpdateGrid(json));
            return;
        }

        Children.Clear();
        RowDefinitions.Clear();

        if (json is null)
        {
            return;
        }

        var row = 0;
        var evenBrush = ThemeResource.Get<Brush>("SystemControlTransparentBrush");
        var oddBrush = ThemeResource.Get<Brush>("SystemControlBackgroundListLowBrush");

        foreach (var kvp in json)
        {
            RowDefinitions.Add(new RowDefinition().Height(GridLength.Auto));

            Children.Add(new Border()
                .Grid(row: row, column: 0)
                .Background(row % 2 == 0 ? evenBrush : oddBrush)
                .CornerRadius(new CornerRadius(2, 0, 0, 2))
                .Padding(new Thickness(4, 2, 8, 2))
                .Child(new SelectableTextBlock()
                    .Text(kvp.Key)));

            Children.Add(new Border()
                .Grid(row: row, column: 1)
                .Background(row % 2 == 0 ? evenBrush : oddBrush)
                .CornerRadius(new CornerRadius(0, 2, 2, 0))
                .Padding(new Thickness(8, 2, 4, 2))
                .Child(new SelectableTextBlock()
                    .Text(kvp.Value?.ToString() ?? string.Empty)
                    .TextWrapping(TextWrapping.Wrap)));

            row++;
        }
    }
}
