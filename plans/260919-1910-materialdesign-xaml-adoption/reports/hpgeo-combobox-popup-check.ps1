<#
.SYNOPSIS
    Proves the HPGeo dialog's ComboBox popup follows the dark theme (the pre-MaterialDesign known gap: the popup kept
    WPF's light template). Starts its own acad.exe (SECURELOAD answered), draws three points, runs HPGEO with
    COLORTHEME 0, expands the province ComboBox through UIA, captures the popup HWND itself with PrintWindow (own
    windows only, never the desktop) and asserts its mean brightness is dark; then collapses, closes the dialog,
    restores COLORTHEME and quits. Windows PowerShell 5.1.
.EXAMPLE
    powershell -NoProfile -ExecutionPolicy Bypass -File hpgeo-combobox-popup-check.ps1 -OutDir .\hpgeo-live
#>
param([Parameter(Mandatory)] [string] $OutDir, [int] $ColorTheme = 0)
$ErrorActionPreference = 'Stop'
$acad = 'C:\Program Files\Autodesk\AutoCAD 2026\acad.exe'
if (Get-Process acad -ErrorAction SilentlyContinue) { throw 'Close every AutoCAD first.' }
New-Item -ItemType Directory -Force $OutDir | Out-Null
$sig = @'
using System; using System.Text; using System.Collections.Generic; using System.Runtime.InteropServices;
public static class PopWin {
  public delegate bool Proc(IntPtr h, IntPtr l);
  [DllImport("user32.dll")] public static extern IntPtr FindWindow(string cls, string title);
  [DllImport("user32.dll")] static extern bool EnumChildWindows(IntPtr parent, Proc p, IntPtr l);
  [DllImport("user32.dll")] static extern bool EnumWindows(Proc p, IntPtr l);
  [DllImport("user32.dll")] static extern int GetWindowText(IntPtr h, StringBuilder t, int n);
  [DllImport("user32.dll")] static extern int GetClassName(IntPtr h, StringBuilder t, int n);
  [DllImport("user32.dll")] static extern uint GetWindowThreadProcessId(IntPtr h, out uint pid);
  [DllImport("user32.dll")] static extern bool IsWindowVisible(IntPtr h);
  [DllImport("user32.dll")] public static extern IntPtr SendMessage(IntPtr h, uint msg, IntPtr w, IntPtr l);
  [DllImport("user32.dll")] public static extern bool PrintWindow(IntPtr hwnd, IntPtr hdc, uint flags);
  [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr hwnd, out RECT r);
  [StructLayout(LayoutKind.Sequential)] public struct RECT { public int L, T, R, B; }
  public static IntPtr FindButton(IntPtr parent, string text) { IntPtr found = IntPtr.Zero;
    EnumChildWindows(parent, (h, l) => { var sb = new StringBuilder(256); GetWindowText(h, sb, 256); if (sb.ToString() == text) { found = h; return false; } return true; }, IntPtr.Zero); return found; }
  public static IntPtr FindTopLevel(uint pid, string titleStart) { IntPtr found = IntPtr.Zero;
    EnumWindows((h, l) => { uint p; GetWindowThreadProcessId(h, out p); if (p != pid || !IsWindowVisible(h)) return true; var sb = new StringBuilder(256); GetWindowText(h, sb, 256); if (sb.ToString().StartsWith(titleStart)) { found = h; return false; } return true; }, IntPtr.Zero); return found; }
  // Visible top-level windows of the pid with an empty title and a WPF popup class (HwndWrapper[...]) — the ComboBox drop-down.
  public static List<IntPtr> FindPopups(uint pid) { var list = new List<IntPtr>();
    EnumWindows((h, l) => { uint p; GetWindowThreadProcessId(h, out p); if (p != pid || !IsWindowVisible(h)) return true; var sb = new StringBuilder(256); GetWindowText(h, sb, 256); var cls = new StringBuilder(256); GetClassName(h, cls, 256); if (sb.Length == 0 && cls.ToString().StartsWith("HwndWrapper")) list.Add(h); return true; }, IntPtr.Zero); return list; }
}
'@
if (-not ('PopWin' -as [type])) { Add-Type -TypeDefinition $sig }
Add-Type -AssemblyName System.Drawing, UIAutomationClient, UIAutomationTypes
function Answer-SecureLoad {
    $dlg = [PopWin]::FindWindow('#32770', 'Security - Unsigned Executable File')
    if ($dlg -eq [IntPtr]::Zero) { return }
    $btn = [PopWin]::FindButton($dlg, 'Always Load')
    if ($btn -ne [IntPtr]::Zero) { [PopWin]::SendMessage($btn, 0x00F5, [IntPtr]::Zero, [IntPtr]::Zero) | Out-Null; Write-Host "SECURELOAD answered at $(Get-Date -Format HH:mm:ss)" }
}
function Save-Window([IntPtr]$h, [string]$path) {
    $r = New-Object PopWin+RECT; [PopWin]::GetWindowRect($h, [ref]$r) | Out-Null
    $bmp = New-Object System.Drawing.Bitmap(($r.R - $r.L), ($r.B - $r.T)); $g = [System.Drawing.Graphics]::FromImage($bmp); $hdc = $g.GetHdc()
    [PopWin]::PrintWindow($h, $hdc, 2) | Out-Null; $g.ReleaseHdc($hdc)
    $mean = 0.0; $n = 0
    for ($y = 4; $y -lt $bmp.Height - 4; $y += 6) { for ($x = 4; $x -lt $bmp.Width - 4; $x += 6) { $c = $bmp.GetPixel($x, $y); $mean += ($c.R + $c.G + $c.B) / 3.0; $n++ } }
    $bmp.Save($path, [System.Drawing.Imaging.ImageFormat]::Png); $g.Dispose(); $bmp.Dispose()
    return @{ Mean = [math]::Round($mean / [math]::Max($n, 1), 1); Width = ($r.R - $r.L); Height = ($r.B - $r.T) }
}
$p = Start-Process -FilePath $acad -ArgumentList @('/nologo', '/product', 'ACAD', '/language', '"en-US"') -PassThru
Write-Host "acad pid $($p.Id) started $(Get-Date -Format HH:mm:ss)"
$sw = [Diagnostics.Stopwatch]::StartNew()
while ($sw.Elapsed.TotalSeconds -lt 45) { Answer-SecureLoad; if ($p.HasExited) { throw 'acad exited during start-up' }; Start-Sleep -Seconds 2 }
$app = $null; $orig = $null
for ($attempt = 1; $attempt -le 20 -and $null -eq $orig; $attempt++) {
    try {
        $ids = @(Get-Process acad | ForEach-Object Id)
        if ($ids.Count -ne 1 -or [string]$ids[0] -ne [string]$p.Id) { throw 'refusing COM: another acad is running' }
        $app = [Runtime.InteropServices.Marshal]::GetActiveObject('AutoCAD.Application')
        $orig = [int]$app.ActiveDocument.GetVariable('COLORTHEME')
    } catch { if ($_.Exception.Message -match 'refusing COM') { throw }; Start-Sleep -Seconds 3 }
}
if ($null -eq $orig) { Stop-Process -Id $p.Id -Force -Confirm:$false; throw 'no COM' }
Write-Host "user COLORTHEME=$orig"
$results = @()
$job = $null
try {
    $app.ActiveDocument.SetVariable('COLORTHEME', [int16]$ColorTheme)
    # SendCommand blocks for the whole modal dialog: hand it to a job, drive the dialog from here.
    $job = Start-Job -ScriptBlock {
        $a = [Runtime.InteropServices.Marshal]::GetActiveObject('AutoCAD.Application')
        $a.ActiveDocument.SendCommand("_.POINT 600125.887,1231608.428`n_.POINT 600124.9,1231587.7`n_.POINT 600130.5,1231543.7`n_.HPGEO`n`n")
        'sent'
    }
    $h = [IntPtr]::Zero; $sw.Restart()
    while ($sw.Elapsed.TotalSeconds -lt 90 -and $h -eq [IntPtr]::Zero) { Answer-SecureLoad; Start-Sleep -Milliseconds 500; $h = [PopWin]::FindTopLevel([uint32]$p.Id, 'HPGeo') }
    if ($h -eq [IntPtr]::Zero) { throw 'HPGEO dialog did not open' }
    Start-Sleep -Seconds 4
    $dialog = Save-Window $h (Join-Path $OutDir 'dialog-before-popup.png')
    $results += "PASS HPGEO dialog opened (mean brightness $($dialog.Mean))"
    $root = [System.Windows.Automation.AutomationElement]::FromHandle($h)
    $combo = $root.FindFirst([System.Windows.Automation.TreeScope]::Descendants, (New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::ControlTypeProperty, [System.Windows.Automation.ControlType]::ComboBox)))
    if ($null -eq $combo) { throw 'no ComboBox found by UIA' }
    $expand = $combo.GetCurrentPattern([System.Windows.Automation.ExpandCollapsePattern]::Pattern)
    $expand.Expand(); Start-Sleep -Milliseconds 1500
    $popups = [PopWin]::FindPopups([uint32]$p.Id)
    if ($popups.Count -eq 0) { throw 'no popup window after Expand' }
    $shot = Save-Window $popups[0] (Join-Path $OutDir 'combobox-popup.png')
    $results += "PASS ComboBox popup captured ($($shot.Width)x$($shot.Height), mean brightness $($shot.Mean), state $($expand.Current.ExpandCollapseState))"
    $results += $(if (($ColorTheme -eq 0 -and $shot.Mean -lt 110) -or ($ColorTheme -eq 1 -and $shot.Mean -gt 150)) { "PASS popup follows the theme (COLORTHEME $ColorTheme)" } else { "FAIL popup brightness $($shot.Mean) does not match COLORTHEME $ColorTheme" })
    $expand.Collapse(); Start-Sleep -Milliseconds 500
    [PopWin]::SendMessage($h, 0x0010, [IntPtr]::Zero, [IntPtr]::Zero) | Out-Null   # WM_CLOSE
    $null = Wait-Job $job -Timeout 60; Receive-Job $job | Out-Null
}
catch { $results += "FAIL $($_.Exception.Message)" }
finally {
    if ($job) { Remove-Job $job -Force -ErrorAction SilentlyContinue }
    try { $app.ActiveDocument.SetVariable('COLORTHEME', [int16]$orig) } catch {}
    try { $app.ActiveDocument.SendCommand("_.QUIT`n_N`n") } catch {}
    if (-not $p.WaitForExit(45000)) { Stop-Process -Id $p.Id -Force -Confirm:$false; $results += 'WARN acad killed after QUIT timeout' } else { $results += 'PASS acad quit cleanly' }
}
$results | ForEach-Object { Write-Host $_ }
$results | Set-Content (Join-Path $OutDir 'combobox-popup-checks.txt') -Encoding UTF8
if (@($results | Where-Object { $_ -like 'FAIL*' }).Count -gt 0) { exit 1 }
