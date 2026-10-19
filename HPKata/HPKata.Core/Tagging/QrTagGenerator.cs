using System;
using System.Globalization;
using System.Linq;
using QRCoder;

namespace HPKata.Core.Tagging
{
    /// <summary>
    /// Generates pipe-delimited payload strings and 2D QR Code barcodes (ECC Level Q) for rebar bundle tags.
    /// </summary>
    public static class QrTagGenerator
    {
        public const string Header = "KATA";

        public static string FormatPayload(QrTagPayload p)
        {
            if (p == null) throw new ArgumentNullException(nameof(p));

            string segs = p.SegmentsMm != null && p.SegmentsMm.Length > 0
                ? string.Join(",", p.SegmentsMm.Select(s => s.ToString("F0", CultureInfo.InvariantCulture)))
                : "0,0,0,0,0,0";

            return string.Join("|",
                Header,
                p.Project ?? string.Empty,
                p.Client ?? string.Empty,
                p.Order ?? string.Empty,
                p.Component ?? string.Empty,
                p.Mark ?? string.Empty,
                p.ShapeCode ?? "01",
                p.DiameterMm.ToString(CultureInfo.InvariantCulture),
                p.CutLengthMm.ToString("F0", CultureInfo.InvariantCulture),
                p.Quantity.ToString(CultureInfo.InvariantCulture),
                p.WeightKg.ToString("F1", CultureInfo.InvariantCulture),
                p.SteelGrade ?? "CB400-V",
                segs);
        }

        public static QrTagPayload ParsePayload(string payload)
        {
            if (string.IsNullOrWhiteSpace(payload)) return new QrTagPayload();

            string[] tokens = payload.Split('|');
            if (tokens.Length < 13 || !string.Equals(tokens[0], Header, StringComparison.OrdinalIgnoreCase))
            {
                return new QrTagPayload();
            }

            var p = new QrTagPayload
            {
                Project = tokens[1],
                Client = tokens[2],
                Order = tokens[3],
                Component = tokens[4],
                Mark = tokens[5],
                ShapeCode = tokens[6],
                DiameterMm = ParseInt(tokens[7]),
                CutLengthMm = ParseDouble(tokens[8]),
                Quantity = ParseInt(tokens[9]),
                WeightKg = ParseDouble(tokens[10]),
                SteelGrade = tokens[11]
            };

            p.SegmentsMm = tokens[12]
                .Split(',')
                .Select(s => ParseDouble(s))
                .ToArray();

            return p;
        }

        public static byte[] GeneratePngBytes(string payload, int pixelsPerModule = 10)
        {
            if (string.IsNullOrEmpty(payload))
            {
                throw new ArgumentException("Payload cannot be null or empty.", nameof(payload));
            }

            using var generator = new QRCodeGenerator();
            using var data = generator.CreateQrCode(payload, QRCodeGenerator.ECCLevel.Q);
            var qrCode = new PngByteQRCode(data);
            return qrCode.GetGraphic(pixelsPerModule);
        }

        private static int ParseInt(string s) =>
            int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out int v) ? v : 0;

        private static double ParseDouble(string s) =>
            double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out double v) ? v : 0.0;
    }
}
