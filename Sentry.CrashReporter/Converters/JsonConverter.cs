using System.Text.Json.Nodes;
using CommunityToolkit.WinUI.Converters;

namespace Sentry.CrashReporter.Converters;

public partial class JsonToBoolConverter : EmptyObjectToObjectConverter
{
    public JsonToBoolConverter()
    {
        NotEmptyValue = true;
        EmptyValue = false;
    }

    protected override bool CheckValueIsEmpty(object value)
    {
        return value switch
        {
            null => true,
            JsonArray { Count: 0 } => true,
            JsonObject { Count: 0 } => true,
            _ => false
        };
    }
}
