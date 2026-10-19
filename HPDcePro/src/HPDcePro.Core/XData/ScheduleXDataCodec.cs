using System;
using System.Globalization;

namespace HPDcePro.Core.XData
{
    public class DceRebarScheduleModel
    {
        public int SoLuong { get; set; } = 1;
        public int DuongKinh { get; set; } = 20;
        public double KC1 { get; set; } = 0.0;
        public double KC2 { get; set; } = 0.0;
        public double KC3 { get; set; } = 0.0;
        public double KC4 { get; set; } = 0.0;
        public string SoHieu { get; set; } = string.Empty;
        public double ChieuDaiL { get; set; } = 0.0;

        public string ToCanonicalString()
        {
            return string.Format(
                CultureInfo.InvariantCulture,
                "SL={0};DK={1};KC1={2:0.##};KC2={3:0.##};KC3={4:0.##};KC4={5:0.##};SH={6};L={7:0.##}",
                SoLuong, DuongKinh, KC1, KC2, KC3, KC4, SoHieu, ChieuDaiL);
        }
    }

    /// <summary>
    /// Codec for DceRebarXDataTKT canonical semicolon-delimited scheduling strings.
    /// </summary>
    public class ScheduleXDataCodec : IXDataCodec<DceRebarScheduleModel>
    {
        public const string ApplicationName = "DceRebarXDataTKT";
        public string RegAppName => ApplicationName;

        public XDataPacket Encode(DceRebarScheduleModel model)
        {
            if (model == null) throw new ArgumentNullException(nameof(model));
            var packet = new XDataPacket(ApplicationName);
            packet.AddString(model.ToCanonicalString());
            return packet;
        }

        public bool TryDecode(XDataPacket packet, out DceRebarScheduleModel? result)
        {
            result = null;
            if (packet == null || !string.Equals(packet.RegAppName, ApplicationName, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            string raw = packet.GetString(0, string.Empty);
            if (string.IsNullOrWhiteSpace(raw))
            {
                return false;
            }

            var model = new DceRebarScheduleModel();
            string[] pairs = raw.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (string pair in pairs)
            {
                int eq = pair.IndexOf('=');
                if (eq <= 0 || eq >= pair.Length - 1) continue;

                string key = pair.Substring(0, eq).Trim().ToUpperInvariant();
                string val = pair.Substring(eq + 1).Trim();

                ParseField(model, key, val);
            }

            result = model;
            return true;
        }

        private static void ParseField(DceRebarScheduleModel model, string key, string val)
        {
            switch (key)
            {
                case "SL":
                    if (int.TryParse(val, NumberStyles.Integer, CultureInfo.InvariantCulture, out int sl)) model.SoLuong = sl;
                    break;
                case "DK":
                    if (int.TryParse(val, NumberStyles.Integer, CultureInfo.InvariantCulture, out int dk)) model.DuongKinh = dk;
                    break;
                case "KC1":
                    if (double.TryParse(val, NumberStyles.Float, CultureInfo.InvariantCulture, out double kc1)) model.KC1 = kc1;
                    break;
                case "KC2":
                    if (double.TryParse(val, NumberStyles.Float, CultureInfo.InvariantCulture, out double kc2)) model.KC2 = kc2;
                    break;
                case "KC3":
                    if (double.TryParse(val, NumberStyles.Float, CultureInfo.InvariantCulture, out double kc3)) model.KC3 = kc3;
                    break;
                case "KC4":
                    if (double.TryParse(val, NumberStyles.Float, CultureInfo.InvariantCulture, out double kc4)) model.KC4 = kc4;
                    break;
                case "SH":
                    model.SoHieu = val;
                    break;
                case "L":
                    if (double.TryParse(val, NumberStyles.Float, CultureInfo.InvariantCulture, out double l)) model.ChieuDaiL = l;
                    break;
            }
        }

        public DceRebarScheduleModel Decode(XDataPacket packet)
        {
            if (!TryDecode(packet, out var result) || result == null)
            {
                throw new InvalidOperationException($"Failed to decode XData packet for application '{ApplicationName}'.");
            }
            return result;
        }
    }
}
