# Live verification of the ETABS MCP: Claude Code's side (HPEtabs.Mcp.Server.exe over stdio) against the HPEtabs.McpBridge
# desktop app on an isolated registry root. Starts the bridge exe, brings the pipe up, ticks the opt-in and drives
# live-verify.py phase by phase:
#   disabled  - before the execution opt-in: execute refused naming the separate app, context answers
#   detached  - opt-in on, nothing attached: guard (E20), static preview, destructive -32001, fast not-attached, inspect_type
#   spike     - (-Phase spike|all, ETABS 22 must be running with a throw-away model) click Attach, then E13/E13b/E17/E18/T1
#   bridge    - (-Phase bridge|all, ETABS 22 running with a SAVED throw-away model) attach, phase bridge (writes + snapshots, D off),
#               tick 'Allow destructive operations', phase bridgedestructive (deletes what bridge added), untick
#   seeds     - (-Phase seeds|all, same model) phase seeds (12 seeds, writes for real), phase registry (MISS → propose → approve →
#               quarantine → restore), tick, phase seedsdestructive (run_analysis, results seeds, cleanup + unlock), untick
#   full      - everything above on ONE registry root per run (never wiped between groups); with -Publish the two exes are published first and
#               the bridge runs from HPEtabs/output/HPEtabs.McpBridge (the self-check must pass from the publish folder)
#   nomodel / modal / closed - (-Interactive) the script asks you to close the model / open a dialog / close ETABS, then runs the phase
# Never starts, stops or drives ETABS itself; closes only the bridge it started. Windows PowerShell 5.1 (UIA); relaunches itself from pwsh.
#
#   powershell.exe -ExecutionPolicy Bypass -File HPEtabs/tools/harness/run-live-verify.ps1 [-Phase detached|spike|bridge|seeds|full|all] [-Publish] [-Interactive]
#                  [-Exe <server exe>] [-BridgeExe <bridge exe>] [-Tag run1] [-Runs 1]
param(
    [ValidateSet('detached', 'spike', 'bridge', 'seeds', 'full', 'all')][string]$Phase = 'detached',
    [switch]$Interactive,
    [switch]$Publish,
    [string]$Exe = '',
    [string]$BridgeExe = '',
    [string]$Tag = '',
    [int]$Runs = 1
)

if ($PSVersionTable.PSEdition -ne 'Desktop') {
    $ps = Join-Path $env:SystemRoot 'System32\WindowsPowerShell\v1.0\powershell.exe'
    $argList = @('-NoProfile', '-ExecutionPolicy', 'Bypass', '-File', $PSCommandPath, '-Phase', $Phase, '-Runs', $Runs)
    if ($Interactive) { $argList += '-Interactive' }
    if ($Publish) { $argList += '-Publish' }
    if ($Exe) { $argList += @('-Exe', $Exe) }
    if ($BridgeExe) { $argList += @('-BridgeExe', $BridgeExe) }
    if ($Tag) { $argList += @('-Tag', $Tag) }
    & $ps @argList
    exit $LASTEXITCODE
}

$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'harness-common.ps1')

