using System;

namespace HPDcePro.Core.XData
{
    public class NhanMbModel
    {
        public string PlanId { get; set; } = "MBKC_T2";
        public double ElevationM { get; set; } = 3.600;
        public string MemberType { get; set; } = "BEAM";
        public string StoryName { get; set; } = "STORY2";
    }

    /// <summary>
    /// Codec for NHAN_XDATA_MB framing plan structural member annotation XData.
    /// </summary>
    public class NhanMbXDataCodec : IXDataCodec<NhanMbModel>
    {
        public const string ApplicationName = XDataRegistry.RegAppNhanMb;
        public string RegAppName => ApplicationName;

        public XDataPacket Encode(NhanMbModel model)
        {
            if (model == null) throw new ArgumentNullException(nameof(model));
            var packet = new XDataPacket(ApplicationName);
            packet.AddString(model.PlanId);
            packet.AddReal(model.ElevationM);
            packet.AddString(model.MemberType);
            packet.AddString(model.StoryName);
            return packet;
        }

        public bool TryDecode(XDataPacket packet, out NhanMbModel? result)
        {
            result = null;
            if (packet == null || !string.Equals(packet.RegAppName, ApplicationName, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            var model = new NhanMbModel
            {
                PlanId = packet.GetString(0, "MBKC_T2"),
                ElevationM = packet.GetDouble(1, 3.600),
                MemberType = packet.GetString(2, "BEAM"),
                StoryName = packet.GetString(3, "STORY2")
            };

            result = model;
            return true;
        }

        public NhanMbModel Decode(XDataPacket packet)
        {
            if (!TryDecode(packet, out var result) || result == null)
            {
                throw new InvalidOperationException($"Failed to decode XData packet for application '{ApplicationName}'.");
            }
            return result;
        }
    }
}
