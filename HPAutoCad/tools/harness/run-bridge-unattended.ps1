# Unattended phase-2 run: starts AutoCAD with bridge.scr (HPMCPBRIDGE + HPMCPSTART), answers SECURELOAD,
# waits for the pipe, ticks the "Allow AI code execution" box through UI Automation (the opt-in is never
# persisted by design), runs the Python pipe scenarios, then the opt-in-off and no-document checks, and
# finally kills AutoCAD (nothing in the bridge may quit the host).
param([int]$StartupTimeoutSec = 420)

# The harness closes drawings without saving, types into AutoCAD through COM and kills acad.exe at the end:
# it must only ever touch the AutoCAD it started itself.
if (Get-Process acad -ErrorAction SilentlyContinue) { throw 'Close every AutoCAD before running the harness: it discards drawings and kills acad.exe.' }

$sig = @'
using System; using System.Text; using System.Runtime.InteropServices;
public static class Native {
  public delegate bool Proc(IntPtr h, IntPtr l);
  [DllImport("user32.dll")] public static extern IntPtr FindWindow(string cls, string title);
  [DllImport("user32.dll")] static extern bool EnumChildWindows(IntPtr parent, Proc p, IntPtr l);
  [DllImport("user32.dll")] static extern int GetWindowText(IntPtr h, StringBuilder t, int n);
  [DllImport("user32.dll")] public static extern IntPtr SendMessage(IntPtr h, uint msg, IntPtr w, IntPtr l);
  public static IntPtr FindButton(IntPtr parent, string text) {
    IntPtr found = IntPtr.Zero;
    EnumChildWindows(parent, (h, l) => { var sb = new StringBuilder(256); GetWindowText(h, sb, 256); if (sb.ToString() == text) { found = h; return false; } return true; }, IntPtr.Zero);
    return found; }
}
'@
Add-Type -TypeDefinition $sig
Add-Type -AssemblyName UIAutomationClient; Add-Type -AssemblyName UIAutomationTypes

function Answer-SecureLoad {
    $dlg = [Native]::FindWindow('#32770', 'Security - Unsigned Executable File')
    if ($dlg -eq [IntPtr]::Zero) { return $false }
    $btn = [Native]::FindButton($dlg, 'Always Load')
    if ($btn -ne [IntPtr]::Zero) { [Native]::SendMessage($btn, 0x00F5, [IntPtr]::Zero, [IntPtr]::Zero) | Out-Null; "SECURELOAD answered 'Always Load' (trusts the bundle folder permanently) at $(Get-Date -Format HH:mm:ss)"; return $true }
    return $false
}

function Find-BridgeWindow {
    # The modeless WPF window is owned by AutoCAD's frame, so UIA lists it under that frame, not under the root.
    $root = [System.Windows.Automation.AutomationElement]::RootElement
    $cond = New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::ProcessIdProperty, [int]$script:acadPid)
    for ($i = 0; $i -lt 4; $i++) {
        try { $w = $root.FindFirst([System.Windows.Automation.TreeScope]::Children, $cond); if ($w) { return $w } }
        catch { Write-Host "UIA lookup retry $($i + 1): $($_.Exception.Message)" }
        Start-Sleep -Seconds 3
    }
    return $null
}

function Set-OptIn([bool]$on) {
    $win = Find-BridgeWindow
    if (-not $win) { Write-Host "opt-in: bridge window not found"; return $false }
    Write-Host "opt-in: window found '$($win.Current.Name)' class $($win.Current.ClassName)"
    $cond = New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::AutomationIdProperty, 'AllowExecution')
    $box = $win.FindFirst([System.Windows.Automation.TreeScope]::Descendants, $cond)
    if (-not $box) { Write-Host "opt-in: checkbox not found"; return $false }
    $toggle = $box.GetCurrentPattern([System.Windows.Automation.TogglePattern]::Pattern)
    $isOn = $toggle.Current.ToggleState -eq [System.Windows.Automation.ToggleState]::On
    if ($isOn -ne $on) { $toggle.Toggle(); Start-Sleep -Milliseconds 700 }
    $isOn = $toggle.Current.ToggleState -eq [System.Windows.Automation.ToggleState]::On
    Write-Host "opt-in now $isOn"
    return ($isOn -eq $on)
}

$log = "$env:LOCALAPPDATA\HPAutoCad\McpBridge\logs"
$scr = Join-Path $PSScriptRoot 'bridge.scr'
$py = Join-Path $PSScriptRoot 'pipe-scenarios.py'
$bridgeLog = Get-ChildItem "$log\mcpbridge-*.log" -ErrorAction SilentlyContinue | Sort-Object LastWriteTime | Select-Object -Last 1
$bridgeLines = if ($bridgeLog) { (Get-Content $bridgeLog.FullName).Count } else { 0 }

$p = Start-Process -FilePath 'C:\Program Files\Autodesk\AutoCAD 2026\acad.exe' -ArgumentList @('/nologo', '/product', 'ACAD', '/language', '"en-US"', '/b', "`"$scr`"") -PassThru
$script:acadPid = $p.Id
$env:HP_HARNESS_ACAD_PID = $p.Id   # every COM call checks it talks to this process and no other
"acad pid $($p.Id) started $(Get-Date -Format HH:mm:ss)"

$sw = [Diagnostics.Stopwatch]::StartNew()
$pipeUp = $false
while ($sw.Elapsed.TotalSeconds -lt $StartupTimeoutSec) {
    Answer-SecureLoad | Out-Null
    if (Test-Path '\\.\pipe\hpautocad-mcp-2026') { $pipeUp = $true; "pipe up after $([int]$sw.Elapsed.TotalSeconds) s"; break }
    if ($p.HasExited) { "acad exited early (code $($p.ExitCode))"; break }
    Start-Sleep -Seconds 2
}

$env:HP_HARNESS_KEYS = '1'
$env:PYTHONIOENCODING = 'utf-8'
try {
    if ($pipeUp) {
        Start-Sleep -Seconds 3
        "=== opt-in OFF: expect -32001"
        $null = Set-OptIn $false
        python $py --only disabled
        "=== opt-in ON: main scenarios"
        if (Set-OptIn $true) {
            python $py
            "=== no document: expect -32003"
            python $py --com "`$a.ActiveDocument.Close(`$false); 'closed via COM, docs left: ' + `$a.Documents.Count"
            Start-Sleep -Seconds 3
            python $py --only nodoc
            "=== busy: LINE waiting for input, expect -32002 after the 10 s grace"
            python $py --com "`$null = `$a.Documents.Add(); 'new drawing, docs: ' + `$a.Documents.Count"
            Start-Sleep -Seconds 3
            python $py --only busy
        }
    }
}
finally {
    "=== killing acad"
    if (-not $p.HasExited) { Stop-Process -Id $p.Id -Force -Confirm:$false }
}

"=== audit lines"
Get-ChildItem "$env:APPDATA\HPAutoCad\McpBridge\audit\*.log" -ErrorAction SilentlyContinue | % { "$($_.Name): $((Get-Content $_.FullName).Count) lines" }
"=== new bridge log lines (filtered)"
$bridgeLog = Get-ChildItem "$log\mcpbridge-*.log" | Sort-Object LastWriteTime | Select-Object -Last 1
Get-Content $bridgeLog.FullName | Select-Object -Skip $bridgeLines | Select-String -Pattern 'starting|ready|listening|connected|refused|failed|Error|WRN|ERR|window|stopped' | Select-Object -First 60
