namespace HPBim.Core.Coordination;

using System;
using System.Text.Json;
using System.Text.Json.Serialization;

/// <summary>
/// Tolerant JSON converter for CoordinationCategory supporting standard enum names,
/// numeric values, and legacy Revit BuiltInCategory string identifiers.
/// </summary>
public sealed class CoordinationCategoryJsonConverter : JsonConverter<CoordinationCategory>
{
    public override CoordinationCategory Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Number && reader.TryGetInt32(out int intVal) && Enum.IsDefined(typeof(CoordinationCategory), intVal))
        {
            return (CoordinationCategory)intVal;
        }

        if (reader.TokenType != JsonTokenType.String)
        {
            throw new JsonException($"Expected string or number for CoordinationCategory, got {reader.TokenType}.");
        }

        string raw = reader.GetString() ?? string.Empty;
        if (Enum.TryParse<CoordinationCategory>(raw, true, out var parsed))
        {
            return parsed;
        }

        return MapLegacyCategory(raw);
    }

    public override void Write(Utf8JsonWriter writer, CoordinationCategory value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString());
    }

    private static CoordinationCategory MapLegacyCategory(string raw)
    {
        string trimmed = raw.Trim();
        if (trimmed.IndexOf("Foundation Slab", StringComparison.OrdinalIgnoreCase) >= 0 ||
            trimmed.IndexOf("Floor", StringComparison.OrdinalIgnoreCase) >= 0 ||
            trimmed.IndexOf("Slab", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            return CoordinationCategory.Slab;
        }

        if (trimmed.IndexOf("GradeBeam", StringComparison.OrdinalIgnoreCase) >= 0 ||
            trimmed.IndexOf("DaKieng", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            return CoordinationCategory.GradeBeam;
        }

        if (trimmed.IndexOf("Foundation", StringComparison.OrdinalIgnoreCase) >= 0 ||
            trimmed.IndexOf("Footing", StringComparison.OrdinalIgnoreCase) >= 0 ||
            trimmed.IndexOf("Coc", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            return CoordinationCategory.Footing;
        }

        if (trimmed.IndexOf("Framing", StringComparison.OrdinalIgnoreCase) >= 0 ||
            trimmed.IndexOf("Beam", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            return CoordinationCategory.Beam;
        }

        if (trimmed.IndexOf("Column", StringComparison.OrdinalIgnoreCase) >= 0)
            return CoordinationCategory.Column;
        if (trimmed.IndexOf("Wall", StringComparison.OrdinalIgnoreCase) >= 0)
            return CoordinationCategory.Wall;
        if (trimmed.IndexOf("Level", StringComparison.OrdinalIgnoreCase) >= 0)
            return CoordinationCategory.Level;
        if (trimmed.IndexOf("Grid", StringComparison.OrdinalIgnoreCase) >= 0)
            return CoordinationCategory.Grid;
        if (trimmed.IndexOf("Window", StringComparison.OrdinalIgnoreCase) >= 0 ||
            trimmed.IndexOf("Door", StringComparison.OrdinalIgnoreCase) >= 0 ||
            trimmed.IndexOf("Opening", StringComparison.OrdinalIgnoreCase) >= 0)
            return CoordinationCategory.Opening;
        if (trimmed.IndexOf("Room", StringComparison.OrdinalIgnoreCase) >= 0)
            return CoordinationCategory.Room;
        if (trimmed.IndexOf("Rebar", StringComparison.OrdinalIgnoreCase) >= 0)
            return CoordinationCategory.Rebar;
        if (trimmed.IndexOf("Plumbing", StringComparison.OrdinalIgnoreCase) >= 0 ||
            trimmed.IndexOf("Fixture", StringComparison.OrdinalIgnoreCase) >= 0)
            return CoordinationCategory.MepFixture;
        if (trimmed.IndexOf("Pipe", StringComparison.OrdinalIgnoreCase) >= 0)
            return CoordinationCategory.PipeRun;

        throw new JsonException($"Cannot convert '{raw}' to CoordinationCategory.");
    }
}
