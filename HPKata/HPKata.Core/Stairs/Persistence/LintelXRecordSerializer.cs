using System;
using System.Text.Json;
using HPKata.Core.Stairs.Models;

namespace HPKata.Core.Stairs.Persistence
{
    /// <summary>
    /// Serializes and deserializes lintel models to JSON payloads stored in DWG Extension Dictionaries
    /// under key "KATA_LINTEL_DATA".
    /// </summary>
    public static class LintelXRecordSerializer
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = false,
            PropertyNameCaseInsensitive = true
        };

        public static string SerializeToJson(LintelModel model)
        {
            if (model == null) throw new ArgumentNullException(nameof(model));
            return JsonSerializer.Serialize(model, JsonOptions);
        }

        public static LintelModel? DeserializeFromJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) return null;
            return JsonSerializer.Deserialize<LintelModel>(json, JsonOptions);
        }
    }
}
