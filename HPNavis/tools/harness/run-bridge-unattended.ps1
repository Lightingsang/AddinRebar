# Unattended bridge verification in a real Navisworks Manage 2026: starts Roamer.exe with a sample model (the bridge
# opens its status window on start through HPNAVIS_MCP_BRIDGE_SHOW_WINDOW=1), checks the window through UI Automation
# (listener toggle, opt-ins, heavy disabled while execution is off), then drives pipe-scenarios.py over the named pipe:
# the default matrix, the clash run and the file append with the heavy opt-in on, a modal dialog, and — with -WithNoDoc —
# one more Roamer without a model for the -32003 path. Repeats the whole cycle -Runs times and writes run-N.log +
# run-N.json under HPNavis/output/spike/. Closes only the Roamer.exe it started. Never saves.
#
#   powershell.exe -ExecutionPolicy Bypass -File HPNavis/tools/harness/run-bridge-unattended.ps1 [-Runs 2] [-Model <.nwd>] [-WithModal] [-WithNoDoc]
#
# Windows PowerShell 5.1 (SendKeys + UIA); relaunches itself when started from pwsh.
param(
    [int]$Runs = 2,
    [string]$Model = '',
    [switch]$WithModal,   # opens the Open dialog (Ctrl+O) on Roamer and expects -32002 while it is up
    [switch]$WithNoDoc,   # one extra Roamer without a model: context isClear, execute -> -32003
    [string]$AppendFile = '',  # .nwc appended with the heavy opt-in on (default: Samples\Getting Started\MEP.nwc)
    [switch]$KeepOpen     # leave Roamer running after the last run (manual inspection)
)

if ($PSVersionTable.PSEdition -ne 'Desktop') {
    $ps = Join-Path $env:SystemRoot 'System32\WindowsPowerShell\v1.0\powershell.exe'
    $argList = @('-NoProfile', '-ExecutionPolicy', 'Bypass', '-File', $PSCommandPath, '-Runs', $Runs)
    if ($Model) { $argList += @('-Model', $Model) }
    if ($WithModal) { $argList += '-WithModal' }
    if ($WithNoDoc) { $argList += '-WithNoDoc' }
    if ($AppendFile) { $argList += @('-AppendFile', $AppendFile) }
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
if (-not $AppendFile) { $AppendFile = Join-Path $script:NavisDir 'Samples\Getting Started\MEP.nwc' }
if (-not (Test-Path $AppendFile)) { throw "append sample not found: $AppendFile" }

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

function Get-BridgeElementEnabled([string]$automationId) {
    $win = Find-BridgeWindow 1
    if (-not $win) { return $null }
    $cond = New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::AutomationIdProperty, $automationId)
    $el = $win.FindFirst([System.Windows.Automation.TreeScope]::Descendants, $cond)
    if ($el) { return $el.Current.IsEnabled } else { return $null }
}

