using System;

namespace HPDcePro.Core.XData
{
    public class SoDoCatModel
    {
        public string StockBarId { get; set; } = "1";
        public int PatternIndex { get; set; } = 1;
        public string CutEquation { get; set; } = "5800+5800=11600";
        public double WasteMm { get; set; } = 100.0;
        public double WasteRatio { get; set; } = 0.0085;
    }

    /// <summary>
    /// Codec for XDATA_APP_SODOCAT cutting pattern scheme XData.
    /// </summary>
    public class SoDoCatXDataCodec : IXDataCodec<SoDoCatModel>
    {
        public const string ApplicationName = XDataRegistry.RegAppSoDoCat;
        public string RegAppName => ApplicationName;

        public XDataPacket Encode(SoDoCatModel model)
        {
            if (model == null) throw new ArgumentNullException(nameof(model));
            var packet = new XDataPacket(ApplicationName);
            packet.AddString(model.StockBarId);
            packet.AddInt32(model.PatternIndex);
            packet.AddString(model.CutEquation);
            packet.AddReal(model.WasteMm);
            packet.AddReal(model.WasteRatio);
            return packet;
        }

        public bool TryDecode(XDataPacket packet, out SoDoCatModel? result)
        {
            result = null;
            if (packet == null || !string.Equals(packet.RegAppName, ApplicationName, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            var model = new SoDoCatModel
            {
                StockBarId = packet.GetString(0, "1"),
                PatternIndex = packet.GetInt32(1, 1),
                CutEquation = packet.GetString(2, string.Empty),
                WasteMm = packet.GetDouble(3, 0.0),
                WasteRatio = packet.GetDouble(4, 0.0)
            };

            result = model;
            return true;
        }

        public SoDoCatModel Decode(XDataPacket packet)
        {
            if (!TryDecode(packet, out var result) || result == null)
            {
                throw new InvalidOperationException($"Failed to decode XData packet for application '{ApplicationName}'.");
            }
            return result;
        }
    }
}
