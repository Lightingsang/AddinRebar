<#
.SYNOPSIS
    Starts HPEtabs.McpBridge.exe with HPETABS_MCP_BRIDGE_THEME=dark then light, screenshots its window (PrintWindow),
    closes it. No ETABS needed (the window shows the detached state). Checks the bridge log for errors.
.EXAMPLE
    pwsh etabs-window-shot.ps1 -Exe ..\..\..\HPEtabs\HPEtabs.McpBridge\bin\Debug\net8.0-windows\HPEtabs.McpBridge.exe -OutDir .\etabs-window
#>
param([Parameter(Mandatory)] [string] $Exe, [Parameter(Mandatory)] [string] $OutDir)
$ErrorActionPreference = 'Stop'
$sig = @'
using System; using System.Text; using System.Runtime.InteropServices;
public static class EtabsShot {
  public delegate bool Proc(IntPtr h, IntPtr l);
  [DllImport("user32.dll")] static extern bool EnumWindows(Proc p, IntPtr l);
  [DllImport("user32.dll")] static extern int GetWindowText(IntPtr h, StringBuilder t, int n);
  [DllImport("user32.dll")] static extern uint GetWindowThreadProcessId(IntPtr h, out uint pid);
  [DllImport("user32.dll")] static extern bool IsWindowVisible(IntPtr h);
  [DllImport("user32.dll")] public static extern bool PostMessage(IntPtr h, uint msg, IntPtr w, IntPtr l);
  [DllImport("user32.dll")] public static extern bool PrintWindow(IntPtr hwnd, IntPtr hdc, uint flags);
  [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr hwnd, out RECT r);
  [StructLayout(LayoutKind.Sequential)] public struct RECT { public int L, T, R, B; }
  public static IntPtr FindTopLevel(uint pid, string titleStart) {
    IntPtr found = IntPtr.Zero;
    EnumWindows((h, l) => { uint p; GetWindowThreadProcessId(h, out p); if (p != pid || !IsWindowVisible(h)) return true; var sb = new StringBuilder(256); GetWindowText(h, sb, 256); if (sb.ToString().StartsWith(titleStart)) { found = h; return false; } return true; }, IntPtr.Zero);
    return found; }
}
'@
if (-not ('EtabsShot' -as [type])) { Add-Type -TypeDefinition $sig }
Add-Type -AssemblyName System.Drawing
New-Item -ItemType Directory -Force $OutDir | Out-Null
$exePath = (Resolve-Path $Exe).Path
$logMark = Get-Date
$results = @()
foreach ($theme in 'dark', 'light') {
    $env:HPETABS_MCP_BRIDGE_THEME = $theme
    $p = Start-Process -FilePath $exePath -PassThru
    $h = [IntPtr]::Zero
    $sw = [Diagnostics.Stopwatch]::StartNew()
    while ($sw.Elapsed.TotalSeconds -lt 30 -and $h -eq [IntPtr]::Zero) { Start-Sleep -Milliseconds 500; if ($p.HasExited) { break }; $h = [EtabsShot]::FindTopLevel([uint32]$p.Id, 'HPEtabs MCP Bridge') }
    if ($h -eq [IntPtr]::Zero) { $results += "FAIL $theme window not found (exited: $($p.HasExited))"; if (-not $p.HasExited) { Stop-Process -Id $p.Id -Force }; continue }
    Start-Sleep -Seconds 4
    $r = New-Object EtabsShot+RECT; [EtabsShot]::GetWindowRect($h, [ref]$r) | Out-Null
    $bmp = New-Object System.Drawing.Bitmap(($r.R - $r.L), ($r.B - $r.T)); $g = [System.Drawing.Graphics]::FromImage($bmp); $hdc = $g.GetHdc()
    [EtabsShot]::PrintWindow($h, $hdc, 2) | Out-Null; $g.ReleaseHdc($hdc)
    $file = Join-Path $OutDir "etabs-bridge-$theme.png"; $bmp.Save($file, [System.Drawing.Imaging.ImageFormat]::Png); $g.Dispose(); $bmp.Dispose()
    [EtabsShot]::PostMessage($h, 0x0010, [IntPtr]::Zero, [IntPtr]::Zero) | Out-Null
    if (-not $p.WaitForExit(15000)) { Stop-Process -Id $p.Id -Force; $results += "FAIL $theme did not exit on WM_CLOSE" } else { $results += "PASS $theme captured -> $file ($($r.R - $r.L)x$($r.B - $r.T)), exit $($p.ExitCode)" }
}
Remove-Item Env:HPETABS_MCP_BRIDGE_THEME -ErrorAction SilentlyContinue
$inv = [Globalization.CultureInfo]::InvariantCulture
$log = @(Get-ChildItem (Join-Path $env:LOCALAPPDATA 'HPEtabs\McpBridge\logs') -Filter '*.log' -ErrorAction SilentlyContinue | ForEach-Object { Get-Content $_.FullName -Encoding UTF8 } |
    Where-Object { $_ -match '^\d{4}-\d\d-\d\d \d\d:\d\d:\d\d' -and [datetime]::ParseExact($_.Substring(0, 19), 'yyyy-MM-dd HH:mm:ss', $inv) -ge $logMark.AddSeconds(-2) })
$errors = @($log | Where-Object { $_ -match '\[ERR\]|Exception' })
$selfCheck = @($log | Where-Object { $_ -match 'self-check OK' }).Count
$results += $(if ($errors.Count -eq 0) { "PASS no error in the bridge log" } else { "FAIL errors: " + (($errors | Select-Object -First 3) -join ' | ') })
$results += $(if ($selfCheck -ge 2) { "PASS scripting self-check OK x$selfCheck" } else { "FAIL self-check lines: $selfCheck" })
$results | ForEach-Object { Write-Host $_ }
$results | Set-Content (Join-Path $OutDir 'checks.txt') -Encoding UTF8
if (@($results | Where-Object { $_ -like 'FAIL*' }).Count -gt 0) { exit 1 }