function Get-RoamerWorkingSetMb {
    $p = Get-Process -Id $script:navisPid -ErrorAction SilentlyContinue
    if ($p) { return [int]($p.WorkingSet64 / 1MB) } else { return 0 }
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
        $win = Find-BridgeWindow 12
        $r.windowFound = [bool]$win
        if (-not $win) { throw 'bridge window not found through UI Automation' }
        $r.selfCheckText = Read-BridgeText 'SelfCheck'

        # window: heavy stays disabled while execution is off; the listener button brings the pipe up and down
        $r.heavyDisabledWhileExecutionOff = (Get-BridgeElementEnabled 'AllowHeavy') -eq $false
        Write-Host "heavy checkbox disabled while execution off: $($r.heavyDisabledWhileExecutionOff)"
        if (-not (Test-Path "\\.\pipe\$script:PipeName")) { Invoke-BridgeButton 'ToggleListener' | Out-Null }
        $r.pipeUp = Wait-Pipe 30
        if (-not $r.pipeUp) { throw 'pipe never appeared' }
        Invoke-BridgeButton 'ToggleListener' | Out-Null
        Start-Sleep -Seconds 2
        $r.pipeDownAfterToggle = -not (Test-Path "\\.\pipe\$script:PipeName")
        Invoke-BridgeButton 'ToggleListener' | Out-Null
        $r.pipeUpAgain = Wait-Pipe 30
        Write-Host "listener toggle: down=$($r.pipeDownAfterToggle) up again=$($r.pipeUpAgain)"
        if (-not $r.pipeUpAgain) { throw 'pipe did not come back after the listener toggle' }

        # execution opt-in is OFF after start → -32001
        $disabled = Invoke-Scenarios 'disabled' $runLog
        $r.disabled = $disabled.summary
        $r.disabledExit = $disabled.exit
        $r.optInTicked = Set-OptIn 'AllowExecution' $true
        if (-not $r.optInTicked) { throw 'could not tick Allow AI code execution' }
        $r.statusAfterOptIn = Read-BridgeText 'StatusDetail'

        # S-03..S-06, S-05b/c, S-11, timeout, cancel
        $main = Invoke-Scenarios '' $runLog
        $r.main = $main.summary
        $r.mainExit = $main.exit

        # heavy opt-in on: clash test created and run through the API, then a real file append; heavy off again
        $r.heavyTicked = Set-OptIn 'AllowHeavy' $true
        if ($r.heavyTicked) {
            $clash = Invoke-Scenarios 'clash' $runLog
            $r.clash = $clash.summary
            $r.clashExit = $clash.exit
            $r.ramBeforeAppendMb = Get-RoamerWorkingSetMb
            $env:HPNAVIS_APPEND_FILE = $AppendFile
            $append = Invoke-Scenarios 'heavyappend' $runLog
            $r.append = $append.summary
            $r.appendExit = $append.exit
            $r.ramAfterAppendMb = Get-RoamerWorkingSetMb
            Write-Host "Roamer working set: $($r.ramBeforeAppendMb) MB -> $($r.ramAfterAppendMb) MB after append"
            $null = Set-OptIn 'AllowHeavy' $false
            $heavyOff = Invoke-Scenarios 'heavy' $runLog
            $r.heavyOffRefusesAgain = $heavyOff.summary
            $r.heavyOffExit = $heavyOff.exit
        }

        if ($WithModal) {
            # S-07: a modal owned by Roamer's main window (the Open dialog) must make execute answer -32002
            if (-not (Set-RoamerMainWindowForeground)) {
                # keystrokes would land in whatever window has focus: skip rather than open a dialog elsewhere
                Write-Host 'S-07 skipped: Roamer could not be brought to the foreground'
                $r.modal = 'skipped: SetForegroundWindow failed'
            }
            else {
                Start-Sleep -Milliseconds 500
                [System.Windows.Forms.SendKeys]::SendWait('^o')
                # only run the scenario once the dialog really disabled the main window; otherwise the keystroke went elsewhere
                $opened = $false
                for ($i = 0; $i -lt 10 -and -not $opened; $i++) { Start-Sleep -Milliseconds 500; $opened = (Test-RoamerMainWindowEnabled) -eq $false }
                if ($opened) {
                    $modal = Invoke-Scenarios 'modal' $runLog
                    $r.modal = $modal.summary
                    $r.modalExit = $modal.exit
                }
                else {
                    Write-Host 'S-07 skipped: the Open dialog did not appear (keystroke lost)'
                    $r.modal = 'skipped: dialog did not open'
                }
                $r.modalClosed = @(Close-RoamerDialogs)
                for ($i = 0; $i -lt 10 -and (Test-RoamerMainWindowEnabled) -ne $true; $i++) { Start-Sleep -Milliseconds 500; $null = Close-RoamerDialogs }
                $r.mainEnabledAfterModal = Test-RoamerMainWindowEnabled
                $afterModal = Invoke-Scenarios 'ping' $runLog
                $r.afterModal = $afterModal.summary
                $r.afterModalExit = $afterModal.exit
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
    # every scenario set that ran must have exited 0 (a skipped modal leaves modalExit unset and does not count)
    $exits = @($r.disabledExit, $r.mainExit, $r.clashExit, $r.appendExit, $r.heavyOffExit, $r.modalExit, $r.afterModalExit) | Where-Object { $null -ne $_ }
    $allZero = ($exits.Count -ge 5) -and -not ($exits | Where-Object { $_ -ne 0 })
    $ok = $r.s01_pluginStarted -and $r.s02_selfCheckOk -and $r.pipeUp -and $r.heavyDisabledWhileExecutionOff -and $r.pipeDownAfterToggle -and $allZero -and -not $r.error -and ($KeepOpen -or ($r.s09_pipeGoneAfterClose -and $r.s09_stoppedLogged))
    if (-not $ok) { $allOk = $false }
    Write-Host "run $run => $(if ($ok) { 'PASS' } else { 'FAIL' })  main=$($r.main)"
}

if ($WithNoDoc) {
    Write-Host "`n=== no-document run ==="
    $nd = [ordered]@{ run = 'nodoc'; startedAt = (Get-Date).ToString('s') }
    $ndLog = Join-Path $outDir 'run-nodoc.log'
    if (Test-Path $ndLog) { Remove-Item $ndLog }
    $app = $null
    try {
        $app = Start-NavisworksWithModel ''
        Start-Sleep -Seconds 3
        if (-not (Find-BridgeWindow 12)) { throw 'bridge window not found' }
        if (-not (Test-Path "\\.\pipe\$script:PipeName")) { Invoke-BridgeButton 'ToggleListener' | Out-Null }
        if (-not (Wait-Pipe 30)) { throw 'pipe never appeared' }
        if (-not (Set-OptIn 'AllowExecution' $true)) { throw 'could not tick Allow AI code execution' }
        $nodoc = Invoke-Scenarios 'nodoc' $ndLog
        $nd.nodoc = $nodoc.summary
        $nd.exit = $nodoc.exit
        if ($nodoc.exit -ne 0) { $allOk = $false }
    }
    catch { $nd.error = $_.Exception.Message; Write-Host "no-document run failed: $($nd.error)"; $allOk = $false }
    finally { Stop-Navisworks $app }
    $nd | ConvertTo-Json -Depth 4 | Set-Content (Join-Path $outDir 'run-nodoc.json')
    Write-Host "no-document run => $(if ($nd.exit -eq 0) { 'PASS' } else { 'FAIL' })  $($nd.nodoc)"
}

Write-Host "`nresults in $outDir"
exit $(if ($allOk) { 0 } else { 1 })
