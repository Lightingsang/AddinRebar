# Phase-1 spike in a real Navisworks Manage 2026: starts Roamer.exe with a sample model (the bridge opens its
# status window on start through HPNAVIS_MCP_BRIDGE_SHOW_WINDOW=1), ticks the opt-ins with UI Automation and drives
# pipe-scenarios.py over the named pipe. Repeats the whole cycle -Runs times (start -> scenarios -> close) and
# writes run-N.log + run-N.json under HPNavis/output/spike/. Closes only the Roamer.exe it started. Never saves.
#
#   powershell.exe -ExecutionPolicy Bypass -File HPNavis/tools/harness/run-bridge-spike.ps1 [-Runs 2] [-Model <.nwd>] [-WithModal]
#
# Windows PowerShell 5.1 (SendKeys + UIA); relaunches itself when started from pwsh.
param(
    [int]$Runs = 2,
    [string]$Model = '',
    [switch]$WithModal,   # S-07: opens the Open dialog (Ctrl+O) on Roamer and expects -32002 while it is up
    [switch]$KeepOpen     # leave Roamer running after the last run (manual inspection)
)

if ($PSVersionTable.PSEdition -ne 'Desktop') {
    $ps = Join-Path $env:SystemRoot 'System32\WindowsPowerShell\v1.0\powershell.exe'
    $argList = @('-NoProfile', '-ExecutionPolicy', 'Bypass', '-File', $PSCommandPath, '-Runs', $Runs)
    if ($Model) { $argList += @('-Model', $Model) }
    if ($WithModal) { $argList += '-WithModal' }
    if ($KeepOpen) { $argList += '-KeepOpen' }
    & $ps @argList
    exit $LASTEXITCODE
}

$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'harness-common.ps1')

$repo = (Resolve-Path (Join-Path $PSScriptRoot '..\..\..')).Path
$outDir = Join-Path $repo 'HPNavis\output\spike'
New-Item -ItemType Directory -Force $outDir | Out-Null
$scenarios = Join-Path $PSScriptRoot 'pipe-scenarios.py'
if (-not $Model) { $Model = Join-Path $script:NavisDir 'Samples\gatehouse\gatehouse_pub.nwd' }
if (-not (Test-Path $Model)) { throw "sample model not found: $Model" }

Assert-NoNavisworksRunning
Assert-PluginDeployed

$foreign = Get-ChildItem (Split-Path $script:PluginDir) -Directory | Where-Object { $_.Name -ne 'HPNavis.McpBridge' } | ForEach-Object Name
Write-Host "other plugins in the Plugins folder (left untouched): $($foreign -join ', ')"

function Invoke-Scenarios([string]$only, [string]$logPath) {
    $argList = @($scenarios)
    if ($only) { $argList += @('--only', $only) }
    # python's stderr (a traceback) must not become a terminating error under $ErrorActionPreference = 'Stop'
    $ErrorActionPreference = 'Continue'
    $out = & python @argList 2>&1 | ForEach-Object { "$_" }
    $code = $LASTEXITCODE
    $ErrorActionPreference = 'Stop'
    $out | Add-Content -Path $logPath -Encoding UTF8   # Tee-Object would write UTF-16
    $out | ForEach-Object { Write-Host "  $_" }
    $summary = $out | Where-Object { $_ -like '{"passed"*' } | Select-Object -Last 1
    return @{ exit = $code; summary = $summary; lines = $out }
}

function Get-LogLinesSince([datetime]$since) {
    $log = Get-ChildItem $script:LogDir -Filter 'mcpbridge-*.log' -ErrorAction SilentlyContinue | Sort-Object LastWriteTime | Select-Object -Last 1
    if (-not $log) { return @() }
    Get-Content $log.FullName | Where-Object {
        $_ -match '^(\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2})' -and [datetime]::ParseExact($Matches[1], 'yyyy-MM-dd HH:mm:ss', $null) -ge $since.AddSeconds(-1)
    }
}

