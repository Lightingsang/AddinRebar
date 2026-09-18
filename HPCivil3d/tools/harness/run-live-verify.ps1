# Live verification of the whole Civil 3D MCP loop, unattended: starts Civil 3D 2026 with the bridge, copies the tutorial
# drawings it needs into a scene folder, then drives the published HPCivil3d.Mcp.Server.exe over one stdio session
# (live-verify.py) through the start-up checks, the execute matrix, every seed, the MISS -> propose -> approve -> HIT loop
# and the quarantine -> restore loop, with the AutoCAD exe checked beside it; optionally proves isolation both ways
# (a second Civil 3D fails fast on the pipe, plain AutoCAD and Advance Steel never load the Civil bundle while AutoCAD
# still loads its own, both products serve their pipes at once, and the AutoCAD harness's own isolation step still holds).
# Kills only the acad.exe processes it started (tracked by pid, graceful COM quit first). The server and the CLI run on an
# isolated registry root under $OutDir (Registry:LibraryPath/DbPath overrides) so the user's %AppData% registry is never
# written; the drawings are copies and are never saved. Publish the exes first (see HPCivil3d/README.md).
param(
    [string]$Exe = (Join-Path $PSScriptRoot '..\..\output\HPCivil3d.Mcp.Server\HPCivil3d.Mcp.Server.exe'),
    [string]$AutocadExe = (Join-Path $PSScriptRoot '..\..\..\HPAutoCad\output\HPAutoCad.Mcp.Server\HPAutoCad.Mcp.Server.exe'),
    [string]$OutDir = (Join-Path $PSScriptRoot '..\..\output\live-verify'),
    [int]$StartupTimeoutSec = 420,
    [int]$Runs = 1,
    [switch]$IncludeIsolation,
    [switch]$OnlyIsolation,
    [switch]$SkipAutocad,
    [switch]$UseLiveRegistry   # write to %AppData%\HPCivil3d\McpServer instead of the isolated root under $OutDir
)

. (Join-Path $PSScriptRoot 'harness-common.ps1')
Assert-NoAutocadRunning
if (-not (Test-Path $Exe)) { throw "Server exe not found: $Exe" }
$Exe = (Resolve-Path $Exe).Path
$autocad = if (-not $SkipAutocad -and (Test-Path $AutocadExe)) { (Resolve-Path $AutocadExe).Path } else { '' }
if (-not $autocad) { Write-Host 'AutoCAD exe not found or skipped: scenario X and the coexistence step will not run' }
New-Item -ItemType Directory -Force $OutDir, (Join-Path $OutDir 'scene') | Out-Null
$OutDir = (Resolve-Path $OutDir).Path
$scene = Join-Path $OutDir 'scene'
$py = Join-Path $PSScriptRoot 'live-verify.py'
$mcp = Join-Path $PSScriptRoot '..\..\..\McpShared\tools\mcp-call.py'   # shared stdio helper (McpShared/tools)
$env:PYTHONIOENCODING = 'utf-8'
$acadDir = 'C:\Program Files\Autodesk\AutoCAD 2026'
$logDir = "$env:LOCALAPPDATA\HPCivil3d\McpBridge\logs"
$loaderLog = Join-Path $logDir 'loader.log'
$summaries = @()

# the scene: copies of the tutorial drawings (Program Files is never opened in place); stale locks would make an open read-only
Get-ChildItem $scene -Force -Include '*.dwl', '*.dwl2' -Recurse -ErrorAction SilentlyContinue | Remove-Item -Force -ErrorAction SilentlyContinue
$tutorials = "$acadDir\C3D\Help\Civil Tutorials\Drawings"
foreach ($f in 'Profile-5F.dwg', 'Corridor-1a.dwg') {
    $src = Join-Path $tutorials $f
    if (Test-Path $src) { Copy-Item $src (Join-Path $scene $f) -Force } else { Write-Host "scene: $f missing in the tutorials folder" }
}

