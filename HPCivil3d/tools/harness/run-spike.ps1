# Civil 3D bridge spike, unattended (Windows PowerShell 5.1 - UI Automation and COM need the desktop CLR):
#   S-01  build + deploy the bundle, start Civil 3D 2026 with the Autodesk shortcut's arguments, wait for the pipe,
#         read the self-check line, tick the opt-in through UIA
#   spike.py  ctx/align/w1/w2/surf/parcel/corr/busy over stdio (one server session), then disabled and nodoc
#   S-02  plain AutoCAD 2026 must NOT load the Civil bundle;  S-02b  Advance Steel 2026 must not either
#   S-03  AutoCAD 2026 and Civil 3D 2026 running at once serve two pipes (needs the HPAutoCad bundle deployed)
# Closes drawings without saving and kills only the acad.exe processes it started. Answers SECURELOAD itself.
param(
    [switch]$SkipBuild,
    [switch]$SkipIsolation,
    [switch]$SkipCoexist,
    [int]$StartupTimeoutSec = 420,
    [string]$Only = ''          # comma list passed to spike.py --only (default: its full main list)
)
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'harness-common.ps1')
Assert-NoAutocadRunning

$root = (Resolve-Path (Join-Path $PSScriptRoot '..\..\..')).Path
$hp = Join-Path $root 'HPCivil3d'
$out = Join-Path $hp 'output\live-verify'
$scene = Join-Path $out 'scene'
$registry = Join-Path $out 'registry'
$exe = Join-Path $hp 'HPCivil3d.Mcp.Server\bin\Debug\net10.0\HPCivil3d.Mcp.Server.exe'
$mcpCall = Join-Path $root 'McpShared\tools\mcp-call.py'
$logDir = Join-Path $env:LOCALAPPDATA 'HPCivil3d\McpBridge\logs'
$loaderLog = Join-Path $logDir 'loader.log'
$tutorials = 'C:\Program Files\Autodesk\AutoCAD 2026\C3D\Help\Civil Tutorials\Drawings'
$results = @()
function Check([string]$name, [bool]$ok, [string]$detail = '') {
    $script:results += [pscustomobject]@{ name = $name; ok = $ok; detail = $detail }
    "{0} {1}{2}" -f $(if ($ok) { 'PASS' } else { 'FAIL' }), $name, $(if ($detail) { " -- $detail" } else { '' })
}
function Loader-Lines { if (Test-Path $loaderLog) { @(Get-Content $loaderLog).Count } else { 0 } }
function Runtime-Log {
    # only what the bridge wrote since this run started acad.exe - an older session's self-check line must not count
    $f = Get-ChildItem (Join-Path $logDir 'mcpbridge-*.log') -ErrorAction SilentlyContinue | Sort-Object LastWriteTime | Select-Object -Last 1
    if (-not $f) { return '' }
    $since = if ($script:runStartedAt) { $script:runStartedAt.ToString('yyyy-MM-dd HH:mm:ss') } else { '' }
    (Get-Content $f.FullName | Where-Object { $_.Length -ge 19 -and $_.Substring(0, 19) -ge $since }) -join "`n"
}

New-Item -ItemType Directory -Force $scene, $registry, (Join-Path $registry 'tools-library') | Out-Null
Get-ChildItem $scene -Force -Include '*.dwl', '*.dwl2' -Recurse -ErrorAction SilentlyContinue | Remove-Item -Force -ErrorAction SilentlyContinue   # lock files are hidden
foreach ($f in 'Align-7C.dwg', 'Profile-1.dwg', 'Profile-5F.dwg', 'Surface-1B.dwg', 'Corridor-1a.dwg', 'Corridor-2b.dwg', 'Corridor-5c.dwg', 'Parcel-1E.dwg', 'Parcel-2C.dwg', 'Points-1a.dwg', 'Pipe Networks-1C.dwg', 'Pipe Networks-3C.dwg') {
    $src = Join-Path $tutorials $f
    if (Test-Path $src) { Copy-Item $src (Join-Path $scene $f) -Force } else { "scene: $f missing in the tutorials folder" }
}

if (-not $SkipBuild) {
    "=== build + deploy bundle"
    & dotnet build (Join-Path $hp 'HPCivil3d.slnx') -c Debug -nologo -v q
    if ($LASTEXITCODE -ne 0) { throw 'build failed' }
}
Check 'bundle deployed' (Test-Path "$env:APPDATA\Autodesk\ApplicationPlugins\HPCivil3d.McpBridge.bundle\Contents\HPCivil3d.McpBridge.Loader.dll") ''
Check 'server exe built' (Test-Path $exe) $exe

