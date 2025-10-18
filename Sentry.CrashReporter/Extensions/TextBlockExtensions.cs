namespace Sentry.CrashReporter.Extensions;

public static class TextBlockExtensions
{
    public static TextBlock WithTextSelection(this TextBlock textBlock, bool enabled = true)
    {
        return textBlock
            .IsTextSelectionEnabled(enabled)
            .SelectionHighlightColor(ThemeResource.Get<SolidColorBrush>("SystemAccentColorBrush"));
    }

    public static TextBlock WithSourceCodePro(this TextBlock textBlock)
    {
        return textBlock
            .FontFamily("ms-appx:///Assets/Fonts/SourceCodePro/SourceCodePro-Regular.ttf#Source Code Pro");
    }
}
