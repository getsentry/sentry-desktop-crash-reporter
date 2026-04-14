using CommunityToolkit.Mvvm.Input;

namespace Sentry.CrashReporter.Extensions;

public record DataAction(string Text, VirtualKey Shortcut, Action Action);

public static class DataGridExtensions
{
    public static T AsDataTable<T>(this T grid, params DataAction[] actions) where T : DataGrid
    {
        grid.IsReadOnly = true;
        grid.AutoGenerateColumns = false;
        grid.GridLinesVisibility = DataGridGridLinesVisibility.None;
        grid.HeadersVisibility = DataGridHeadersVisibility.Column;
        grid.ClipboardCopyMode = DataGridClipboardCopyMode.None;
        grid.SelectionMode = DataGridSelectionMode.Extended;
        grid.RowHeight = 28;
        grid.Resources["DataGridCellFocusVisualPrimaryBrush"] = new SolidColorBrush(Colors.Transparent);
        grid.Resources["DataGridCellFocusVisualSecondaryBrush"] = new SolidColorBrush(Colors.Transparent);

        void UpdateAlternatingRowBackground(DataGrid g)
        {
            if (Application.Current.Resources.TryGetValue("SystemControlBackgroundListLowBrush", out var brush))
                g.AlternatingRowBackground = (Brush)brush;
        }

        UpdateAlternatingRowBackground(grid);
        grid.ActualThemeChanged += (s, _) => UpdateAlternatingRowBackground((DataGrid)s);

        grid.LoadingRow += (_, e) =>
        {
            if (e.Row.DataContext is { } item)
                ToolTipService.SetToolTip(e.Row, FormatRowToolTip(item));
        };

        if (actions.Length > 0)
            SetupDataActions(grid, grid, actions);

        return grid;
    }

    public static void WithDataActions(this FrameworkElement acceleratorOwner, DataGrid grid, params DataAction[] actions)
    {
        SetupDataActions(acceleratorOwner, grid, actions);
    }

    private static void SetupDataActions(FrameworkElement acceleratorOwner, DataGrid grid, DataAction[] actions)
    {
        var menu = new MenuFlyout();
        foreach (var action in actions)
        {
            menu.Items.Add(new MenuFlyoutItem
            {
                Text = action.Text,
                Command = new RelayCommand(action.Action),
            });
        }
        grid.ContextFlyout = menu;

        var modifier = OperatingSystem.IsMacOS()
            ? VirtualKeyModifiers.Windows
            : VirtualKeyModifiers.Control;

        acceleratorOwner.KeyboardAcceleratorPlacementMode = KeyboardAcceleratorPlacementMode.Hidden;

        var accelerators = new List<KeyboardAccelerator>();
        foreach (var action in actions)
        {
            var accel = new KeyboardAccelerator { Key = action.Shortcut, Modifiers = modifier, IsEnabled = false };
            accel.Invoked += (_, e) => { e.Handled = true; action.Action(); };
            acceleratorOwner.KeyboardAccelerators.Add(accel);
            accelerators.Add(accel);
        }

        var escAccel = new KeyboardAccelerator { Key = VirtualKey.Escape, IsEnabled = false };
        escAccel.Invoked += (_, e) => { e.Handled = true; grid.SelectedItems.Clear(); };
        acceleratorOwner.KeyboardAccelerators.Add(escAccel);
        accelerators.Add(escAccel);

        acceleratorOwner.RegisterPropertyChangedCallback(UIElement.VisibilityProperty, (_, _) =>
        {
            var enabled = acceleratorOwner.Visibility == Visibility.Visible;
            foreach (var accel in accelerators)
                accel.IsEnabled = enabled;
        });
    }

    private static string? FormatRowToolTip(object item) => item switch
    {
        Attachment a => a.Filename,
        KeyValuePair<string, System.Text.Json.Nodes.JsonNode> kv => $"{kv.Key}  {kv.Value}",
        _ => item.ToString()
    };
}