function Run([string]$only, [string[]]$extra) {
    $argv = @('--exe', $Exe, '--scene-dir', $scene, '--out', $OutDir) + $extra
    if ($only) { $argv += @('--only', $only) }
    python $py @argv
    $script:summaries += [pscustomobject]@{ step = $(if ($only) { $only } else { 'main' }); exit = $LASTEXITCODE }
}
function Check([string]$name, [bool]$ok, [string]$detail) {
    Write-Host ("{0} {1} {2}" -f $(if ($ok) { 'PASS' } else { 'FAIL' }), $name, $detail)
    $script:summaries += [pscustomobject]@{ step = $name; exit = $(if ($ok) { 0 } else { 1 }) }
}
function Skip([string]$name, [string]$why) {
    Write-Host "SKIP $name -- $why"
    $script:summaries += [pscustomobject]@{ step = "SKIP $name"; exit = 0 }
}
function Loader-Lines { if (Test-Path $loaderLog) { @(Get-Content $loaderLog).Count } else { 0 } }
function Pipe-Up([string]$name) { Test-PipeUp $name }
function Read-StatusDetail([int]$processId) {
    # The bridge window's StatusDetail TextBlock (AutomationId) — where a listener fault is shown to the user.
    $root = [System.Windows.Automation.AutomationElement]::RootElement
    $byPid = New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::ProcessIdProperty, $processId)
    $byId = New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::AutomationIdProperty, 'StatusDetail')
    foreach ($w in @($root.FindAll([System.Windows.Automation.TreeScope]::Children, $byPid))) {
        $el = $w.FindFirst([System.Windows.Automation.TreeScope]::Descendants, $byId)
        if ($el) { return $el.Current.Name }
    }
    return $null
}
function Registry-Reset([switch]$KeepExisting) {
    if ($UseLiveRegistry) { return }
    if ($KeepExisting -and $env:HPCIVIL3D_MCP_Registry__DbPath -and (Test-Path $env:HPCIVIL3D_MCP_Registry__DbPath)) { return }   # the isolation steps reuse the last run's root: its tools are the evidence
    $registryRoot = Join-Path $OutDir 'registry'
    if (Test-Path $registryRoot) { Remove-Item $registryRoot -Recurse -Force }
    New-Item -ItemType Directory -Force $registryRoot | Out-Null
    $env:HPCIVIL3D_MCP_Registry__LibraryPath = Join-Path $registryRoot 'tools-library'
    $env:HPCIVIL3D_MCP_Registry__DbPath = Join-Path $registryRoot 'registry.db'
    Write-Host "registry root for this run: $registryRoot"
}
function Close-AllDrawings([int]$processId) {
    # Civil 3D answers RPC_E_CALL_REJECTED while it is still busy right after the main scenarios; retry a few times.
    powershell -NoProfile -Command "`$ids = @(Get-Process acad -ErrorAction SilentlyContinue | % Id); if (`$ids.Count -ne 1 -or [string]`$ids[0] -ne '$processId') { throw 'refusing COM' }; `$a = [Runtime.InteropServices.Marshal]::GetActiveObject('AutoCAD.Application'); for (`$i = 1; `$i -le 8; `$i++) { try { while (`$a.Documents.Count -gt 0) { `$a.Documents.Item(0).Close(`$false) }; break } catch { if (`$i -eq 8) { throw }; 'close rejected (attempt ' + `$i + '), retrying'; Start-Sleep -Seconds 4 } }; 'closed, docs left: ' + `$a.Documents.Count"
}

