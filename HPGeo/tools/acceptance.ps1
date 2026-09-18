<#
.SYNOPSIS
  Unattended acceptance of the HPGeo bundle in AutoCAD 2026: loads, HPGEOINFO reports, -HPGEOKMZ writes a KMZ whose
  coordinates match the golden fixture, and the refusal paths refuse.

.DESCRIPTION
  Starts its own acad.exe (/product ACAD) with a generated script: LOGFILEON, HPGEOINFO on the empty drawing,
  13 POINTs + one closed PLINE (the reference tool's sample block, TP. Ho Chi Minh 105°45'), INSUNITS 6, HPGEOINFO
  again, then -HPGEOKMZ five times (full export, points-only, wrong zone -> outside Viet Nam, mm unit -> refused,
  unknown key -> refused) and QUIT. Everything the commands print goes to the AutoCAD text log; the KMZ files go
  to a space-free folder under %LocalAppData%\HPGeo\acceptance. Answers SECURELOAD ("Always Load") for the acad it
  started, kills only that process, never saves the user's drawings. FILEDIA, DYNMODE, OSMODE, LOGFILEPATH and
  COLORTHEME are profile settings AutoCAD writes back on a clean exit: the script reads the user's values over COM
  before it runs and restores them before QUIT (and again in the registry afterwards). Windows PowerShell 5.1 or pwsh.

.PARAMETER TimeoutSec
  How long to wait for the script to finish before killing AutoCAD (default 300).
#>
[CmdletBinding()]
param([int]$TimeoutSec = 300)

$ErrorActionPreference = 'Stop'
$here = Split-Path -Parent $MyInvocation.MyCommand.Path
$root = Split-Path -Parent $here
$golden = Join-Path $root 'HPGeo.Tests\Fixtures\golden-vn2000-to-wgs84.json'
$work = Join-Path $env:LOCALAPPDATA 'HPGeo\acceptance'
$evidence = Join-Path $root 'output\acceptance'
$hpgeoLogDir = Join-Path $env:LOCALAPPDATA 'HPGeo\logs'
$acad = 'C:\Program Files\Autodesk\AutoCAD 2026\acad.exe'
$bundle = Join-Path $env:APPDATA 'Autodesk\ApplicationPlugins\HPGeo.bundle\Contents\HPGeo.AutoCad.Loader.dll'

if (Get-Process acad -ErrorAction SilentlyContinue) { throw 'Close every AutoCAD first: the acceptance kills the acad.exe it starts and must not confuse it with yours.' }
if (-not (Test-Path $acad)) { throw "acad.exe not found at $acad" }
if (-not (Test-Path $bundle)) { throw "Bundle not deployed: $bundle (run dotnet build HPGeo.slnx -c Debug)" }
if (-not (Test-Path $golden)) { throw "Golden fixture missing: $golden (run node tools/gen-golden-vn2000-to-wgs84.js)" }

