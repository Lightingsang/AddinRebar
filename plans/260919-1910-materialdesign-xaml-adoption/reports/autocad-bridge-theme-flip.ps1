<#
.SYNOPSIS
    Live theme check for an AutoCAD-family MCP bridge window: starts acad.exe (own pid, SECURELOAD answered), runs the
    bridge command, captures the window, flips COLORTHEME through COM while the window stays open, captures again,
    restores the user's COLORTHEME and quits. Gate = both captures exist and the window's pixels differ (dark vs light).
.EXAMPLE
    powershell -NoProfile -ExecutionPolicy Bypass -File autocad-bridge-theme-flip.ps1 -Command HPMCPBRIDGE -Title 'HPAutoCad MCP Bridge' -OutDir .\autocad-live
    ... -Product C3D -Command HPC3DMCPBRIDGE -Title 'HPCivil3d MCP Bridge' -LogDir "$env:LOCALAPPDATA\HPCivil3d\McpBridge\logs"
#>
param(
    [string] $Command = 'HPMCPBRIDGE',
    [string] $Title = 'HPAutoCad MCP Bridge',
    [string] $Product = 'ACAD',
    [string] $LogDir = (Join-Path $env:LOCALAPPDATA 'HPAutoCad\McpBridge\logs'),
    [Parameter(Mandatory)] [string] $OutDir,
    [int] $TimeoutSec = 240
)
$ErrorActionPreference = 'Stop'
$acad = 'C:\Program Files\Autodesk\AutoCAD 2026\acad.exe'
if (Get-Process acad -ErrorAction SilentlyContinue) { throw 'Close every AutoCAD first.' }
New-Item -ItemType Directory -Force $OutDir | Out-Null
$sig = @'
using System; using System.Text; using System.Runtime.InteropServices;
public static class FlipWin {
  public delegate bool Proc(IntPtr h, IntPtr l);
  [DllImport("user32.dll")] public static extern IntPtr FindWindow(string cls, string title);
  [DllImport("user32.dll")] static extern bool EnumChildWindows(IntPtr parent, Proc p, IntPtr l);
  [DllImport("user32.dll")] static extern bool EnumWindows(Proc p, IntPtr l);
  [DllImport("user32.dll")] static extern int GetWindowText(IntPtr h, StringBuilder t, int n);
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
}
'@
if (-not ('FlipWin' -as [type])) { Add-Type -TypeDefinition $sig }
Add-Type -AssemblyName System.Drawing
function Answer-SecureLoad {
    $dlg = [FlipWin]::FindWindow('#32770', 'Security - Unsigned Executable File')
    if ($dlg -eq [IntPtr]::Zero) { return }
    $btn = [FlipWin]::FindButton($dlg, 'Always Load')
    if ($btn -ne [IntPtr]::Zero) { [FlipWin]::SendMessage($btn, 0x00F5, [IntPtr]::Zero, [IntPtr]::Zero) | Out-Null; Write-Host "SECURELOAD answered at $(Get-Date -Format HH:mm:ss)" }
}
function Save-Window([IntPtr]$h, [string]$path) {
    $r = New-Object FlipWin+RECT; [FlipWin]::GetWindowRect($h, [ref]$r) | Out-Null
    $bmp = New-Object System.Drawing.Bitmap(($r.R - $r.L), ($r.B - $r.T)); $g = [System.Drawing.Graphics]::FromImage($bmp); $hdc = $g.GetHdc()
    [FlipWin]::PrintWindow($h, $hdc, 2) | Out-Null; $g.ReleaseHdc($hdc)
    $mean = 0.0; $n = 0
    for ($y = 40; $y -lt $bmp.Height - 10; $y += 12) { for ($x = 10; $x -lt $bmp.Width - 10; $x += 12) { $c = $bmp.GetPixel($x, $y); $mean += ($c.R + $c.G + $c.B) / 3.0; $n++ } }
    $bmp.Save($path, [System.Drawing.Imaging.ImageFormat]::Png); $g.Dispose(); $bmp.Dispose()
    return [math]::Round($mean / [math]::Max($n, 1), 1)
}
$logMark = Get-Date
$argList = @('/nologo')
if ($Product -eq 'C3D') { $argList += @('/ld', '"C:\Program Files\Autodesk\AutoCAD 2026\AecBase.dbx"', '/p', '"<<C3D_Metric>>"') }   # the Civil 3D shortcut's arguments
$argList += @('/product', $Product, '/language', '"en-US"')
$p = Start-Process -FilePath $acad -ArgumentList $argList -PassThru
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
try {
    $app.ActiveDocument.SetVariable('COLORTHEME', [int16]0)
    $app.ActiveDocument.SendCommand("_.$Command`n")
    $h = [IntPtr]::Zero; $sw.Restart()
    while ($sw.Elapsed.TotalSeconds -lt 60 -and $h -eq [IntPtr]::Zero) { Answer-SecureLoad; Start-Sleep -Milliseconds 500; $h = [FlipWin]::FindTopLevel([uint32]$p.Id, $Title) }
    if ($h -eq [IntPtr]::Zero) { throw 'bridge window did not open' }
    Start-Sleep -Seconds 4
    $dark = Save-Window $h (Join-Path $OutDir 'bridge-dark.png')
    $app.ActiveDocument.SetVariable('COLORTHEME', [int16]1)
    Start-Sleep -Seconds 3
    $light = Save-Window $h (Join-Path $OutDir 'bridge-light-live.png')
    $app.ActiveDocument.SetVariable('COLORTHEME', [int16]0)
    Start-Sleep -Seconds 3
    $darkAgain = Save-Window $h (Join-Path $OutDir 'bridge-dark-again.png')
    $results += "PASS window opened and captured (mean brightness dark=$dark light=$light darkAgain=$darkAgain)"
    $results += $(if ($light - $dark -gt 60 -and [math]::Abs($darkAgain - $dark) -lt 15) { 'PASS the open window re-themed on COLORTHEME 0 -> 1 -> 0' } else { "FAIL live re-theme: brightness dark=$dark light=$light darkAgain=$darkAgain" })
}
catch { $results += "FAIL $($_.Exception.Message)" }
finally {
    try { $app.ActiveDocument.SetVariable('COLORTHEME', [int16]$orig) } catch {}
    try { $app.ActiveDocument.SendCommand("_.QUIT`n_Y`n") } catch {}
    if (-not $p.WaitForExit(45000)) { Stop-Process -Id $p.Id -Force -Confirm:$false; $results += 'WARN acad killed after QUIT timeout' } else { $results += 'PASS acad quit cleanly' }
}
$inv = [Globalization.CultureInfo]::InvariantCulture
$log = @(Get-ChildItem $LogDir -Include '*.log' -Recurse -ErrorAction SilentlyContinue | ForEach-Object { Get-Content $_.FullName -Encoding UTF8 } |
    Where-Object { $_ -match '^\d{4}-\d\d-\d\d \d\d:\d\d:\d\d' -and [datetime]::ParseExact($_.Substring(0, 19), 'yyyy-MM-dd HH:mm:ss', $inv) -ge $logMark.AddSeconds(-2) })
$errors = @($log | Where-Object { $_ -match '\[ERR\]|Exception' })
$results += $(if ($errors.Count -eq 0) { 'PASS no error / exception in the bridge logs' } else { 'FAIL log errors: ' + (($errors | Select-Object -First 3) -join ' | ') })
$results | ForEach-Object { Write-Host $_ }
$results | Set-Content (Join-Path $OutDir 'theme-flip-checks.txt') -Encoding UTF8
if (@($results | Where-Object { $_ -like 'FAIL*' }).Count -gt 0) { exit 1 }
