Add-Type -TypeDefinition @'
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

public class FileLockFinder {
    [DllImport("rstrtmgr.dll", CharSet = CharSet.Unicode)]
    static extern int RmStartSession(out uint pSessionHandle, int dwSessionFlags, string strSessionKey);

    [DllImport("rstrtmgr.dll")]
    static extern int RmEndSession(uint pSessionHandle);

    [DllImport("rstrtmgr.dll", CharSet = CharSet.Unicode)]
    static extern int RmRegisterResources(uint pSessionHandle, uint nFiles, string[] rgsFilenames,
        uint nApplications, [In] uint[] rgApplications, uint nServices, [In] string[] rgsServiceNames);

    [DllImport("rstrtmgr.dll")]
    static extern int RmGetList(uint pSessionHandle, out uint pnProcInfoNeeded,
        ref uint pnProcInfo, [In, Out] RM_PROCESS_INFO[] rgAffectedApps, ref uint lpdwRebootReasons);

    [StructLayout(LayoutKind.Sequential)]
    public struct RM_UNIQUE_PROCESS {
        public int dwProcessId;
        public System.Runtime.InteropServices.ComTypes.FILETIME ProcessStartTime;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct RM_PROCESS_INFO {
        public RM_UNIQUE_PROCESS Process;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
        public string strAppName;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
        public string strServiceShortName;
        public int ApplicationType;
        public uint AppStatus;
        public uint TSSessionId;
        [MarshalAs(UnmanagedType.Bool)]
        public bool bRestartable;
    }

    public static List<int> WhoIsLocking(string path) {
        uint handle;
        string key = Guid.NewGuid().ToString();
        int res = RmStartSession(out handle, 0, key);
        if (res != 0) return new List<int>();

        try {
            string[] files = new string[] { path };
            res = RmRegisterResources(handle, (uint)files.Length, files, 0, null, 0, null);
            if (res != 0) return new List<int>();

            uint procInfoNeeded = 0;
            uint procInfo = 0;
            uint rebootReasons = 0;
            res = RmGetList(handle, out procInfoNeeded, ref procInfo, null, ref rebootReasons);

            if (procInfoNeeded == 0) return new List<int>();

            RM_PROCESS_INFO[] infos = new RM_PROCESS_INFO[procInfoNeeded];
            procInfo = procInfoNeeded;
            res = RmGetList(handle, out procInfoNeeded, ref procInfo, infos, ref rebootReasons);

            var list = new List<int>();
            for (int i = 0; i < procInfo; i++) {
                list.Add(infos[i].Process.dwProcessId);
            }
            return list;
        } finally {
            RmEndSession(handle);
        }
    }
}
'@

param(
    [string]$FilePath = 'g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPDynamo\HPDynamo.McpBridge.Tests\bin\Debug\net10.0-windows7.0\TestResults\HPDynamo.McpBridge.Tests_net10.0-windows7.0_x64.log'
)
$path = $FilePath
$pids = [FileLockFinder]::WhoIsLocking($path)
if ($pids.Count -eq 0) {
    Write-Host "No process locking the file."
} else {
    foreach ($p in $pids) {
        Get-Process -Id $p -ErrorAction SilentlyContinue | Select-Object Id, ProcessName, Path | Format-Table -AutoSize
    }
}
