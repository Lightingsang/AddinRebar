<#
.SYNOPSIS
    Spike S0-B: two MaterialDesignThemes copies in two isolated AssemblyLoadContexts of one acad.exe
    (HPAutoCad MCP bridge + HPGeo), opened in a given order, both COLORTHEMEs.

.DESCRIPTION
    Same driving pattern as HPGeo/tools/dialog-check.ps1: own acad.exe, 45 s settle answering SECURELOAD,
    script handed over COM in a background job (SendCommand blocks for the whole script), windows found by
    pid + title, screenshots, clean _.QUIT. Assumes both spike bundles are already deployed.
    Order A: HPMCPBRIDGE first, then HPGEOIMPORT (dark), HPGEOIMPORT (light).
    Order B: HPGEOIMPORT first (dark), then HPMCPBRIDGE, then HPGEOIMPORT (light).
    Gate = both windows render, the HPGeo ComboBox popup opens, the MD-SPIKE log lines of both add-ins say
    same=True (BAML-created toolkit objects live in the same ALC as the add-in's own typeof), no error in either log.

.EXAMPLE
    powershell -NoProfile -ExecutionPolicy Bypass -File s0b-two-alc.ps1 -Order A -OutDir ...\reports\s0b-order-a
#>
param(
    [ValidateSet('A', 'B')] [string] $Order = 'A',
    [Parameter(Mandatory)] [string] $OutDir,
    [int] $TimeoutSec = 240,
    [switch] $Repacked
)

$ErrorActionPreference = 'Stop'
$acad = 'C:\Program Files\Autodesk\AutoCAD 2026\acad.exe'
if (Get-Process acad -ErrorAction SilentlyContinue) { throw 'Close every AutoCAD first.' }
New-Item -ItemType Directory -Force $OutDir | Out-Null

$sig = @'
using System; using System.Text; using System.Runtime.InteropServices;
public static class S0bWin {
  public delegate bool Proc(IntPtr h, IntPtr l);
  [DllImport("user32.dll")] public static extern IntPtr FindWindow(string cls, string title);
  [DllImport("user32.dll")] static extern bool EnumChildWindows(IntPtr parent, Proc p, IntPtr l);
  [DllImport("user32.dll")] static extern bool EnumWindows(Proc p, IntPtr l);
  [DllImport("user32.dll")] static extern int GetWindowText(IntPtr h, StringBuilder t, int n);
  [DllImport("user32.dll")] static extern uint GetWindowThreadProcessId(IntPtr h, out uint pid);
  [DllImport("user32.dll")] static extern bool IsWindowVisible(IntPtr h);
  [DllImport("user32.dll")] public static extern IntPtr SendMessage(IntPtr h, uint msg, IntPtr w, IntPtr l);
  [DllImport("user32.dll")] public static extern bool PostMessage(IntPtr h, uint msg, IntPtr w, IntPtr l);
  [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr hwnd, out RECT r);
  [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr hwnd);
  [StructLayout(LayoutKind.Sequential)] public struct RECT { public int L, T, R, B; }
  public static IntPtr FindButton(IntPtr parent, string text) {
    IntPtr found = IntPtr.Zero;
    EnumChildWindows(parent, (h, l) => { var sb = new StringBuilder(256); GetWindowText(h, sb, 256); if (sb.ToString() == text) { found = h; return false; } return true; }, IntPtr.Zero);
    return found; }
  public static IntPtr FindTopLevel(uint pid, string titleStart) {
    IntPtr found = IntPtr.Zero;
    EnumWindows((h, l) => { uint p; GetWindowThreadProcessId(h, out p); if (p != pid || !IsWindowVisible(h)) return true; var sb = new StringBuilder(256); GetWindowText(h, sb, 256); if (sb.ToString().StartsWith(titleStart)) { found = h; return false; } return true; }, IntPtr.Zero);
    return found; }
}
'@
if (-not ('S0bWin' -as [type])) { Add-Type -TypeDefinition $sig }
Add-Type -AssemblyName System.Drawing
Add-Type -AssemblyName UIAutomationClient
Add-Type -AssemblyName UIAutomationTypes

function Answer-SecureLoad {
    $dlg = [S0bWin]::FindWindow('#32770', 'Security - Unsigned Executable File')
    if ($dlg -eq [IntPtr]::Zero) { return $false }
    $btn = [S0bWin]::FindButton($dlg, 'Always Load')
    if ($btn -ne [IntPtr]::Zero) { [S0bWin]::SendMessage($btn, 0x00F5, [IntPtr]::Zero, [IntPtr]::Zero) | Out-Null; Write-Host "SECURELOAD answered at $(Get-Date -Format HH:mm:ss)"; return $true }
    return $false
}

# Screen capture of the window rectangle plus a margin below, so a ComboBox popup (its own HWND) is included.
function Save-Screen([IntPtr]$h, [string]$path, [int]$extraBelow) {
    $r = New-Object S0bWin+RECT; [S0bWin]::GetWindowRect($h, [ref]$r) | Out-Null
    $w = $r.R - $r.L; $ht = ($r.B - $r.T) + $extraBelow
    $bmp = New-Object System.Drawing.Bitmap($w, $ht); $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.CopyFromScreen($r.L, $r.T, 0, 0, (New-Object System.Drawing.Size($w, $ht)))
    $bmp.Save($path, [System.Drawing.Imaging.ImageFormat]::Png); $g.Dispose(); $bmp.Dispose()
    "${w}x${ht}"
}

function Expand-FirstComboBox([IntPtr]$hwnd) {
    try {
        $root = [System.Windows.Automation.AutomationElement]::FromHandle($hwnd)
        $cond = New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::ControlTypeProperty, [System.Windows.Automation.ControlType]::ComboBox)
        $combos = $root.FindAll([System.Windows.Automation.TreeScope]::Descendants, $cond)
        foreach ($combo in $combos) {
            if (-not $combo.Current.IsEnabled) { continue }
            $combo.GetCurrentPattern([System.Windows.Automation.ExpandCollapsePattern]::Pattern).Expand()
            return $true
        }
        Write-Host "no enabled ComboBox among $($combos.Count) found by UIA"; return $false
    } catch { Write-Host "UIA expand failed: $($_.Exception.Message)"; return $false }
}

