namespace Sentry.CrashReporter.Extensions;

public static class ToolTipExtensions
{
    public static T ToolTip<T>(this T element, string toolTip) where T : FrameworkElement
    {
        return element.ToolTipService(null, null, toolTip);
    }

    public static T ToolTip<T>(this T element, Action<ToolTipServicePropertyBuilder> configure) where T : FrameworkElement
    {
        return element.ToolTipService(configure);
    }
}
