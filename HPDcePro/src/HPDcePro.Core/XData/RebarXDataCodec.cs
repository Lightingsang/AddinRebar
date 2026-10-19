using System;
using System.Globalization;

namespace HPDcePro.Core.XData
{
    public class RebarXdataDceModel
    {
        public string TenCauKien { get; set; } = string.Empty;
        public string SoHieu { get; set; } = string.Empty;
        public string MaThep { get; set; } = string.Empty;
        public string DuongKinh { get; set; } = "20";
        public string SoLuong { get; set; } = "1";
        public string LoaiThep { get; set; } = "CTop";
        public double DoanA { get; set; } = 0.0;
        public double DoanB { get; set; } = 0.0;
        public double DoanC { get; set; } = 0.0;
        public double DoanD { get; set; } = 0.0;
        public double ChieuDaiL { get; set; } = 0.0;
        public short HinhDang { get; set; } = 1;
    }

    /// <summary>
    /// Codec for RebarXdataDCE 12-element ordered XData array.
    /// </summary>
    public class RebarXDataCodec : IXDataCodec<RebarXdataDceModel>
    {
        public const string ApplicationName = "RebarXdataDCE";
        public string RegAppName => ApplicationName;

        public XDataPacket Encode(RebarXdataDceModel model)
        {
            if (model == null) throw new ArgumentNullException(nameof(model));
            var packet = new XDataPacket(ApplicationName);
            packet.AddString(model.TenCauKien);
            packet.AddString(model.SoHieu);
            packet.AddString(model.MaThep);
            packet.AddString(model.DuongKinh);
            packet.AddString(model.SoLuong);
            packet.AddString(model.LoaiThep);
            packet.AddReal(model.DoanA);
            packet.AddReal(model.DoanB);
            packet.AddReal(model.DoanC);
            packet.AddReal(model.DoanD);
            packet.AddReal(model.ChieuDaiL);
            packet.AddInt16(model.HinhDang);
            return packet;
        }

        public bool TryDecode(XDataPacket packet, out RebarXdataDceModel? result)
        {
            result = null;
            if (packet == null || !string.Equals(packet.RegAppName, ApplicationName, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            var model = new RebarXdataDceModel
            {
                TenCauKien = packet.GetString(0, string.Empty),
                SoHieu = packet.GetString(1, string.Empty),
                MaThep = packet.GetString(2, string.Empty),
                DuongKinh = packet.GetString(3, "20"),
                SoLuong = packet.GetString(4, "1"),
                LoaiThep = packet.GetString(5, "CTop"),
                DoanA = packet.GetDouble(6, 0.0),
                DoanB = packet.GetDouble(7, 0.0),
                DoanC = packet.GetDouble(8, 0.0),
                DoanD = packet.GetDouble(9, 0.0),
                ChieuDaiL = packet.GetDouble(10, 0.0),
                HinhDang = packet.GetInt16(11, 1)
            };

            result = model;
            return true;
        }

        public RebarXdataDceModel Decode(XDataPacket packet)
        {
            if (!TryDecode(packet, out var result) || result == null)
            {
                throw new InvalidOperationException($"Failed to decode XData packet for application '{ApplicationName}'.");
            }
            return result;
        }
    }
}