$bridgeTitle = 'HPAutoCad MCP Bridge'
$geoTitle = 'HPGeo'
$userSettings = Join-Path $env:APPDATA 'HPGeo\settings.json'
$userSettingsBackup = if (Test-Path $userSettings) { [IO.File]::ReadAllBytes($userSettings) } else { $null }
$logMark = Get-Date
$work = Join-Path $env:LOCALAPPDATA 'HPGeo\s0b'
New-Item -ItemType Directory -Force $work | Out-Null

$p = Start-Process -FilePath $acad -ArgumentList @('/nologo', '/product', 'ACAD', '/language', '"en-US"') -PassThru
Write-Host "acad pid $($p.Id) started $(Get-Date -Format HH:mm:ss) (order $Order)"
$sw = [Diagnostics.Stopwatch]::StartNew()
while ($sw.Elapsed.TotalSeconds -lt 45) { Answer-SecureLoad | Out-Null; if ($p.HasExited) { throw 'acad exited during start-up' }; Start-Sleep -Seconds 2 }

$origTheme = $null
for ($attempt = 1; $attempt -le 20 -and $null -eq $origTheme; $attempt++) {
    try {
        $ids = @(Get-Process acad | ForEach-Object Id)
        if ($ids.Count -ne 1 -or [string]$ids[0] -ne [string]$p.Id) { throw 'refusing COM: another acad is running' }
        $app = [Runtime.InteropServices.Marshal]::GetActiveObject('AutoCAD.Application')
        $origTheme = [string]$app.ActiveDocument.GetVariable('COLORTHEME')
    } catch { if ($_.Exception.Message -match 'refusing COM') { throw }; Start-Sleep -Seconds 3 }
}
if ($null -eq $origTheme) { Stop-Process -Id $p.Id -Force -Confirm:$false; throw 'could not read COLORTHEME over COM' }
Write-Host "user COLORTHEME=$origTheme"

