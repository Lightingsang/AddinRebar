using System;
using System.Text.Json;
using HPKata.Core.Walls.Models;

namespace HPKata.Core.Walls.Persistence
{
    /// <summary>
    /// Serializes and deserializes shear wall detailing models to JSON payloads
    /// stored within DWG Extension Dictionaries (XRecord "KATA_WALL").
    /// </summary>
    public static class WallXRecordSerializer
    {
        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            WriteIndented = false,
            PropertyNameCaseInsensitive = true
        };

        public static string SerializeToJson(ShearWallModel wall)
        {
            if (wall == null) throw new ArgumentNullException(nameof(wall));
            return JsonSerializer.Serialize(wall, JsonOptions);
        }

        public static ShearWallModel? DeserializeFromJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) return null;
            return JsonSerializer.Deserialize<ShearWallModel>(json, JsonOptions);
        }
    }
}
