# Shared by the Robot harness scripts. Windows PowerShell 5.1 (powershell.exe): starts HPRobot.McpBridge.exe
# and uses UI Automation on that window only: tick opt-ins, start listener, click Attach/Detach.
# Dot-source it: `. (Join-Path $PSScriptRoot 'harness-common.ps1')`.

if ($PSVersionTable.PSEdition -ne 'Desktop') { throw 'Run the Robot harness with Windows PowerShell 5.1 (powershell.exe).' }

$script:PipeName = 'hprobot-mcp-2026'
$script:WindowTitle = 'HPRobot MCP Bridge — 2026'
$script:LogDir = Join-Path $env:LOCALAPPDATA 'HPRobot\McpBridge\logs'
$script:bridgePid = 0
$script:bridgeWindow = $null

Add-Type -ReferencedAssemblies "UIAutomationClient", "UIAutomationTypes" -TypeDefinition @"
using System;
using System.Runtime.InteropServices;

public class BridgeLauncher
{
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct STARTUPINFO
    {
        public int cb;
        public string lpReserved;
        public string lpDesktop;
        public string lpTitle;
        public int dwX;
        public int dwY;
        public int dwXSize;
        public int dwYSize;
        public int dwXCountChars;
        public int dwYCountChars;
        public int dwFillAttribute;
        public int dwFlags;
        public short wShowWindow;
        public short cbReserved2;
        public IntPtr lpReserved2;
        public IntPtr hStdInput;
        public IntPtr hStdOutput;
        public IntPtr hStdError;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct PROCESS_INFORMATION
    {
        public IntPtr hProcess;
        public IntPtr hThread;
        public int dwProcessId;
        public int dwThreadId;
    }

    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern bool CreateProcess(
        string lpApplicationName,
        string lpCommandLine,
        IntPtr lpProcessAttributes,
        IntPtr lpThreadAttributes,
        bool bInheritHandles,
        uint dwCreationFlags,
        IntPtr lpEnvironment,
        string lpCurrentDirectory,
        ref STARTUPINFO lpStartupInfo,
        out PROCESS_INFORMATION lpProcessInformation);

    public static int Launch(string exePath, string arguments)
    {
        STARTUPINFO si = new STARTUPINFO();
        si.cb = Marshal.SizeOf(si);
        PROCESS_INFORMATION pi = new PROCESS_INFORMATION();
        string cmd = "\"" + exePath + "\" " + arguments;
        bool res = CreateProcess(null, cmd, IntPtr.Zero, IntPtr.Zero, false, 0, IntPtr.Zero, null, ref si, out pi);
        if (!res) throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
        return pi.dwProcessId;
    }
}
"@ -ErrorAction SilentlyContinue

function Start-BridgeProcess {
    param([string]$ExePath, [string]$Args = "")
    Write-Host "Starting Bridge: $ExePath $Args"
    $pidNum = [BridgeLauncher]::Launch($ExePath, $Args)
    $script:bridgePid = $pidNum
    Start-Sleep -Seconds 2
    return $pidNum
}

function Stop-BridgeProcess {
    if ($script:bridgePid -gt 0) {
        $p = Get-Process -Id $script:bridgePid -ErrorAction SilentlyContinue
        if ($p) {
            Write-Host "Stopping Bridge (PID: $($script:bridgePid))..."
            $p.Kill()
            $p.WaitForExit(3000)
        }
        $script:bridgePid = 0
    }
}
