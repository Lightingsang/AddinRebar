<#
.SYNOPSIS
    Drives the Kata windows of the running Revit through UI Automation for the live check (scoped to the Revit pid):
    opens a ribbon command of the HPRebar tab, waits for a window, presses buttons by AutomationId or text, reads
    texts and list items, screenshots and closes the window. Windows PowerShell 5.1 (self-relaunch).
.EXAMPLE
    revit-kata-ui.ps1 -Ribbon 'Kata Export' -Window 'Xuất Dầm sang KATA Excel' -Click 'Xuất Excel' -Wait 4 -Close
    revit-kata-ui.ps1 -Window 'HPRebar — Bố trí' -Click KataRebarGenerate -Wait 20 -Read KataRebarStatus -List KataRebarMessages
#>
param(
    [string] $Ribbon,
    [Parameter(Mandatory)] [string] $Window,
    [string[]] $Click = @(),
    [int] $Wait = 3,
    [string[]] $Read = @(),
    [string] $List,
    [string] $Shot,
    [switch] $Close)
if ($PSVersionTable.PSEdition -ne 'Desktop') {
    $ps = Join-Path $env:SystemRoot 'System32\WindowsPowerShell\v1.0\powershell.exe'
    $a = @('-NoProfile', '-ExecutionPolicy', 'Bypass', '-File', $PSCommandPath, '-Window', $Window, '-Wait', $Wait)
    if ($Ribbon) { $a += @('-Ribbon', $Ribbon) }
    if ($Click.Count) { $a += '-Click'; $a += ($Click -join ',') }
    if ($Read.Count) { $a += '-Read'; $a += ($Read -join ',') }
    if ($List) { $a += @('-List', $List) }
    if ($Shot) { $a += @('-Shot', $Shot) }
    if ($Close) { $a += '-Close' }
    & $ps @a
    exit $LASTEXITCODE
}
$Click = @($Click | ForEach-Object { $_ -split ',' } | Where-Object { $_ })
$Read = @($Read | ForEach-Object { $_ -split ',' } | Where-Object { $_ })
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName UIAutomationClient; Add-Type -AssemblyName UIAutomationTypes; Add-Type -AssemblyName System.Drawing; Add-Type -AssemblyName System.Windows.Forms
$sig = @'
using System; using System.Text; using System.Runtime.InteropServices;
public static class KataUi {
  public delegate bool Proc(IntPtr h, IntPtr l);
  [DllImport("user32.dll")] static extern bool EnumWindows(Proc p, IntPtr l);
  [DllImport("user32.dll", CharSet = CharSet.Unicode)] static extern int GetWindowText(IntPtr h, StringBuilder t, int n);
  [DllImport("user32.dll")] static extern uint GetWindowThreadProcessId(IntPtr h, out uint pid);
  [DllImport("user32.dll")] static extern bool IsWindowVisible(IntPtr h);
  [DllImport("user32.dll")] public static extern bool PrintWindow(IntPtr hwnd, IntPtr hdc, uint flags);
  [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr hwnd, out RECT r);
  [DllImport("user32.dll")] public static extern bool PostMessage(IntPtr h, uint msg, IntPtr w, IntPtr l);
  [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr h);
  [DllImport("user32.dll")] public static extern void mouse_event(uint f, uint x, uint y, uint d, UIntPtr e);
  [StructLayout(LayoutKind.Sequential)] public struct RECT { public int L, T, R, B; }
  public static IntPtr FindTopLevel(uint pid, string titleStart) {
    IntPtr found = IntPtr.Zero;
    EnumWindows((h, l) => { uint p; GetWindowThreadProcessId(h, out p); if (p != pid || !IsWindowVisible(h)) return true; var sb = new StringBuilder(512); GetWindowText(h, sb, 512); if (sb.ToString().StartsWith(titleStart)) { found = h; return false; } return true; }, IntPtr.Zero);
    return found; }
}
'@
if (-not ('KataUi' -as [type])) { Add-Type -TypeDefinition $sig }
$UIA = [System.Windows.Automation.AutomationElement]
$revit = Get-Process Revit | Select-Object -First 1
if (-not $revit) { throw 'Revit is not running' }

