<#
.SYNOPSIS
  Opens the HPGEO dialog in AutoCAD 2026 on the 13 sample points, screenshots it (both COLORTHEMEs), presses its
  "Chen anh ve tinh vao CAD" button on the second run (UIA) so a RasterImage is inserted, then HPGEOIMPORT.

.DESCRIPTION
  Same driving pattern as acceptance.ps1 (own acad.exe, settle, script over COM, SECURELOAD answered, QUIT).
  The script draws the sample points and a closed polyline, runs HPGEO twice (COLORTHEME 0 then 1) answering the
  selection prompt with Enter (= all of model space); each time the modal dialog is up, this harness finds it by
  title under the acad pid, saves output\acceptance\dialog-dark.png / dialog-light.png through PrintWindow (only
  the dialog, never the desktop) and sends WM_CLOSE so the script can go on. Evidence for the human review of the
  dialog; the view model itself is covered by xUnit.
#>
[CmdletBinding()]
param([int]$TimeoutSec = 240)

$ErrorActionPreference = 'Stop'
$here = Split-Path -Parent $MyInvocation.MyCommand.Path
$root = Split-Path -Parent $here
$golden = Join-Path $root 'HPGeo.Tests\Fixtures\golden-vn2000-to-wgs84.json'
$work = Join-Path $env:LOCALAPPDATA 'HPGeo\dialog-check'
$evidence = Join-Path $root 'output\acceptance'
$acad = 'C:\Program Files\Autodesk\AutoCAD 2026\acad.exe'
$dialogTitle = 'HPGeo'

if (Get-Process acad -ErrorAction SilentlyContinue) { throw 'Close every AutoCAD first.' }

$sig = @'
using System; using System.Text; using System.Collections.Generic; using System.Runtime.InteropServices;
public static class HPGeoWin {
  public delegate bool Proc(IntPtr h, IntPtr l);
  [DllImport("user32.dll")] public static extern IntPtr FindWindow(string cls, string title);
  [DllImport("user32.dll")] static extern bool EnumChildWindows(IntPtr parent, Proc p, IntPtr l);
  [DllImport("user32.dll")] static extern bool EnumWindows(Proc p, IntPtr l);
  [DllImport("user32.dll")] static extern int GetWindowText(IntPtr h, StringBuilder t, int n);
  [DllImport("user32.dll")] static extern uint GetWindowThreadProcessId(IntPtr h, out uint pid);
  [DllImport("user32.dll")] static extern bool IsWindowVisible(IntPtr h);
  [DllImport("user32.dll")] public static extern IntPtr SendMessage(IntPtr h, uint msg, IntPtr w, IntPtr l);
  [DllImport("user32.dll")] public static extern bool PostMessage(IntPtr h, uint msg, IntPtr w, IntPtr l);
  [DllImport("user32.dll")] public static extern bool PrintWindow(IntPtr hwnd, IntPtr hdc, uint flags);
  [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr hwnd, out RECT r);
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
if (-not ('HPGeoWin' -as [type])) { Add-Type -TypeDefinition $sig }
Add-Type -AssemblyName System.Drawing
Add-Type -AssemblyName UIAutomationClient
Add-Type -AssemblyName UIAutomationTypes
$imageButtonName = 'Chèn ảnh vệ tinh vào CAD'

# Presses a WPF button of our own dialog through UI Automation (Invoke pattern); the dialog is ours, found by pid + title.
function Invoke-DialogButton([IntPtr]$hwnd, [string]$name) {
    try {
        $root = [System.Windows.Automation.AutomationElement]::FromHandle($hwnd)
        $condition = New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::NameProperty, $name)
        $button = $root.FindFirst([System.Windows.Automation.TreeScope]::Descendants, $condition)
        if ($null -eq $button) { Write-Host "button '$name' not found in the dialog"; return $false }
        if (-not $button.Current.IsEnabled) { Write-Host "button '$name' is disabled"; return $false }
        $button.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern).Invoke()
        return $true
    } catch { Write-Host "UIA invoke failed: $($_.Exception.Message)"; return $false }
}

