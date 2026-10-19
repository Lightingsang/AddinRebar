namespace HPBim.Core.Coordination;

using System;
using System.Text.Json;
using System.Text.Json.Serialization;

/// <summary>
/// Tolerant JSON converter for RfiStatus supporting standard enum names, numbers,
/// and legacy status values like "OPEN", "RESOLVED", "CLOSED".
/// </summary>
public sealed class RfiStatusJsonConverter : JsonConverter<RfiStatus>
{
    public override RfiStatus Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Number && reader.TryGetInt32(out int intVal) && Enum.IsDefined(typeof(RfiStatus), intVal))
        {
            return (RfiStatus)intVal;
        }

        if (reader.TokenType != JsonTokenType.String)
        {
            throw new JsonException($"Expected string or number for RfiStatus, got {reader.TokenType}.");
        }

        string raw = reader.GetString() ?? string.Empty;
        if (Enum.TryParse<RfiStatus>(raw, true, out var parsed))
        {
            return parsed;
        }

        string trimmed = raw.Trim().ToUpperInvariant();
        if (trimmed == "OPEN" || trimmed == "PENDING" || trimmed == "IN_REVIEW")
            return RfiStatus.PendingChiefEngineer;
        if (trimmed == "APPROVED" || trimmed == "RESOLVED" || trimmed == "ACCEPTED")
            return RfiStatus.Approved;
        if (trimmed == "REJECTED" || trimmed == "CLOSED" || trimmed == "CANCELLED")
            return RfiStatus.Rejected;

        throw new JsonException($"Cannot convert '{raw}' to RfiStatus.");
    }

    public override void Write(Utf8JsonWriter writer, RfiStatus value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString());
    }
}
