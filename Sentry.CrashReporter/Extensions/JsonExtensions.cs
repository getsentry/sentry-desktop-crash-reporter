using System.Globalization;
using System.Text.Json.Nodes;

namespace Sentry.CrashReporter.Extensions;

public static class JsonExtensions
{
    public static JsonNode? TryGetProperty(this JsonObject json, string propertyName)
    {
        JsonNode? node = json;
        foreach (var path in propertyName.Split('.'))
        {
            if (node is JsonObject obj && obj.TryGetPropertyValue(path, out var next))
            {
                node = next;
            }
            else
            {
                return null;
            }
        }

        return node;
    }

    public static string? TryGetString(this JsonObject json, string propertyName)
    {
        return json.TryGetProperty(propertyName)?.GetValue<string>();
    }

    public static DateTime? TryGetDateTime(this JsonObject json, string propertyName)
    {
        return DateTime.TryParse(
            json.TryGetString(propertyName),
            CultureInfo.InvariantCulture,
            DateTimeStyles.AdjustToUniversal,
            out var timestamp)
            ? timestamp
            : null;
    }

    public static JsonObject AsFlatObject(this JsonNode source)
    {
        var nodes = new Dictionary<string, JsonNode?>();

        Flatten(source, "");

        var result = new JsonObject();
        foreach (var kvp in nodes)
        {
            result[kvp.Key] = kvp.Value?.DeepClone();
        }

        return result;

        void Flatten(JsonNode? node, string prefix)
        {
            switch (node)
            {
                case JsonObject obj:
                    foreach (var (key, value) in obj)
                    {
                        var newKey = string.IsNullOrEmpty(prefix) ? key : $"{prefix}.{key}";
                        Flatten(value, newKey);
                    }

                    break;

                case JsonArray array:
                    if (array.Any(v => v is JsonObject or JsonArray))
                    {
                        for (var i = 0; i < array.Count; i++)
                        {
                            var newKey = $"{prefix}[{i}]";
                            Flatten(array[i], newKey);
                        }
                    }
                    else
                    {
                        nodes[prefix] = JsonValue.Create(string.Join(", ", array.Select(n => n.FormatNode())));
                    }

                    break;

                case JsonValue val:
                    nodes[prefix] = val;
                    break;

                default:
                    nodes[prefix] = node;
                    break;
            }
        }
    }

    private static object? FormatNode(this JsonNode? node)
    {
        return node switch
        {
            JsonValue v when v.TryGetValue<bool>(out var b) => b ? "true" : "false",
            JsonValue v when v.TryGetValue<long>(out var l) => l.ToString(),
            JsonValue v when v.TryGetValue<double>(out var d) => d.ToString(CultureInfo.InvariantCulture),
            JsonValue v when v.TryGetValue<string>(out var s) => s,
            _ => node?.ToJsonString() ?? "null"
        };
    }
}
