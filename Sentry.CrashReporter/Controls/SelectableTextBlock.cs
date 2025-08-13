namespace Sentry.CrashReporter.Controls;

public class SelectableTextBlock : TextBlock
{
    public SelectableTextBlock()
    {
        IsTextSelectionEnabled = true;
        UpdateSelectionHighlightColor();

        ActualThemeChanged += (_, _) => UpdateSelectionHighlightColor();
    }

    private void UpdateSelectionHighlightColor()
    {
        SelectionHighlightColor = Application.Current.Resources.TryGetValue("SystemAccentColorBrush", out var value) && value is SolidColorBrush brush
            ? brush
            : new SolidColorBrush(Colors.Blue);
    }
}
