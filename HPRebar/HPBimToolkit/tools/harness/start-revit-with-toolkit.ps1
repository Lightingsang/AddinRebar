<#
.SYNOPSIS
    Starts a SECOND Revit 2026 on a copy of a model with the HP BIM Toolkit's WebView2 DevTools port enabled, answers the
    "Unsigned Add-In" prompt with Load Once for that Revit only, then opens HPRebar ▸ BIM Toolkit ▸ Dashboard by UI
    Automation. Never touches a Revit it did not start. Windows PowerShell 5.1 (self-relaunch from pwsh).
.OUTPUTS
    Prints "REVIT_PID=<pid>" and "DEVTOOLS=http://127.0.0.1:<port>" for the CDP scripts beside this one.
#>
param(
    [string] $Model = 'C:\ProgramData\Autodesk\RVT 2026\Dynamo\Samples\Data\DynamoSampleModel.rvt',
    [string] $RunFolder = (Join-Path $env:LOCALAPPDATA 'HPBimToolkit\live-verify'),
    [int] $DebugPort = 9333,
    [int] $TimeoutSec = 240
)
if ($PSVersionTable.PSEdition -ne 'Desktop') {
    $ps = Join-Path $env:SystemRoot 'System32\WindowsPowerShell\v1.0\powershell.exe'
    & $ps -NoProfile -ExecutionPolicy Bypass -File $PSCommandPath -Model $Model -RunFolder $RunFolder -DebugPort $DebugPort -TimeoutSec $TimeoutSec
    exit $LASTEXITCODE
}
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName UIAutomationClient; Add-Type -AssemblyName UIAutomationTypes
Add-Type -AssemblyName System.Windows.Forms; Add-Type -AssemblyName System.Drawing
$sig = @'
using System; using System.Text; using System.Runtime.InteropServices;
public static class TkWin {
  public delegate bool Proc(IntPtr h, IntPtr l);
  [DllImport("user32.dll")] static extern bool EnumWindows(Proc p, IntPtr l);
  [DllImport("user32.dll")] static extern bool EnumChildWindows(IntPtr parent, Proc p, IntPtr l);
  [DllImport("user32.dll")] static extern int GetWindowText(IntPtr h, StringBuilder t, int n);
  [DllImport("user32.dll")] static extern uint GetWindowThreadProcessId(IntPtr h, out uint pid);
  [DllImport("user32.dll")] static extern bool IsWindowVisible(IntPtr h);
  [DllImport("user32.dll")] public static extern IntPtr SendMessage(IntPtr h, uint msg, IntPtr w, IntPtr l);
  [DllImport("user32.dll")] public static extern void mouse_event(uint f, uint x, uint y, uint d, UIntPtr e);
  public static IntPtr FindTitled(uint pid, string title, bool exact) {
    IntPtr found = IntPtr.Zero;
    EnumWindows((h, l) => { uint p; GetWindowThreadProcessId(h, out p); if (p != pid || !IsWindowVisible(h)) return true;
      var sb = new StringBuilder(512); GetWindowText(h, sb, 512); var t = sb.ToString();
      if (exact ? t == title : t.Contains(title)) { found = h; return false; } return true; }, IntPtr.Zero);
    return found; }
  public static IntPtr FindButton(IntPtr parent, string text) {
    IntPtr found = IntPtr.Zero;
    EnumChildWindows(parent, (h, l) => { var sb = new StringBuilder(256); GetWindowText(h, sb, 256);
      if (sb.ToString().Replace("&", "") == text) { found = h; return false; } return true; }, IntPtr.Zero);
    return found; }
}
'@
if (-not ('TkWin' -as [type])) { Add-Type -TypeDefinition $sig }

$revitExe = 'C:\Program Files\Autodesk\Revit 2026\Revit.exe'
if (-not (Test-Path $Model)) { throw "model not found: $Model" }
New-Item -ItemType Directory -Force $RunFolder | Out-Null
$copy = Join-Path $RunFolder ('toolkit-' + (Get-Date -Format 'yyyyMMdd-HHmmss') + '.rvt')
Copy-Item $Model $copy