$lines = New-Object System.Collections.Generic.List[string]
$lines.Add('COLORTHEME'); $lines.Add('0')
if ($Order -eq 'A') { $lines.Add('_.HPMCPBRIDGE'); $lines.Add('_.HPGEOIMPORT') } else { $lines.Add('_.HPGEOIMPORT'); $lines.Add('_.HPMCPBRIDGE') }
$lines.Add('COLORTHEME'); $lines.Add('1')
$lines.Add('_.HPGEOIMPORT')
$lines.Add('COLORTHEME'); $lines.Add("$origTheme")
$lines.Add('_.QUIT'); $lines.Add('_Y')
$scr = Join-Path $work "s0b-$Order.scr"
[IO.File]::WriteAllLines($scr, $lines, (New-Object Text.UTF8Encoding($false)))

$job = Start-Job -ArgumentList $p.Id, $scr -ScriptBlock {
    param($acadPid, $scriptPath)
    $ids = @(Get-Process acad | ForEach-Object Id)
    if ($ids.Count -ne 1 -or [string]$ids[0] -ne [string]$acadPid) { throw 'refusing COM: another acad is running' }
    $app = [Runtime.InteropServices.Marshal]::GetActiveObject('AutoCAD.Application')
    for ($attempt = 1; $attempt -le 20; $attempt++) {
        try { $app.ActiveDocument.SendCommand("FILEDIA`n0`n_.SCRIPT`n$scriptPath`n"); return "script sent (attempt $attempt)" }
        catch { Start-Sleep -Seconds 3 }
    }
    throw 'could not hand the script over COM'
}
Write-Host "script handed to a background job at $(Get-Date -Format HH:mm:ss)"

$shots = @{}
$geoSeen = 0
$bridgeShot = $false
while ($sw.Elapsed.TotalSeconds -lt $TimeoutSec -and -not $p.HasExited) {
    Answer-SecureLoad | Out-Null
    $hb = [S0bWin]::FindTopLevel([uint32]$p.Id, $bridgeTitle)
    if ($hb -ne [IntPtr]::Zero -and -not $bridgeShot) {
        Start-Sleep -Milliseconds 3000
        [S0bWin]::SetForegroundWindow($hb) | Out-Null; Start-Sleep -Milliseconds 300
        $file = Join-Path $OutDir 'bridge-dark.png'
        $size = Save-Screen $hb $file 0
        $shots['bridge'] = $file; $bridgeShot = $true
        Write-Host "bridge window captured -> $file ($size) at $(Get-Date -Format HH:mm:ss)"
    }
    $hg = [S0bWin]::FindTopLevel([uint32]$p.Id, $geoTitle)
    if ($hg -ne [IntPtr]::Zero -and $geoSeen -lt 2) {
        Start-Sleep -Milliseconds 3000
        [S0bWin]::SetForegroundWindow($hg) | Out-Null; Start-Sleep -Milliseconds 300
        $expanded = Expand-FirstComboBox $hg
        Start-Sleep -Milliseconds 800
        $name = if ($geoSeen -eq 0) { 'geo-dark-combo-open.png' } else { 'geo-light-combo-open.png' }
        $file = Join-Path $OutDir $name
        $size = Save-Screen $hg $file 160
        $shots["geo$geoSeen"] = $file
        Write-Host "HPGeo window captured -> $file ($size, combo expanded: $expanded) at $(Get-Date -Format HH:mm:ss)"
        $geoSeen++
        [S0bWin]::PostMessage($hg, 0x0010, [IntPtr]::Zero, [IntPtr]::Zero) | Out-Null   # WM_CLOSE
        Start-Sleep -Seconds 2
    }
    Start-Sleep -Milliseconds 500
}
$done = $p.HasExited
if (-not $done) { Write-Host 'acad still running - killing'; Stop-Process -Id $p.Id -Force -Confirm:$false -ErrorAction SilentlyContinue }
$jobResult = try { Receive-Job $job -Wait -ErrorAction Stop } catch { "job failed: $($_.Exception.Message)" }
Remove-Job $job -Force -ErrorAction SilentlyContinue
Write-Host "COM job: $jobResult"
try {
    if ($null -ne $userSettingsBackup) { [IO.File]::WriteAllBytes($userSettings, $userSettingsBackup) }
    elseif (Test-Path $userSettings) { Remove-Item $userSettings -Force }
} catch { Write-Host "settings.json restore failed: $($_.Exception.Message)" }

