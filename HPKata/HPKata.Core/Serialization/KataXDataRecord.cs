using System;
using System.Collections.Generic;

namespace HPKata.Core.Serialization
{
    /// <summary>
    /// Data model representing the structured XData payload under RegApp "KATA" (DxfCodes 1000, 1070, 1005).
    /// </summary>
    public class KataXDataRecord
    {
        public const string RegAppName = "KATA";

        public string Dang { get; set; } = "01";
        public short STT { get; set; } = 1;
        public short Phi { get; set; } = 16;
        public string KhoangCach { get; set; } = string.Empty;
        public string SoLuong { get; set; } = "1";
        public short SoLuongCauKien { get; set; } = 1;
        public double[] ChieuDaiCacDoanMm { get; set; } = Array.Empty<double>();
        public string GocUon { get; set; } = string.Empty;
        public string TenCauKien { get; set; } = string.Empty;
        public string SttThepChinh { get; set; } = string.Empty;
        public string ChuThich { get; set; } = string.Empty;
        public string Z { get; set; } = string.Empty;

        // Dynamic 2-way handle links (DxfCode 1005)
        public string LinkedHandle { get; set; } = string.Empty;
        public string LinkedDimHandle { get; set; } = string.Empty;
        public string LinkType { get; set; } = "THEP_DON";

        public string FormatLengths() =>
            string.Join(";", Array.ConvertAll(ChieuDaiCacDoanMm, d => d.ToString("F0", System.Globalization.CultureInfo.InvariantCulture)));

        public void ParseLengths(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
            {
                ChieuDaiCacDoanMm = Array.Empty<double>();
                return;
            }

            string[] tokens = raw.Split(';');
            var list = new List<double>(tokens.Length);
            foreach (string t in tokens)
            {
                if (double.TryParse(t.Trim(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out double val))
                {
                    list.Add(val);
                }
            }
            ChieuDaiCacDoanMm = list.ToArray();
        }
    }
}
