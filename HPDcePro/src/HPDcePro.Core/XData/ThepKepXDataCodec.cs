using System;

namespace HPDcePro.Core.XData
{
    public class ThepKepModel
    {
        public int BundleCount { get; set; } = 2;
        public double SpacingMm { get; set; } = 30.0;
        public double ClearanceMm { get; set; } = 25.0;
        public string PrimaryBarHandle { get; set; } = "0";
    }

    /// <summary>
    /// Codec for Dce_ThepKep bundled/paired reinforcement XData.
    /// </summary>
    public class ThepKepXDataCodec : IXDataCodec<ThepKepModel>
    {
        public const string ApplicationName = XDataRegistry.RegAppDceThepKep;
        public string RegAppName => ApplicationName;

        public XDataPacket Encode(ThepKepModel model)
        {
            if (model == null) throw new ArgumentNullException(nameof(model));
            var packet = new XDataPacket(ApplicationName);
            packet.AddInt16((short)model.BundleCount);
            packet.AddReal(model.SpacingMm);
            packet.AddReal(model.ClearanceMm);
            packet.AddHandle(model.PrimaryBarHandle);
            return packet;
        }

        public bool TryDecode(XDataPacket packet, out ThepKepModel? result)
        {
            result = null;
            if (packet == null || !string.Equals(packet.RegAppName, ApplicationName, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            var model = new ThepKepModel
            {
                BundleCount = packet.GetInt16(0, 2),
                SpacingMm = packet.GetDouble(1, 30.0),
                ClearanceMm = packet.GetDouble(2, 25.0),
                PrimaryBarHandle = packet.GetHandle(3, "0")
            };

            result = model;
            return true;
        }

        public ThepKepModel Decode(XDataPacket packet)
        {
            if (!TryDecode(packet, out var result) || result == null)
            {
                throw new InvalidOperationException($"Failed to decode XData packet for application '{ApplicationName}'.");
            }
            return result;
        }
    }
}
