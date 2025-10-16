using System.Collections;
using System.Text.Json.Nodes;

namespace Sentry.CrashReporter.Converters;

// TODO: implement IBindingConvertor?
public static class BindingConverter
{
    public static Visibility ToVisibility(object? obj)
    {
        return IsNullOrEmpty(obj) ? Visibility.Collapsed : Visibility.Visible;
    }

    private static bool IsNullOrEmpty(object? obj)
    {
        return obj switch
        {
            string s => string.IsNullOrEmpty(s),
            IList { Count: 0 } => true,
            JsonArray { Count: 0 } => true, 
            JsonObject { Count: 0 } => true,
            _ => obj is null
        };
    }
}
