using System;

namespace HPDcePro.Core.XData
{
    public class ThangMoiModel
    {
        public string StairMark { get; set; } = "THANG-1";
        public double FloorHeightMm { get; set; } = 3600.0;
        public int RiserCount { get; set; } = 21;
        public double TreadWidthMm { get; set; } = 280.0;
        public double WaistThicknessMm { get; set; } = 120.0;
        public string MainRebar { get; set; } = "Phi12 a150";
    }

    /// <summary>
    /// Codec for SYS_XData_ThangMoi staircase parameterization XData.
    /// </summary>
    public class ThangMoiXDataCodec : IXDataCodec<ThangMoiModel>
    {
        public const string ApplicationName = XDataRegistry.RegAppThangMoi;
        public string RegAppName => ApplicationName;

        public XDataPacket Encode(ThangMoiModel model)
        {
            if (model == null) throw new ArgumentNullException(nameof(model));
            var packet = new XDataPacket(ApplicationName);
            packet.AddString(model.StairMark);
            packet.AddReal(model.FloorHeightMm);
            packet.AddInt32(model.RiserCount);
            packet.AddReal(model.TreadWidthMm);
            packet.AddReal(model.WaistThicknessMm);
            packet.AddString(model.MainRebar);
            return packet;
        }

        public bool TryDecode(XDataPacket packet, out ThangMoiModel? result)
        {
            result = null;
            if (packet == null || !string.Equals(packet.RegAppName, ApplicationName, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            var model = new ThangMoiModel
            {
                StairMark = packet.GetString(0, "THANG-1"),
                FloorHeightMm = packet.GetDouble(1, 3600.0),
                RiserCount = packet.GetInt32(2, 21),
                TreadWidthMm = packet.GetDouble(3, 280.0),
                WaistThicknessMm = packet.GetDouble(4, 120.0),
                MainRebar = packet.GetString(5, "Phi12 a150")
            };

            result = model;
            return true;
        }

        public ThangMoiModel Decode(XDataPacket packet)
        {
            if (!TryDecode(packet, out var result) || result == null)
            {
                throw new InvalidOperationException($"Failed to decode XData packet for application '{ApplicationName}'.");
            }
            return result;
        }
    }
}