function Matches([System.Windows.Automation.AutomationElement] $scope, [string] $key) {
    $byId = $scope.FindAll([System.Windows.Automation.TreeScope]::Descendants, (New-Object System.Windows.Automation.PropertyCondition($UIA::AutomationIdProperty, $key)))
    if ($byId.Count -gt 0) { return $byId }
    return $scope.FindAll([System.Windows.Automation.TreeScope]::Descendants, (New-Object System.Windows.Automation.PropertyCondition($UIA::NameProperty, $key)))
}
function Press([System.Windows.Automation.AutomationElement] $scope, [string] $key) {
    $walker = [System.Windows.Automation.TreeWalker]::ControlViewWalker
    foreach ($el in (Matches $scope $key)) {
        $cur = $el
        for ($i = 0; $i -lt 4 -and $null -ne $cur; $i++) {
            $pat = $null
            if ($cur.TryGetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern, [ref]$pat)) {
                if (-not $cur.Current.IsEnabled) { return "$key -> DISABLED" }
                $pat.Invoke(); return "$key -> invoke"
            }
            if ($cur.TryGetCurrentPattern([System.Windows.Automation.SelectionItemPattern]::Pattern, [ref]$pat)) { $pat.Select(); return "$key -> select" }
            $cur = $walker.GetParent($cur)
        }
    }
    foreach ($el in (Matches $scope $key)) {
        $r = $el.Current.BoundingRectangle
        if ($r.Width -le 0 -or $el.Current.IsOffscreen) { continue }
        [System.Windows.Forms.Cursor]::Position = New-Object System.Drawing.Point([int]($r.X + $r.Width / 2), [int]($r.Y + $r.Height / 2))
        [KataUi]::mouse_event(2, 0, 0, 0, [UIntPtr]::Zero); Start-Sleep -Milliseconds 80; [KataUi]::mouse_event(4, 0, 0, 0, [UIntPtr]::Zero)
        return "$key -> clicked"
    }
    throw "'$key' not found"
}

if ($Ribbon) {
    $main = $UIA::RootElement.FindFirst([System.Windows.Automation.TreeScope]::Children, (New-Object System.Windows.Automation.PropertyCondition($UIA::ProcessIdProperty, $revit.Id)))
    Press $main 'HPRebar'; Start-Sleep -Milliseconds 800
    Press $main $Ribbon
}

$h = [IntPtr]::Zero; $sw = [Diagnostics.Stopwatch]::StartNew()
while ($sw.Elapsed.TotalSeconds -lt 30 -and $h -eq [IntPtr]::Zero) { Start-Sleep -Milliseconds 500; $h = [KataUi]::FindTopLevel([uint32]$revit.Id, $Window) }
if ($h -eq [IntPtr]::Zero) { throw "window '$Window' not found" }
Start-Sleep -Seconds 2
$win = $UIA::FromHandle($h)
foreach ($key in $Click) { Press $win $key; Start-Sleep -Seconds $Wait }

foreach ($key in $Read) {
    foreach ($el in (Matches $win $key)) { "READ ${key}: $($el.Current.Name)"; break }
}
if ($List) {
    $box = Matches $win $List | Select-Object -First 1
    if ($box) {
        $items = $box.FindAll([System.Windows.Automation.TreeScope]::Descendants, (New-Object System.Windows.Automation.PropertyCondition($UIA::ControlTypeProperty, [System.Windows.Automation.ControlType]::Text)))
        foreach ($t in $items) { if ($t.Current.Name -and $t.Current.Name -ne '• ') { "ITEM: $($t.Current.Name)" } }
    }
}
if ($Shot) {
    $r = New-Object KataUi+RECT; [KataUi]::GetWindowRect($h, [ref]$r) | Out-Null
    $bmp = New-Object System.Drawing.Bitmap(($r.R - $r.L), ($r.B - $r.T)); $g = [System.Drawing.Graphics]::FromImage($bmp); $hdc = $g.GetHdc()
    [KataUi]::PrintWindow($h, $hdc, 2) | Out-Null; $g.ReleaseHdc($hdc); $bmp.Save($Shot, [System.Drawing.Imaging.ImageFormat]::Png); $g.Dispose(); $bmp.Dispose()
    "SHOT $Shot"
}
if ($Close) { [KataUi]::PostMessage($h, 0x0010, [IntPtr]::Zero, [IntPtr]::Zero) | Out-Null; 'closed' }