$allOk = $true
for ($run = 1; $run -le $Runs; $run++) {
    $runLog = Join-Path $outDir "run-$run.log"
    if (Test-Path $runLog) { Remove-Item $runLog }
    $started = Get-Date
    $r = [ordered]@{ run = $run; startedAt = $started.ToString('s'); model = $Model; foreignPlugins = @($foreign) }
    Write-Host "`n=== run $run/$Runs ==="
    $app = $null
    try {
        $app = Start-NavisworksWithModel $Model
        $r.roamerPid = $script:navisPid
        Start-Sleep -Seconds 3

        # S-01 / S-02: plugin loaded, self-check ran
        $boot = Get-LogLinesSince $started
        $r.s01_pluginStarted = [bool]($boot | Where-Object { $_ -like '*HPNavis MCP bridge starting*' })
        $r.s02_selfCheckOk = [bool]($boot | Where-Object { $_ -like '*MCP scripting self-check OK*' })
        $r.s02_resolvedCount = @($boot | Where-Object { $_ -like '*MCP bridge resolved*' }).Count
        $r.s02_selfCheckLine = [string]($boot | Where-Object { $_ -like '*scripting self-check*' } | Select-Object -Last 1)   # [string]: Get-Content strings carry PS* note properties that ConvertTo-Json would expand
        Write-Host "S-01 plugin started: $($r.s01_pluginStarted) | S-02 self-check OK: $($r.s02_selfCheckOk) (resolved $($r.s02_resolvedCount))"

        # the window opened itself (HPNAVIS_MCP_BRIDGE_SHOW_WINDOW=1 on this process only)
        $win = Find-BridgeWindow 8
        $r.windowFound = [bool]$win
        if (-not $win) { throw 'bridge window not found through UI Automation' }
        $r.selfCheckText = Read-BridgeText 'SelfCheck'

        if (-not (Test-Path "\\.\pipe\$script:PipeName")) { Invoke-BridgeButton 'ToggleListener' | Out-Null }
        $r.pipeUp = Wait-Pipe 30
        if (-not $r.pipeUp) { throw 'pipe never appeared' }

        # S-04a: execution opt-in is OFF after start → -32001
        $r.disabled = (Invoke-Scenarios 'disabled' $runLog).summary
        $r.optInTicked = Set-OptIn 'AllowExecution' $true
        if (-not $r.optInTicked) { throw 'could not tick Allow AI code execution' }

        # S-03..S-06, S-05b/c, S-11, timeout, cancel
        $main = Invoke-Scenarios '' $runLog
        $r.main = $main.summary
        $r.mainExit = $main.exit

        # S-08 (script-driven): heavy opt-in on, clash test created and run through the API, heavy off again
        $r.heavyTicked = Set-OptIn 'AllowHeavy' $true
        if ($r.heavyTicked) {
            $clash = Invoke-Scenarios 'clash' $runLog
            $r.clash = $clash.summary
            $r.clashExit = $clash.exit
            $null = Set-OptIn 'AllowHeavy' $false
        }

        if ($WithModal) {
            # S-07: a modal owned by Roamer's main window (the Open dialog) must make execute answer -32002
            $shell = New-Object -ComObject WScript.Shell
            if (-not $shell.AppActivate($script:navisPid)) {
                # keystrokes would land in whatever window has focus: skip rather than open a dialog elsewhere
                Write-Host 'S-07 skipped: Roamer could not be brought to the foreground'
                $r.modal = 'skipped: AppActivate failed'
            }
            else {
                Start-Sleep -Milliseconds 500
                [System.Windows.Forms.SendKeys]::SendWait('^o')
                Start-Sleep -Seconds 2
                $r.modal = (Invoke-Scenarios 'modal' $runLog).summary
                if ($shell.AppActivate($script:navisPid)) { [System.Windows.Forms.SendKeys]::SendWait('{ESC}') }
                Start-Sleep -Seconds 1
                $r.afterModal = (Invoke-Scenarios 'ping' $runLog).summary
            }
        }

        $r.progressLines = @(Get-LogLinesSince $started | Where-Object { $_ -like '*progress depth*' }).Count
        $r.progressDepthZeroAfterLoad = [bool](Get-LogLinesSince $started | Where-Object { $_ -like '*ProgressEnded*depth 0' })
    }
    catch {
        $r.error = $_.Exception.Message
        Write-Host "run $run failed: $($r.error)"
        $allOk = $false
    }
    finally {
        # S-09: graceful close -> OnUnloading -> Dispose -> pipe gone, "stopped" logged
        if ($KeepOpen -and $run -eq $Runs) { Write-Host 'leaving Roamer open (-KeepOpen)' }
        else {
            Stop-Navisworks $app
            Start-Sleep -Seconds 2
            $r.s09_pipeGoneAfterClose = -not (Test-Path "\\.\pipe\$script:PipeName")
            $tail = Get-LogLinesSince $started
            $r.s09_stoppedLogged = [bool]($tail | Where-Object { $_ -like '*HPNavis MCP bridge stopped*' })
            $r.exceptionsInLog = @($tail | Where-Object { $_ -match '\[ERR\]|Exception' }).Count
            Write-Host "S-09 pipe gone: $($r.s09_pipeGoneAfterClose) | stopped logged: $($r.s09_stoppedLogged) | [ERR]/Exception lines: $($r.exceptionsInLog)"
            Add-Content $runLog "`n--- bridge log since $($started.ToString('HH:mm:ss')) ---"
            $tail | Add-Content $runLog
        }
    }
    $r | ConvertTo-Json -Depth 4 | Set-Content (Join-Path $outDir "run-$run.json")
    $ok = $r.s01_pluginStarted -and $r.s02_selfCheckOk -and $r.pipeUp -and ($r.mainExit -eq 0) -and ($r.clashExit -eq 0) -and -not $r.error -and ($KeepOpen -or ($r.s09_pipeGoneAfterClose -and $r.s09_stoppedLogged))
    if (-not $ok) { $allOk = $false }
    Write-Host "run $run => $(if ($ok) { 'PASS' } else { 'FAIL' })  main=$($r.main)"
}

Write-Host "`nresults in $outDir"
exit $(if ($allOk) { 0 } else { 1 })