# Started through `cmd /c start` so Revit is not a child of this script (cmd exits at once): stopping the harness must
# never kill Revit. The debug port travels as an inherited environment variable.
$env:HPBIM_TOOLKIT_DEBUG_PORT = "$DebugPort"
$before = @(Get-Process Revit -ErrorAction SilentlyContinue | ForEach-Object Id)
Start-Process -FilePath cmd.exe -ArgumentList "/c start `"`" `"$revitExe`" `"$copy`"" -WindowStyle Hidden
$sw0 = [Diagnostics.Stopwatch]::StartNew(); $revitPid = 0
while ($revitPid -eq 0 -and $sw0.Elapsed.TotalSeconds -lt 30) {
    $new = @(Get-Process Revit -ErrorAction SilentlyContinue | Where-Object { $before -notcontains $_.Id })
    if ($new.Count -gt 0) { $revitPid = [uint32]$new[0].Id } else { Start-Sleep -Milliseconds 500 }
}
if ($revitPid -eq 0) { throw 'Revit did not start' }
"REVIT_PID=$revitPid"
"MODEL_COPY=$copy"

# Answer every unsigned-add-in prompt of this Revit with Load Once until the model's window title appears.
$modelName = [IO.Path]::GetFileName($copy)
$sw = [Diagnostics.Stopwatch]::StartNew(); $answered = 0; $main = [IntPtr]::Zero
while ($sw.Elapsed.TotalSeconds -lt $TimeoutSec) {
    $dlg = [TkWin]::FindTitled($revitPid, 'Security - Unsigned Add-In', $true)
    if ($dlg -ne [IntPtr]::Zero) {
        $btn = [TkWin]::FindButton($dlg, 'Load Once')
        if ($btn -ne [IntPtr]::Zero) { [TkWin]::SendMessage($btn, 0x00F5, [IntPtr]::Zero, [IntPtr]::Zero) | Out-Null; $answered++; "unsigned prompt answered Load Once ($answered)" }
        Start-Sleep -Seconds 2; continue
    }
    $main = [TkWin]::FindTitled($revitPid, $modelName, $false)
    if ($main -ne [IntPtr]::Zero) { break }
    Start-Sleep -Seconds 1
}
if ($main -eq [IntPtr]::Zero) { throw "Revit $revitPid did not open $modelName within $TimeoutSec s" }
"model open after $([int]$sw.Elapsed.TotalSeconds) s"
Start-Sleep -Seconds 8

# Revit restores the pane if it was open when it last closed. Then the page is already on the debug port and the
# ribbon is not touched: a UIA walk over an open pane enumerates Chromium's accessibility tree and can hang.
try {
    $pages = Invoke-RestMethod "http://127.0.0.1:$DebugPort/json" -TimeoutSec 5
    if (@($pages | Where-Object { $_.url -like 'https://toolkit.hpbim.local/*' }).Count -gt 0) {
        'pane already open (restored by Revit)'
        "DEVTOOLS=http://127.0.0.1:$DebugPort"
        exit 0
    }
} catch { 'pane not open yet' }

$root = [System.Windows.Automation.AutomationElement]::RootElement
$pidCond = New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::ProcessIdProperty, [int]$revitPid)
$uiaMain = $root.FindFirst([System.Windows.Automation.TreeScope]::Children, $pidCond)
if ($null -eq $uiaMain) { throw 'Revit main window not found by UIA' }

# Ribbon names can belong to several elements (tab header, caption, button): invoke/select the first that allows it,
# else click the centre of the first one on screen.
function PressByName([string] $name) {
    $c = New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::NameProperty, $name)
    $all = $uiaMain.FindAll([System.Windows.Automation.TreeScope]::Descendants, $c)
    foreach ($el in $all) {
        $pat = $null
        if ($el.TryGetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern, [ref]$pat)) { $pat.Invoke(); return "$name -> invoke" }
        if ($el.TryGetCurrentPattern([System.Windows.Automation.SelectionItemPattern]::Pattern, [ref]$pat)) { $pat.Select(); return "$name -> select" }
    }
    foreach ($el in $all) {
        $rect = $el.Current.BoundingRectangle
        if ($rect.Width -le 0 -or $el.Current.IsOffscreen) { continue }
        [System.Windows.Forms.Cursor]::Position = New-Object System.Drawing.Point([int]($rect.X + $rect.Width / 2), [int]($rect.Y + $rect.Height / 2))
        [TkWin]::mouse_event(0x0002, 0, 0, 0, [UIntPtr]::Zero); Start-Sleep -Milliseconds 80; [TkWin]::mouse_event(0x0004, 0, 0, 0, [UIntPtr]::Zero)
        return "$name -> clicked"
    }
    throw "'$name' not found on the ribbon ($($all.Count) matches)"
}
PressByName 'HPRebar'
Start-Sleep -Milliseconds 800
PressByName 'Dashboard'
"DEVTOOLS=http://127.0.0.1:$DebugPort"
