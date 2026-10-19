using System;
using System.Collections.Generic;

namespace HPDcePro.Core.XData
{
    /// <summary>
    /// Master registry of the 13 DCE Pro AutoCAD Extended Entity Data (XData) application schemas.
    /// Provides corruption recovery and packet normalization.
    /// </summary>
    public static class XDataRegistry
    {
        public const string RegAppRebarXdataDCE = "RebarXdataDCE";
        public const string RegAppDceRebarXDataTKT = "DceRebarXDataTKT";
        public const string RegAppLinksXDataObject = "Links_XDataObject";
        public const string RegAppDceThepKep = "Dce_ThepKep";
        public const string RegAppSoDoCat = "XDATA_APP_SODOCAT";
        public const string RegAppKhoThepDu = "XDATA_APP_KHOTHEPDU";
        public const string RegAppMatBangMong = "XDATA_MAT_BANG_MONG";
        public const string RegAppMongCoc = "XDATA_MONG_COC";
        public const string RegAppThangMoi = "SYS_XData_ThangMoi";
        public const string RegAppSysApp = "SYS_XData_App";
        public const string RegAppNhanCoc = "NHAN_XDATA_COC";
        public const string RegAppNhanDamCong = "NHAN_XDATA_DAM_CONG";
        public const string RegAppNhanMb = "NHAN_XDATA_MB";

        private static readonly HashSet<string> s_knownApps = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            RegAppRebarXdataDCE,
            RegAppDceRebarXDataTKT,
            RegAppLinksXDataObject,
            RegAppDceThepKep,
            RegAppSoDoCat,
            RegAppKhoThepDu,
            RegAppMatBangMong,
            RegAppMongCoc,
            RegAppThangMoi,
            RegAppSysApp,
            RegAppNhanCoc,
            RegAppNhanDamCong,
            RegAppNhanMb
        };

        public static IReadOnlyCollection<string> KnownRegApps => s_knownApps;

        public static bool IsDceRegApp(string? appName)
        {
            return !string.IsNullOrEmpty(appName) && s_knownApps.Contains(appName!);
        }

        public static XDataPacket RecoverMalformedPacket(XDataPacket packet)
        {
            if (packet == null)
            {
                throw new ArgumentNullException(nameof(packet));
            }

            var cleanPacket = new XDataPacket(packet.RegAppName);
            int braceCount = 0;

            foreach (var (code, val) in packet.Values)
            {
                if (code == DxfCodeConstants.ExtendedDataControlString)
                {
                    string s = val?.ToString() ?? string.Empty;
                    if (s == "{") braceCount++;
                    else if (s == "}") braceCount--;
                }
                cleanPacket.Values.Add((code, val ?? string.Empty));
            }

            // Fix unclosed control braces
            while (braceCount > 0)
            {
                cleanPacket.AddControlString("}");
                braceCount--;
            }

            return cleanPacket;
        }
    }
}