$civil = $null; $acad = $null; $second = $null
try {
    if ($OnlyIsolation) { $IncludeIsolation = $true }
    if (-not $OnlyIsolation) {
        for ($run = 1; $run -le $Runs; $run++) {
            "=== run $run of ${Runs}: start Civil 3D 2026 with the bridge"
            Registry-Reset
            $civil = Start-AcadWithBridge (Join-Path $PSScriptRoot 'bridge.scr') $StartupTimeoutSec
            if (-not $pipeUp) { throw 'bridge pipe never came up' }
            Start-Sleep -Seconds 3

            "=== opt-in OFF: execute must be refused"
            if (-not (Set-OptIn $false)) { Check 'opt-in unticked' $false '' }
            Run 'disabled' @()

            "=== opt-in ON: start-up, matrix, seeds, registry loops" + $(if ($autocad) { ', AutoCAD exe beside' } else { '' })
            if (-not (Set-OptIn $true)) { throw 'could not tick the opt-in' }
            $mainArgs = @('--reset-verify-tools')
            if ($autocad) { $mainArgs += @('--autocad-exe', $autocad) }
            Run '' $mainArgs
            if (-not $autocad) { Skip 'X AutoCAD exe beside' $(if ($SkipAutocad) { '-SkipAutocad' } else { "no published exe at $AutocadExe" }) }

            "=== no drawing: close every drawing through COM, expect a refusal"
            Close-AllDrawings $civil.Id
            Start-Sleep -Seconds 3
            Run 'nodoc' @()

            "=== busy: LINE waiting for input -> refused after the grace; ESC -> retry succeeds"
            powershell -NoProfile -Command "`$ids = @(Get-Process acad -ErrorAction SilentlyContinue | % Id); if (`$ids.Count -ne 1 -or [string]`$ids[0] -ne '$($civil.Id)') { throw 'refusing COM' }; `$a = [Runtime.InteropServices.Marshal]::GetActiveObject('AutoCAD.Application'); `$null = `$a.Documents.Add(); 'new drawing, docs: ' + `$a.Documents.Count"
            Start-Sleep -Seconds 3
            Run 'busy' @()

            if ($run -lt $Runs -or $IncludeIsolation) { Stop-Acad $civil; $civil = $null }
        }
    }

    if ($IncludeIsolation) {
        Registry-Reset -KeepExisting
        $acadIso = Join-Path $OutDir 'registry-autocad'
        New-Item -ItemType Directory -Force (Join-Path $acadIso 'tools-library') | Out-Null
        $acadEnv = @('--env', "HPAUTOCAD_MCP_Registry__LibraryPath=$acadIso\tools-library", '--env', "HPAUTOCAD_MCP_Registry__DbPath=$acadIso\registry.db")
        if (-not $civil) {
            "=== isolation: start Civil 3D 2026 with the bridge"
            $civil = Start-AcadWithBridge (Join-Path $PSScriptRoot 'bridge.scr') $StartupTimeoutSec
            if (-not $pipeUp) { throw 'bridge pipe never came up' }
            Start-Sleep -Seconds 3
        }

        "=== isolation 1: a second Civil 3D 2026 must fail fast on the pipe while the first keeps serving"
        $firstPid = $script:acadPid
        $second = Start-Process -FilePath "$acadDir\acad.exe" -ArgumentList @('/nologo', '/ld', "`"$acadDir\AecBase.dbx`"", '/p', '"<<C3D_Metric>>"', '/product', 'C3D', '/language', '"en-US"', '/b', "`"$(Join-Path $PSScriptRoot 'bridge.scr')`"") -PassThru
        $script:acadPid = $second.Id   # SECURELOAD answers go to the newcomer while it loads
        $detail = $null
        $sw = [Diagnostics.Stopwatch]::StartNew()
        while ($sw.Elapsed.TotalSeconds -lt 300 -and -not $detail) {
            Answer-SecureLoad | Out-Null
            Start-Sleep -Seconds 5
            try { $detail = Read-StatusDetail $second.Id } catch { $detail = $null }
            if ($detail -and $detail -notmatch 'in use') { $detail = $null }
        }
        $script:acadPid = $firstPid
        Check 'I second Civil 3D reports the pipe in use (status window)' ([bool]$detail) ("after $([int]$sw.Elapsed.TotalSeconds) s: '$detail'")
        $logText = (Get-ChildItem "$logDir\mcpbridge-*.log" | Sort-Object LastWriteTime | Select-Object -Last 1 | Get-Content -Tail 120) -join "`n"
        Check 'I second instance logged the pipe fault (shared log file)' ($logText -match 'could not create pipe') ''
        $ctx = python $mcp $Exe tools/call get_civil3d_context '{}' --env "HPCIVIL3D_MCP_Registry__LibraryPath=$env:HPCIVIL3D_MCP_Registry__LibraryPath" --env "HPCIVIL3D_MCP_Registry__DbPath=$env:HPCIVIL3D_MCP_Registry__DbPath" | ConvertFrom-Json
        $ctxText = $ctx.result.content[0].text | ConvertFrom-Json
        Check 'I first Civil 3D still serves during the second' ($ctxText.host -eq 'civil3d' -and -not $ctx.result.isError) ("host=$($ctxText.host) product=$($ctxText.civil3d.product)")
        # two acad.exe are alive, so the pid-guarded graceful quit refuses and the newcomer is killed (it holds no drawing of ours)
        if (-not $second.HasExited) { Stop-Process -Id $second.Id -Force -Confirm:$false }
        $second = $null
        Start-Sleep -Seconds 5

        "=== isolation 2: plain AutoCAD 2026 must not load the Civil bundle but must load its own (coexistence: two pipes)"
        Check 'I no AutoCAD pipe before AutoCAD starts (baseline)' (-not (Pipe-Up 'hpautocad-mcp-2026')) ''
        $linesBefore = Loader-Lines
        $acadScr = Join-Path $PSScriptRoot '..\..\..\HPAutoCad\tools\harness\bridge.scr'   # HPMCPBRIDGE + HPMCPSTART: the AutoCAD listener is opt-in per session too
        $acadArgs = @('/nologo', '/product', 'ACAD', '/language', '"en-US"')
        if (Test-Path $acadScr) { $acadArgs += @('/b', "`"$acadScr`"") }
        $acad = Start-Process -FilePath "$acadDir\acad.exe" -ArgumentList $acadArgs -PassThru
        $script:acadPid = $acad.Id
        $sw = [Diagnostics.Stopwatch]::StartNew()
        $acadPipe = $false
        while ($sw.Elapsed.TotalSeconds -lt 240 -and -not $acadPipe) { Answer-SecureLoad | Out-Null; Start-Sleep -Seconds 5; $acadPipe = Pipe-Up 'hpautocad-mcp-2026' }
        $script:acadPid = $firstPid
        Start-Sleep -Seconds 20
        Check 'I AutoCAD 2026 loaded its own bundle (pipe hpautocad-mcp-2026 up)' $acadPipe ("after $([int]$sw.Elapsed.TotalSeconds) s")
        Check 'I AutoCAD 2026 never wrote to the Civil loader.log' ((Loader-Lines) -eq $linesBefore) "$linesBefore -> $(Loader-Lines)"
        if ($autocad -and $acadPipe) {
            $ac = python $mcp $autocad tools/call get_autocad_context '{}' @acadEnv | ConvertFrom-Json
            $acText = $ac.result.content[0].text | ConvertFrom-Json
            $cv = python $mcp $Exe tools/call get_civil3d_context '{}' --env "HPCIVIL3D_MCP_Registry__LibraryPath=$env:HPCIVIL3D_MCP_Registry__LibraryPath" --env "HPCIVIL3D_MCP_Registry__DbPath=$env:HPCIVIL3D_MCP_Registry__DbPath" | ConvertFrom-Json
            $cvText = $cv.result.content[0].text | ConvertFrom-Json
            Check 'I coexistence: both servers answer from their own host' ($acText.host -eq 'autocad' -and -not $ac.result.isError -and $cvText.host -eq 'civil3d' -and -not $cv.result.isError -and $cvText.civil3d.product -eq 'Civil3D') ("autocad=$($acText.autocad.insunits) civil3d=$($cvText.civil3d.drawingUnit)")
        }
        else { Skip 'I coexistence: both servers answer from their own host' 'no AutoCAD exe published or its pipe never came up' }
        if ($acad -and -not $acad.HasExited) { Stop-Process -Id $acad.Id -Force -Confirm:$false }
        $acad = $null
        Start-Sleep -Seconds 5

        "=== isolation 3: Advance Steel 2026 must load neither bundle"
        $linesBefore = Loader-Lines
        $acad = Start-Process -FilePath "$acadDir\acad.exe" -ArgumentList @('/nologo', '/p', '"<<ADVS>>"', '/product', '"ADVS"', '/language', '"en-US"') -PassThru
        $script:acadPid = $acad.Id
        $sw = [Diagnostics.Stopwatch]::StartNew()
        while ($sw.Elapsed.TotalSeconds -lt 150) {
            Answer-SecureLoad | Out-Null
            Start-Sleep -Seconds 5
            $acad.Refresh()
            if ($acad.HasExited) { break }
            if ($sw.Elapsed.TotalSeconds -ge 120 -and $acad.MainWindowHandle -ne 0) { break }
        }
        $script:acadPid = $firstPid
        $acad.Refresh()
        Check 'I Advance Steel started, no Civil loader.log line, no AutoCAD pipe' (($acad.MainWindowHandle -ne 0) -and ((Loader-Lines) -eq $linesBefore) -and -not (Pipe-Up 'hpautocad-mcp-2026')) ("window=$($acad.MainWindowHandle -ne 0) loader $linesBefore -> $(Loader-Lines) after $([int]$sw.Elapsed.TotalSeconds) s")
        if ($acad -and -not $acad.HasExited) { Stop-Process -Id $acad.Id -Force -Confirm:$false }
        $acad = $null
        Start-Sleep -Seconds 5
        Stop-Acad $civil; $civil = $null

        "=== isolation 4: the AutoCAD harness's own isolation step still holds with the Civil bundle deployed"
        $acadHarness = Join-Path $PSScriptRoot '..\..\..\HPAutoCad\tools\harness\run-live-verify.ps1'
        $acadExePublished = Join-Path $PSScriptRoot '..\..\..\HPAutoCad\output\HPAutoCad.Mcp.Server\HPAutoCad.Mcp.Server.exe'
        if ((Test-Path $acadHarness) -and (Test-Path $acadExePublished)) {
            $log = Join-Path $OutDir 'autocad-only-isolation.log'
            pwsh -NoProfile -ExecutionPolicy Bypass -File $acadHarness -OnlyIsolation -SkipRevit *> $log
            $text = Get-Content $log -Raw
            $passed = ([regex]::Matches($text, '^PASS ', 'Multiline')).Count
            $failed = ([regex]::Matches($text, '^FAIL ', 'Multiline')).Count
            Check 'I AutoCAD harness -OnlyIsolation (second AutoCAD in use, Civil 3D never loads the AutoCAD bundle)' ($passed -ge 4 -and $failed -eq 0) "$passed PASS / $failed FAIL (log: $log)"
        }
        else { Skip 'I AutoCAD harness -OnlyIsolation' 'HPAutoCad/tools/harness/run-live-verify.ps1 or the published AutoCAD exe not found' }
    }
}
catch {
    Check 'wrapper aborted' $false $_.Exception.Message
}
finally {
    "=== stopping what we started"
    Stop-Acad $civil
    foreach ($proc in @($acad, $second)) { if ($proc -and -not $proc.HasExited) { Stop-Process -Id $proc.Id -Force -Confirm:$false -ErrorAction SilentlyContinue } }
    Get-ChildItem $scene -Force -Include '*.dwl', '*.dwl2' -Recurse -ErrorAction SilentlyContinue | Remove-Item -Force -ErrorAction SilentlyContinue
    Remove-Item Env:HPCIVIL3D_MCP_Registry__LibraryPath, Env:HPCIVIL3D_MCP_Registry__DbPath, Env:HP_HARNESS_ACAD_PID -ErrorAction SilentlyContinue
}

"=== pipes seen by this user"
[System.IO.Directory]::GetFiles('\\.\pipe\') | ForEach-Object { [System.IO.Path]::GetFileName($_) } | Where-Object { $_ -like 'hp*-mcp-*' }
"=== audit lines"
Get-ChildItem "$env:APPDATA\HPCivil3d\McpBridge\audit\*.log" -ErrorAction SilentlyContinue | ForEach-Object { "$($_.Name): $((Get-Content $_.FullName).Count) lines" }
$summaries | ConvertTo-Json | Set-Content (Join-Path $OutDir 'wrapper-summary.json')
$failedSteps = @($summaries | Where-Object exit -ne 0)
$skipped = @($summaries | Where-Object { $_.step -like 'SKIP *' })
"wrapper: $($summaries.Count) steps, $($failedSteps.Count) failed, $($skipped.Count) skipped" + $(if ($failedSteps) { ': ' + (($failedSteps | % step) -join ', ') } else { '' })
exit $(if ($failedSteps.Count -eq 0) { 0 } else { 1 })
