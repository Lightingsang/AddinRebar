<#
.SYNOPSIS
    Golden run: Column, Foundation and Beam Rebar with their default settings on a fresh copy of the fixture model, in a
    Revit 2026 this script starts and closes, with read-only MCP snapshots before and after each feature.
.DESCRIPTION
    1. refuses to start while any Revit runs (pipe, DLL lock, user models);
    2. builds Debug.R26 (deploys HPRebar + HPRebar.McpBridge) unless -SkipBuild;
    3. copies HPRebar.Tests/Fixtures/column-stack-2-storey.rvt to HPRebar/output/golden/<run>/work/fixture.rvt and opens it;
    4. answers the unsigned add-in prompts Load Once, opens the MCP bridge window, ticks the opt-in;
    5. snapshot all -> s0.json; per feature: select its marks (the command skips the pick), ribbon button, read every input
       (<f>.readback.json), press the run button, read + close the result dialog (<f>.dialog.txt), snapshot -> <f>.json;
    6. unticks the opt-in, closes Revit without saving, deletes the work copy, writes run-meta.json.
    Compare two runs with compare-golden.py. Windows PowerShell 5.1.
#>
param([string]$Tag = '', [switch]$SkipBuild, [switch]$KeepRevit, [string[]]$Features = @('column', 'foundation', 'beam'))
if ($PSVersionTable.PSEdition -ne 'Desktop') {
    $ps = Join-Path $env:SystemRoot 'System32\WindowsPowerShell\v1.0\powershell.exe'
    $argsList = @('-NoProfile', '-ExecutionPolicy', 'Bypass', '-File', $PSCommandPath)
    if ($Tag) { $argsList += @('-Tag', $Tag) }; if ($SkipBuild) { $argsList += '-SkipBuild' }; if ($KeepRevit) { $argsList += '-KeepRevit' }
    $argsList += @('-Features', ($Features -join ','))
    & $ps @argsList; exit $LASTEXITCODE
}
$ErrorActionPreference = 'Stop'
if ($Features.Count -eq 1 -and $Features[0] -match ',') { $Features = $Features[0].Split(',') }
$here = $PSScriptRoot
$repo = (Resolve-Path (Join-Path $here '..\..\..')).Path
. (Join-Path $here 'golden-ui.ps1')

if (Get-Process Revit -ErrorAction SilentlyContinue) { throw 'Close every Revit before a golden run (pipe, DLL lock, user models).' }

if (-not $SkipBuild) {
    & dotnet build (Join-Path $repo 'HPRebar\HPRebar.slnx') -c Debug.R26 -v quiet -nologo | Select-String -Pattern ' error |Build succeeded'
    if ($LASTEXITCODE -ne 0) { throw 'Debug.R26 build failed' }
}

$sha = (& git -C $repo rev-parse --short HEAD).Trim()
$dirty = [bool](& git -C $repo status --porcelain -- HPRebar McpShared)
$name = (Get-Date -Format 'yyMMdd-HHmm') + '-' + $sha + $(if ($dirty) { '-dirty' } else { '' }) + $(if ($Tag) { '-' + $Tag } else { '' })
$runDir = Join-Path $repo "HPRebar\output\golden\$name"
$work = Join-Path $runDir 'work'
New-Item -ItemType Directory -Force $work | Out-Null
$fixture = Join-Path $repo 'HPRebar\HPRebar.Tests\Fixtures\column-stack-2-storey.rvt'
$copy = Join-Path $work 'fixture.rvt'
Copy-Item $fixture $copy
$dll = Join-Path $env:APPDATA 'Autodesk\Revit\Addins\2026\HPRebar\HPRebar.dll'
$meta = [ordered]@{ run = $name; gitSha = $sha; sourceDirty = $dirty; startedAt = (Get-Date).ToString('s')
    fixtureSha256 = (Get-FileHash $fixture -Algorithm SHA256).Hash; hprebarDllSha256 = (Get-FileHash $dll -Algorithm SHA256).Hash; features = [ordered]@{} }
"run $name -> $runDir"

function Invoke-Mcp([string[]]$mcpArgs) {
    $out = & python (Join-Path $here 'golden-mcp.py') --run-dir $runDir @mcpArgs
    if ($LASTEXITCODE -ne 0) { throw "golden-mcp $($mcpArgs -join ' ') failed: $out" }
    return $out
}