$repo = (Resolve-Path (Join-Path $PSScriptRoot '..\..\..')).Path
if ($Publish) {
    # The shipped shape: single-file server, folder-published bridge (Roslyn needs real file locations). Publish fails while a bridge or a server from these folders is running.
    Write-Host 'publishing HPEtabs.Mcp.Server (single-file) and HPEtabs.McpBridge (folder)…'
    & dotnet publish (Join-Path $repo 'HPEtabs\HPEtabs.Mcp.Server') -c Release -r win-x64 -p:PublishSingleFile=true -p:SelfContained=false -o (Join-Path $repo 'HPEtabs\output\HPEtabs.Mcp.Server') --nologo -v q | Out-Null
    if ($LASTEXITCODE -ne 0) { throw 'publish of the server failed' }
    & dotnet publish (Join-Path $repo 'HPEtabs\HPEtabs.McpBridge') -c Release -r win-x64 -p:SelfContained=false -o (Join-Path $repo 'HPEtabs\output\HPEtabs.McpBridge') --nologo -v q | Out-Null
    if ($LASTEXITCODE -ne 0) { throw 'publish of the bridge failed' }
    if (-not $Exe) { $Exe = Join-Path $repo 'HPEtabs\output\HPEtabs.Mcp.Server\HPEtabs.Mcp.Server.exe' }
    if (-not $BridgeExe) { $BridgeExe = Join-Path $repo 'HPEtabs\output\HPEtabs.McpBridge\HPEtabs.McpBridge.exe' }
}
if (-not $Exe) { $Exe = Join-Path $repo 'HPEtabs\HPEtabs.Mcp.Server\bin\Debug\net10.0\HPEtabs.Mcp.Server.exe' }
if (-not $BridgeExe) { $BridgeExe = Join-Path $repo 'HPEtabs\HPEtabs.McpBridge\bin\Debug\net8.0-windows\HPEtabs.McpBridge.exe' }
if (-not (Test-Path $Exe)) { throw "server exe not found: $Exe - build HPEtabs/HPEtabs.slnx first" }
if (-not (Test-Path $BridgeExe)) { throw "bridge exe not found: $BridgeExe - build HPEtabs/HPEtabs.slnx first" }
$Exe = (Resolve-Path $Exe).Path
$BridgeExe = (Resolve-Path $BridgeExe).Path
$verify = Join-Path $PSScriptRoot 'live-verify.py'
$outDir = Join-Path $repo 'HPEtabs\output\live-verify'
if ($Tag) { $outDir = Join-Path $outDir $Tag }
New-Item -ItemType Directory -Force $outDir | Out-Null
# One isolated registry root per run (a run = a fresh install; inside a run nothing is wiped between the groups, so the
# opt-in OFF/ON steps and the registry loop see the same tools-library and registry.db).
$registry = $null
$logPath = Join-Path $outDir 'live-verify.log'
if (Test-Path $logPath) { Remove-Item $logPath }
$env:PYTHONIOENCODING = 'utf-8'
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8

Assert-NoBridgeRunning
if ($Phase -ne 'detached') { Assert-EtabsRunning }

$result = [ordered]@{ startedAt = (Get-Date).ToString('s'); exe = $Exe; bridge = $BridgeExe; runs = [ordered]@{} }
$allOk = $true
$clock = [Diagnostics.Stopwatch]::StartNew()

function Invoke-Phase([string]$run, [string]$phase) {
    Write-Host "`n--- $run / phase $phase ($([int]$clock.Elapsed.TotalSeconds) s) ---"
    $ErrorActionPreference = 'Continue'
    $out = & python $verify $Exe --registry $script:registry --phase $phase --out $outDir 2>&1 | ForEach-Object { "$_" }
    $exit = $LASTEXITCODE
    $ErrorActionPreference = 'Stop'
    "=== $run / phase $phase ===" | Add-Content -Path $logPath -Encoding UTF8
    $out | Add-Content -Path $logPath -Encoding UTF8
    $out | ForEach-Object { Write-Host "  $_" }
    $summary = @($out | Where-Object { $_ -like '{"phase"*' }) | Select-Object -Last 1
    $script:result.runs["$run/$phase"] = [ordered]@{ exit = $exit; summary = $summary }
    if ($exit -ne 0) { $script:allOk = $false }
    return $exit
}

function Wait-UserStep([string]$instruction) {
    Write-Host "`n>>> $instruction" -ForegroundColor Yellow
    Write-Host '    press Enter here when done...'
    [void](Read-Host)
}

