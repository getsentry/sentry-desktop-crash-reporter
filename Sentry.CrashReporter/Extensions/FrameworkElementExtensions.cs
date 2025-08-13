namespace Sentry.CrashReporter.Extensions;

public static class FrameworkElementExtensions
{
    public static T ToolTip<T>(this T element, string toolTip) where T : FrameworkElement
    {
        return element.ToolTipService(null, null, toolTip);
    }

    public static T ToolTip<T>(this T element, Action<IDependencyPropertyBuilder<string>> toolTip) where T : FrameworkElement
    {
        return element.ToolTipService(null, null, toolTip);
    }
}
