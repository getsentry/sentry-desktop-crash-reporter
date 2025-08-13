namespace Sentry.CrashReporter.Extensions;

public static class TextBlockExtensions
{
    public static TextBlock WithSourceCodePro(this TextBlock textBlock)
    {
        return textBlock
            .FontFamily("ms-appx:///Assets/Fonts/SourceCodePro/SourceCodePro-Regular.ttf#Source Code Pro");
    }
}
