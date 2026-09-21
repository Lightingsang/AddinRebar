# Shared by the SAP2000 harness scripts. Windows PowerShell 5.1 (powershell.exe): it starts HPSap2000.McpBridge.exe (our own
# desktop app — SAP2000 itself is never started, stopped or given keystrokes by the harness) and uses UI Automation on that
# window only: tick the two opt-ins, start the listener, click Attach/Detach, read the attach state. Dot-source it:
# `. (Join-Path $PSScriptRoot 'harness-common.ps1')`. Every script that loads this closes the bridge it started at the end.

if ($PSVersionTable.PSEdition -ne 'Desktop') { throw 'Run the SAP2000 harness with Windows PowerShell 5.1 (powershell.exe).' }

$script:PipeName = 'hpsap2000-mcp-27'
$script:WindowTitle = 'HPSap2000 MCP Bridge'
$script:LogDir = Join-Path $env:LOCALAPPDATA 'HPSap2000\McpBridge\logs'
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

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr OpenDesktop(string lpszDesktop, uint dwFlags, bool fInherit, uint dwDesiredAccess);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool SetThreadDesktop(IntPtr hDesktop);

    public static int StartBridge(string exe)
    {
        var si = new STARTUPINFO();
        si.cb = Marshal.SizeOf(si);
        si.lpDesktop = @"WinSta0\Default";
        var pi = new PROCESS_INFORMATION();
        bool ok = CreateProcess(null, "\"" + exe + "\"", IntPtr.Zero, IntPtr.Zero, false, 0, IntPtr.Zero, null, ref si, out pi);
        if (!ok) throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
        return pi.dwProcessId;
    }

    private static T RunOnDefault<T>(Func<T> action)
    {
        T result = default(T);
        Exception ex = null;
        var t = new System.Threading.Thread(() =>
        {
            try
            {
                IntPtr h = OpenDesktop("Default", 0, false, 0x01FF);
                if (h != IntPtr.Zero) SetThreadDesktop(h);
                result = action();
            }
            catch (Exception e) { ex = e; }
        });
        t.Start();
        t.Join();
        if (ex != null) throw ex;
        return result;
    }

    private static System.Windows.Automation.AutomationElement GetWindow(int pid)
    {
        var root = System.Windows.Automation.AutomationElement.RootElement;
        var byPid = new System.Windows.Automation.PropertyCondition(System.Windows.Automation.AutomationElement.ProcessIdProperty, pid);
        foreach (System.Windows.Automation.AutomationElement top in root.FindAll(System.Windows.Automation.TreeScope.Children, byPid))
        {
            if (top.Current.Name == "HPSap2000 MCP Bridge") return top;
        }
        return null;
    }

    public static bool HasWindow(int pid)
    {
        return RunOnDefault(() => GetWindow(pid) != null);
    }

    public static bool InvokeButton(int pid, string automationId)
    {
        return RunOnDefault(() =>
        {
            var win = GetWindow(pid);
            if (win == null) return false;
            var byBtn = new System.Windows.Automation.PropertyCondition(System.Windows.Automation.AutomationElement.AutomationIdProperty, automationId);
            var btn = win.FindFirst(System.Windows.Automation.TreeScope.Descendants, byBtn);
            if (btn == null || !btn.Current.IsEnabled) return false;
            var invoke = btn.GetCurrentPattern(System.Windows.Automation.InvokePattern.Pattern) as System.Windows.Automation.InvokePattern;
            if (invoke == null) return false;
            invoke.Invoke();
            return true;
        });
    }

    public static bool SetOptIn(int pid, string automationId, bool on)
    {
        return RunOnDefault(() =>
        {
            var win = GetWindow(pid);
            if (win == null) return false;
            var cond = new System.Windows.Automation.PropertyCondition(System.Windows.Automation.AutomationElement.AutomationIdProperty, automationId);
            var box = win.FindFirst(System.Windows.Automation.TreeScope.Descendants, cond);
            if (box == null) return false;
            var toggle = box.GetCurrentPattern(System.Windows.Automation.TogglePattern.Pattern) as System.Windows.Automation.TogglePattern;
            if (toggle == null) return false;
            bool isOn = toggle.Current.ToggleState == System.Windows.Automation.ToggleState.On;
            if (isOn != on)
            {
                toggle.Toggle();
                System.Threading.Thread.Sleep(700);
            }
            isOn = toggle.Current.ToggleState == System.Windows.Automation.ToggleState.On;
            return isOn == on;
        });
    }

    public static string ReadText(int pid, string automationId)
    {
        return RunOnDefault(() =>
        {
            var win = GetWindow(pid);
            if (win == null) return null;
            var cond = new System.Windows.Automation.PropertyCondition(System.Windows.Automation.AutomationElement.AutomationIdProperty, automationId);
            var el = win.FindFirst(System.Windows.Automation.TreeScope.Descendants, cond);
            return el != null ? el.Current.Name : null;
        });
    }

    public static void CloseBridge(int pid)
    {
        RunOnDefault(() =>
        {
            var win = GetWindow(pid);
            if (win != null)
            {
                var wp = win.GetCurrentPattern(System.Windows.Automation.WindowPattern.Pattern) as System.Windows.Automation.WindowPattern;
                if (wp != null) wp.Close();
            }
            return true;
        });
    }
}
"@

