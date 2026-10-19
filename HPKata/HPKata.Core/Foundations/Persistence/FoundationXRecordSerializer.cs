using System;
using System.Text.Json;
using HPKata.Core.Foundations.Models;

namespace HPKata.Core.Foundations.Persistence
{
    /// <summary>
    /// Serializes and deserializes foundation models to JSON payloads stored in DWG Extension Dictionaries
    /// under key "KATA_FOUNDATION_DATA".
    /// </summary>
    public static class FoundationXRecordSerializer
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = false,
            PropertyNameCaseInsensitive = true
        };

        public static string SerializeToJson(FoundationRebarLayout layout)
        {
            if (layout == null) throw new ArgumentNullException(nameof(layout));
            return JsonSerializer.Serialize(layout, JsonOptions);
        }

        public static FoundationRebarLayout? DeserializeFromJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) return null;
            return JsonSerializer.Deserialize<FoundationRebarLayout>(json, JsonOptions);
        }

        public static string SerializeFooting(IsolatedFootingModel footing)
        {
            if (footing == null) throw new ArgumentNullException(nameof(footing));
            return JsonSerializer.Serialize(footing, JsonOptions);
        }

        public static IsolatedFootingModel? DeserializeFooting(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) return null;
            return JsonSerializer.Deserialize<IsolatedFootingModel>(json, JsonOptions);
        }

        public static string SerializePileCap(PileCapModel cap)
        {
            if (cap == null) throw new ArgumentNullException(nameof(cap));
            return JsonSerializer.Serialize(cap, JsonOptions);
        }

        public static PileCapModel? DeserializePileCap(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) return null;
            return JsonSerializer.Deserialize<PileCapModel>(json, JsonOptions);
        }
    }
}
