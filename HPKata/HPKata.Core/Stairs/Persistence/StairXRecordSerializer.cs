using System;
using System.Text.Json;
using HPKata.Core.Stairs.Models;

namespace HPKata.Core.Stairs.Persistence
{
    /// <summary>
    /// Serializes and deserializes stair models to JSON payloads stored in DWG Extension Dictionaries
    /// under key "KATA_STAIR_DATA".
    /// </summary>
    public static class StairXRecordSerializer
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = false,
            PropertyNameCaseInsensitive = true
        };

        public static string SerializeToJson(StairReinforcementModel model)
        {
            if (model == null) throw new ArgumentNullException(nameof(model));
            return JsonSerializer.Serialize(model, JsonOptions);
        }

        public static StairReinforcementModel? DeserializeFromJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) return null;
            return JsonSerializer.Deserialize<StairReinforcementModel>(json, JsonOptions);
        }

        public static string SerializeFlight(StairFlightModel flight)
        {
            if (flight == null) throw new ArgumentNullException(nameof(flight));
            return JsonSerializer.Serialize(flight, JsonOptions);
        }

        public static StairFlightModel? DeserializeFlight(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) return null;
            return JsonSerializer.Deserialize<StairFlightModel>(json, JsonOptions);
        }
    }
}