for ($run = 1; $run -le $Runs; $run++) {
    $proc = $null
    $runName = "run$run"
    $script:registry = Join-Path $outDir "registry-$runName"
    if (Test-Path $script:registry) { Remove-Item -Recurse -Force $script:registry }
    New-Item -ItemType Directory -Force $script:registry | Out-Null
    try {
        $proc = Start-Bridge $BridgeExe
        Start-Sleep -Seconds 2
        $selfCheck = Read-BridgeText 'SelfCheck'
        Write-Host "self-check text: $selfCheck"
        if ($selfCheck -notlike 'Scripting self-check OK*') { throw "self-check not OK: $selfCheck" }
        if (-not (Test-Path "\\.\pipe\$script:PipeName")) { Invoke-BridgeButton 'ToggleListener' | Out-Null }
        if (-not (Wait-Pipe 30)) { throw 'pipe never appeared' }

        $null = Invoke-Phase $runName 'disabled'
        if (-not (Set-OptIn 'AllowExecution' $true)) { throw 'could not tick Allow AI code execution' }
        $null = Invoke-Phase $runName 'detached'

        if ($Phase -ne 'detached') {
            if (-not (Invoke-BridgeButton 'Attach')) { throw 'Attach button not available' }
            $state = Wait-BridgeText 'AttachState' '^Attached to ETABS' 40
            if (-not $state) { throw "attach did not complete: $(Read-BridgeText 'AttachState') / $(Read-BridgeText 'AttachWarning')" }
            Write-Host "attach: $state"
            $null = Invoke-Phase $runName 'spike'

            if ($Phase -in @('bridge', 'full', 'all')) {
                $null = Invoke-Phase $runName 'bridge'
                if (-not (Set-OptIn 'AllowDestructive' $true)) { throw 'could not tick Allow destructive operations' }
                try { $null = Invoke-Phase $runName 'bridgedestructive' }
                finally { Set-OptIn 'AllowDestructive' $false | Out-Null }
            }

            if ($Phase -in @('seeds', 'full', 'all')) {
                $null = Invoke-Phase $runName 'seeds'
                $null = Invoke-Phase $runName 'registry'
                if (-not (Set-OptIn 'AllowDestructive' $true)) { throw 'could not tick Allow destructive operations' }
                try { $null = Invoke-Phase $runName 'seedsdestructive' }
                finally { Set-OptIn 'AllowDestructive' $false | Out-Null }
            }

            if ($Interactive) {
                Wait-UserStep 'In ETABS: close the model (File > Close) but keep ETABS open.'
                $null = Invoke-Phase $runName 'nomodel'
                Wait-UserStep 'In ETABS: open the model again, then open a modal dialog (e.g. Define > Material Properties...) and leave it open.'
                $null = Invoke-Phase $runName 'modal'
                Wait-UserStep 'In ETABS: close the dialog, then close ETABS completely.'
                $null = Invoke-Phase $runName 'closed'
                Wait-UserStep 'Start ETABS 22 again and open the throw-away model.'
                if (-not (Invoke-BridgeButton 'Attach')) { throw 'Attach button not available after ETABS restart' }
                $state = Wait-BridgeText 'AttachState' '^Attached to ETABS' 40
                $script:result.runs["$runName/reattach"] = [ordered]@{ exit = $(if ($state) { 0 } else { 1 }); summary = "$state" }
                if (-not $state) { $script:allOk = $false }
                $null = Invoke-Phase $runName 'spike'
            }
        }
    }
    catch {
        Write-Host "run $run failed: $($_.Exception.Message)" -ForegroundColor Red
        $script:result.runs["$runName/error"] = [ordered]@{ exit = 1; summary = $_.Exception.Message }
        $allOk = $false
    }
    finally {
        Get-BridgeLogTail 40 | Set-Content -Encoding UTF8 (Join-Path $outDir "bridge-log-tail-$runName.txt")
        Stop-Bridge $proc
    }
}

$result.finishedAt = (Get-Date).ToString('s')
$result.seconds = [int]$clock.Elapsed.TotalSeconds
$result.ok = $allOk
$result | ConvertTo-Json -Depth 5 | Set-Content -Encoding UTF8 (Join-Path $outDir 'summary.json')
Write-Host "`nsummary: $(Join-Path $outDir 'summary.json') ok=$allOk in $($result.seconds) s"
exit $(if ($allOk) { 0 } else { 1 })
