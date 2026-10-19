using System;
using System.Text.Json;

namespace HPDcePro.Mcp.Execution
{
    public static class JsonArgHelper
    {
        public static string GetString(JsonElement? elem, string propertyName, string defaultValue = "")
        {
            if (!elem.HasValue || elem.Value.ValueKind != JsonValueKind.Object) return defaultValue;
            if (elem.Value.TryGetProperty(propertyName, out var prop))
            {
                if (prop.ValueKind == JsonValueKind.String)
                {
                    return prop.GetString() ?? defaultValue;
                }
                return prop.ToString();
            }
            return defaultValue;
        }

        public static double GetDouble(JsonElement? elem, string propertyName, double defaultValue = 0.0)
        {
            if (!elem.HasValue || elem.Value.ValueKind != JsonValueKind.Object) return defaultValue;
            if (elem.Value.TryGetProperty(propertyName, out var prop))
            {
                if (prop.ValueKind == JsonValueKind.Number && prop.TryGetDouble(out double val))
                {
                    return val;
                }
                if (double.TryParse(prop.ToString(), out double parsed))
                {
                    return parsed;
                }
            }
            return defaultValue;
        }

        public static int GetInt(JsonElement? elem, string propertyName, int defaultValue = 0)
        {
            if (!elem.HasValue || elem.Value.ValueKind != JsonValueKind.Object) return defaultValue;
            if (elem.Value.TryGetProperty(propertyName, out var prop))
            {
                if (prop.ValueKind == JsonValueKind.Number && prop.TryGetInt32(out int val))
                {
                    return val;
                }
                if (int.TryParse(prop.ToString(), out int parsed))
                {
                    return parsed;
                }
            }
            return defaultValue;
        }

        public static bool GetBool(JsonElement? elem, string propertyName, bool defaultValue = false)
        {
            if (!elem.HasValue || elem.Value.ValueKind != JsonValueKind.Object) return defaultValue;
            if (elem.Value.TryGetProperty(propertyName, out var prop))
            {
                if (prop.ValueKind is JsonValueKind.True or JsonValueKind.False)
                {
                    return prop.GetBoolean();
                }
                if (bool.TryParse(prop.ToString(), out bool parsed))
                {
                    return parsed;
                }
            }
            return defaultValue;
        }

        public static JsonElement? GetProperty(JsonElement? elem, string propertyName)
        {
            if (!elem.HasValue || elem.Value.ValueKind != JsonValueKind.Object) return null;
            if (elem.Value.TryGetProperty(propertyName, out var prop))
            {
                return prop;
            }
            return null;
        }
    }
}