# Logs written since the run started.
$inv = [Globalization.CultureInfo]::InvariantCulture
function Read-Logs([string]$dir) {
    @(Get-ChildItem $dir -Include '*.log' -Recurse -ErrorAction SilentlyContinue | ForEach-Object { Get-Content $_.FullName -Encoding UTF8 } |
        Where-Object { $_ -match '^\d{4}-\d\d-\d\d \d\d:\d\d:\d\d' -and [datetime]::ParseExact($_.Substring(0, 19), 'yyyy-MM-dd HH:mm:ss', $inv) -ge $logMark.AddSeconds(-2) })
}
$bridgeLog = Read-Logs (Join-Path $env:LOCALAPPDATA 'HPAutoCad\McpBridge\logs')
$geoLog = Read-Logs (Join-Path $env:LOCALAPPDATA 'HPGeo\logs')
$all = ($bridgeLog + $geoLog) -join "`n"
[IO.File]::WriteAllText((Join-Path $OutDir 'session-logs.txt'), $all, (New-Object Text.UTF8Encoding($false)))

$spikeLines = @(($bridgeLog + $geoLog) | Where-Object { $_ -match 'MD-SPIKE' })
$loadLines = @(($bridgeLog + $geoLog) | Where-Object { $_ -match 'load MaterialDesignThemes\.Wpf' })
$errLines = @(($bridgeLog + $geoLog) | Where-Object { $_ -match '\[ERR\]|Exception|failed' })

$checks = @()
function Check([string]$name, [bool]$ok, [string]$detail = '') { $script:checks += [pscustomobject]@{ name = $name; ok = $ok; detail = $detail }; Write-Host ("{0} {1} {2}" -f $(if ($ok) { 'PASS' } else { 'FAIL' }), $name, $detail) }
Check 'bridge window opened + captured' $shots.ContainsKey('bridge') $shots['bridge']
Check 'HPGeo import window (dark) opened + captured' $shots.ContainsKey('geo0') $shots['geo0']
Check 'HPGeo import window (light) opened + captured' $shots.ContainsKey('geo1') $shots['geo1']
if ($Repacked) {
    $copyZero = @($spikeLines | Where-Object { $_ -match 'copiesInProcess=0' }).Count
    Check 'repacked: no assembly named MaterialDesignThemes.Wpf loaded (0 loader lines, every MD-SPIKE line copiesInProcess=0)' ($loadLines.Count -eq 0 -and $spikeLines.Count -ge 3 -and $copyZero -eq $spikeLines.Count) ("loads=$($loadLines.Count) spikeLines=$($spikeLines.Count) zero=$copyZero")
} else {
    Check 'loose: each ALC loaded its own MaterialDesignThemes.Wpf (loader logs)' ($loadLines.Count -ge 2) ($loadLines -join ' | ')
}
$bridgeSpike = @($spikeLines | Where-Object { $_ -match 'MD-SPIKE HPAutoCad' })
$geoSpike = @($spikeLines | Where-Object { $_ -match 'MD-SPIKE HPGeo' })
Check 'bridge MD-SPIKE line: same=True, packIcon=True' ($bridgeSpike.Count -ge 1 -and ($bridgeSpike[-1] -match 'same=True') -and ($bridgeSpike[-1] -match 'packIcon=True')) ($bridgeSpike -join ' | ')
Check 'HPGeo MD-SPIKE line: same=True' ($geoSpike.Count -ge 1 -and ($geoSpike[-1] -match 'same=True')) ($geoSpike -join ' | ')
Check 'no error / exception in either log' ($errLines.Count -eq 0) ($errLines -join ' | ')
Check 'acad quit cleanly' $done
$failed = @($checks | Where-Object { -not $_.ok }).Count
Write-Host ("SUMMARY order {0}: {1} pass, {2} fail" -f $Order, ($checks.Count - $failed), $failed)
$checks | ForEach-Object { "{0} {1} {2}" -f $(if ($_.ok) { 'PASS' } else { 'FAIL' }), $_.name, $_.detail } | Set-Content (Join-Path $OutDir 'checks.txt') -Encoding UTF8
if ($failed -gt 0) { exit 1 }
