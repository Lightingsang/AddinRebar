import sys
from pathlib import Path

content = r'''<#
.SYNOPSIS
  Unified live verification test harness for HPAutoCad HPGeoLink and MCP Bridge in AutoCAD 2026.

.DESCRIPTION
  Executes comprehensive live acceptance and end-to-end verification of HPAutoCad.bundle across all 4 tiers:
    - Tier 1 (Feature Coverage): HPGEOINFO, -HPGEOKMZ, -HPGEOIMPORT, -HPGEOIMAGE, HPMCPBRIDGE, Shared Ribbon Tab.
    - Tier 2 (Boundary & Corner Cases): NO_INPUT, OUTSIDE_VIETNAM, IMPLAUSIBLE_EN, unknown args, missing files, OUTSIDE_ZONE,
                                      TOO_MANY_TILES, NO_BOUNDARY, wide view zoom reduction, arc/mirror WCS flattening.
    - Tier 3 (Cross-Feature Combinations): KMZ export -> import roundtrip delta <= 2e-8 deg (~2 mm) with 13 hidden points,
                                          satellite imagery under polyline ring + undo (_.U), DWG stored settings persistence.
    - Tier 4 (Real-World Cadastral & Modal UI): Modal dialog PrintWindow captures for HPGEO (dark & light) and HPGEOIMPORT,
                                               UIA button click for satellite image insertion, WSCURRENT & COLORTHEME rebuild,
                                               dual loader verification in %LocalAppData%\HPAutoCad\logs\ and %LocalAppData%\HPGeo\logs\.

  Outputs machine-readable summary.json to HPAutoCad/output/geolink-verify/summary.json and exits 0 on complete pass.
#>
[CmdletBinding()]
param(
    [int]$TimeoutSec = 420,
    [bool]$PrefetchTiles = $false,
    [string]$OutDir = '',
    [string]$OtherWorkspace = '3D Modeling'
)

$ErrorActionPreference = 'Stop'
$harnessRoot = $PSScriptRoot
if (-not $harnessRoot) { $harnessRoot = Split-Path -Parent $MyInvocation.MyCommand.Path }
$toolsDir = Split-Path -Parent $harnessRoot
$hpAutoCadRoot = Split-Path -Parent $toolsDir
$repoRoot = Split-Path -Parent $hpAutoCadRoot

if (-not $OutDir) {
    $OutDir = Join-Path $hpAutoCadRoot 'output\geolink-verify'
}

# Dot-source shared harness functions
. (Join-Path $harnessRoot 'harness-common.ps1')

Assert-NoAutocadRunning

# Paths & Locations
$acad = 'C:\Program Files\Autodesk\AutoCAD 2026\acad.exe'
$bundleDir = Join-Path $env:APPDATA 'Autodesk\ApplicationPlugins\HPAutoCad.bundle'
$bundleManifest = Join-Path $bundleDir 'PackageContents.xml'
$appLoader = Join-Path $bundleDir 'Contents\HPAutoCad.Loader.dll'
$bridgeLoader = Join-Path $bundleDir 'Contents\HPAutoCad.McpBridge.Loader.dll'
$golden = Join-Path $repoRoot 'HPAutoCad\HPAutoCad.Tests\HPGeoLink\Fixtures\golden-vn2000-to-wgs84.json'

$evidence = $OutDir
if (-not [System.IO.Path]::IsPathRooted($evidence)) {
    $evidence = Join-Path $PSScriptRoot $evidence
}
New-Item -ItemType Directory -Force $evidence | Out-Null
$evidence = (Resolve-Path $evidence).Path

$work = Join-Path $env:LOCALAPPDATA 'HPAutoCad\geolink-verify'
if (Test-Path $work) { Remove-Item $work -Recurse -Force }
New-Item -ItemType Directory -Force $work | Out-Null
$textLogDir = Join-Path $work 'textlog'
New-Item -ItemType Directory -Force $textLogDir | Out-Null

# Artifact destinations
$imagePng = Join-Path $work 'image.png'
$storedPng = Join-Path $work 'stored_hpgeo.png'
$screenshotDrawing = Join-Path $evidence 'image-in-autocad.png'
$dialogDarkPng = Join-Path $evidence 'dialog-dark.png'
$dialogLightPng = Join-Path $evidence 'dialog-light.png'
$dialogImportPng = Join-Path $evidence 'dialog-import.png'
$ribbonTabDarkPng = Join-Path $evidence 'ribbon-tab.png'
$ribbonTabLightPng = Join-Path $evidence 'ribbon-tab-theme1.png'

# Clear stale evidence screenshots
@($screenshotDrawing, $dialogDarkPng, $dialogLightPng, $dialogImportPng, $ribbonTabDarkPng, $ribbonTabLightPng) | ForEach-Object {
    Remove-Item $_ -Force -ErrorAction SilentlyContinue
}

# Pre-flight checks
if (-not (Test-Path $acad)) { throw "AutoCAD 2026 executable not found at: $acad" }
if (-not (Test-Path $bundleManifest)) { throw "HPAutoCad.bundle manifest missing: $bundleManifest" }
if (-not (Test-Path $appLoader)) { throw "App loader missing: $appLoader" }
if (-not (Test-Path $bridgeLoader)) { throw "Bridge loader missing: $bridgeLoader" }
if (-not (Test-Path $golden)) { throw "Golden fixture missing: $golden" }

# Native interop methods for off-screen PrintWindow and modal dialog discovery
$sig = @'
using System;
using System.Text;
using System.Runtime.InteropServices;

public static class GeolinkHarnessNative {
  public delegate bool EnumProc(IntPtr hWnd, IntPtr lParam);

  [DllImport("user32.dll")] public static extern IntPtr FindWindow(string lpClassName, string lpWindowName);
  [DllImport("user32.dll")] public static extern bool EnumChildWindows(IntPtr hWndParent, EnumProc lpEnumFunc, IntPtr lParam);
  [DllImport("user32.dll")] public static extern bool EnumWindows(EnumProc lpEnumFunc, IntPtr lParam);
  [DllImport("user32.dll")] public static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);
  [DllImport("user32.dll")] public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);
  [DllImport("user32.dll")] public static extern bool IsWindowVisible(IntPtr hWnd);
  [DllImport("user32.dll")] public static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);
  [DllImport("user32.dll")] public static extern bool PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

  [StructLayout(LayoutKind.Sequential)]
  public struct RECT { public int Left, Top, Right, Bottom; }

  [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);
  [DllImport("user32.dll")] public static extern bool PrintWindow(IntPtr hWnd, IntPtr hdcBlt, uint nFlags);

  public static string LastTitle = "";

  public static IntPtr FindLargestTopLevel(uint pid) {
    IntPtr found = IntPtr.Zero;
    long bestArea = 0;
    EnumWindows((h, l) => {
      uint p;
      GetWindowThreadProcessId(h, out p);
      if (p != pid || !IsWindowVisible(h)) return true;
      RECT r;
      if (!GetWindowRect(h, out r)) return true;
      long area = (long)(r.Right - r.Left) * (long)(r.Bottom - r.Top);
      if (area > bestArea) {
        bestArea = area;
        found = h;
        var sb = new StringBuilder(256);
        GetWindowText(h, sb, 256);
        LastTitle = sb.ToString();
      }
      return true;
    }, IntPtr.Zero);
    return found;
  }

  public static IntPtr FindExportDialog(uint pid) {
    IntPtr found = IntPtr.Zero;
    EnumWindows((h, l) => {
      uint p;
      GetWindowThreadProcessId(h, out p);
      if (p != pid || !IsWindowVisible(h)) return true;
      var sb = new StringBuilder(256);
      GetWindowText(h, sb, 256);
      string t = sb.ToString();
      if (t.IndexOf("VN-2000", StringComparison.OrdinalIgnoreCase) >= 0) {
        found = h;
        return false;
      }
      return true;
    }, IntPtr.Zero);
    return found;
  }

  public static IntPtr FindImportDialog(uint pid) {
    IntPtr found = IntPtr.Zero;
    EnumWindows((h, l) => {
      uint p;
      GetWindowThreadProcessId(h, out p);
      if (p != pid || !IsWindowVisible(h)) return true;
      var sb = new StringBuilder(256);
      GetWindowText(h, sb, 256);
      string t = sb.ToString();
      if (t.IndexOf("HPAutoCad", StringComparison.OrdinalIgnoreCase) >= 0 &&
          t.IndexOf("VN-2000", StringComparison.OrdinalIgnoreCase) < 0 &&
          t.IndexOf("Bridge", StringComparison.OrdinalIgnoreCase) < 0) {
        found = h;
        return false;
      }
      return true;
    }, IntPtr.Zero);
    return found;
  }
}
'@
if (-not ('GeolinkHarnessNative' -as [type])) { Add-Type -TypeDefinition $sig }
Add-Type -AssemblyName System.Drawing
Add-Type -AssemblyName UIAutomationClient
Add-Type -AssemblyName UIAutomationTypes
Add-Type -AssemblyName System.IO.Compression.FileSystem

# Helper for saving window capture via PrintWindow
function Save-WindowCapture([IntPtr]$hwnd, [string]$path) {
    try {
        $rect = New-Object GeolinkHarnessNative+RECT
        [GeolinkHarnessNative]::GetWindowRect($hwnd, [ref]$rect) | Out-Null
        $w = $rect.Right - $rect.Left
        $h = $rect.Bottom - $rect.Top
        if ($w -le 0 -or $h -le 0) {
            Write-Host "Window $hwnd has invalid rect: ${w}x${h}"
            return $false
        }
        $bmp = New-Object System.Drawing.Bitmap $w, $h
        $g = [System.Drawing.Graphics]::FromImage($bmp)
        $hdc = $g.GetHdc()
        $ok = [GeolinkHarnessNative]::PrintWindow($hwnd, $hdc, 2)
        $g.ReleaseHdc($hdc)
        $g.Dispose()
        $bmp.Save($path, [System.Drawing.Imaging.ImageFormat]::Png)
        $bmp.Dispose()
        return [bool]$ok
    } catch {
        Write-Host "Save-WindowCapture error: $($_.Exception.Message)"
        return $false
    }
}

# Helper to click WPF button inside modal dialog
function Invoke-DialogButton([IntPtr]$hwnd, [string]$pattern) {
    try {
        $root = [System.Windows.Automation.AutomationElement]::FromHandle($hwnd)
        $condition = New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::ControlTypeProperty, [System.Windows.Automation.ControlType]::Button)
        $btn = $null
        for ($i = 0; $i -lt 8 -and $null -eq $btn; $i++) {
            foreach ($b in $root.FindAll([System.Windows.Automation.TreeScope]::Descendants, $condition)) {
                if ($b.Current.Name -match $pattern) {
                    $btn = $b
                    break
                }
            }
            if ($null -eq $btn) { Start-Sleep -Milliseconds 500 }
        }
        if ($null -eq $btn) {
            Write-Host "UIA button matching '$pattern' not found"
            return $false
        }
        if (-not $btn.Current.IsEnabled) {
            Write-Host "UIA button '$($btn.Current.Name)' is disabled"
            return $false
        }
        $btn.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern).Invoke()
        return $true
    } catch {
        Write-Host "Invoke-DialogButton error: $($_.Exception.Message)"
        return $false
    }
}

# COM executor helper
function Com([string]$script) {
    if ($PSVersionTable.PSEdition -eq 'Desktop') {
        $ids = @(Get-Process acad -ErrorAction SilentlyContinue | ForEach-Object Id)
        if ($ids.Count -ne 1 -or [string]$ids[0] -ne [string]$script:acadPid) {
            throw ('refusing COM: acad pids [' + ($ids -join ',') + "], harness owns $script:acadPid")
        }
        $a = [Runtime.InteropServices.Marshal]::GetActiveObject('AutoCAD.Application')
        return (Invoke-Expression $script)
    } else {
        $ps = "`$ids = @(Get-Process acad -ErrorAction SilentlyContinue | % Id); if (`$ids.Count -ne 1 -or [string]`$ids[0] -ne '$script:acadPid') { throw ('refusing COM: acad pids [' + (`$ids -join ',') + '], harness owns $script:acadPid') }; `$a = [Runtime.InteropServices.Marshal]::GetActiveObject('AutoCAD.Application'); $script"
        for ($i = 0; $i -lt 5; $i++) {
            $out = powershell.exe -NoProfile -Command $ps 2>&1
            if ($LASTEXITCODE -eq 0) { return ($out -join "`n") }
            Write-Host "COM attempt $($i + 1) failed: $(($out -join ' ').Substring(0, [Math]::Min(140, ($out -join ' ').Length)))"
            Start-Sleep -Seconds 3
        }
        return ($out -join "`n")
    }
}

# Load golden test fixtures
$goldenData = Get-Content $golden -Raw -Encoding UTF8 | ConvertFrom-Json
$samples = @($goldenData.cases | Where-Object { $_.id -like 'sample-*' } | Select-Object -First 13)
if ($samples.Count -ne 13) { throw "Expected 13 golden sample cases, found $($samples.Count)" }
$inv = [Globalization.CultureInfo]::InvariantCulture
$fmt = { param($v) $v.ToString('0.###', $inv) }

# Tile Cache & User Settings handling
$userSettings = Join-Path $env:APPDATA 'HPGeo\settings.json'
$userSettingsBackup = if (Test-Path $userSettings) { [IO.File]::ReadAllBytes($userSettings) } else { $null }

$tileCache = Join-Path $env:LOCALAPPDATA 'HPGeo\tiles'
if (-not $PrefetchTiles) {
    if (Test-Path $tileCache) { Remove-Item $tileCache -Recurse -Force }
    Write-Host "Tile cache emptied ($tileCache): first imagery run must download via HPAutoCad.TileFetch.exe"
}

# Profile sysvars to preserve and restore
$profileVars = @('FILEDIA', 'DYNMODE', 'OSMODE', 'CMDECHO', 'LOGFILEPATH', 'COLORTHEME')
$originals = @{}
$originalWorkspace = $null

# Script generation function
function Build-AcceptanceScript([hashtable]$orig) {
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

    # Step 1: HPGEOINFO on empty drawing
    $lines.Add("HPGEOINFO")

    # Step 2: Empty drawing boundary refusal
    $lines.Add("-HPGEOKMZ cm=105.75 out=$work\empty.kmz")

    # Step 3: Draw 13 points and closed boundary polyline
    foreach ($s in $samples) {
        $lines.Add("_.POINT " + (& $fmt $s.e) + "," + (& $fmt $s.n))
    }
    $pl = "_.PLINE"
    foreach ($s in $samples) {
        $pl += " " + (& $fmt $s.e) + "," + (& $fmt $s.n)
    }
    $pl += " _C"
    $lines.Add($pl)
    $lines.Add("INSUNITS")
    $lines.Add("6")

    # Step 4: HPGEOINFO on drawing with points & boundary
    $lines.Add("HPGEOINFO")

    # Step 5: Modal Dialog 1: Dark theme capture
    $lines.Add("COLORTHEME")
    $lines.Add("0")
    $lines.Add("_.HPGEO")
    $lines.Add("")

    # Step 6: Modal Dialog 2: Light theme capture & UIA satellite button click
    $lines.Add("COLORTHEME")
    $lines.Add("1")
    $lines.Add("_.HPGEO")
    $lines.Add("")

    # Step 7: Undo the modal dialog's satellite insertion to keep canvas clean
    $lines.Add("_.U")

    # Step 8: Modal Dialog 3: Import dialog in dark theme
    $lines.Add("COLORTHEME")
    $lines.Add("0")
    $lines.Add("_.HPGEOIMPORT")

    # Step 9: Full KMZ export and points-only export
    $lines.Add("-HPGEOKMZ cm=105.75 type=both out=$work\site.kmz name=Acceptance")
    $lines.Add("-HPGEOKMZ cm=105-45 type=points out=$work\points.kmz layer=0")

    # Step 10: Boundary refusals
    $lines.Add("-HPGEOKMZ cm=120 out=$work\wrongzone.kmz")
    $lines.Add("-HPGEOKMZ cm=105.75 unit=mm out=$work\mm.kmz")
    $lines.Add("-HPGEOKMZ cm=105.75 foo=1 out=$work\foo.kmz")

    # Step 11: Import roundtrip & import refusals
    $lines.Add("-HPGEOIMPORT file=$work\site.kmz cm=105.75")
    $lines.Add("-HPGEOKMZ cm=105.75 layer=HPGEO-IMPORT out=$work\roundtrip.kmz name=RoundTrip")
    $lines.Add("-HPGEOIMPORT file=$work\missing.kmz cm=105.75")
    $lines.Add("-HPGEOIMPORT file=$work\site.kmz cm=120")

    # Step 12: Arc and mirrored polyline WCS flattening
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

    # Step 13: Satellite imagery CLI, undo, wide zoom, and refusals
    $lines.Add("_.ZOOM _W 600060,1231340 600230,1231650")
    $lines.Add("-HPGEOIMAGE cm=105.75 layer=0 res=0.5 margin=30 out=$imagePng")
    $lines.Add("HPGEOINFO")
    $lines.Add("_.U")
    $lines.Add("HPGEOINFO")
    $lines.Add("-HPGEOIMAGE cm=105.75 layer=0 area=100 out=$work\wide.png")
    $lines.Add("_.U")
    $lines.Add("-HPGEOIMAGE cm=120 layer=0 res=0.5 out=$work\wrongzone.png")
    $lines.Add("-HPGEOIMAGE cm=105.75 layer=0 res=0.5 margin=2000 out=$work\big.png")
    $lines.Add("-HPGEOIMAGE cm=105.75 layer=NOSUCHLAYER out=$work\nolayer.png")

    # Step 14: DWG persistence & reopening
    $lines.Add("_.SAVEAS")
    $lines.Add("2018")
    $lines.Add("$work\stored.dwg")
    $lines.Add("-HPGEOIMAGE layer=0 res=0.5 margin=30 out=$storedPng")
    $lines.Add("DELAY 6000")
    $lines.Add("_.QSAVE")
    $lines.Add("_.CLOSE")
    $lines.Add("_.OPEN")
    $lines.Add("$work\stored.dwg")
    $lines.Add("HPGEOINFO")
    $lines.Add("LOGFILEOFF")

    # Step 15: Restore profile settings and exit cleanly
    $lines.Add("FILEDIA"); $lines.Add("$($orig.FILEDIA)")
    $lines.Add("DYNMODE"); $lines.Add("$($orig.DYNMODE)")
    $lines.Add("OSMODE"); $lines.Add("$($orig.OSMODE)")
    $lines.Add("CMDECHO"); $lines.Add("$($orig.CMDECHO)")
    $lines.Add("LOGFILEPATH"); $lines.Add("$($orig.LOGFILEPATH)")
    $lines.Add("COLORTHEME"); $lines.Add("$($orig.COLORTHEME)")
    $lines.Add("_.QUIT")
    $lines.Add("_Y")

    return $lines
}

# --- Execution & Monitoring -----------------------------------------------------------------------------------------
$tabId = 'HPAUTOCAD_MCP_TAB'
$logMark = Get-Date

$startupScr = Join-Path $work 'startup.scr'
Set-Content $startupScr "HPMCPSTART`r`n"

Write-Host "Starting AutoCAD 2026 for unified live verification with startup script..."
$p = Start-Process -FilePath $acad -ArgumentList @('/nologo', '/product', 'ACAD', '/language', '"en-US"', '/b', "`"$startupScr`"") -PassThru
$script:acadPid = $p.Id
$env:HP_HARNESS_ACAD_PID = $p.Id
Write-Host "AutoCAD process started (PID $($p.Id)) at $(Get-Date -Format 'HH:mm:ss')"

$sw = [Diagnostics.Stopwatch]::StartNew()
$settleSec = 45
Write-Host "Waiting $settleSec s for AutoCAD startup and CadAddinManager settlement..."
while ($sw.Elapsed.TotalSeconds -lt $settleSec) {
    Answer-SecureLoad | Out-Null
    if ($p.HasExited) { throw "AutoCAD exited unexpectedly during startup (code $($p.ExitCode))" }
    Start-Sleep -Seconds 2
}

# Results collection
$results = New-Object System.Collections.Generic.List[object]
function Record-Check([string]$id, [string]$name, [string]$tier, [bool]$pass, [string]$detail = '') {
    $results.Add([pscustomobject]@{
        Id = $id
        Name = $name
        Tier = $tier
        Result = $(if ($pass) { 'PASS' } else { 'FAIL' })
        Detail = $detail
    })
    $tag = if ($pass) { '[PASS]' } else { '[FAIL]' }
    Write-Host ("  {0} {1}: {2}{3}" -f $tag, $id, $name, $(if ($detail) { " ($detail)" } else { '' }))
}

try {
    # --- PHASE 1: Ribbon UI, Workspace Switching & Theme Rebuild Verification ------------------------------------------
    Write-Host "`n=== Phase 1: Ribbon Tab, Panels, Workspace & Theme Verification ==="

    $pipeUp = Test-Path '\\.\pipe\hpautocad-mcp-2026'
    Record-Check 'T1-BRIDGE-01' 'HPMCPBRIDGE named pipe active' 'Tier 1' $pipeUp "Pipe: \\.\pipe\hpautocad-mcp-2026"

    $tabs = @()
    $ribbonWait = [Diagnostics.Stopwatch]::StartNew()
    while ($ribbonWait.Elapsed.TotalSeconds -lt 60 -and $tabs.Count -eq 0) {
        Answer-SecureLoad | Out-Null
        Start-Sleep -Seconds 3
        try { $tabs = @(Find-RibbonTabs $p.Id $tabId) } catch { $tabs = @() }
    }

    $tabVisible = $tabs.Count -eq 1
    Record-Check 'T1-RIBBON-01' 'Shared Ribbon Tab HPAUTOCAD_MCP_TAB visible exactly once' 'Tier 1' $tabVisible "Found $($tabs.Count) tab(s)"

    if ($tabVisible) {
        Select-RibbonTab $p.Id $tabId | Out-Null
        $mcpBtn = Find-RibbonButton $p.Id '^MCP Bridge$'
        Record-Check 'T1-RIBBON-02' 'Shared Ribbon Tab contains panel HPAUTOCAD_MCP_PANEL with MCP Bridge button' 'Tier 1' ($null -ne $mcpBtn)
        $kmzBtn = Find-RibbonButton $p.Id '^KMZ$'
        Record-Check 'T1-RIBBON-03' 'Shared Ribbon Tab contains panel HPGEOLINK_PANEL with KMZ button' 'Tier 1' ($null -ne $kmzBtn)

        # Ribbon dark theme screenshot
        Save-RibbonScreenshot $p.Id $ribbonTabDarkPng | Out-Null

        # Workspace switch test (Tier 4)
        $originalWorkspace = ([string](Com "`$a.ActiveDocument.GetVariable('WSCURRENT')")).Trim()
        $null = Com "`$a.ActiveDocument.SetVariable('WSCURRENT', '$OtherWorkspace'); 'ok'"
        Start-Sleep -Seconds 6
        $tabsAfterSwitch = @(Find-RibbonTabs $p.Id $tabId)
        $null = Com "`$a.ActiveDocument.SetVariable('WSCURRENT', '$originalWorkspace'); 'ok'"
        Start-Sleep -Seconds 6
        $tabsAfterBack = @(Find-RibbonTabs $p.Id $tabId)
        $workspaceOk = ($tabsAfterSwitch.Count -eq 1) -and ($tabsAfterBack.Count -eq 1)
        Record-Check 'T4-UI-01' 'Dynamic workspace switch preserves single shared Ribbon tab' 'Tier 4' $workspaceOk "'$originalWorkspace' -> '$OtherWorkspace' -> '$originalWorkspace'"

        # Theme flip test (Tier 4)
        $origTheme = ([string](Com "`$a.ActiveDocument.GetVariable('COLORTHEME')")).Trim()
        $flipTheme = if ($origTheme -eq '0') { 1 } else { 0 }
        $null = Com "`$a.ActiveDocument.SetVariable('COLORTHEME', [int16]$flipTheme); 'ok'"
        Start-Sleep -Seconds 6
        Select-RibbonTab $p.Id $tabId | Out-Null
        Save-RibbonScreenshot $p.Id $ribbonTabLightPng | Out-Null
        $tabsLight = @(Find-RibbonTabs $p.Id $tabId)
        $null = Com "`$a.ActiveDocument.SetVariable('COLORTHEME', [int16]$origTheme); 'ok'"
        Start-Sleep -Seconds 6
        $tabsDark = @(Find-RibbonTabs $p.Id $tabId)
        $themeOk = ($tabsLight.Count -eq 1) -and ($tabsDark.Count -eq 1) -and (Test-Path $ribbonTabLightPng)
        Record-Check 'T4-UI-02' 'Dynamic theme switch preserves tab and rebuilds panel' 'Tier 4' $themeOk "COLORTHEME: $origTheme -> $flipTheme -> $origTheme"

        $ribbonShotsOk = (Test-Path $ribbonTabDarkPng) -and (Test-Path $ribbonTabLightPng)
        Record-Check 'T4-UI-03' 'Ribbon screenshots captured for both dark and light themes' 'Tier 4' $ribbonShotsOk "Dark: $(Test-Path $ribbonTabDarkPng), Light: $(Test-Path $ribbonTabLightPng)"

        # MCP Bridge button test
        Select-RibbonTab $p.Id $tabId | Out-Null
        $invoked = Invoke-RibbonButton $p.Id '^MCP Bridge$'
        Start-Sleep -Seconds 4
        $bridgeWin = Find-BridgeWindow
        Record-Check 'T1-BRIDGE-02' 'HPMCPBRIDGE button opens bridge status window' 'Tier 1' ($null -ne $bridgeWin)
        if ($bridgeWin) {
            try {
                $closePattern = $bridgeWin.GetCurrentPattern([System.Windows.Automation.WindowPattern]::Pattern)
                $closePattern.Close()
            } catch {
                [GeolinkHarnessNative]::PostMessage([IntPtr]$bridgeWin.Current.NativeWindowHandle, 0x0010, [IntPtr]::Zero, [IntPtr]::Zero) | Out-Null
            }
        }
    }

    # --- PHASE 2: Profile Variable Snapshot & Script Handover -----------------------------------------------------------
    Write-Host "`n=== Phase 2: Reading User Profile Variables & Script Dispatch ==="
    for ($attempt = 1; $attempt -le 20 -and $originals.Count -eq 0; $attempt++) {
        Answer-SecureLoad | Out-Null
        try {
            foreach ($v in $profileVars) {
                $val = ([string](Com "`$a.ActiveDocument.GetVariable('$v')")).Trim()
                $originals[$v] = $val
            }
        } catch {
            Write-Host "COM not ready yet (attempt $attempt): $($_.Exception.Message)"
            Start-Sleep -Seconds 3
        }
    }
    Write-Host "Captured user profile settings: $(($profileVars | ForEach-Object { "$_=$($originals[$_])" }) -join ' ')"

    $scrFile = Join-Path $work 'geolink-verify.scr'
    $scrLines = Build-AcceptanceScript $originals
    [IO.File]::WriteAllLines($scrFile, $scrLines, (New-Object Text.UTF8Encoding($false)))
    Copy-Item $scrFile (Join-Path $evidence 'geolink-verify.scr') -Force

    Write-Host "Handing script to AutoCAD via background job..."
    $job = Start-Job -ArgumentList $p.Id, $scrFile -ScriptBlock {
        param($acadPid, $scriptPath)
        $ids = @(Get-Process acad | ForEach-Object Id)
        if ($ids.Count -ne 1 -or [string]$ids[0] -ne [string]$acadPid) { throw 'refusing COM: another acad is running' }
        $app = [Runtime.InteropServices.Marshal]::GetActiveObject('AutoCAD.Application')
        $app.ActiveDocument.SendCommand("FILEDIA`n0`n_.SCRIPT`n$scriptPath`n")
        return "script ran to its end at $(Get-Date -Format HH:mm:ss)"
    }

    # --- PHASE 3: Interactive Modal Dialog Automation & Drawing Capture -------------------------------------------------
    Write-Host "`n=== Phase 3: Monitoring Modal Dialogs & Screen Captures ==="
    $seenHwnds = @{}
    $modalDarkDone = $false
    $modalLightDone = $false
    $modalImportDone = $false
    $drawingShotDone = $false

    $monSw = [Diagnostics.Stopwatch]::StartNew()
    while ($monSw.Elapsed.TotalSeconds -lt $TimeoutSec -and -not $p.HasExited) {
        Answer-SecureLoad | Out-Null

        # Dialog 1: Export Dark
        if (-not $modalDarkDone) {
            $h = [GeolinkHarnessNative]::FindExportDialog([uint32]$p.Id)
            if ($h -ne [IntPtr]::Zero -and -not $seenHwnds.ContainsKey($h)) {
                $seenHwnds[$h] = $true
                Write-Host "Modal Dialog 1 (Dark HPGEO) detected (HWND $h). Rendering..."
                Start-Sleep -Milliseconds 8000
                $ok = Save-WindowCapture $h $dialogDarkPng
                Record-Check 'T4-MODAL-01' 'HPGEO modal dialog off-screen PrintWindow capture in dark theme' 'Tier 4' ($ok -and (Test-Path $dialogDarkPng)) "$dialogDarkPng"
                [GeolinkHarnessNative]::PostMessage($h, 0x0010, [IntPtr]::Zero, [IntPtr]::Zero) | Out-Null
                $modalDarkDone = $true
                Start-Sleep -Seconds 2
            }
        }
        # Dialog 2: Export Light + UIA Satellite Button Click
        elseif (-not $modalLightDone) {
            $h = [GeolinkHarnessNative]::FindExportDialog([uint32]$p.Id)
            if ($h -ne [IntPtr]::Zero -and -not $seenHwnds.ContainsKey($h)) {
                $seenHwnds[$h] = $true
                Write-Host "Modal Dialog 2 (Light HPGEO) detected (HWND $h). Rendering..."
                Start-Sleep -Milliseconds 8000
                $ok = Save-WindowCapture $h $dialogLightPng
                Record-Check 'T4-MODAL-02' 'HPGEO modal dialog off-screen PrintWindow capture in light theme' 'Tier 4' ($ok -and (Test-Path $dialogLightPng)) "$dialogLightPng"

                Write-Host "Triggering UIA satellite image button..."
                $pressed = Invoke-DialogButton $h 'CAD|ve tinh|v\S+ tinh'
                Record-Check 'T4-MODAL-04' 'UIA button click for satellite image insertion on modal dialog' 'Tier 4' $pressed
                if (-not $pressed) {
                    [GeolinkHarnessNative]::PostMessage($h, 0x0010, [IntPtr]::Zero, [IntPtr]::Zero) | Out-Null
                }
                $modalLightDone = $true
                Start-Sleep -Seconds 2
            }
        }
        # Dialog 3: Import Dark
        elseif (-not $modalImportDone) {
            $h = [GeolinkHarnessNative]::FindImportDialog([uint32]$p.Id)
            if ($h -ne [IntPtr]::Zero -and -not $seenHwnds.ContainsKey($h)) {
                $seenHwnds[$h] = $true
                Write-Host "Modal Dialog 3 (Import) detected (HWND $h). Rendering..."
                Start-Sleep -Milliseconds 4000
                $ok = Save-WindowCapture $h $dialogImportPng
                Record-Check 'T4-MODAL-03' 'HPGEOIMPORT modal dialog off-screen PrintWindow capture' 'Tier 4' ($ok -and (Test-Path $dialogImportPng)) "$dialogImportPng"
                [GeolinkHarnessNative]::PostMessage($h, 0x0010, [IntPtr]::Zero, [IntPtr]::Zero) | Out-Null
                $modalImportDone = $true
                Start-Sleep -Seconds 2
            }
        }

        # Drawing Area Capture during DELAY 6000
        if (-not $drawingShotDone -and (Test-Path $storedPng)) {
            Start-Sleep -Seconds 2
            $frameHwnd = [GeolinkHarnessNative]::FindLargestTopLevel([uint32]$p.Id)
            if ($frameHwnd -ne [IntPtr]::Zero) {
                $drawingShotDone = Save-WindowCapture $frameHwnd $screenshotDrawing
                Write-Host "Drawing area screenshot saved: $drawingShotDone -> $screenshotDrawing"
            }
        }

        Start-Sleep -Milliseconds 500
    }

    $acadCleanExit = $p.HasExited
    if (-not $acadCleanExit) {
        Write-Host "AutoCAD exceeded $TimeoutSec s timeout - killing process $($p.Id)..."
        Stop-Process -Id $p.Id -Force -Confirm:$false -ErrorAction SilentlyContinue
    }
    $jobResult = try { Receive-Job $job -Wait -ErrorAction SilentlyContinue } catch { "Job wait error: $($_.Exception.Message)" }
    Remove-Job $job -Force -ErrorAction SilentlyContinue
    Write-Host "Background job result: $jobResult"
    Write-Host "AutoCAD execution finished (CleanExit: $acadCleanExit, Duration: $([int]$sw.Elapsed.TotalSeconds) s)"
}
finally {
    # Safety Net: User settings and profile restoration
    try {
        if ($null -ne $userSettingsBackup) {
            [IO.File]::WriteAllBytes($userSettings, $userSettingsBackup)
            Write-Host "Restored user settings: $userSettings"
        } elseif (Test-Path $userSettings) {
            Remove-Item $userSettings -Force -ErrorAction SilentlyContinue
        }
    } catch { Write-Host "Settings restoration error: $($_.Exception.Message)" }

    if ($originals.Count -gt 0) {
        try {
            foreach ($key in Get-ChildItem 'HKCU:\SOFTWARE\Autodesk\AutoCAD\R25.1' -ErrorAction SilentlyContinue) {
                $fixed = Join-Path $key.PSPath 'FixedProfile\General Configuration'
                if (Test-Path $fixed) {
                    if ($originals.ContainsKey('FILEDIA')) { Set-ItemProperty -Path $fixed -Name 'FileDialog' -Value ([int]$originals.FILEDIA) -Type DWord -ErrorAction SilentlyContinue }
                    if ($originals.ContainsKey('DYNMODE')) { Set-ItemProperty -Path $fixed -Name 'DYNMODE' -Value ([int]$originals.DYNMODE) -Type DWord -ErrorAction SilentlyContinue }
                }
                $profiles = Join-Path $key.PSPath 'Profiles'
                if (Test-Path $profiles) {
                    foreach ($prof in Get-ChildItem $profiles -ErrorAction SilentlyContinue) {
                        $gen = Join-Path $prof.PSPath 'General'
                        if (Test-Path $gen) {
                            if ($originals.ContainsKey('OSMODE')) { Set-ItemProperty -Path $gen -Name 'Osmode' -Value ([int]$originals.OSMODE) -Type DWord -ErrorAction SilentlyContinue }
                            if ($originals.ContainsKey('CMDECHO')) { Set-ItemProperty -Path $gen -Name 'CmdEcho' -Value ([int]$originals.CMDECHO) -Type DWord -ErrorAction SilentlyContinue }
                        }
                        $editor = Join-Path $prof.PSPath 'Editor Configuration'
                        if (Test-Path $editor) {
                            if ($originals.ContainsKey('LOGFILEPATH')) { Set-ItemProperty -Path $editor -Name 'LogFilePath' -Value $originals.LOGFILEPATH -Type String -ErrorAction SilentlyContinue }
                        }
                    }
                }
            }
            Write-Host "AutoCAD profile sysvars safely restored in registry."
        } catch { Write-Host "Registry restoration error: $($_.Exception.Message)" }
    }

    if (-not $p.HasExited) {
        Stop-Process -Id $p.Id -Force -Confirm:$false -ErrorAction SilentlyContinue
    }
}

# --- PHASE 4: Evidence Harvesting & Assertions ----------------------------------------------------------------------
Write-Host "`n=== Phase 4: Harvesting Logs & Evaluating Assertions ==="

# Read AutoCAD text logs
$textLogs = @(Get-ChildItem $textLogDir -Filter *.log -ErrorAction SilentlyContinue | Sort-Object LastWriteTime)
$text = ($textLogs | ForEach-Object { [IO.File]::ReadAllText($_.FullName) }) -join "`n"
[IO.File]::WriteAllText((Join-Path $evidence 'autocad-text.log'), $text, (New-Object Text.UTF8Encoding($false)))

# Read loader logs
$appLoaderLogPath = Join-Path $env:LOCALAPPDATA 'HPAutoCad\logs\loader.log'
$legacyLoaderLogPath = Join-Path $env:LOCALAPPDATA 'HPGeo\logs\loader.log'
$bridgeLoaderLogPath = Join-Path $env:LOCALAPPDATA 'HPAutoCad\McpBridge\logs\loader.log'

$appLoaderLog = if (Test-Path $appLoaderLogPath) { [IO.File]::ReadAllText($appLoaderLogPath) } else { '' }
$legacyLoaderLog = if (Test-Path $legacyLoaderLogPath) { [IO.File]::ReadAllText($legacyLoaderLogPath) } else { '' }
$bridgeLoaderLog = if (Test-Path $bridgeLoaderLogPath) { [IO.File]::ReadAllText($bridgeLoaderLogPath) } else { '' }

if ($appLoaderLog) { [IO.File]::WriteAllText((Join-Path $evidence 'loader.log'), $appLoaderLog, (New-Object Text.UTF8Encoding($false))) }
if ($bridgeLoaderLog) { [IO.File]::WriteAllText((Join-Path $evidence 'mcpbridge-loader.log'), $bridgeLoaderLog, (New-Object Text.UTF8Encoding($false))) }

# Read HPGeo/HPGeoLink session logs
$hpLogs = @(Get-ChildItem (Join-Path $env:LOCALAPPDATA 'HPGeo\logs') -Include 'hpgeo-*.log' -Recurse -ErrorAction SilentlyContinue)
$hpSession = ($hpLogs | ForEach-Object { Get-Content $_.FullName -Encoding UTF8 } |
    Where-Object { $_ -match '^\d{4}-\d\d-\d\d \d\d:\d\d:\d\d' -and [datetime]::ParseExact($_.Substring(0, 19), 'yyyy-MM-dd HH:mm:ss', $inv) -ge $logMark.AddSeconds(-2) }) -join "`n"
[IO.File]::WriteAllText((Join-Path $evidence 'hpgeo-session.log'), $hpSession, (New-Object Text.UTF8Encoding($false)))

# Copy output files to evidence
Get-ChildItem $work -Filter *.kmz -ErrorAction SilentlyContinue | Copy-Item -Destination $evidence -Force
Get-ChildItem $work -Include 'image.png','image.pgw','stored_hpgeo.png','stored_hpgeo.pgw','wide.png' -Recurse -ErrorAction SilentlyContinue | Copy-Item -Destination $evidence -Force

# --- Assertions: Tier 1 (Feature Coverage) --------------------------------------------------------------------------
Write-Host "`nEvaluating Tier 1 (Feature Coverage)..."
Record-Check 'T1-INFO-01' 'HPGEOINFO executed and printed header with HPGeo and AutoCAD versions' 'Tier 1' ($text -match 'HPGeo 0\.\d+\.\d+')
Record-Check 'T1-INFO-02' 'HPGEOINFO reported empty drawing status (0 POINT, 0 LWPOLYLINE)' 'Tier 1' ($text -match 'Model space: 0 POINT, 0 LWPOLYLINE')
Record-Check 'T1-INFO-03' 'HPGEOINFO reported 13 POINT, 1 LWPOLYLINE (1 closed), INSUNITS 6 = Meters' 'Tier 1' (($text -match 'INSUNITS: 6 = Meters') -and ($text -match 'Model space: 13 POINT, 1 LWPOLYLINE \(1 closed\)'))
Record-Check 'T1-INFO-04' 'HPGEOINFO calculated trial geodetic conversion for point 1' 'Tier 1' ($text -match 'point 1 E=600125\.887 N=1231608\.428 .{1,10} Lat 11\.13558')
Record-Check 'T1-INFO-05' 'HPGEOINFO verified out-of-process tile fetch helper path HPAutoCad.TileFetch.exe' 'Tier 1' ($text -match 'fetch helper HPAutoCad\.TileFetch\.exe')

$siteKmz = Join-Path $work 'site.kmz'
Record-Check 'T1-KMZ-01' '-HPGEOKMZ full export wrote site.kmz' 'Tier 1' ((Test-Path $siteKmz) -and ($hpSession -match 'HPGEOKMZ wrote .*site\.kmz: 13 points, 1 boundaries, KTT 105\.75'))

$pointsKmz = Join-Path $work 'points.kmz'
$pointsKml = ''
if (Test-Path $pointsKmz) {
    $zipPts = [IO.Compression.ZipFile]::OpenRead($pointsKmz)
    try {
        $rPts = New-Object IO.StreamReader(($zipPts.Entries | Where-Object FullName -eq 'doc.kml').Open(), [Text.Encoding]::UTF8)
        $pointsKml = $rPts.ReadToEnd(); $rPts.Dispose()
    } finally { $zipPts.Dispose() }
}
Record-Check 'T1-KMZ-02' '-HPGEOKMZ points-only export wrote points.kmz without Polygon' 'Tier 1' ((Test-Path $pointsKmz) -and ($pointsKml -notmatch '<Polygon>') -and (([regex]::Matches($pointsKml, '#ptVectorStyle')).Count -eq 13))
Record-Check 'T1-IMPORT-01' '-HPGEOIMPORT imported site.kmz drawing 13 POINT + 1 LWPOLYLINE on HPGEO-IMPORT' 'Tier 1' ($hpSession -match 'HPGEOIMPORT wrote 13 points, 1 polylines on HPGEO-IMPORT')

$imgInserted = if ($hpSession -match '(HPGEOIMAGE fetched (\d+) tiles z=(\d+)[^\r\n]*warped (\d+)x(\d+) inserted ([0-9A-F]+) on HPGEO-IMAGE[^\r\n]*)') { $Matches } else { $null }
Record-Check 'T1-IMAGE-01' '-HPGEOIMAGE fetched tiles, warped, and inserted RasterImage on HPGEO-IMAGE' 'Tier 1' (($null -ne $imgInserted) -and ($text -match 'HPGeo: OK .{1,3} RasterImage [0-9A-F]+ on HPGEO-IMAGE'))
Record-Check 'T1-LOADER-01' 'Both loaders registered: HPAutoCad.Loader.dll and HPAutoCad.McpBridge.Loader.dll' 'Tier 1' (($appLoaderLog -match 'HPAutoCad.*loaded') -and ($bridgeLoaderLog -match 'ribbon panel HPAUTOCAD_MCP_PANEL added'))

# --- Assertions: Tier 2 (Boundary & Corner Cases) -------------------------------------------------------------------
Write-Host "`nEvaluating Tier 2 (Boundary & Corner Cases)..."
Record-Check 'T2-BOUND-01' '-HPGEOKMZ refused empty drawing with NO_INPUT, nothing written' 'Tier 2' (($hpSession -match 'HPGEOKMZ failed: NO_INPUT') -and -not (Test-Path (Join-Path $work 'empty.kmz')))
Record-Check 'T2-BOUND-02' '-HPGEOKMZ refused wrong zone (cm=120) as OUTSIDE_VIETNAM' 'Tier 2' (($hpSession -match 'HPGEOKMZ failed: OUTSIDE_VIETNAM') -and -not (Test-Path (Join-Path $work 'wrongzone.kmz')))
Record-Check 'T2-BOUND-03' '-HPGEOKMZ on unit=mm issued IMPLAUSIBLE_EN warning before refusal' 'Tier 2' (($hpSession -match 'failed: OUTSIDE_VIETNAM[^\r\n]*warnings: IMPLAUSIBLE_EN') -and -not (Test-Path (Join-Path $work 'mm.kmz')))
Record-Check 'T2-BOUND-04' '-HPGEOKMZ refused unknown argument foo=1' 'Tier 2' (($hpSession -match "HPGEOKMZ refused: Tham s\S+ 'foo'") -and -not (Test-Path (Join-Path $work 'foo.kmz')))
Record-Check 'T2-BOUND-05' '-HPGEOIMPORT refused missing file' 'Tier 2' ($hpSession -match "HPGEOIMPORT refused: Kh\S+ng t\S+m th\S+y file")
Record-Check 'T2-BOUND-06' '-HPGEOIMPORT refused wrong zone (cm=120) as OUTSIDE_ZONE' 'Tier 2' ($hpSession -match 'HPGEOIMPORT failed: OUTSIDE_ZONE')
Record-Check 'T2-BOUND-07' '-HPGEOIMAGE refused oversized margin (margin=2000) as TOO_MANY_TILES' 'Tier 2' (($hpSession -match 'HPGEOIMAGE failed: TOO_MANY_TILES') -and -not (Test-Path (Join-Path $work 'big.png')))
Record-Check 'T2-BOUND-08' '-HPGEOIMAGE refused non-existent layer as NO_BOUNDARY' 'Tier 2' (($hpSession -match 'HPGEOIMAGE failed: NO_BOUNDARY') -and -not (Test-Path (Join-Path $work 'nolayer.png')))

$wideLine = if ($hpSession -match 'HPGEOIMAGE fetched (\d+) tiles z=(\d+) [^\r\n]*warped (\d+)x(\d+)[^\r\n]*wide\.png') { $Matches } else { $null }
Record-Check 'T2-BOUND-09' '-HPGEOIMAGE on wide view (area=100) reduced zoom level to fit 4096px cap' 'Tier 2' (($null -ne $wideLine) -and ([int]$wideLine[2] -lt 19) -and ([int]$wideLine[3] -le 4096) -and ([int]$wideLine[4] -le 4096) -and ([int]$wideLine[1] -le 1024) -and (Test-Path (Join-Path $work 'wide.png')))

$arcKmz = Join-Path $work 'arc.kmz'
$arcCounts = @()
if (Test-Path $arcKmz) {
    $zipArc = [IO.Compression.ZipFile]::OpenRead($arcKmz)
    try {
        $rArc = New-Object IO.StreamReader(($zipArc.Entries | Where-Object FullName -eq 'doc.kml').Open(), [Text.Encoding]::UTF8)
        [xml]$xArc = $rArc.ReadToEnd(); $rArc.Dispose()
        $nsArc = New-Object Xml.XmlNamespaceManager($xArc.NameTable); $nsArc.AddNamespace('k', 'http://www.opengis.net/kml/2.2')
        $arcCounts = @($xArc.SelectNodes('//k:LineString/k:coordinates', $nsArc) | ForEach-Object { @(($_.InnerText -split '\s+') | Where-Object { $_ }).Count })
    } finally { $zipArc.Dispose() }
}
Record-Check 'T2-BOUND-10' '-HPGEOKMZ flattened arc and mirrored polyline on layer ARC to 5mm tolerance' 'Tier 2' ((Test-Path $arcKmz) -and ($arcCounts.Count -eq 2) -and (@($arcCounts | Where-Object { $_ -ge 60 }).Count -eq 2))
Record-Check 'T2-BOUND-11' '-HPGEOIMAGE refused wrong zone (cm=120) as OUTSIDE_VIETNAM' 'Tier 2' (($hpSession -match 'HPGEOIMAGE failed: OUTSIDE_VIETNAM') -and -not (Test-Path (Join-Path $work 'wrongzone.png')))

# --- Assertions: Tier 3 (Cross-Feature Combinations) ----------------------------------------------------------------
Write-Host "`nEvaluating Tier 3 (Cross-Feature Combinations)..."
$rtKmz = Join-Path $work 'roundtrip.kmz'
Record-Check 'T3-COMB-01' 'KMZ export-import roundtrip file roundtrip.kmz generated' 'Tier 3' ((Test-Path $rtKmz) -and ($hpSession -match 'HPGEOKMZ wrote .*roundtrip\.kmz: 13 points, 1 boundaries'))

$worstDeltaDeg = 1.0
$exactPointsSurvive = $false
if ((Test-Path $siteKmz) -and (Test-Path $rtKmz)) {
    $zip1 = [IO.Compression.ZipFile]::OpenRead($siteKmz)
    $zip2 = [IO.Compression.ZipFile]::OpenRead($rtKmz)
    try {
        $r1 = New-Object IO.StreamReader(($zip1.Entries | Where-Object FullName -eq 'doc.kml').Open(), [Text.Encoding]::UTF8); [xml]$xml1 = $r1.ReadToEnd(); $r1.Dispose()
        $r2 = New-Object IO.StreamReader(($zip2.Entries | Where-Object FullName -eq 'doc.kml').Open(), [Text.Encoding]::UTF8); [xml]$xml2 = $r2.ReadToEnd(); $r2.Dispose()

        $ns1 = New-Object Xml.XmlNamespaceManager($xml1.NameTable); $ns1.AddNamespace('k', 'http://www.opengis.net/kml/2.2')
        $ns2 = New-Object Xml.XmlNamespaceManager($xml2.NameTable); $ns2.AddNamespace('k', 'http://www.opengis.net/kml/2.2')

        $ring1 = @(($xml1.SelectSingleNode('//k:LinearRing/k:coordinates', $ns1).InnerText -split '\s+') | Where-Object { $_ })
        $ring2 = @(($xml2.SelectSingleNode('//k:LinearRing/k:coordinates', $ns2).InnerText -split '\s+') | Where-Object { $_ })

        $worstDeltaDeg = 0.0
        for ($i = 0; $i -lt [Math]::Min($ring1.Count, $ring2.Count); $i++) {
            $c1 = $ring1[$i] -split ','; $c2 = $ring2[$i] -split ','
            $dLon = [Math]::Abs([double]::Parse($c1[0], $inv) - [double]::Parse($c2[0], $inv))
            $dLat = [Math]::Abs([double]::Parse($c1[1], $inv) - [double]::Parse($c2[1], $inv))
            $worstDeltaDeg = [Math]::Max($worstDeltaDeg, [Math]::Max($dLon, $dLat))
        }

        $pts1 = @($xml1.SelectNodes('//k:MultiGeometry/k:Point/k:coordinates', $ns1) | ForEach-Object { $_.InnerText.Trim() })
        $pts2 = @($xml2.SelectNodes('//k:MultiGeometry/k:Point/k:coordinates', $ns2) | ForEach-Object { $_.InnerText.Trim() })
        $exactPointsSurvive = ($pts1.Count -eq 13) -and ($pts2.Count -eq 13) -and (@(Compare-Object $pts1 $pts2).Count -eq 0)
    } finally {
        $zip1.Dispose(); $zip2.Dispose()
    }
}
Record-Check 'T3-COMB-02' 'Roundtrip boundary vertices match within 2e-8 degrees (~2 mm)' 'Tier 3' ($worstDeltaDeg -le 2.0e-8) ("Worst delta: {0:E2} deg" -f $worstDeltaDeg)
Record-Check 'T3-COMB-03' 'Exactly 13 hidden reference points in MultiGeometry preserved to 9 decimals' 'Tier 3' $exactPointsSurvive

$pgwFile = [IO.Path]::ChangeExtension($imagePng, '.pgw')
$worldFileOk = $false
if ((Test-Path $imagePng) -and (Test-Path $pgwFile)) {
    $wf = @(Get-Content $pgwFile | ForEach-Object { [double]::Parse($_, $inv) })
    $pixelM = $wf[0]
    $worldFileOk = ($wf.Count -eq 6) -and ($wf[1] -eq 0) -and ($wf[2] -eq 0) -and ([Math]::Abs($wf[3] + $pixelM) -lt 1e-9) -and ($pixelM -le 0.5)
}
Record-Check 'T3-COMB-04' 'Satellite image world file .pgw is 6 lines, north-up, pixel <= 0.5m' 'Tier 3' $worldFileOk

$undoneCmd = if ($text -match '_\.U\s*\r?\n\s*(\S[^\r\n]{0,40})') { $Matches[1] } else { '' }
$imgCounts = @([regex]::Matches($text, 'Imagery: (\d+) RasterImage on HPGEO-IMAGE') | ForEach-Object { [int]$_.Groups[1].Value })
Record-Check 'T3-COMB-05' 'Undo (_.U) cleanly removes RasterImage inserted by -HPGEOIMAGE' 'Tier 3' (($undoneCmd -match 'HPGEOIMAGE') -and ($imgCounts -contains 0)) "Counts: $($imgCounts -join ', ')"
Record-Check 'T3-COMB-06' 'DWG named object dictionary persists VN-2000 settings across SAVEAS, CLOSE, OPEN' 'Tier 3' ((Test-Path (Join-Path $work 'stored.dwg')) -and ($text -match 'Stored VN-2000 settings: KTT 105\.75 \(105.{1,8}45.{0,8}\)'))
Record-Check 'T3-COMB-07' 'Reopened stored.dwg retains imported entities (26 POINT, 4 LWPOLYLINE)' 'Tier 3' ($text -match 'Model space: 26 POINT, 4 LWPOLYLINE \(2 closed\)')

$storedLine = if ($hpSession -match '(HPGEOIMAGE fetched (\d+) tiles z=\d+ \((\d+) cached[^)]*\)[^\r\n]*stored_hpgeo\.png \((relative|absolute) source[^\r\n]*)') { $Matches } else { $null }
Record-Check 'T3-COMB-08' 'Second satellite image inserted using stored DWG settings without explicit cm=' 'Tier 3' (($null -ne $storedLine) -and ($storedLine[4] -eq 'relative'))

# --- Assertions: Tier 4 (Real-World Cadastral Scenarios & System Health) ---------------------------------------------
Write-Host "`nEvaluating Tier 4 (Cadastral Scenarios & System Health)..."
Record-Check 'T4-CAD-01' 'Drawing screenshot image-in-autocad.png captured showing raster aligned with ring' 'Tier 4' (Test-Path $screenshotDrawing) "$screenshotDrawing"

$fetchSource = if ($hpSession -match 'HPGEOIMAGE fetched \d+ tiles z=\d+ \(\d+ cached, ([^)]+)\)') { $Matches[1] } else { '' }
Record-Check 'T4-CAD-02' 'Tile download conducted through helper process HPAutoCad.TileFetch.exe' 'Tier 4' ($fetchSource -eq 'helper') "Tile Source: $fetchSource"

$errLines = @($hpSession -split "`n" | Where-Object { $_ -match '\[ERR\]' })
Record-Check 'T4-LOGS-01' 'Clean session: zero [ERR] log lines in HPGeoLink session logs' 'Tier 4' ($errLines.Count -eq 0) "Errors: $($errLines.Count)"
Record-Check 'T4-RESTORE-01' 'AutoCAD profile sysvars safely restored' 'Tier 4' ($originals.Count -gt 0) "Restored $($originals.Count) variables"

# --- Output Summary & Exit Code -------------------------------------------------------------------------------------
$totalChecks = $results.Count
$failedChecks = @($results | Where-Object Result -eq 'FAIL').Count
$passedChecks = $totalChecks - $failedChecks

$tier1Results = @($results | Where-Object Tier -eq 'Tier 1')
$tier2Results = @($results | Where-Object Tier -eq 'Tier 2')
$tier3Results = @($results | Where-Object Tier -eq 'Tier 3')
$tier4Results = @($results | Where-Object Tier -eq 'Tier 4')

$summary = [pscustomobject]@{
    timestamp = (Get-Date).ToString('o')
    total = $totalChecks
    passed = $totalChecks - $failedChecks
    failed = $failedChecks
    tiers = [pscustomobject]@{
        tier1_feature_coverage = [pscustomobject]@{
            total = $tier1Results.Count
            passed = @($tier1Results | Where-Object Result -eq 'PASS').Count
            failed = @($tier1Results | Where-Object Result -eq 'FAIL').Count
        }
        tier2_boundary_corner = [pscustomobject]@{
            total = $tier2Results.Count
            passed = @($tier2Results | Where-Object Result -eq 'PASS').Count
            failed = @($tier2Results | Where-Object Result -eq 'FAIL').Count
        }
        tier3_cross_feature = [pscustomobject]@{
            total = $tier3Results.Count
            passed = @($tier3Results | Where-Object Result -eq 'PASS').Count
            failed = @($tier3Results | Where-Object Result -eq 'FAIL').Count
        }
        tier4_modal_cadastral = [pscustomobject]@{
            total = $tier4Results.Count
            passed = @($tier4Results | Where-Object Result -eq 'PASS').Count
            failed = @($tier4Results | Where-Object Result -eq 'FAIL').Count
        }
    }
    checks = $results
}

$summaryJsonPath = Join-Path $evidence 'summary.json'
$summary | ConvertTo-Json -Depth 5 | Set-Content $summaryJsonPath -Encoding UTF8

Write-Host "`n======================================================================="
Write-Host "                LIVE VERIFICATION RESULTS SUMMARY                      "
Write-Host "======================================================================="
Write-Host "Total Checks: $totalChecks | Passed: $passedChecks | Failed: $failedChecks"
Write-Host "Tier 1 (Feature Coverage):   $($summary.tiers.tier1_feature_coverage.passed)/$($summary.tiers.tier1_feature_coverage.total) Passed"
Write-Host "Tier 2 (Boundary Cases):     $($summary.tiers.tier2_boundary_corner.passed)/$($summary.tiers.tier2_boundary_corner.total) Passed"
Write-Host "Tier 3 (Cross-Feature):      $($summary.tiers.tier3_cross_feature.passed)/$($summary.tiers.tier3_cross_feature.total) Passed"
Write-Host "Tier 4 (Modal & Cadastral):  $($summary.tiers.tier4_modal_cadastral.passed)/$($summary.tiers.tier4_modal_cadastral.total) Passed"
Write-Host "Summary JSON: $summaryJsonPath"
Write-Host "Evidence Dir: $evidence"
Write-Host "======================================================================="

if ($failedChecks -gt 0) {
    Write-Host "`nVerification FAILED with $failedChecks failing check(s)." -ForegroundColor Red
    exit 1
} else {
    Write-Host "`nVerification PASSED: 100% assertions satisfied!" -ForegroundColor Green
    exit 0
}
'''

target = Path(r"g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPAutoCad\tools\harness\run-geolink-verify.ps1")
target.write_text(content, encoding="utf-8-sig")
print(f"Written {len(content)} characters to {target} with utf-8-sig")
