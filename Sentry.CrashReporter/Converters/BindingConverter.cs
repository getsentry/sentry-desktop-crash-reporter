using System.Collections;
using System.Text.Json.Nodes;

namespace Sentry.CrashReporter.Converters;

// TODO: implement IBindingConvertor?
public static class BindingConverter
{
    public static bool ToEnabled(object? obj)
    {
        return !IsFalsy(obj);
    }

    public static Visibility ToVisibility(object? obj)
    {
        return IsFalsy(obj) ? Visibility.Collapsed : Visibility.Visible;
    }

    private static bool IsFalsy(object? obj)
    {
        return obj switch
        {
            bool b => !b,
            string s => string.IsNullOrEmpty(s),
            IList { Count: 0 } => true,
            JsonArray { Count: 0 } => true, 
            JsonObject { Count: 0 } => true,
            _ => obj is null
        };
    }
}
