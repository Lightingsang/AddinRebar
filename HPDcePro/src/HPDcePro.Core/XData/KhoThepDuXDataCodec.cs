using System;

namespace HPDcePro.Core.XData
{
    public class KhoThepDuModel
    {
        public string OffcutId { get; set; } = "DU-001";
        public int DiameterMm { get; set; } = 20;
        public double LengthMm { get; set; } = 3500.0;
        public string StorageRack { get; set; } = "RACK-A1";
    }

    /// <summary>
    /// Codec for XDATA_APP_KHOTHEPDU inventory remnant/offcut rebar XData.
    /// </summary>
    public class KhoThepDuXDataCodec : IXDataCodec<KhoThepDuModel>
    {
        public const string ApplicationName = XDataRegistry.RegAppKhoThepDu;
        public string RegAppName => ApplicationName;

        public XDataPacket Encode(KhoThepDuModel model)
        {
            if (model == null) throw new ArgumentNullException(nameof(model));
            var packet = new XDataPacket(ApplicationName);
            packet.AddString(model.OffcutId);
            packet.AddInt32(model.DiameterMm);
            packet.AddReal(model.LengthMm);
            packet.AddString(model.StorageRack);
            return packet;
        }

        public bool TryDecode(XDataPacket packet, out KhoThepDuModel? result)
        {
            result = null;
            if (packet == null || !string.Equals(packet.RegAppName, ApplicationName, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            var model = new KhoThepDuModel
            {
                OffcutId = packet.GetString(0, "DU-001"),
                DiameterMm = packet.GetInt32(1, 20),
                LengthMm = packet.GetDouble(2, 0.0),
                StorageRack = packet.GetString(3, "RACK-A1")
            };

            result = model;
            return true;
        }

        public KhoThepDuModel Decode(XDataPacket packet)
        {
            if (!TryDecode(packet, out var result) || result == null)
            {
                throw new InvalidOperationException($"Failed to decode XData packet for application '{ApplicationName}'.");
            }
            return result;
        }
    }
}
