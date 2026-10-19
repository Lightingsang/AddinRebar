using System;
using System.Text.Json;
using HPKata.Core.Columns.Models;

namespace HPKata.Core.Columns.Persistence
{
    /// <summary>
    /// Serializes and deserializes column detailing models to JSON payloads
    /// stored within DWG Extension Dictionaries (XRecord "KATA_COLUMN").
    /// </summary>
    public static class ColumnXRecordSerializer
    {
        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            WriteIndented = false,
            PropertyNameCaseInsensitive = true
        };

        public static string SerializeToJson(ColumnRebarLayout layout)
        {
            if (layout == null) throw new ArgumentNullException(nameof(layout));
            return JsonSerializer.Serialize(layout, JsonOptions);
        }

        public static ColumnRebarLayout? DeserializeFromJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) return null;
            return JsonSerializer.Deserialize<ColumnRebarLayout>(json, JsonOptions);
        }
    }
}
