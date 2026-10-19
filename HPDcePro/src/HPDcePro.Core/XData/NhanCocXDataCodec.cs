using System;

namespace HPDcePro.Core.XData
{
    public class NhanCocModel
    {
        public string PileMark { get; set; } = "C1";
        public double DiameterMm { get; set; } = 400.0;
        public double CapacityTons { get; set; } = 65.0;
        public double CutoffElevationM { get; set; } = -2.400;
        public double CoordX { get; set; } = 0.0;
        public double CoordY { get; set; } = 0.0;
    }

    /// <summary>
    /// Codec for NHAN_XDATA_COC pile annotation and coordinate metadata XData.
    /// </summary>
    public class NhanCocXDataCodec : IXDataCodec<NhanCocModel>
    {
        public const string ApplicationName = XDataRegistry.RegAppNhanCoc;
        public string RegAppName => ApplicationName;

        public XDataPacket Encode(NhanCocModel model)
        {
            if (model == null) throw new ArgumentNullException(nameof(model));
            var packet = new XDataPacket(ApplicationName);
            packet.AddString(model.PileMark);
            packet.AddReal(model.DiameterMm);
            packet.AddReal(model.CapacityTons);
            packet.AddReal(model.CutoffElevationM);
            packet.AddReal(model.CoordX);
            packet.AddReal(model.CoordY);
            return packet;
        }

        public bool TryDecode(XDataPacket packet, out NhanCocModel? result)
        {
            result = null;
            if (packet == null || !string.Equals(packet.RegAppName, ApplicationName, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            var model = new NhanCocModel
            {
                PileMark = packet.GetString(0, "C1"),
                DiameterMm = packet.GetDouble(1, 400.0),
                CapacityTons = packet.GetDouble(2, 65.0),
                CutoffElevationM = packet.GetDouble(3, -2.400),
                CoordX = packet.GetDouble(4, 0.0),
                CoordY = packet.GetDouble(5, 0.0)
            };

            result = model;
            return true;
        }

        public NhanCocModel Decode(XDataPacket packet)
        {
            if (!TryDecode(packet, out var result) || result == null)
            {
                throw new InvalidOperationException($"Failed to decode XData packet for application '{ApplicationName}'.");
            }
            return result;
        }
    }
}
