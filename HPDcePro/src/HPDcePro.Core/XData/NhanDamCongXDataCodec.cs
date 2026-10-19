using System;

namespace HPDcePro.Core.XData
{
    public class NhanDamCongModel
    {
        public string BeamMark { get; set; } = "DC-1";
        public double RadiusMm { get; set; } = 8500.0;
        public double ArcAngleDeg { get; set; } = 45.0;
        public double SectionWidthMm { get; set; } = 220.0;
        public double SectionHeightMm { get; set; } = 400.0;
    }

    /// <summary>
    /// Codec for NHAN_XDATA_DAM_CONG curved beam annotation XData.
    /// </summary>
    public class NhanDamCongXDataCodec : IXDataCodec<NhanDamCongModel>
    {
        public const string ApplicationName = XDataRegistry.RegAppNhanDamCong;
        public string RegAppName => ApplicationName;

        public XDataPacket Encode(NhanDamCongModel model)
        {
            if (model == null) throw new ArgumentNullException(nameof(model));
            var packet = new XDataPacket(ApplicationName);
            packet.AddString(model.BeamMark);
            packet.AddReal(model.RadiusMm);
            packet.AddReal(model.ArcAngleDeg);
            packet.AddReal(model.SectionWidthMm);
            packet.AddReal(model.SectionHeightMm);
            return packet;
        }

        public bool TryDecode(XDataPacket packet, out NhanDamCongModel? result)
        {
            result = null;
            if (packet == null || !string.Equals(packet.RegAppName, ApplicationName, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            var model = new NhanDamCongModel
            {
                BeamMark = packet.GetString(0, "DC-1"),
                RadiusMm = packet.GetDouble(1, 8500.0),
                ArcAngleDeg = packet.GetDouble(2, 45.0),
                SectionWidthMm = packet.GetDouble(3, 220.0),
                SectionHeightMm = packet.GetDouble(4, 400.0)
            };

            result = model;
            return true;
        }

        public NhanDamCongModel Decode(XDataPacket packet)
        {
            if (!TryDecode(packet, out var result) || result == null)
            {
                throw new InvalidOperationException($"Failed to decode XData packet for application '{ApplicationName}'.");
            }
            return result;
        }
    }
}
