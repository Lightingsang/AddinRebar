<#
.SYNOPSIS
    Co-load gate for the AutoCAD family: the HPAutoCad MCP bridge window and the HPGeo KMZ dialog — two bundles, each with
    its own ILRepack-merged copy of the MaterialDesign toolkit — opened in one acad.exe in both orders. Each window is
    captured with PrintWindow (own windows only) and must render dark under COLORTHEME 0; both add-in logs must stay
    free of errors. Windows PowerShell 5.1.
.EXAMPLE
    powershell -NoProfile -ExecutionPolicy Bypass -File autocad-coload-check.ps1 -Order bridge-first -OutDir .\autocad-live\coload
#>
param([ValidateSet('bridge-first', 'hpgeo-first')] [string] $Order = 'bridge-first', [Parameter(Mandatory)] [string] $OutDir)
$ErrorActionPreference = 'Stop'
$acad = 'C:\Program Files\Autodesk\AutoCAD 2026\acad.exe'
if (Get-Process acad -ErrorAction SilentlyContinue) { throw 'Close every AutoCAD first.' }
New-Item -ItemType Directory -Force $OutDir | Out-Null
$sig = @'
using System; using System.Text; using System.Runtime.InteropServices;
public static class CoWin {
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
if (-not ('CoWin' -as [type])) { Add-Type -TypeDefinition $sig }
Add-Type -AssemblyName System.Drawing
function Answer-SecureLoad {
    $dlg = [CoWin]::FindWindow('#32770', 'Security - Unsigned Executable File')
    if ($dlg -eq [IntPtr]::Zero) { return }
    $btn = [CoWin]::FindButton($dlg, 'Always Load')
    if ($btn -ne [IntPtr]::Zero) { [CoWin]::SendMessage($btn, 0x00F5, [IntPtr]::Zero, [IntPtr]::Zero) | Out-Null; Write-Host "SECURELOAD answered at $(Get-Date -Format HH:mm:ss)" }
}
function Save-Window([IntPtr]$h, [string]$path) {
    $r = New-Object CoWin+RECT; [CoWin]::GetWindowRect($h, [ref]$r) | Out-Null
    $bmp = New-Object System.Drawing.Bitmap(($r.R - $r.L), ($r.B - $r.T)); $g = [System.Drawing.Graphics]::FromImage($bmp); $hdc = $g.GetHdc()
    [CoWin]::PrintWindow($h, $hdc, 2) | Out-Null; $g.ReleaseHdc($hdc)
    $mean = 0.0; $n = 0
    for ($y = 40; $y -lt $bmp.Height - 10; $y += 12) { for ($x = 10; $x -lt $bmp.Width - 10; $x += 12) { $c = $bmp.GetPixel($x, $y); $mean += ($c.R + $c.G + $c.B) / 3.0; $n++ } }
    $bmp.Save($path, [System.Drawing.Imaging.ImageFormat]::Png); $g.Dispose(); $bmp.Dispose()
    return [math]::Round($mean / [math]::Max($n, 1), 1)
}
function Wait-Window([uint32]$processId, [string]$title, [int]$seconds) {
    $h = [IntPtr]::Zero; $sw = [Diagnostics.Stopwatch]::StartNew()
    while ($sw.Elapsed.TotalSeconds -lt $seconds -and $h -eq [IntPtr]::Zero) { Answer-SecureLoad; Start-Sleep -Milliseconds 500; $h = [CoWin]::FindTopLevel($processId, $title) }
    if ($h -eq [IntPtr]::Zero) { throw "window '$title' did not open" }
    Start-Sleep -Seconds 4
    return $h
}
$logMark = Get-Date
$p = Start-Process -FilePath $acad -ArgumentList @('/nologo', '/product', 'ACAD', '/language', '"en-US"') -PassThru
Write-Host "acad pid $($p.Id) started $(Get-Date -Format HH:mm:ss) (order $Order)"
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
$results = @(); $job = $null
function Open-Bridge {
    $script:app.ActiveDocument.SendCommand("_.HPMCPBRIDGE`n")
    $h = Wait-Window ([uint32]$script:p.Id) 'HPAutoCad MCP Bridge' 60
    $mean = Save-Window $h (Join-Path $OutDir "bridge-$Order.png")
    $script:results += $(if ($mean -lt 110) { "PASS MCP bridge window rendered dark (mean $mean)" } else { "FAIL MCP bridge window brightness $mean" })
    [CoWin]::SendMessage($h, 0x0010, [IntPtr]::Zero, [IntPtr]::Zero) | Out-Null
    Start-Sleep -Seconds 1
}
function Open-HPGeo {
    # SendCommand blocks for the whole modal dialog: hand it to a job, drive the dialog from here.
    $script:job = Start-Job -ScriptBlock {
        $a = [Runtime.InteropServices.Marshal]::GetActiveObject('AutoCAD.Application')
        $a.ActiveDocument.SendCommand("_.POINT 600125.887,1231608.428`n_.POINT 600124.9,1231587.7`n_.HPGEO`n`n")
        'sent'
    }
    $h = Wait-Window ([uint32]$script:p.Id) 'HPGeo' 90
    $mean = Save-Window $h (Join-Path $OutDir "hpgeo-$Order.png")
    $script:results += $(if ($mean -lt 110) { "PASS HPGeo dialog rendered dark (mean $mean)" } else { "FAIL HPGeo dialog brightness $mean" })
    [CoWin]::SendMessage($h, 0x0010, [IntPtr]::Zero, [IntPtr]::Zero) | Out-Null
    $null = Wait-Job $script:job -Timeout 60; Receive-Job $script:job | Out-Null; Remove-Job $script:job -Force -ErrorAction SilentlyContinue; $script:job = $null
}
try {
    $app.ActiveDocument.SetVariable('COLORTHEME', [int16]0)
    if ($Order -eq 'bridge-first') { Open-Bridge; Open-HPGeo; Open-Bridge } else { Open-HPGeo; Open-Bridge; Open-HPGeo }
}
catch { $results += "FAIL $($_.Exception.Message)" }
finally {
    if ($job) { Remove-Job $job -Force -ErrorAction SilentlyContinue }
    try { $app.ActiveDocument.SetVariable('COLORTHEME', [int16]$orig) } catch {}
    try { $app.ActiveDocument.SendCommand("_.QUIT`n_N`n") } catch {}
    if (-not $p.WaitForExit(45000)) { Stop-Process -Id $p.Id -Force -Confirm:$false; $results += 'WARN acad killed after QUIT timeout' } else { $results += 'PASS acad quit cleanly' }
}
$inv = [Globalization.CultureInfo]::InvariantCulture
foreach ($log in @(@{ Name = 'HPAutoCad bridge'; Dir = (Join-Path $env:LOCALAPPDATA 'HPAutoCad\McpBridge\logs') }, @{ Name = 'HPGeo'; Dir = (Join-Path $env:LOCALAPPDATA 'HPGeo\logs') })) {
    $lines = @(Get-ChildItem $log.Dir -Include '*.log' -Recurse -ErrorAction SilentlyContinue | ForEach-Object { Get-Content $_.FullName -Encoding UTF8 } |
        Where-Object { $_ -match '^\d{4}-\d\d-\d\d \d\d:\d\d:\d\d' -and [datetime]::ParseExact($_.Substring(0, 19), 'yyyy-MM-dd HH:mm:ss', $inv) -ge $logMark.AddSeconds(-2) })
    $errors = @($lines | Where-Object { $_ -match '\[ERR\]|Exception' })
    $results += $(if ($errors.Count -eq 0) { "PASS no error / exception in the $($log.Name) log ($($lines.Count) lines this session)" } else { "FAIL $($log.Name) log errors: " + (($errors | Select-Object -First 3) -join ' | ') })
}
$results | ForEach-Object { Write-Host $_ }
$results | Set-Content (Join-Path $OutDir "coload-$Order.txt") -Encoding UTF8
if (@($results | Where-Object { $_ -like 'FAIL*' }).Count -gt 0) { exit 1 }