# The insert downloads its 15 tiles through the add-in's helper process (acad.exe itself is denied the network on this
# machine by a firewall rule); HPGeo's own cache is emptied first so the run proves the download, not a warm cache.
$tileCache = Join-Path $env:LOCALAPPDATA 'HPGeo\tiles'
if (Test-Path $tileCache) { Remove-Item $tileCache -Recurse -Force }
Write-Host "tile cache emptied ($tileCache) - the imagery button must download through HPGeo.TileFetch.exe"

function Answer-SecureLoad {
    $dlg = [HPGeoWin]::FindWindow('#32770', 'Security - Unsigned Executable File')
    if ($dlg -eq [IntPtr]::Zero) { return $false }
    $btn = [HPGeoWin]::FindButton($dlg, 'Always Load')
    if ($btn -ne [IntPtr]::Zero) { [HPGeoWin]::SendMessage($btn, 0x00F5, [IntPtr]::Zero, [IntPtr]::Zero) | Out-Null; Write-Host "SECURELOAD answered at $(Get-Date -Format HH:mm:ss)"; return $true }
    return $false
}

function Save-Window([IntPtr]$h, [string]$path) {
    $r = New-Object HPGeoWin+RECT; [HPGeoWin]::GetWindowRect($h, [ref]$r) | Out-Null
    $w = $r.R - $r.L; $ht = $r.B - $r.T
    $bmp = New-Object System.Drawing.Bitmap($w, $ht); $g = [System.Drawing.Graphics]::FromImage($bmp); $hdc = $g.GetHdc()
    [HPGeoWin]::PrintWindow($h, $hdc, 2) | Out-Null; $g.ReleaseHdc($hdc)
    $bmp.Save($path, [System.Drawing.Imaging.ImageFormat]::Png); $g.Dispose(); $bmp.Dispose()
    "${w}x${ht}"
}

$g = Get-Content $golden -Raw -Encoding UTF8 | ConvertFrom-Json
$samples = @($g.cases | Where-Object { $_.id -like 'sample-*' } | Select-Object -First 13)
$inv = [Globalization.CultureInfo]::InvariantCulture
New-Item -ItemType Directory -Force $work | Out-Null
New-Item -ItemType Directory -Force $evidence | Out-Null
$fmt = { param($v) $v.ToString('0.###', $inv) }
$profileVars = @('FILEDIA', 'DYNMODE', 'OSMODE', 'COLORTHEME')
$originals = @{}
function Build-Script([hashtable]$orig) {
$lines = New-Object System.Collections.Generic.List[string]
$lines.Add('DYNMODE'); $lines.Add('0'); $lines.Add('OSMODE'); $lines.Add('0')
foreach ($s in $samples) { $lines.Add('_.POINT ' + (& $fmt $s.e) + ',' + (& $fmt $s.n)) }
$pl = '_.PLINE'; foreach ($s in $samples) { $pl += ' ' + (& $fmt $s.e) + ',' + (& $fmt $s.n) }; $lines.Add($pl + ' _C')
$lines.Add('INSUNITS'); $lines.Add('6')
$lines.Add('COLORTHEME'); $lines.Add('0')
$lines.Add('_.HPGEO'); $lines.Add('')          # Enter at the selection prompt = all of model space -> modal dialog
$lines.Add('COLORTHEME'); $lines.Add('1')
$lines.Add('_.HPGEO'); $lines.Add('')
$lines.Add('COLORTHEME'); $lines.Add('0')
$lines.Add('_.HPGEOIMPORT')                  # the import dialog (dark), closed by the harness without drawing
# The user's profile settings go back before the clean exit saves the profile.
$lines.Add('COLORTHEME'); $lines.Add("$($orig.COLORTHEME)")
$lines.Add('FILEDIA'); $lines.Add("$($orig.FILEDIA)")
$lines.Add('DYNMODE'); $lines.Add("$($orig.DYNMODE)")
$lines.Add('OSMODE'); $lines.Add("$($orig.OSMODE)")
$lines.Add('_.QUIT'); $lines.Add('_Y')
return $lines
}
$scr = Join-Path $work 'dialog-check.scr'

