namespace HPBim.Core.Coordination;

using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

public sealed class CompositeSourceKeyJsonConverter : JsonConverter<CompositeSourceKey>
{
    public override CompositeSourceKey? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
        {
            return CompositeSourceKey.Empty;
        }

        if (reader.TokenType == JsonTokenType.String)
        {
            string? raw = reader.GetString();
            if (string.IsNullOrWhiteSpace(raw))
            {
                return CompositeSourceKey.Empty;
            }
            return CompositeSourceKey.TryParse(raw, out var parsed, allowEmptyDoc: true)
                ? parsed!
                : throw new JsonException($"Invalid CompositeSourceKey string format: '{raw}'");
        }

        if (reader.TokenType == JsonTokenType.StartObject)
        {
            return ReadLegacyJsonObject(ref reader);
        }

        throw new JsonException($"Expected string or object for CompositeSourceKey, got {reader.TokenType}.");
    }

    public override void Write(Utf8JsonWriter writer, CompositeSourceKey value, JsonSerializerOptions options)
    {
        if (value is null)
        {
            writer.WriteNullValue();
            return;
        }

        if (value == CompositeSourceKey.Empty || string.IsNullOrWhiteSpace(value.PrimaryHandle))
        {
            writer.WriteStringValue(string.Empty);
            return;
        }

        if (value.ProvenanceHandles != null && value.ProvenanceHandles.Count > 0)
        {
            writer.WriteStartObject();
            writer.WriteString("docIdentifier", value.DocIdentifier);
            writer.WriteString("category", value.Category.ToString());
            writer.WriteString("primaryHandle", value.PrimaryHandle);
            writer.WriteStartArray("provenanceHandles");
            foreach (var handle in value.ProvenanceHandles)
            {
                writer.WriteStringValue(handle);
            }
            writer.WriteEndArray();
            writer.WriteEndObject();
            return;
        }

        writer.WriteStringValue(value.ToString());
    }

    public override void WriteAsPropertyName(Utf8JsonWriter writer, CompositeSourceKey value, JsonSerializerOptions options) =>
        writer.WritePropertyName(value?.ToString() ?? string.Empty);

    public override CompositeSourceKey ReadAsPropertyName(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        string? raw = reader.GetString();
        return !string.IsNullOrWhiteSpace(raw) && CompositeSourceKey.TryParse(raw, out var key) ? key! : CompositeSourceKey.Empty;
    }

    private static CompositeSourceKey ReadLegacyJsonObject(ref Utf8JsonReader reader)
    {
        using var doc = JsonDocument.ParseValue(ref reader);
        var root = doc.RootElement;
        string docId = string.Empty;
        string primaryHandle = string.Empty;
        CoordinationCategory category = CoordinationCategory.Column;
        List<string>? provenance = null;

        foreach (var prop in root.EnumerateObject())
        {
            if (string.Equals(prop.Name, "docIdentifier", StringComparison.OrdinalIgnoreCase))
            {
                docId = prop.Value.GetString() ?? string.Empty;
            }
            else if (string.Equals(prop.Name, "primaryHandle", StringComparison.OrdinalIgnoreCase))
            {
                primaryHandle = prop.Value.GetString() ?? string.Empty;
            }
            else if (string.Equals(prop.Name, "category", StringComparison.OrdinalIgnoreCase))
            {
                category = ParseCategoryProperty(prop.Value);
            }
            else if (string.Equals(prop.Name, "provenanceHandles", StringComparison.OrdinalIgnoreCase) &&
                     prop.Value.ValueKind == JsonValueKind.Array)
            {
                provenance = ParseProvenance(prop.Value);
            }
            else if (string.Equals(prop.Name, "key", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(prop.Name, "sourceKey", StringComparison.OrdinalIgnoreCase))
            {
                string? rawKey = prop.Value.GetString();
                if (!string.IsNullOrWhiteSpace(rawKey) && CompositeSourceKey.TryParse(rawKey, out var parsedKey))
                {
                    if (string.IsNullOrEmpty(docId)) docId = parsedKey!.DocIdentifier;
                    if (string.IsNullOrEmpty(primaryHandle)) primaryHandle = parsedKey!.PrimaryHandle;
                    category = parsedKey!.Category;
                }
            }
        }

        return new CompositeSourceKey(docId, category, primaryHandle, provenance);
    }

    private static List<string> ParseProvenance(JsonElement arrayElement)
    {
        var list = new List<string>();
        foreach (var item in arrayElement.EnumerateArray())
        {
            if (item.ValueKind == JsonValueKind.String && item.GetString() is { } s)
            {
                list.Add(s);
            }
        }
        return list;
    }

    private static CoordinationCategory ParseCategoryProperty(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Number && element.TryGetInt32(out int catInt) &&
            Enum.IsDefined(typeof(CoordinationCategory), catInt))
        {
            return (CoordinationCategory)catInt;
        }

        if (element.ValueKind == JsonValueKind.String)
        {
            string catStr = element.GetString() ?? string.Empty;
            if (Enum.TryParse<CoordinationCategory>(catStr, true, out var cat))
            {
                return cat;
            }
        }

        return CoordinationCategory.Column;
    }
}
