using System;

namespace HPDcePro.Core.XData
{
    public class MongCocModel
    {
        public string CapMark { get; set; } = "DC-1";
        public double DimX { get; set; } = 1800.0;
        public double DimY { get; set; } = 1800.0;
        public double HeightH { get; set; } = 900.0;
        public string RebarBottomX { get; set; } = "Phi16 a150";
        public string RebarBottomY { get; set; } = "Phi16 a150";
        public double ConcreteVolumeM3 { get; set; } = 2.916;
    }

    /// <summary>
    /// Codec for XDATA_MONG_COC pile cap detailing XData.
    /// </summary>
    public class MongCocXDataCodec : IXDataCodec<MongCocModel>
    {
        public const string ApplicationName = XDataRegistry.RegAppMongCoc;
        public string RegAppName => ApplicationName;

        public XDataPacket Encode(MongCocModel model)
        {
            if (model == null) throw new ArgumentNullException(nameof(model));
            var packet = new XDataPacket(ApplicationName);
            packet.AddString(model.CapMark);
            packet.AddReal(model.DimX);
            packet.AddReal(model.DimY);
            packet.AddReal(model.HeightH);
            packet.AddString(model.RebarBottomX);
            packet.AddString(model.RebarBottomY);
            packet.AddReal(model.ConcreteVolumeM3);
            return packet;
        }

        public bool TryDecode(XDataPacket packet, out MongCocModel? result)
        {
            result = null;
            if (packet == null || !string.Equals(packet.RegAppName, ApplicationName, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            var model = new MongCocModel
            {
                CapMark = packet.GetString(0, "DC-1"),
                DimX = packet.GetDouble(1, 1800.0),
                DimY = packet.GetDouble(2, 1800.0),
                HeightH = packet.GetDouble(3, 900.0),
                RebarBottomX = packet.GetString(4, "Phi16 a150"),
                RebarBottomY = packet.GetString(5, "Phi16 a150"),
                ConcreteVolumeM3 = packet.GetDouble(6, 0.0)
            };

            result = model;
            return true;
        }

        public MongCocModel Decode(XDataPacket packet)
        {
            if (!TryDecode(packet, out var result) || result == null)
            {
                throw new InvalidOperationException($"Failed to decode XData packet for application '{ApplicationName}'.");
            }
            return result;
        }
    }
}
