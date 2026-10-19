using System;

namespace HPDcePro.Core.XData
{
    public class SysAppModel
    {
        public string AppVersion { get; set; } = "HPDcePro_2026";
        public string ProjectCode { get; set; } = "PRJ-01";
        public string Timestamp { get; set; } = string.Empty;
    }

    /// <summary>
    /// Codec for SYS_XData_App project and system metadata XData.
    /// </summary>
    public class SysAppXDataCodec : IXDataCodec<SysAppModel>
    {
        public const string ApplicationName = XDataRegistry.RegAppSysApp;
        public string RegAppName => ApplicationName;

        public XDataPacket Encode(SysAppModel model)
        {
            if (model == null) throw new ArgumentNullException(nameof(model));
            var packet = new XDataPacket(ApplicationName);
            packet.AddString(model.AppVersion);
            packet.AddString(model.ProjectCode);
            packet.AddString(model.Timestamp);
            return packet;
        }

        public bool TryDecode(XDataPacket packet, out SysAppModel? result)
        {
            result = null;
            if (packet == null || !string.Equals(packet.RegAppName, ApplicationName, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            var model = new SysAppModel
            {
                AppVersion = packet.GetString(0, "HPDcePro_2026"),
                ProjectCode = packet.GetString(1, "PRJ-01"),
                Timestamp = packet.GetString(2, string.Empty)
            };

            result = model;
            return true;
        }

        public SysAppModel Decode(XDataPacket packet)
        {
            if (!TryDecode(packet, out var result) || result == null)
            {
                throw new InvalidOperationException($"Failed to decode XData packet for application '{ApplicationName}'.");
            }
            return result;
        }
    }
}