# The add-in's own settings.json is the user's: every run writes zone/imagery values into it, so it is put back afterwards.
$userSettings = Join-Path $env:APPDATA 'HPGeo\settings.json'
$userSettingsBackup = if (Test-Path $userSettings) { [IO.File]::ReadAllBytes($userSettings) } else { $null }
$logMark = Get-Date
$p = Start-Process -FilePath $acad -ArgumentList @('/nologo', '/product', 'ACAD', '/language', '"en-US"') -PassThru
Write-Host "acad pid $($p.Id) started $(Get-Date -Format HH:mm:ss)"
$sw = [Diagnostics.Stopwatch]::StartNew()
while ($sw.Elapsed.TotalSeconds -lt 45) { Answer-SecureLoad | Out-Null; if ($p.HasExited) { throw 'acad exited during start-up' }; Start-Sleep -Seconds 2 }
# SendCommand is synchronous: it returns only when the script has run to its end, i.e. after every modal dialog
# has closed. It therefore runs in a background job while this process watches for the dialogs.
# Read the user's profile settings first (they are restored by the script's last lines), then hand the script over.
for ($attempt = 1; $attempt -le 20 -and $originals.Count -eq 0; $attempt++) {
    try {
        $ids = @(Get-Process acad | ForEach-Object Id)
        if ($ids.Count -ne 1 -or [string]$ids[0] -ne [string]$p.Id) { throw 'refusing COM: another acad is running' }
        $app = [Runtime.InteropServices.Marshal]::GetActiveObject('AutoCAD.Application')
        foreach ($v in $profileVars) { $originals[$v] = [string]$app.ActiveDocument.GetVariable($v) }
    } catch { if ($_.Exception.Message -match 'refusing COM') { throw }; Start-Sleep -Seconds 3 }
}
if ($originals.Count -eq 0) { Stop-Process -Id $p.Id -Force -Confirm:$false; throw 'could not read the profile settings over COM' }
Write-Host ("user profile settings: " + (($profileVars | ForEach-Object { "$_=$($originals[$_])" }) -join ' '))
[IO.File]::WriteAllLines($scr, (Build-Script $originals), (New-Object Text.UTF8Encoding($false)))
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

