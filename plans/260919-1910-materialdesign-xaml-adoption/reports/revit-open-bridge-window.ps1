<#
.SYNOPSIS
    Opens HPRebar ▸ MCP ▸ MCP Bridge in the running Revit through UI Automation (the ribbon is AdWindows, like AutoCAD's:
    the tab header is a Button named after the tab, the command a Button named after its text), screenshots the
    bridge window (PrintWindow) and leaves it open. Windows PowerShell 5.1 (self-relaunch), UIA scoped to the Revit pid.
#>
param([string] $OutDir = '.', [switch] $CloseAfter)
if ($PSVersionTable.PSEdition -ne 'Desktop') {
    $ps = Join-Path $env:SystemRoot 'System32\WindowsPowerShell\v1.0\powershell.exe'
    & $ps -NoProfile -ExecutionPolicy Bypass -File $PSCommandPath -OutDir $OutDir @(if ($CloseAfter) { '-CloseAfter' })
    exit $LASTEXITCODE
}
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName UIAutomationClient; Add-Type -AssemblyName UIAutomationTypes; Add-Type -AssemblyName System.Drawing
$sig = @'
using System; using System.Text; using System.Runtime.InteropServices;
public static class RvShot {
  public delegate bool Proc(IntPtr h, IntPtr l);
  [DllImport("user32.dll")] static extern bool EnumWindows(Proc p, IntPtr l);
  [DllImport("user32.dll")] static extern int GetWindowText(IntPtr h, StringBuilder t, int n);
  [DllImport("user32.dll")] static extern uint GetWindowThreadProcessId(IntPtr h, out uint pid);
  [DllImport("user32.dll")] static extern bool IsWindowVisible(IntPtr h);
  [DllImport("user32.dll")] public static extern bool PrintWindow(IntPtr hwnd, IntPtr hdc, uint flags);
  [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr hwnd, out RECT r);
  [DllImport("user32.dll")] public static extern bool PostMessage(IntPtr h, uint msg, IntPtr w, IntPtr l);
  [StructLayout(LayoutKind.Sequential)] public struct RECT { public int L, T, R, B; }
  public static IntPtr FindTopLevel(uint pid, string titleStart) {
    IntPtr found = IntPtr.Zero;
    EnumWindows((h, l) => { uint p; GetWindowThreadProcessId(h, out p); if (p != pid || !IsWindowVisible(h)) return true; var sb = new StringBuilder(256); GetWindowText(h, sb, 256); if (sb.ToString().StartsWith(titleStart)) { found = h; return false; } return true; }, IntPtr.Zero);
    return found; }
}
'@
if (-not ('RvShot' -as [type])) { Add-Type -TypeDefinition $sig }
$revit = Get-Process Revit | Select-Object -First 1
$root = [System.Windows.Automation.AutomationElement]::RootElement
$pidCond = New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::ProcessIdProperty, $revit.Id)
$main = $root.FindFirst([System.Windows.Automation.TreeScope]::Children, $pidCond)
if ($null -eq $main) { throw 'Revit main window not found by UIA' }
function Find([System.Windows.Automation.AutomationElement] $scope, [string] $name) {
    $c = New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::NameProperty, $name)
    return $scope.FindFirst([System.Windows.Automation.TreeScope]::Descendants, $c)
}
function Press([System.Windows.Automation.AutomationElement] $el) {
    $p = $null
    if ($el.TryGetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern, [ref]$p)) { $p.Invoke(); return 'invoke' }
    if ($el.TryGetCurrentPattern([System.Windows.Automation.SelectionItemPattern]::Pattern, [ref]$p)) { $p.Select(); return 'select' }
    throw "no Invoke/SelectionItem pattern on '$($el.Current.Name)'"
}
$sigc = 'using System; using System.Runtime.InteropServices; public static class RvClick { [DllImport("user32.dll")] public static extern void mouse_event(uint f, uint x, uint y, uint d, UIntPtr e); }'
if (-not ('RvClick' -as [type])) { Add-Type -TypeDefinition $sigc }
Add-Type -AssemblyName System.Windows.Forms
# Several elements can carry a ribbon name (tab header, tab page, caption text, the button itself): take the first one
# that can be invoked/selected, else click the centre of the first on-screen one's bounding rectangle.
function PressByName([System.Windows.Automation.AutomationElement] $scope, [string] $name) {
    $c = New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::NameProperty, $name)
    $all = $scope.FindAll([System.Windows.Automation.TreeScope]::Descendants, $c)
    $kinds = @($all | ForEach-Object { $_.Current.ControlType.ProgrammaticName.Replace('ControlType.', '') + '/' + $_.Current.ClassName }) -join ', '
    foreach ($el in $all) {
        $pat = $null
        if ($el.TryGetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern, [ref]$pat)) { $pat.Invoke(); return "$name -> invoke ($kinds)" }
        if ($el.TryGetCurrentPattern([System.Windows.Automation.SelectionItemPattern]::Pattern, [ref]$pat)) { $pat.Select(); return "$name -> select ($kinds)" }
    }
    foreach ($el in $all) {
        $rect = $el.Current.BoundingRectangle
        if ($rect.Width -le 0 -or $rect.Height -le 0 -or $el.Current.IsOffscreen) { continue }
        $x = [int]($rect.X + $rect.Width / 2); $y = [int]($rect.Y + $rect.Height / 2)
        [System.Windows.Forms.Cursor]::Position = New-Object System.Drawing.Point($x, $y)
        [RvClick]::mouse_event(0x0002, 0, 0, 0, [UIntPtr]::Zero); Start-Sleep -Milliseconds 80; [RvClick]::mouse_event(0x0004, 0, 0, 0, [UIntPtr]::Zero)
        return "$name -> clicked at $x,$y ($kinds)"
    }
    throw "'$name' not found on the ribbon ($($all.Count) name matches, none on screen)"
}
PressByName $main 'HPRebar'
Start-Sleep -Milliseconds 800
PressByName $main 'MCP Bridge'
$h = [IntPtr]::Zero; $sw = [Diagnostics.Stopwatch]::StartNew()
while ($sw.Elapsed.TotalSeconds -lt 20 -and $h -eq [IntPtr]::Zero) { Start-Sleep -Milliseconds 500; $h = [RvShot]::FindTopLevel([uint32]$revit.Id, 'HPRebar MCP Bridge') }
if ($h -eq [IntPtr]::Zero) { throw 'bridge window did not appear' }
Start-Sleep -Seconds 3
$r = New-Object RvShot+RECT; [RvShot]::GetWindowRect($h, [ref]$r) | Out-Null
$bmp = New-Object System.Drawing.Bitmap(($r.R - $r.L), ($r.B - $r.T)); $g = [System.Drawing.Graphics]::FromImage($bmp); $hdc = $g.GetHdc()
[RvShot]::PrintWindow($h, $hdc, 2) | Out-Null; $g.ReleaseHdc($hdc)
New-Item -ItemType Directory -Force $OutDir | Out-Null
$file = Join-Path (Resolve-Path $OutDir) 'revit-mcp-bridge-window.png'; $bmp.Save($file, [System.Drawing.Imaging.ImageFormat]::Png); $g.Dispose(); $bmp.Dispose()
"PASS bridge window captured -> $file ($($r.R - $r.L)x$($r.B - $r.T))"
if ($CloseAfter) { [RvShot]::PostMessage($h, 0x0010, [IntPtr]::Zero, [IntPtr]::Zero) | Out-Null; 'window closed' }
