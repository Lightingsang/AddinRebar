using System;
using System.Text.Json;
using HPKata.Core.Slabs.Models;

namespace HPKata.Core.Slabs.Persistence
{
    /// <summary>
    /// Serializes and deserializes slab models to JSON payloads stored in DWG Extension Dictionaries
    /// under key "KATA_SLAB_DATA".
    /// </summary>
    public static class SlabXRecordSerializer
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = false,
            PropertyNameCaseInsensitive = true
        };

        public static string SerializeToJson(SlabRebarLayout layout)
        {
            if (layout == null) throw new ArgumentNullException(nameof(layout));
            return JsonSerializer.Serialize(layout, JsonOptions);
        }

        public static SlabRebarLayout? DeserializeFromJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) return null;
            return JsonSerializer.Deserialize<SlabRebarLayout>(json, JsonOptions);
        }

        public static string SerializePanel(SlabPanelModel panel)
        {
            if (panel == null) throw new ArgumentNullException(nameof(panel));
            return JsonSerializer.Serialize(panel, JsonOptions);
        }

        public static SlabPanelModel? DeserializePanel(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) return null;
            return JsonSerializer.Deserialize<SlabPanelModel>(json, JsonOptions);
        }
    }
}