$shots = @()
$expected = @('dialog-dark.png', 'dialog-light.png', 'dialog-import.png')
while ($sw.Elapsed.TotalSeconds -lt $TimeoutSec -and -not $p.HasExited) {
    Answer-SecureLoad | Out-Null
    $h = [HPGeoWin]::FindTopLevel([uint32]$p.Id, $dialogTitle)
    if ($h -ne [IntPtr]::Zero -and $shots.Count -lt $expected.Count) {
        Start-Sleep -Milliseconds 8000   # WPF layout + WebView2 start-up + first OpenStreetMap tile
        $file = Join-Path $evidence $expected[$shots.Count]
        $size = Save-Window $h $file
        $shots += $file
        Write-Host "dialog captured -> $file ($size) at $(Get-Date -Format HH:mm:ss)"
        if ($shots.Count -eq 2) {
            # The light-theme KMZ dialog is closed by its own "Chen anh ve tinh vao CAD" button (UIA Invoke): the HPGEO
            # command then fetches (from the cache the prefetch filled), warps and inserts the RasterImage - the log proves it.
            $pressed = Invoke-DialogButton $h $imageButtonName
            Write-Host "imagery button pressed via UIA: $pressed at $(Get-Date -Format HH:mm:ss)"
            if (-not $pressed) { [HPGeoWin]::PostMessage($h, 0x0010, [IntPtr]::Zero, [IntPtr]::Zero) | Out-Null }
        } else {
            [HPGeoWin]::PostMessage($h, 0x0010, [IntPtr]::Zero, [IntPtr]::Zero) | Out-Null   # WM_CLOSE
        }
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
    if ($null -ne $userSettingsBackup) { [IO.File]::WriteAllBytes($userSettings, $userSettingsBackup); Write-Host 'user settings.json restored' }
    elseif (Test-Path $userSettings) { Remove-Item $userSettings -Force; Write-Host 'user settings.json removed (did not exist before the run)' }
} catch { Write-Host "settings.json restore failed: $($_.Exception.Message)" }
# Registry safety net for the profile settings (a clean exit already saved the restored values).
try {
    $fixed = 'HKCU:\SOFTWARE\Autodesk\AutoCAD\R25.1\ACAD-9101:409\FixedProfile\General Configuration'
    $prof = 'HKCU:\SOFTWARE\Autodesk\AutoCAD\R25.1\ACAD-9101:409\Profiles\<<Unnamed Profile>>'
    Set-ItemProperty -Path $fixed -Name 'FileDialog' -Value ([int]$originals.FILEDIA) -Type DWord
    Set-ItemProperty -Path $fixed -Name 'DYNMODE' -Value ([int]$originals.DYNMODE) -Type DWord
    Set-ItemProperty -Path "$prof\General" -Name 'Osmode' -Value ([int]$originals.OSMODE) -Type DWord
    Write-Host "profile settings restored in the registry (FILEDIA $($originals.FILEDIA), DYNMODE $($originals.DYNMODE), OSMODE $($originals.OSMODE), COLORTHEME $($originals.COLORTHEME))"
} catch { Write-Host "registry restore failed: $($_.Exception.Message)" }

$hp = @(Get-ChildItem (Join-Path $env:LOCALAPPDATA 'HPGeo\logs') -Include 'hpgeo-*.log','loader.log' -Recurse -ErrorAction SilentlyContinue |
    ForEach-Object { Get-Content $_.FullName -Encoding UTF8 } |
    Where-Object { $_ -match '^\d{4}-\d\d-\d\d \d\d:\d\d:\d\d' -and [datetime]::ParseExact($_.Substring(0, 19), 'yyyy-MM-dd HH:mm:ss', $inv) -ge $logMark.AddSeconds(-2) }) -join "`n"
$errors = @($hp -split "`n" | Where-Object { $_ -match '\[ERR\]|failed' })
Write-Host ("`n{0} dialog screenshot(s): {1}" -f $shots.Count, ($shots -join ', '))
Write-Host ("acad quit cleanly: {0}" -f $done)
Write-Host ("errors in the HPGeo logs: {0}" -f $errors.Count)
$errors | ForEach-Object { Write-Host "  $_" }
$webviewUp = ([regex]::Matches($hp, 'WebView2 [\d.]+ initialised')).Count
$tileUp = ([regex]::Matches($hp, 'map tile loaded')).Count
Write-Host ("WebView2 initialised: {0} x, map tile loaded: {1} x (spike gate: >= 1 each)" -f $webviewUp, $tileUp)
$imageLine = if ($hp -match '(HPGEOIMAGE fetched (\d+) tiles z=\d+ \((\d+) cached, ([^)]+)\)[^\r\n]*inserted ([0-9A-F]+) on HPGEO-IMAGE -> ([^\r\n]*?\.png)[^\r\n]*)') { $Matches } else { $null }
$imageOk = ($null -ne $imageLine) -and ($imageLine[6] -match '\\HPGeo\\images\\Drawing1_hpgeo_esri-\d{8}-\d{6}\.png$') -and ($imageLine[4] -eq 'helper') -and ([int]$imageLine[3] -eq 0)
Write-Host ("imagery button -> RasterImage inserted from the KMZ dialog: {0}" -f $(if ($imageOk) { "yes ($($imageLine[2]) tiles downloaded through the helper, handle $($imageLine[5]), $($imageLine[6]) - unsaved drawing, so under %LocalAppData%\HPGeo\images)" } elseif ($imageLine) { "inserted, but source '$($imageLine[4])' / $($imageLine[3]) cached / PNG at $($imageLine[6])" } else { 'NO - no HPGEOIMAGE line in the log' }))
$evidenceLog = Join-Path $evidence 'dialog-check-session.log'
[IO.File]::WriteAllText($evidenceLog, $hp, (New-Object Text.UTF8Encoding($false)))
if ($shots.Count -ne $expected.Count -or -not $done -or $errors.Count -gt 0 -or $webviewUp -lt 1 -or $tileUp -lt 1 -or -not $imageOk) { exit 1 }
