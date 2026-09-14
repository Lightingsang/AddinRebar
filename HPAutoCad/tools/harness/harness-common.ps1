# Shared by the harness scripts: the SECURELOAD auto-answer, the UI Automation opt-in toggle and the
# guarded AutoCAD start. Dot-source it: `. (Join-Path $PSScriptRoot 'harness-common.ps1')`.
# Every script that loads this closes drawings without saving and kills acad.exe at the end, so it only
# ever touches the AutoCAD it started itself.

function Assert-NoAutocadRunning {
    if (Get-Process acad -ErrorAction SilentlyContinue) { throw 'Close every AutoCAD before running the harness: it discards drawings and kills acad.exe.' }
}

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


# Starts acad.exe with the given script, answers SECURELOAD, waits for the bridge pipe. Returns the process.
function Start-AcadWithBridge([string]$scriptPath, [int]$timeoutSec = 420) {
    $p = Start-Process -FilePath 'C:\Program Files\Autodesk\AutoCAD 2026\acad.exe' -ArgumentList @('/nologo', '/product', 'ACAD', '/language', '"en-US"', '/b', "`"$scriptPath`"") -PassThru
    $script:acadPid = $p.Id
    $env:HP_HARNESS_ACAD_PID = $p.Id   # every COM call checks it talks to this process and no other
    Write-Host "acad pid $($p.Id) started $(Get-Date -Format HH:mm:ss)"

    $sw = [Diagnostics.Stopwatch]::StartNew()
    $script:pipeUp = $false
    while ($sw.Elapsed.TotalSeconds -lt $timeoutSec) {
        Answer-SecureLoad | Out-Null
        if (Test-Path '\\.\pipe\hpautocad-mcp-2026') { $script:pipeUp = $true; Write-Host "pipe up after $([int]$sw.Elapsed.TotalSeconds) s"; break }
        if ($p.HasExited) { Write-Host "acad exited early (code $($p.ExitCode))"; break }
        Start-Sleep -Seconds 2
    }
    return $p
}

# ---- Ribbon (UI Automation) -------------------------------------------------------------------------------------
# AdWindows exposes the Ribbon through WPF automation peers: a tab header is a Button whose AutomationId is the
# RibbonTab.Id and whose Name is its title; a RibbonButton is a Button named after its text (newlines become
# spaces) once its tab is selected. Everything is looked up under the AutoCAD frame of the harness's own pid.

function Get-AcadFrame([int]$processId) {
    $root = [System.Windows.Automation.AutomationElement]::RootElement
    $byPid = New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::ProcessIdProperty, $processId)
    $frames = $root.FindAll([System.Windows.Automation.TreeScope]::Children, $byPid)
    foreach ($f in $frames) { if ($f.Current.ClassName -like 'Afx*') { return $f } }
    if ($frames.Count -gt 0) { return $frames[0] }
    return $null
}

function Find-RibbonTabs([int]$processId, [string]$tabId) {
    $frame = Get-AcadFrame $processId
    if (-not $frame) { return @() }
    $cond = New-Object System.Windows.Automation.AndCondition(
        (New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::ControlTypeProperty, [System.Windows.Automation.ControlType]::Button)),
        (New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::AutomationIdProperty, $tabId)))
    $found = $frame.FindAll([System.Windows.Automation.TreeScope]::Descendants, $cond)
    $list = @(); foreach ($t in $found) { $list += $t }
    return $list
}

function Select-RibbonTab([int]$processId, [string]$tabId) {
    $tabs = Find-RibbonTabs $processId $tabId
    if ($tabs.Count -eq 0) { return $false }
    try { $tabs[0].GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern).Invoke(); Start-Sleep -Milliseconds 1500; return $true } catch { Write-Host "select tab failed: $($_.Exception.Message)"; return $false }
}

function Find-RibbonButton([int]$processId, [string]$namePattern) {
    $frame = Get-AcadFrame $processId
    if (-not $frame) { return $null }
    $cond = New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::ControlTypeProperty, [System.Windows.Automation.ControlType]::Button)
    foreach ($b in $frame.FindAll([System.Windows.Automation.TreeScope]::Descendants, $cond)) {
        $name = ($b.Current.Name -replace "`r?`n", ' ')
        if ($name -match $namePattern) { return $b }
    }
    return $null
}

function Invoke-RibbonButton([int]$processId, [string]$namePattern) {
    $button = Find-RibbonButton $processId $namePattern
    if (-not $button) { Write-Host "ribbon button /$namePattern/ not found"; return $false }
    try { $button.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern).Invoke(); return $true }
    catch { Write-Host "invoke /$namePattern/ failed: $($_.Exception.Message)"; return $false }
}
