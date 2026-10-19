using System;
using System.Text.Json;
using HPKata.Core.Beams.Models;

namespace HPKata.Core.Beams.Persistence
{
    /// <summary>
    /// Serializes and deserializes continuous beam reinforcement models to JSON payloads
    /// stored within DWG Extension Dictionaries (XRecord "KATA_BEAM").
    /// </summary>
    public static class BeamXRecordSerializer
    {
        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            WriteIndented = false,
            PropertyNameCaseInsensitive = true
        };

        public static string SerializeToJson(BeamRebarLayout layout)
        {
            if (layout == null) throw new ArgumentNullException(nameof(layout));
            return JsonSerializer.Serialize(layout, JsonOptions);
        }

        public static BeamRebarLayout? DeserializeFromJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) return null;
            return JsonSerializer.Deserialize<BeamRebarLayout>(json, JsonOptions);
        }
    }
}
