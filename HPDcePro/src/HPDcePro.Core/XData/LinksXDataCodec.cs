using System;

namespace HPDcePro.Core.XData
{
    public class LinksXDataModel
    {
        public string HandleThep { get; set; } = "0";
        public string HandleTag { get; set; } = "0";
        public string HandleDim { get; set; } = "0";
        public string HandleLeader { get; set; } = "0";
        public string LuaChonField { get; set; } = string.Empty;
        public short TrangThaiKhoa { get; set; } = 0;
    }

    /// <summary>
    /// Codec for Links_XDataObject dynamic entity handle associations.
    /// </summary>
    public class LinksXDataCodec : IXDataCodec<LinksXDataModel>
    {
        public const string ApplicationName = "Links_XDataObject";
        public string RegAppName => ApplicationName;

        public XDataPacket Encode(LinksXDataModel model)
        {
            if (model == null) throw new ArgumentNullException(nameof(model));
            var packet = new XDataPacket(ApplicationName);
            packet.AddHandle(model.HandleThep);
            packet.AddHandle(model.HandleTag);
            packet.AddHandle(model.HandleDim);
            packet.AddHandle(model.HandleLeader);
            packet.AddString(model.LuaChonField);
            packet.AddInt16(model.TrangThaiKhoa);
            return packet;
        }

        public bool TryDecode(XDataPacket packet, out LinksXDataModel? result)
        {
            result = null;
            if (packet == null || !string.Equals(packet.RegAppName, ApplicationName, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            var model = new LinksXDataModel
            {
                HandleThep = packet.GetHandle(0, "0"),
                HandleTag = packet.GetHandle(1, "0"),
                HandleDim = packet.GetHandle(2, "0"),
                HandleLeader = packet.GetHandle(3, "0"),
                LuaChonField = packet.GetString(4, string.Empty),
                TrangThaiKhoa = packet.GetInt16(5, 0)
            };

            result = model;
            return true;
        }

        public LinksXDataModel Decode(XDataPacket packet)
        {
            if (!TryDecode(packet, out var result) || result == null)
            {
                throw new InvalidOperationException($"Failed to decode XData packet for application '{ApplicationName}'.");
            }
            return result;
        }
    }
}