function Assert-NoBridgeRunning {
    if (Get-Process HPSap2000.McpBridge -ErrorAction SilentlyContinue) { throw 'Close every HPSap2000.McpBridge.exe before running the harness: it starts and closes its own.' }
}

function Assert-Sap2000Running {
    if (-not (Get-Process SAP2000 -ErrorAction SilentlyContinue)) { throw 'Start SAP2000 27 with a throw-away model first: the harness attaches to it and never starts it.' }
}

# Starts the bridge exe on WinSta0\Default and waits for its window. Returns the Process.
function Start-Bridge([string]$exe, [int]$timeoutSec = 40) {
    $script:bridgePid = [BridgeLauncher]::StartBridge($exe)
    $script:bridgeWindow = $null
    $proc = [System.Diagnostics.Process]::GetProcessById($script:bridgePid)
    Write-Host "bridge pid $script:bridgePid started $(Get-Date -Format HH:mm:ss)"
    $sw = [Diagnostics.Stopwatch]::StartNew()
    while ($sw.Elapsed.TotalSeconds -lt $timeoutSec) {
        Start-Sleep -Seconds 1
        $proc.Refresh()
        if ($proc.HasExited) { throw "bridge exited during startup with code $($proc.ExitCode)" }
        if ([BridgeLauncher]::HasWindow($script:bridgePid)) { Write-Host "bridge window after $([int]$sw.Elapsed.TotalSeconds) s"; return $proc }
    }
    throw "bridge window did not appear within $timeoutSec s"
}

function Invoke-BridgeButton([string]$automationId) {
    $ok = [BridgeLauncher]::InvokeButton($script:bridgePid, $automationId)
    if ($ok) { Write-Host "button ${automationId}: invoked" }
    else { Write-Host "button ${automationId}: failed or not found" }
    return $ok
}

function Set-OptIn([string]$automationId, [bool]$on) {
    $ok = [BridgeLauncher]::SetOptIn($script:bridgePid, $automationId, $on)
    Write-Host "opt-in ${automationId} now $ok"
    return $ok
}

function Read-BridgeText([string]$automationId) {
    return [BridgeLauncher]::ReadText($script:bridgePid, $automationId)
}

function Wait-BridgeText([string]$automationId, [string]$pattern, [int]$timeoutSec = 30) {
    $sw = [Diagnostics.Stopwatch]::StartNew()
    while ($sw.Elapsed.TotalSeconds -lt $timeoutSec) {
        $t = Read-BridgeText $automationId
        if ($t -and $t -match $pattern) { return $t }
        Start-Sleep -Milliseconds 700
    }
    return $null
}

function Wait-Pipe([int]$timeoutSec = 60) {
    $sw = [Diagnostics.Stopwatch]::StartNew()
    while ($sw.Elapsed.TotalSeconds -lt $timeoutSec) {
        if (Test-Path "\\.\pipe\$script:PipeName") { Write-Host "pipe up after $([int]$sw.Elapsed.TotalSeconds) s"; return $true }
        Start-Sleep -Seconds 1
    }
    Write-Host 'pipe did not appear'
    return $false
}

function Get-BridgeLogTail([int]$lines = 60) {
    $log = Get-ChildItem $script:LogDir -Filter 'mcpbridge-*.log' -ErrorAction SilentlyContinue | Sort-Object LastWriteTime | Select-Object -Last 1
    if ($log) { Get-Content $log.FullName -Tail $lines } else { @('<no bridge log>') }
}

# Closes only the bridge this harness started: the Close button first (App.OnExit stops the worker), then a kill.
function Stop-Bridge($proc) {
    if (-not $proc) { return }
    try {
        $proc.Refresh()
        if (-not $proc.HasExited) {
            [BridgeLauncher]::CloseBridge($proc.Id)
            $sw = [Diagnostics.Stopwatch]::StartNew()
            while (-not $proc.HasExited -and $sw.Elapsed.TotalSeconds -lt 8) { Start-Sleep -Milliseconds 500; $proc.Refresh() }
        }
    } catch { Write-Host "close failed: $($_.Exception.Message)" }
    $proc.Refresh()
    if (-not $proc.HasExited) { Write-Host 'bridge did not exit after Close; killing'; Stop-Process -Id $proc.Id -Force -ErrorAction SilentlyContinue }
    else { Write-Host "bridge exited with code $($proc.ExitCode)" }
}