$sig = @'
using System; using System.Text; using System.Runtime.InteropServices;
public static class HPGeoNative {
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
if (-not ('HPGeoNative' -as [type])) { Add-Type -TypeDefinition $sig }

function Answer-SecureLoad {
    $dlg = [HPGeoNative]::FindWindow('#32770', 'Security - Unsigned Executable File')
    if ($dlg -eq [IntPtr]::Zero) { return $false }
    $btn = [HPGeoNative]::FindButton($dlg, 'Always Load')
    if ($btn -ne [IntPtr]::Zero) { [HPGeoNative]::SendMessage($btn, 0x00F5, [IntPtr]::Zero, [IntPtr]::Zero) | Out-Null; Write-Host "SECURELOAD answered 'Always Load' at $(Get-Date -Format HH:mm:ss)"; return $true }
    return $false
}

# ---- fixtures ------------------------------------------------------------------------------------------------------
$g = Get-Content $golden -Raw -Encoding UTF8 | ConvertFrom-Json
$samples = @($g.cases | Where-Object { $_.id -like 'sample-*' } | Select-Object -First 13)
if ($samples.Count -ne 13) { throw "expected 13 sample cases in the golden, got $($samples.Count)" }
$inv = [Globalization.CultureInfo]::InvariantCulture

if (Test-Path $work) { Remove-Item $work -Recurse -Force }
New-Item -ItemType Directory -Force $work | Out-Null
New-Item -ItemType Directory -Force $evidence | Out-Null
$textLogDir = Join-Path $work 'textlog'
New-Item -ItemType Directory -Force $textLogDir | Out-Null

$fmt = { param($v) $v.ToString('0.###', $inv) }
# Profile sysvars the script changes; the user's values are read over COM once acad is up and put back before QUIT.
$profileVars = @('FILEDIA', 'DYNMODE', 'OSMODE', 'LOGFILEPATH', 'CMDECHO')
$originals = @{}
function Build-Script([hashtable]$orig) {
$lines = New-Object System.Collections.Generic.List[string]
$lines.Add("LOGFILEPATH")
$lines.Add("$textLogDir\")
$lines.Add("LOGFILEON")
$lines.Add("DYNMODE")
$lines.Add("0")
$lines.Add("OSMODE")
$lines.Add("0")
$lines.Add("CMDECHO")
$lines.Add("1")
$lines.Add("HPGEOINFO")
$lines.Add("-HPGEOKMZ cm=105.75 out=$work\empty.kmz")
foreach ($s in $samples) { $lines.Add("_.POINT " + (& $fmt $s.e) + "," + (& $fmt $s.n)) }
$pl = "_.PLINE"
foreach ($s in $samples) { $pl += " " + (& $fmt $s.e) + "," + (& $fmt $s.n) }
$pl += " _C"
$lines.Add($pl)
$lines.Add("INSUNITS")
$lines.Add("6")
$lines.Add("HPGEOINFO")
$lines.Add("-HPGEOKMZ cm=105.75 type=both out=$work\site.kmz name=Acceptance")
$lines.Add("-HPGEOKMZ cm=105-45 type=points out=$work\points.kmz layer=0")
$lines.Add("-HPGEOKMZ cm=120 out=$work\wrongzone.kmz")
$lines.Add("-HPGEOKMZ cm=105.75 unit=mm out=$work\mm.kmz")
$lines.Add("-HPGEOKMZ cm=105.75 foo=1 out=$work\foo.kmz")
# P6: import the exported KMZ back (13 POINT + 1 LWPOLYLINE on HPGEO-IMPORT), export that layer again for a round-trip compare
$lines.Add("-HPGEOIMPORT file=$work\site.kmz cm=105.75")
$lines.Add("-HPGEOKMZ cm=105.75 layer=HPGEO-IMPORT out=$work\roundtrip.kmz name=RoundTrip")
$lines.Add("-HPGEOIMPORT file=$work\missing.kmz cm=105.75")
$lines.Add("-HPGEOIMPORT file=$work\site.kmz cm=120")
# Arcs + a mirrored polyline (normal -Z): read in WCS and flattened to a 5 mm chord tolerance
$lines.Add("-LAYER")
$lines.Add("_M")
$lines.Add("ARC")
$lines.Add("")
$lines.Add("_.PLINE 600000,1231000 _A 600100,1231100 _L 600200,1231000")
$lines.Add("")
$lines.Add("_.MIRROR _L")
$lines.Add("")
$lines.Add("600300,1230000 600300,1232000 _N")
$lines.Add("-HPGEOKMZ cm=105.75 type=boundaries layer=ARC out=$work\arc.kmz")
# P7: the zone is stored in the drawing: save, close, reopen, HPGEOINFO must print it
$lines.Add("_.SAVEAS")
$lines.Add("2018")
$lines.Add("$work\stored.dwg")
$lines.Add("_.CLOSE")
$lines.Add("_.OPEN")
$lines.Add("$work\stored.dwg")
$lines.Add("HPGEOINFO")
$lines.Add("LOGFILEOFF")
# Put the user's profile settings back before the clean exit saves the profile.
$lines.Add("FILEDIA"); $lines.Add("$($orig.FILEDIA)")
$lines.Add("DYNMODE"); $lines.Add("$($orig.DYNMODE)")
$lines.Add("OSMODE"); $lines.Add("$($orig.OSMODE)")
$lines.Add("CMDECHO"); $lines.Add("$($orig.CMDECHO)")
$lines.Add("LOGFILEPATH"); $lines.Add("$($orig.LOGFILEPATH)")
$lines.Add("_.QUIT")
$lines.Add("_Y")
return $lines
}
$scr = Join-Path $work 'acceptance.scr'

# ---- run -----------------------------------------------------------------------------------------------------------
# AutoCAD is started without a script: another bundle on this machine (CadAddinManager) injects an InitAddinManager
# command ~15 s after start-up, which cancels a running start-up script. So the harness waits for the application to
# settle, then hands the script over through COM to the acad.exe it started (pid-guarded) and waits for QUIT to end it.
$logMark = Get-Date
$p = Start-Process -FilePath $acad -ArgumentList @('/nologo', '/product', 'ACAD', '/language', '"en-US"') -PassThru
Write-Host "acad pid $($p.Id) started $(Get-Date -Format HH:mm:ss); script $scr"
$sw = [Diagnostics.Stopwatch]::StartNew()
$settleSec = 45
while ($sw.Elapsed.TotalSeconds -lt $settleSec) {
    Answer-SecureLoad | Out-Null
    if ($p.HasExited) { throw "acad exited during start-up (code $($p.ExitCode))" }
    Start-Sleep -Seconds 2
}
$sent = $false
for ($attempt = 1; $attempt -le 20 -and -not $sent; $attempt++) {
    Answer-SecureLoad | Out-Null
    try {
        $ids = @(Get-Process acad | ForEach-Object Id)
        if ($ids.Count -ne 1 -or [string]$ids[0] -ne [string]$p.Id) { throw "refusing COM: acad processes $($ids -join ',') vs ours $($p.Id)" }
        $app = [Runtime.InteropServices.Marshal]::GetActiveObject('AutoCAD.Application')
        foreach ($v in $profileVars) { $originals[$v] = [string]$app.ActiveDocument.GetVariable($v) }
        Write-Host ("user profile settings: " + (($profileVars | ForEach-Object { "$_=$($originals[$_])" }) -join ' '))
        [IO.File]::WriteAllLines($scr, (Build-Script $originals), (New-Object Text.UTF8Encoding($false)))
        Copy-Item $scr (Join-Path $evidence 'acceptance.scr') -Force
        $app.ActiveDocument.SendCommand("FILEDIA`n0`n_.SCRIPT`n$scr`n")
        $sent = $true
        Write-Host "script sent over COM at $(Get-Date -Format HH:mm:ss) (attempt $attempt)"
    } catch {
        if ($_.Exception.Message -match 'refusing COM') { throw }
        Write-Host "COM not ready (attempt $attempt): $($_.Exception.Message.Split("`n")[0])"
        Start-Sleep -Seconds 3
    }
}
if (-not $sent) { Stop-Process -Id $p.Id -Force -Confirm:$false -ErrorAction SilentlyContinue; throw 'could not hand the script to AutoCAD over COM' }
$done = $false
while ($sw.Elapsed.TotalSeconds -lt $TimeoutSec) {
    Answer-SecureLoad | Out-Null
    if ($p.HasExited) { $done = $true; Write-Host "acad exited after $([int]$sw.Elapsed.TotalSeconds) s (code $($p.ExitCode))"; break }
    Start-Sleep -Seconds 2
}
if (-not $done) {
    Write-Host "acad still running after $TimeoutSec s - killing pid $($p.Id)"
    Stop-Process -Id $p.Id -Force -Confirm:$false -ErrorAction SilentlyContinue
    Start-Sleep -Seconds 3
}

# ---- profile safety net ------------------------------------------------------------------------------------------------
# A clean exit saved the restored values; if anything went wrong before the restore lines ran, put the registry right.
if ($originals.Count -gt 0) {
    try {
        $fixed = 'HKCU:\SOFTWARE\Autodesk\AutoCAD\R25.1\ACAD-9101:409\FixedProfile\General Configuration'
        $prof = 'HKCU:\SOFTWARE\Autodesk\AutoCAD\R25.1\ACAD-9101:409\Profiles\<<Unnamed Profile>>'
        Set-ItemProperty -Path $fixed -Name 'FileDialog' -Value ([int]$originals.FILEDIA) -Type DWord
        Set-ItemProperty -Path $fixed -Name 'DYNMODE' -Value ([int]$originals.DYNMODE) -Type DWord
        Set-ItemProperty -Path "$prof\General" -Name 'Osmode' -Value ([int]$originals.OSMODE) -Type DWord
        Set-ItemProperty -Path "$prof\Editor Configuration" -Name 'LogFilePath' -Value $originals.LOGFILEPATH -Type String
        Write-Host "profile settings restored in the registry (FILEDIA $($originals.FILEDIA), DYNMODE $($originals.DYNMODE), OSMODE $($originals.OSMODE), LOGFILEPATH $($originals.LOGFILEPATH))"
    } catch { Write-Host "registry restore failed: $($_.Exception.Message)" }
}

# ---- evidence ------------------------------------------------------------------------------------------------------
# AutoCAD opens one text log per drawing (Drawing1, then stored.dwg): read them all, oldest first.
$textLogs = @(Get-ChildItem $textLogDir -Filter *.log -ErrorAction SilentlyContinue | Sort-Object LastWriteTime)
$text = ($textLogs | ForEach-Object { [IO.File]::ReadAllText($_.FullName) }) -join "`n"
$hpLogs = @(Get-ChildItem $hpgeoLogDir -Include 'hpgeo-*.log','loader.log' -Recurse -ErrorAction SilentlyContinue)
$hp = ($hpLogs | ForEach-Object { Get-Content $_.FullName -Encoding UTF8 } | Where-Object { $_ -match '^\d{4}-\d\d-\d\d \d\d:\d\d:\d\d' -and [datetime]::ParseExact($_.Substring(0, 19), 'yyyy-MM-dd HH:mm:ss', $inv) -ge $logMark.AddSeconds(-2) }) -join "`n"
[IO.File]::WriteAllText((Join-Path $evidence 'autocad-text.log'), $text, (New-Object Text.UTF8Encoding($false)))
[IO.File]::WriteAllText((Join-Path $evidence 'hpgeo-session.log'), $hp, (New-Object Text.UTF8Encoding($false)))
Get-ChildItem $work -Filter *.kmz -ErrorAction SilentlyContinue | Copy-Item -Destination $evidence -Force

# ---- checks --------------------------------------------------------------------------------------------------------
$results = New-Object System.Collections.Generic.List[object]
function Check([string]$name, [bool]$ok, [string]$detail = '') {
    $results.Add([pscustomobject]@{ Check = $name; Result = $(if ($ok) { 'PASS' } else { 'FAIL' }); Detail = $detail })
    Write-Host ("  [{0}] {1}{2}" -f $(if ($ok) { 'PASS' } else { 'FAIL' }), $name, $(if ($detail) { " - $detail" } else { '' }))
}

Write-Host "`nP2 - host adapter"
Check 'acad ran the script to QUIT' $done "exit within $TimeoutSec s"
Check 'HPGeo logged its load in AutoCAD' ($hp -match 'HPGeo \S+ loaded in "?AutoCAD') ($(if ($hp -match '(HPGeo \S+ loaded in [^\r\n]*)') { $Matches[1] } else { 'no load line' }))
Check 'HPGeo panel added to the shared HPAutoCad tab (no tab of its own)' (($hp -match 'ribbon panel HPGEO_VN2000_PANEL added to tab HPAUTOCAD_MCP_TAB \(tab (created|existing)') -and -not ($hp -match 'HPGEO_TAB'))
$acadLoaderLog = Join-Path $env:LOCALAPPDATA 'HPAutoCad\McpBridge\logs\loader.log'
$acadLoader = if (Test-Path $acadLoaderLog) { (Get-Content $acadLoaderLog -Encoding UTF8 | Where-Object { $_ -match '^\d{4}-\d\d-\d\d \d\d:\d\d:\d\d' -and [datetime]::ParseExact($_.Substring(0, 19), 'yyyy-MM-dd HH:mm:ss', $inv) -ge $logMark.AddSeconds(-2) }) -join "`n" } else { '' }
$tabCreators = @(@($hp, $acadLoader) | Where-Object { $_ -match 'added to tab HPAUTOCAD_MCP_TAB \(tab created' }).Count
$tabJoiners = @(@($hp, $acadLoader) | Where-Object { $_ -match 'added to tab HPAUTOCAD_MCP_TAB \(tab existing' }).Count
Check 'HPAutoCad MCP bridge and HPGeo share one tab: one created it, the other joined it' (($tabCreators -eq 1) -and ($tabJoiners -eq 1)) "created by $tabCreators add-in(s), joined by $tabJoiners (HPAutoCad loader.log $(if ($acadLoader) { 'read' } else { 'absent' }))"
Check 'HPGEOINFO printed the header' ($text -match 'HPGeo 0\.\d+\.\d+')
Check 'HPGEOINFO saw the empty drawing' ($text -match 'Model space: 0 POINT, 0 LWPOLYLINE')
Check 'HPGEOINFO saw 13 points + 1 closed polyline in metres' (($text -match 'INSUNITS: 6 = Meters') -and ($text -match 'Model space: 13 POINT, 1 LWPOLYLINE \(1 closed\)'))
Check 'HPGEOINFO trial conversion printed' ($text -match 'point 1 E=600125\.887 N=1231608\.428 .{1,10} Lat 11\.13558')

Write-Host "`nP3 - -HPGEOKMZ"
Check 'empty drawing refused (NO_INPUT), nothing written' (($hp -match 'HPGEOKMZ failed: NO_INPUT') -and -not (Test-Path (Join-Path $work 'empty.kmz')))
$site = Join-Path $work 'site.kmz'
Check 'site.kmz written' (Test-Path $site) $site
$kml = ''
if (Test-Path $site) {
    Add-Type -AssemblyName System.IO.Compression.FileSystem
    $zip = [IO.Compression.ZipFile]::OpenRead($site)
    try {
        $entry = $zip.Entries | Where-Object { $_.FullName -eq 'doc.kml' }
        Check 'doc.kml is the first entry' ($zip.Entries[0].FullName -eq 'doc.kml')
        $reader = New-Object IO.StreamReader($entry.Open(), [Text.Encoding]::UTF8)
        $kml = $reader.ReadToEnd(); $reader.Dispose()
    } finally { $zip.Dispose() }
    [IO.File]::WriteAllText((Join-Path $evidence 'site-doc.kml'), $kml, (New-Object Text.UTF8Encoding($false)))
    [xml]$x = $kml
    $ns = New-Object Xml.XmlNamespaceManager($x.NameTable); $ns.AddNamespace('k', 'http://www.opengis.net/kml/2.2')
    $pm = $x.SelectNodes('//k:Placemark', $ns)
    $vector = @($pm | Where-Object { $_.styleUrl -eq '#ptVectorStyle' })
    $labels = @($pm | Where-Object { $_.styleUrl -eq '#ptLabelStyle' })
    $polys = @($x.SelectNodes('//k:Polygon', $ns))
    Check '13 vector markers + 13 labels + 1 polygon' (($vector.Count -eq 13) -and ($labels.Count -eq 13) -and ($polys.Count -eq 1)) "$($vector.Count)/$($labels.Count)/$($polys.Count)"
    Check 'document name from name=' ($x.kml.Document.name -eq 'Acceptance')
    $ring = @(($x.SelectSingleNode('//k:LinearRing/k:coordinates', $ns).InnerText -split '\s+') | Where-Object { $_ })
    Check 'ring has 13 vertices + closing vertex' (($ring.Count -eq 14) -and ($ring[0] -eq $ring[13])) "$($ring.Count) coordinates"
    $worst = 0.0
    for ($i = 0; $i -lt [Math]::Min(13, $ring.Count); $i++) {
        $parts = $ring[$i] -split ','
        $lon = [double]::Parse($parts[0], $inv); $lat = [double]::Parse($parts[1], $inv)
        $dLat = [Math]::Abs($lat - [double]$samples[$i].lat); $dLon = [Math]::Abs($lon - [double]$samples[$i].lon)
        $worst = [Math]::Max($worst, [Math]::Max($dLat, $dLon))
    }
    Check 'ring vertices match the golden within 1e-7 deg' ($worst -le 1.0e-7) ("worst {0:E2} deg" -f $worst)
    $desc = $vector[0].description
    Check 'first marker description carries VN2000 X(N)/Y(E) and WGS84' (($desc -match 'X\(N\)=1231608\.428, Y\(E\)=600125\.887') -and ($desc -match 'Lat=11\.1355893'))
}
Check 'log: site.kmz 13 points, 1 boundary, KTT 105.75' ($hp -match 'HPGEOKMZ wrote .*site\.kmz: 13 points, 1 boundaries, KTT 105\.75')
$points = Join-Path $work 'points.kmz'
$pointsKml = ''
if (Test-Path $points) { $z2 = [IO.Compression.ZipFile]::OpenRead($points); try { $r2 = New-Object IO.StreamReader(($z2.Entries | Where-Object FullName -eq 'doc.kml').Open(), [Text.Encoding]::UTF8); $pointsKml = $r2.ReadToEnd(); $r2.Dispose() } finally { $z2.Dispose() } }
Check 'points.kmz (type=points, cm=105-45, layer=0) has 13 markers and no polygon' ((Test-Path $points) -and ($hp -match 'HPGEOKMZ wrote .*points\.kmz: 13 points, 1 boundaries, KTT 105\.75') -and ($pointsKml -notmatch '<Polygon>') -and (([regex]::Matches($pointsKml, '#ptVectorStyle')).Count -eq 13))
Check 'wrong zone (cm=120) and unit=mm both refused as outside Viet Nam, nothing written' ((([regex]::Matches($hp, 'HPGEOKMZ failed: OUTSIDE_VIETNAM')).Count -ge 1) -and -not (Test-Path (Join-Path $work 'wrongzone.kmz')) -and -not (Test-Path (Join-Path $work 'mm.kmz')))
Check 'unit=mm run reported the implausible-E/N hint before failing' ($hp -match 'failed: OUTSIDE_VIETNAM[^

]*warnings: IMPLAUSIBLE_EN')
Check 'unknown key refused' (($hp -match "HPGEOKMZ refused: Tham s\S+ 'foo'") -and -not (Test-Path (Join-Path $work 'foo.kmz')))
Write-Host "`nP6 - -HPGEOIMPORT (reverse)"
$rt = Join-Path $work 'roundtrip.kmz'
Check 'import of site.kmz drew 13 POINT + 1 LWPOLYLINE on HPGEO-IMPORT' ($hp -match 'HPGEOIMPORT wrote 13 points, 1 polylines on HPGEO-IMPORT')
Check 'roundtrip.kmz written from the HPGEO-IMPORT layer' ((Test-Path $rt) -and ($hp -match 'HPGEOKMZ wrote .*roundtrip\.kmz: 13 points, 1 boundaries'))
if ((Test-Path $rt) -and $kml) {
    $zr = [IO.Compression.ZipFile]::OpenRead($rt)
    try { $rr = New-Object IO.StreamReader(($zr.Entries | Where-Object FullName -eq 'doc.kml').Open(), [Text.Encoding]::UTF8); $rtKml = $rr.ReadToEnd(); $rr.Dispose() } finally { $zr.Dispose() }
    [xml]$xr = $rtKml
    $nsr = New-Object Xml.XmlNamespaceManager($xr.NameTable); $nsr.AddNamespace('k', 'http://www.opengis.net/kml/2.2')
    $ringA = @(($x.SelectSingleNode('//k:LinearRing/k:coordinates', $ns).InnerText -split '\s+') | Where-Object { $_ })
    $ringB = @(($xr.SelectSingleNode('//k:LinearRing/k:coordinates', $nsr).InnerText -split '\s+') | Where-Object { $_ })
    $worstRt = 0.0
    for ($i = 0; $i -lt [Math]::Min($ringA.Count, $ringB.Count); $i++) {
        $a = $ringA[$i] -split ','; $b = $ringB[$i] -split ','
        $worstRt = [Math]::Max($worstRt, [Math]::Max([Math]::Abs([double]::Parse($a[0], $inv) - [double]::Parse($b[0], $inv)), [Math]::Abs([double]::Parse($a[1], $inv) - [double]::Parse($b[1], $inv))))
    }
    Check 'round trip ring matches the original within 2e-8 deg (~2 mm)' (($ringA.Count -eq $ringB.Count) -and ($worstRt -le 2.0e-8)) ("worst {0:E2} deg over {1} vertices" -f $worstRt, $ringB.Count)
    $pointsA = @($x.SelectNodes('//k:MultiGeometry/k:Point/k:coordinates', $ns) | ForEach-Object { $_.InnerText.Trim() })
    $pointsB = @($xr.SelectNodes('//k:MultiGeometry/k:Point/k:coordinates', $nsr) | ForEach-Object { $_.InnerText.Trim() })
    Check '13 hidden exact points survive the round trip to 9 decimals' (($pointsA.Count -eq 13) -and ($pointsB.Count -eq 13) -and (@(Compare-Object $pointsA $pointsB).Count -eq 0)) "$($pointsA.Count)/$($pointsB.Count)"
}
Check 'missing file refused' ($hp -match "HPGEOIMPORT refused: Kh\S+ng t\S+m th\S+y file")
Check 'wrong zone (cm=120) import refused, nothing drawn' ($hp -match 'HPGEOIMPORT failed: OUTSIDE_ZONE')

$arc = Join-Path $work 'arc.kmz'
Check 'arc.kmz written from layer ARC (2 polylines, one mirrored)' ((Test-Path $arc) -and ($hp -match 'HPGEOKMZ wrote .*arc\.kmz: 0 points, 2 boundaries'))
if (Test-Path $arc) {
    $za = [IO.Compression.ZipFile]::OpenRead($arc)
    try { $ra = New-Object IO.StreamReader(($za.Entries | Where-Object FullName -eq 'doc.kml').Open(), [Text.Encoding]::UTF8); $arcKml = $ra.ReadToEnd(); $ra.Dispose() } finally { $za.Dispose() }
    [xml]$xa = $arcKml
    $nsa = New-Object Xml.XmlNamespaceManager($xa.NameTable); $nsa.AddNamespace('k', 'http://www.opengis.net/kml/2.2')
    $counts = @($xa.SelectNodes('//k:LineString/k:coordinates', $nsa) | ForEach-Object { @(($_.InnerText -split '\s+') | Where-Object { $_ }).Count })
    Check 'both arcs flattened to a 5 mm tolerance (>= 60 chords each), the mirrored one read in WCS' (($counts.Count -eq 2) -and (@($counts | Where-Object { $_ -ge 60 }).Count -eq 2)) ("vertices: " + ($counts -join ', '))
}

Write-Host "`nP7 - stored settings"
Check 'stored.dwg saved and reopened' ((Test-Path (Join-Path $work 'stored.dwg')) -and ($text -match 'Drawing: stored\.dwg'))
Check 'HPGEOINFO on the reopened drawing prints the stored zone' ($text -match 'Stored VN-2000 settings: KTT 105\.75 \(105.{1,8}45.{0,8}\), province -, catalog current')
Check 'reopened drawing still holds the imported entities' ($text -match 'Model space: 26 POINT, 4 LWPOLYLINE \(2 closed\)')
Check 'no error in the HPGeo log' (-not ($hp -match '\[ERR\]'))

$fails = @($results | Where-Object Result -eq 'FAIL').Count
$summary = [pscustomobject]@{ ranAt = (Get-Date).ToString('s'); pass = $results.Count - $fails; fail = $fails; checks = $results }
$summary | ConvertTo-Json -Depth 4 | Set-Content (Join-Path $evidence 'summary.json') -Encoding UTF8
Write-Host "`n$($results.Count - $fails) PASS, $fails FAIL - evidence in $evidence"
if ($fails -gt 0) { exit 1 }
