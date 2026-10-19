using System;

namespace HPDcePro.Core.XData
{
    public class MatBangMongModel
    {
        public string GridMark { get; set; } = "DC-1";
        public double TopElevationM { get; set; } = -1.600;
        public double BottomElevationM { get; set; } = -2.500;
        public string PileType { get; set; } = "D400";
        public int PileCount { get; set; } = 4;
    }

    /// <summary>
    /// Codec for XDATA_MAT_BANG_MONG foundation layout plan XData.
    /// </summary>
    public class MatBangMongXDataCodec : IXDataCodec<MatBangMongModel>
    {
        public const string ApplicationName = XDataRegistry.RegAppMatBangMong;
        public string RegAppName => ApplicationName;

        public XDataPacket Encode(MatBangMongModel model)
        {
            if (model == null) throw new ArgumentNullException(nameof(model));
            var packet = new XDataPacket(ApplicationName);
            packet.AddString(model.GridMark);
            packet.AddReal(model.TopElevationM);
            packet.AddReal(model.BottomElevationM);
            packet.AddString(model.PileType);
            packet.AddInt32(model.PileCount);
            return packet;
        }

        public bool TryDecode(XDataPacket packet, out MatBangMongModel? result)
        {
            result = null;
            if (packet == null || !string.Equals(packet.RegAppName, ApplicationName, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            var model = new MatBangMongModel
            {
                GridMark = packet.GetString(0, "DC-1"),
                TopElevationM = packet.GetDouble(1, -1.600),
                BottomElevationM = packet.GetDouble(2, -2.500),
                PileType = packet.GetString(3, "D400"),
                PileCount = packet.GetInt32(4, 4)
            };

            result = model;
            return true;
        }

        public MatBangMongModel Decode(XDataPacket packet)
        {
            if (!TryDecode(packet, out var result) || result == null)
            {
                throw new InvalidOperationException($"Failed to decode XData packet for application '{ApplicationName}'.");
            }
            return result;
        }
    }
}