$revit = Start-Process 'C:\Program Files\Autodesk\Revit 2026\Revit.exe' -ArgumentList ('"' + $copy + '"') -PassThru
$script:revitPid = $revit.Id
"Revit pid $($revit.Id) started $(Get-Date -Format HH:mm:ss)"
try {
    Answer-UnsignedAddins 150 | Out-Null
    $sw = [Diagnostics.Stopwatch]::StartNew()
    while ($sw.Elapsed.TotalSeconds -lt 240) { Start-Sleep 3; $revit.Refresh(); if ($revit.MainWindowTitle -match 'fixture') { break } }
    if ($revit.MainWindowTitle -notmatch 'fixture') { throw "fixture did not open: '$($revit.MainWindowTitle)'" }
    "Revit '$($revit.MainWindowTitle)' after $([int]$sw.Elapsed.TotalSeconds) s"
    $meta.revitVersion = (Get-Item $revit.Path).VersionInfo.ProductVersion
    Start-Sleep -Seconds 8

    Invoke-RibbonButton 'MCP Bridge' | Out-Null
    Start-Sleep -Seconds 4
    Set-BridgeOptIn $true
    $context = Invoke-Mcp @('context') | ConvertFrom-Json
    if ($context.docPath -ne $copy) { throw "active document is '$($context.docPath)', not the work copy" }
    Invoke-Mcp @('snapshot', '--scope', 'all', '--out', (Join-Path $runDir 's0.json')) | Out-Null
    's0 snapshot written'

    foreach ($feature in $Features) {
        $spec = Get-Content (Join-Path $here "specs\$feature.json") -Raw | ConvertFrom-Json
        "--- $feature"
        Invoke-Mcp (@('select', '--marks') + @($spec.marks)) | Out-Null
        $before = @(Get-RevitWindows | Where-Object { $_.Class -eq '#32770' } | ForEach-Object { $_.Title })
        Invoke-RibbonButton $spec.ribbon | Out-Null
        $window = Wait-RevitWindow $spec.window 30 -OrDialog -existingDialogs $before
        if (-not $window) {
            Save-WindowShot $revit.MainWindowHandle (Join-Path $runDir "$feature.fail.png")
            throw "$feature window '$($spec.window)' did not open (pick prompt?)"
        }
        if ($window.Class -eq '#32770') {
            # the command refused the selection: record its message as this feature's result and go on
            $refusal = Close-ResultDialog 5
            "$feature REFUSED: $($refusal.Text -replace "`n", ' | ')"
            Set-Content (Join-Path $runDir "$feature.dialog.txt") -Value ("REFUSED " + $refusal.Title + "`n" + $refusal.Text) -Encoding UTF8
            $meta.features[$feature] = [ordered]@{ refused = $refusal.Text }
            continue
        }
        Start-Sleep -Seconds 3
        $inputs = Read-WindowInputs $window.Handle
        $inputs | ConvertTo-Json -Depth 4 | Set-Content (Join-Path $runDir "$feature.readback.json") -Encoding UTF8
        $existing = @(Get-RevitWindows | Where-Object { $_.Class -eq '#32770' } | ForEach-Object { $_.Title })
        Invoke-WindowButton $window.Handle $spec.run | Out-Null
        $dialog = Close-ResultDialog 600 $existing
        if (-not $dialog) { throw "$feature produced no result dialog within 600 s" }
        "$($dialog.Title): $($dialog.Text -replace "`n", ' | ')"
        Set-Content (Join-Path $runDir "$feature.dialog.txt") -Value ($dialog.Title + "`n" + $dialog.Text) -Encoding UTF8
        Start-Sleep -Seconds 2
        $still = Get-RevitWindows | Where-Object { $_.Handle -eq $window.Handle }
        if ($still) { Close-RevitWindow $window.Handle; Start-Sleep -Seconds 2 }
        Invoke-Mcp @('snapshot', '--scope', $spec.scope, '--out', (Join-Path $runDir "$feature.json")) | Out-Null
        $meta.features[$feature] = [ordered]@{ dialogTitle = $dialog.Title; dialogSeconds = $dialog.Seconds }
        "$feature snapshot written"
    }
    $meta.finishedAt = (Get-Date).ToString('s')
    $meta.result = 'complete'
}
catch {
    $meta.result = 'failed: ' + $_.Exception.Message
    "FAILED: $($_.Exception.Message)"
}
finally {
    try { Set-BridgeOptIn $false | Out-Null } catch { }
    if (-not $KeepRevit) {
        $revit.Refresh()
        if (-not $revit.HasExited) {
            # a modal dialog left open (refusal, failure) swallows the close request
            while (Get-RevitWindows | Where-Object { $_.Class -eq '#32770' }) { $left = Close-ResultDialog 3; if (-not $left) { break }; "closed leftover dialog '$($left.Title)'" }
            $null = $revit.CloseMainWindow()
            $sw = [Diagnostics.Stopwatch]::StartNew()
            while (-not $revit.HasExited -and $sw.Elapsed.TotalSeconds -lt 60) {
                Start-Sleep -Seconds 2
                foreach ($d in Get-RevitWindows | Where-Object { $_.Class -eq '#32770' }) {
                    # "Save File: Do you want to save changes to fixture.rvt?" - never save the work copy
                    $pressed = Invoke-DialogButton $d.Handle '^(No|Don.t save|Do not save)$'
                    if ($pressed) { "answered '$pressed' to '$($d.Title)'" }
                }
                $revit.Refresh()
            }
            if (-not $revit.HasExited) { 'Revit did not close; killing our pid'; Stop-Process -Id $revit.Id -Force }
        }
        Start-Sleep -Seconds 3
        Remove-Item $work -Recurse -Force -ErrorAction SilentlyContinue
    }
    Remove-Item (Join-Path $runDir 'registry') -Recurse -Force -ErrorAction SilentlyContinue
    $meta | ConvertTo-Json -Depth 4 | Set-Content (Join-Path $runDir 'run-meta.json') -Encoding UTF8
    "run-meta: $($meta.result)"
}
if ($meta.result -ne 'complete') { exit 1 }
