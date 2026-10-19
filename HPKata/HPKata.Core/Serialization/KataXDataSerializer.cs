using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace HPKata.Core.Serialization
{
    /// <summary>
    /// Two-way serializer and deserializer for KataPro XData payloads (RegApp "KATA") and Extension Dictionary JSON.
    /// </summary>
    public static class KataXDataSerializer
    {
        public const int DxfCodeExtendedDataRegAppName = 1001;
        public const int DxfCodeExtendedDataAsciiString = 1000;
        public const int DxfCodeExtendedDataInteger16 = 1070;
        public const int DxfCodeExtendedDataHandle = 1005;

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        public static List<KataTypedValue> SerializeToDxf(KataXDataRecord r)
        {
            var list = new List<KataTypedValue>(16);
            list.Add(new(DxfCodeExtendedDataRegAppName, KataXDataRecord.RegAppName));
            list.Add(new(DxfCodeExtendedDataAsciiString, r.Dang ?? string.Empty));
            list.Add(new(DxfCodeExtendedDataInteger16, r.STT));
            list.Add(new(DxfCodeExtendedDataInteger16, r.Phi));
            list.Add(new(DxfCodeExtendedDataAsciiString, r.KhoangCach ?? string.Empty));
            list.Add(new(DxfCodeExtendedDataAsciiString, r.SoLuong ?? string.Empty));
            list.Add(new(DxfCodeExtendedDataInteger16, r.SoLuongCauKien));
            list.Add(new(DxfCodeExtendedDataAsciiString, r.FormatLengths()));
            list.Add(new(DxfCodeExtendedDataAsciiString, r.GocUon ?? string.Empty));
            list.Add(new(DxfCodeExtendedDataAsciiString, r.TenCauKien ?? string.Empty));
            list.Add(new(DxfCodeExtendedDataAsciiString, r.SttThepChinh ?? string.Empty));
            list.Add(new(DxfCodeExtendedDataAsciiString, r.ChuThich ?? string.Empty));
            list.Add(new(DxfCodeExtendedDataAsciiString, r.Z ?? string.Empty));

            if (!string.IsNullOrEmpty(r.LinkedHandle))
            {
                list.Add(new(DxfCodeExtendedDataHandle, r.LinkedHandle));
                list.Add(new(DxfCodeExtendedDataAsciiString, r.LinkType ?? "THEP_DON"));
            }
            if (!string.IsNullOrEmpty(r.LinkedDimHandle))
            {
                list.Add(new(DxfCodeExtendedDataHandle, r.LinkedDimHandle));
            }

            return list;
        }

        public static KataXDataRecord DeserializeFromDxf(IReadOnlyList<KataTypedValue> values)
        {
            var r = new KataXDataRecord();
            if (values == null || values.Count == 0) return r;

            int idx = 0;
            if (values[0].DxfCode == DxfCodeExtendedDataRegAppName &&
                string.Equals(values[0].Value?.ToString(), KataXDataRecord.RegAppName, StringComparison.OrdinalIgnoreCase))
            {
                idx = 1;
            }

            ParseStandardFields(r, values, ref idx);
            ParseHandleFields(r, values, idx);
            return r;
        }

        private static void ParseStandardFields(KataXDataRecord r, IReadOnlyList<KataTypedValue> v, ref int idx)
        {
            if (idx < v.Count && v[idx].DxfCode == DxfCodeExtendedDataAsciiString) r.Dang = v[idx++].Value?.ToString() ?? string.Empty;
            if (idx < v.Count && v[idx].DxfCode == DxfCodeExtendedDataInteger16) r.STT = Convert.ToInt16(v[idx++].Value);
            if (idx < v.Count && v[idx].DxfCode == DxfCodeExtendedDataInteger16) r.Phi = Convert.ToInt16(v[idx++].Value);
            if (idx < v.Count && v[idx].DxfCode == DxfCodeExtendedDataAsciiString) r.KhoangCach = v[idx++].Value?.ToString() ?? string.Empty;
            if (idx < v.Count && v[idx].DxfCode == DxfCodeExtendedDataAsciiString) r.SoLuong = v[idx++].Value?.ToString() ?? string.Empty;
            if (idx < v.Count && v[idx].DxfCode == DxfCodeExtendedDataInteger16) r.SoLuongCauKien = Convert.ToInt16(v[idx++].Value);
            if (idx < v.Count && v[idx].DxfCode == DxfCodeExtendedDataAsciiString) r.ParseLengths(v[idx++].Value?.ToString() ?? string.Empty);
            if (idx < v.Count && v[idx].DxfCode == DxfCodeExtendedDataAsciiString) r.GocUon = v[idx++].Value?.ToString() ?? string.Empty;
            if (idx < v.Count && v[idx].DxfCode == DxfCodeExtendedDataAsciiString) r.TenCauKien = v[idx++].Value?.ToString() ?? string.Empty;
            if (idx < v.Count && v[idx].DxfCode == DxfCodeExtendedDataAsciiString) r.SttThepChinh = v[idx++].Value?.ToString() ?? string.Empty;
            if (idx < v.Count && v[idx].DxfCode == DxfCodeExtendedDataAsciiString) r.ChuThich = v[idx++].Value?.ToString() ?? string.Empty;
            if (idx < v.Count && v[idx].DxfCode == DxfCodeExtendedDataAsciiString) r.Z = v[idx++].Value?.ToString() ?? string.Empty;
        }

        private static void ParseHandleFields(KataXDataRecord r, IReadOnlyList<KataTypedValue> v, int idx)
        {
            while (idx < v.Count)
            {
                if (v[idx].DxfCode == DxfCodeExtendedDataHandle)
                {
                    if (string.IsNullOrEmpty(r.LinkedHandle))
                    {
                        r.LinkedHandle = v[idx++].Value?.ToString() ?? string.Empty;
                        if (idx < v.Count && v[idx].DxfCode == DxfCodeExtendedDataAsciiString)
                        {
                            r.LinkType = v[idx++].Value?.ToString() ?? "THEP_DON";
                        }
                    }
                    else if (string.IsNullOrEmpty(r.LinkedDimHandle))
                    {
                        r.LinkedDimHandle = v[idx++].Value?.ToString() ?? string.Empty;
                    }
                    else
                    {
                        idx++;
                    }
                }
                else
                {
                    idx++;
                }
            }
        }

        public static string SerializeToJson(KataXDataRecord record)
        {
            if (record == null) return "{}";
            return JsonSerializer.Serialize(record, JsonOptions);
        }

        public static KataXDataRecord DeserializeFromJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) return new KataXDataRecord();
            try
            {
                return JsonSerializer.Deserialize<KataXDataRecord>(json, JsonOptions) ?? new KataXDataRecord();
            }
            catch (JsonException)
            {
                // Fallback to empty record on invalid JSON input
                return new KataXDataRecord();
            }
        }
    }
}