# server isolation (inherited by every server the harness spawns)
$env:HPCIVIL3D_MCP_Registry__LibraryPath = Join-Path $registry 'tools-library'
$env:HPCIVIL3D_MCP_Registry__DbPath = Join-Path $registry 'registry.db'
$scr = Join-Path $out 'spike.scr'
"HPC3DMCPBRIDGE`r`nHPC3DMCPSTART`r`n" | Set-Content -Encoding ASCII $scr

$civil = $null; $acad = $null
try {
    "=== S-01: Civil 3D 2026 loads the bundle"
    $linesBefore = Loader-Lines
    $script:runStartedAt = Get-Date
    $civil = Start-AcadWithBridge $scr $StartupTimeoutSec -Product C3D -ProfileName '<<C3D_Metric>>' -PipeName 'hpcivil3d-mcp-2026'
    Check 'S-01 pipe hpcivil3d-mcp-2026 up' $script:pipeUp ''
    Start-Sleep -Seconds 3
    $rt = Runtime-Log
    Check 'S-01 self-check OK in the runtime log' ($rt -match 'MCP scripting self-check OK') ''
    Check 'S-03b Roslyn in load context HPCivil3d.McpBridge' ($rt -match "in load context 'HPCivil3d.McpBridge'") ''
    Check 'S-01 self-check probe says product Civil3D' ($rt -match 'civil Civil3D') ''
    Check 'S-01 loader.log grew' ((Loader-Lines) -gt $linesBefore) "$linesBefore -> $(Loader-Lines)"
    $rtHead = ($rt -split "`n" | Select-Object -First 6) -join ' | '
    "runtime log head: $rtHead"

    if ($script:pipeUp) {
        $on = Set-OptIn $true
        Check 'opt-in ticked (UIA)' $on ''
        "=== spike.py"
        $onlyArgs = @(); if ($Only) { $onlyArgs = @('--only', $Only) }
        & python (Join-Path $PSScriptRoot 'spike.py') --exe $exe --scene-dir $scene --out $out @onlyArgs
        Check 'spike.py main steps exit 0' ($LASTEXITCODE -eq 0) "exit $LASTEXITCODE"
        $off = Set-OptIn $false
        Check 'opt-in unticked (UIA)' $off ''
        & python (Join-Path $PSScriptRoot 'spike.py') --exe $exe --scene-dir $scene --out $out --only disabled
        Check 'spike.py disabled exit 0' ($LASTEXITCODE -eq 0) "exit $LASTEXITCODE"
        Set-OptIn $true | Out-Null
        & python (Join-Path $PSScriptRoot 'spike.py') --exe $exe --scene-dir $scene --out $out --only nodoc
        Check 'spike.py nodoc exit 0' ($LASTEXITCODE -eq 0) "exit $LASTEXITCODE"
    }
    Stop-Acad $civil; $civil = $null

    if (-not $SkipIsolation) {
        "=== S-02: plain AutoCAD 2026 must not load the Civil bundle"
        $lines = Loader-Lines
        $acad = Start-AcadWithBridge '' 5 -Product ACAD -ProfileName '' -PipeName ''
        $sw = [Diagnostics.Stopwatch]::StartNew()
        while ($sw.Elapsed.TotalSeconds -lt 150) { Answer-SecureLoad | Out-Null; $acad.Refresh(); if ($acad.MainWindowHandle -ne 0 -and $sw.Elapsed.TotalSeconds -gt 90) { break }; if ($acad.HasExited) { break }; Start-Sleep -Seconds 5 }
        $acad.Refresh()
        Check 'S-02 AutoCAD started (window)' ($acad.MainWindowHandle -ne 0) "after $([int]$sw.Elapsed.TotalSeconds) s"
        Check 'S-02 AutoCAD never wrote to the Civil loader.log' ((Loader-Lines) -eq $lines) "$lines -> $(Loader-Lines)"
        Check 'S-02 no hpcivil3d pipe from AutoCAD' (-not (Test-Path '\\.\pipe\hpcivil3d-mcp-2026')) ''
        Stop-Acad $acad; $acad = $null

        "=== S-02b: Advance Steel 2026 must not load the Civil bundle"
        $lines = Loader-Lines
        $acad = Start-AcadWithBridge '' 5 -Product ADVS -ProfileName '<<ADVS>>' -PipeName '' -NoAecBase
        $sw = [Diagnostics.Stopwatch]::StartNew()
        while ($sw.Elapsed.TotalSeconds -lt 240) { Answer-SecureLoad | Out-Null; $acad.Refresh(); if ($acad.MainWindowHandle -ne 0 -and $sw.Elapsed.TotalSeconds -gt 120) { break }; if ($acad.HasExited) { break }; Start-Sleep -Seconds 5 }
        $acad.Refresh()
        Check 'S-02b Advance Steel started (window)' ($acad.MainWindowHandle -ne 0) "after $([int]$sw.Elapsed.TotalSeconds) s"
        Check 'S-02b Advance Steel never wrote to the Civil loader.log' ((Loader-Lines) -eq $lines) "$lines -> $(Loader-Lines)"
        Check 'S-02b no hpcivil3d pipe from Advance Steel' (-not (Test-Path '\\.\pipe\hpcivil3d-mcp-2026')) ''
        Stop-Acad $acad; $acad = $null
    }

    if (-not $SkipCoexist) {
        "=== S-03: AutoCAD 2026 + Civil 3D 2026 at once, two pipes"
        $acadExe = Join-Path $root 'HPAutoCad\HPAutoCad.Mcp.Server\bin\Debug\net10.0\HPAutoCad.Mcp.Server.exe'
        $acadScr = Join-Path $out 'acad-bridge.scr'
        "HPMCPBRIDGE`r`nHPMCPSTART`r`n" | Set-Content -Encoding ASCII $acadScr
        if (-not (Test-Path $acadExe) -or -not (Test-Path "$env:APPDATA\Autodesk\ApplicationPlugins\HPAutoCad.McpBridge.bundle\PackageContents.xml")) {
            Check 'S-03 skipped: HPAutoCad exe or bundle missing' $true 'SKIP'
        } else {
            $civil = Start-AcadWithBridge $scr $StartupTimeoutSec -Product C3D -ProfileName '<<C3D_Metric>>' -PipeName 'hpcivil3d-mcp-2026'
            $civilUp = $script:pipeUp; $civilPid = $civil.Id
            $acad = Start-AcadWithBridge $acadScr $StartupTimeoutSec -Product ACAD -ProfileName '' -PipeName 'hpautocad-mcp-2026'
            $acadUp = $script:pipeUp
            Check 'S-03 both pipes up' ($civilUp -and $acadUp -and (Test-Path '\\.\pipe\hpcivil3d-mcp-2026') -and (Test-Path '\\.\pipe\hpautocad-mcp-2026')) "civil=$civilUp acad=$acadUp"
            $c = & python $mcpCall $exe tools/call get_civil3d_context '{}' --env "HPCIVIL3D_MCP_Registry__LibraryPath=$($env:HPCIVIL3D_MCP_Registry__LibraryPath)" --env "HPCIVIL3D_MCP_Registry__DbPath=$($env:HPCIVIL3D_MCP_Registry__DbPath)" | ConvertFrom-Json
            $ct = $c.result.content[0].text | ConvertFrom-Json
            $a = & python $mcpCall $acadExe tools/call get_autocad_context '{}' --env "HPAUTOCAD_MCP_Registry__LibraryPath=$(Join-Path $registry 'acad-tools-library')" --env "HPAUTOCAD_MCP_Registry__DbPath=$(Join-Path $registry 'acad-registry.db')" | ConvertFrom-Json
            $at = $a.result.content[0].text | ConvertFrom-Json
            Check 'S-03 civil3d context served (host=civil3d, product Civil3D)' ($ct.host -eq 'civil3d' -and $ct.civil3d.product -eq 'Civil3D') (($ct | ConvertTo-Json -Compress).Substring(0, [Math]::Min(160, ($ct | ConvertTo-Json -Compress).Length)))
            Check 'S-03 autocad context served (host=autocad)' ($at.host -eq 'autocad') (($at | ConvertTo-Json -Compress).Substring(0, [Math]::Min(120, ($at | ConvertTo-Json -Compress).Length)))
            $rt = Runtime-Log
            Check 'S-03 Civil bridge never reported its pipe in use' ($rt -notmatch 'could not create pipe') ''
            # two acad.exe are alive here, so the pid-guarded COM quit refuses and the AutoCAD instance is killed
            # (Drawing Recovery entry for it only); the Civil instance, which held the drawings, then quits gracefully
            Stop-Acad $acad; $acad = $null
            Stop-Acad $civil; $civil = $null
        }
    }
}
catch {
    Check 'wrapper aborted' $false $_.Exception.Message
}
finally {
    "=== killing what we started"
    Stop-Acad $civil; Stop-Acad $acad
    Get-ChildItem $scene -Force -Include '*.dwl', '*.dwl2' -Recurse -ErrorAction SilentlyContinue | Remove-Item -Force -ErrorAction SilentlyContinue   # lock files are hidden
    Remove-Item Env:HPCIVIL3D_MCP_Registry__LibraryPath, Env:HPCIVIL3D_MCP_Registry__DbPath, Env:HP_HARNESS_ACAD_PID -ErrorAction SilentlyContinue
}

""
"=== summary"
$results | Format-Table -AutoSize | Out-String -Width 220
$fails = @($results | Where-Object { -not $_.ok }).Count
$results | ConvertTo-Json -Depth 4 | Set-Content -Encoding UTF8 (Join-Path $out 'spike-wrapper-summary.json')
"PowerShell checks: $($results.Count) total, $fails failed"
exit $fails
