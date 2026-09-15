# Unattended live verification of the Navisworks MCP: Claude Code's side (the published HPNavis.Mcp.Server.exe over
# stdio) against the real bridge inside Navisworks Manage 2026, on an isolated registry root. Starts Roamer.exe with
# the sample model, brings the pipe up and drives live-verify.py phase by phase:
#   disabled  - before the execution opt-in: execute refused, context answers
#   main      - E execute matrix, S every seed, R registry loop (propose/test/publish/CLI approve/quarantine/restore), C context+resources+prompts
#   modal     - the Open dialog (Ctrl+O) is up: execute answers busy after the grace; then closed -> aftermodal runs again
#   heavy     - after ticking "Allow heavy operations": create_and_run_clash_test for real, a file append, context
#   nodoc     - (-WithNoDoc) a second start without a model: context isClear, execute refused
#   isolation - (-IncludeIsolation) a second Roamer beside the first logs "pipe already in use"; our plugin folder renamed
#               away -> a Roamer start writes no bridge log line (restored in finally). Never touches other plugins.
# Closes only the Roamer it started, never saves the model. Windows PowerShell 5.1 (UIA + SendKeys); relaunches itself from pwsh.
#
#   powershell.exe -ExecutionPolicy Bypass -File HPNavis/tools/harness/run-live-verify.ps1 [-Exe <path>] [-Model <.nwd>] [-AppendFile <.nwc>]
#                  [-WithNoDoc] [-IncludeIsolation] [-SkipModal] [-SkipHeavy] [-Tag run1]
param(
    [string]$Exe = '',
    [string]$Model = '',
    [string]$AppendFile = '',
    [switch]$WithNoDoc,
    [switch]$IncludeIsolation,
    [switch]$SkipModal,
    [switch]$SkipHeavy,
    [string]$Tag = ''
)

if ($PSVersionTable.PSEdition -ne 'Desktop') {
    $ps = Join-Path $env:SystemRoot 'System32\WindowsPowerShell\v1.0\powershell.exe'
    $argList = @('-NoProfile', '-ExecutionPolicy', 'Bypass', '-File', $PSCommandPath)
    if ($Exe) { $argList += @('-Exe', $Exe) }
    if ($Model) { $argList += @('-Model', $Model) }
    if ($AppendFile) { $argList += @('-AppendFile', $AppendFile) }
    if ($WithNoDoc) { $argList += '-WithNoDoc' }
    if ($IncludeIsolation) { $argList += '-IncludeIsolation' }
    if ($SkipModal) { $argList += '-SkipModal' }
    if ($SkipHeavy) { $argList += '-SkipHeavy' }
    if ($Tag) { $argList += @('-Tag', $Tag) }
    & $ps @argList
    exit $LASTEXITCODE
}

$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'harness-common.ps1')

$repo = (Resolve-Path (Join-Path $PSScriptRoot '..\..\..')).Path
if (-not $Exe) { $Exe = Join-Path $repo 'HPNavis\output\HPNavis.Mcp.Server\HPNavis.Mcp.Server.exe' }
if (-not (Test-Path $Exe)) { throw "server exe not found: $Exe - publish first (see HPNavis/README.md)" }
if (-not $Model) { $Model = Join-Path $script:NavisDir 'Samples\gatehouse\gatehouse_pub.nwd' }
if (-not (Test-Path $Model)) { throw "model not found: $Model" }
if (-not $AppendFile) { $AppendFile = Join-Path $script:NavisDir 'Samples\Getting Started\MEP.nwc' }
$verify = Join-Path $PSScriptRoot 'live-verify.py'
$outDir = Join-Path $repo 'HPNavis\output\live-verify'
if ($Tag) { $outDir = Join-Path $outDir $Tag }
New-Item -ItemType Directory -Force $outDir | Out-Null
# the registry root is recreated per run so the MISS/propose/approve loop starts from the 12 seeds every time
$registry = Join-Path $outDir 'registry'
if (Test-Path $registry) { Remove-Item -Recurse -Force $registry }
New-Item -ItemType Directory -Force $registry | Out-Null
$logPath = Join-Path $outDir 'live-verify.log'
if (Test-Path $logPath) { Remove-Item $logPath }
$env:PYTHONIOENCODING = 'utf-8'
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8   # python prints UTF-8; PowerShell 5.1 would decode it as OEM

Assert-NoNavisworksRunning
Assert-PluginDeployed

$result = [ordered]@{ startedAt = (Get-Date).ToString('s'); model = $Model; exe = $Exe; phases = [ordered]@{} }
$allOk = $true
$clock = [Diagnostics.Stopwatch]::StartNew()

# Runs one live-verify.py phase, appends its output to the log and records {exit, summary} under the phase name.
function Invoke-Phase([string]$phase, [string[]]$extra = @()) {
    Write-Host "`n--- phase $phase ($([int]$clock.Elapsed.TotalSeconds) s) ---"
    $ErrorActionPreference = 'Continue'
    $out = & python $verify $Exe --registry $registry --phase $phase --out $outDir @extra 2>&1 | ForEach-Object { "$_" }
    $exit = $LASTEXITCODE
    $ErrorActionPreference = 'Stop'
    "=== phase $phase ===" | Add-Content -Path $logPath -Encoding UTF8
    $out | Add-Content -Path $logPath -Encoding UTF8
    $out | ForEach-Object { Write-Host "  $_" }
    $summary = @($out | Where-Object { $_ -like '{"phase"*' }) | Select-Object -Last 1
    $script:result.phases[$phase] = [ordered]@{ exit = $exit; summary = $summary }
    if ($exit -ne 0) { $script:allOk = $false }
    return $exit
}

# Starts Roamer and brings the pipe up; a Roamer whose bridge never came up is closed here, because the caller
# only learns the Process object from the return value and could not close it from its finally block.
function Start-BridgeOn([string]$model) {
    $p = Start-NavisworksWithModel $model
    try {
        Start-Sleep -Seconds 3
        if (-not (Find-BridgeWindow 12)) { throw 'bridge window not found' }
        if (-not (Test-Path "\\.\pipe\$script:PipeName")) { Invoke-BridgeButton 'ToggleListener' | Out-Null }
        if (-not (Wait-Pipe 30)) { throw 'pipe never appeared' }
        return $p
    }
    catch { Stop-Navisworks $p; throw }
}

$proc = $null
$logStart = Get-Date
try {
    $proc = Start-BridgeOn $Model
    $null = Invoke-Phase 'disabled'
    if (-not (Set-OptIn 'AllowExecution' $true)) { throw 'could not tick Allow AI code execution' }
    $null = Invoke-Phase 'main'

    if (-not $SkipModal) {
        if (-not (Set-RoamerMainWindowForeground)) {
            Write-Host 'modal skipped: Roamer could not be brought to the foreground (keystrokes would land elsewhere)'
            $result.phases['modal'] = [ordered]@{ exit = $null; summary = 'skipped: SetForegroundWindow failed' }
        }
        else {
            Start-Sleep -Milliseconds 500
            [System.Windows.Forms.SendKeys]::SendWait('^o')
            $opened = $false
            for ($i = 0; $i -lt 10 -and -not $opened; $i++) { Start-Sleep -Milliseconds 500; $opened = (Test-RoamerMainWindowEnabled) -eq $false }
            if ($opened) { $null = Invoke-Phase 'modal' }
            else {
                Write-Host 'modal skipped: the Open dialog did not appear (keystroke lost)'
                $result.phases['modal'] = [ordered]@{ exit = $null; summary = 'skipped: dialog did not open' }
            }
            $null = Close-RoamerDialogs
            for ($i = 0; $i -lt 10 -and (Test-RoamerMainWindowEnabled) -ne $true; $i++) { Start-Sleep -Milliseconds 500; $null = Close-RoamerDialogs }
            if (-not (Test-RoamerMainWindowEnabled)) { throw 'Roamer main window still disabled after closing the dialog' }
            $null = Invoke-Phase 'aftermodal'
        }
    }

    if (-not $SkipHeavy) {
        if (-not (Set-OptIn 'AllowHeavy' $true)) { throw 'could not tick Allow heavy operations' }
        try { $null = Invoke-Phase 'heavy' @('--append-file', $AppendFile) }
        finally { $null = Set-OptIn 'AllowHeavy' $false }
    }

    if ($IncludeIsolation) {
        # a second Roamer beside the first: its listener must refuse the pipe and say which host/version owns it
        Write-Host "`n--- isolation: second Roamer ---"
        $first = $proc
        $firstPid = $script:navisPid
        $second = $null
        $secondStart = Get-Date
        try {
            $second = Start-NavisworksWithModel ''
            Start-Sleep -Seconds 3
            if (-not (Find-BridgeWindow 12)) { throw 'second bridge window not found' }
            Invoke-BridgeButton 'ToggleListener' | Out-Null
            # the friendly reason goes to the status window (Faulted -> StatusDetail); the shared log gets the IOException
            $detail = ''
            for ($i = 0; $i -lt 10 -and $detail -notlike '*in use*'; $i++) { Start-Sleep -Milliseconds 500; $detail = [string](Read-BridgeText 'StatusDetail') }
            # only lines written since the second Roamer started count (the daily log is shared with earlier runs)
            $faultLogged = @(Get-BridgeLogTail 200 | Where-Object { $_ -like '*could not create pipe*' -and $_ -match '^(\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2})' -and [datetime]::ParseExact($Matches[1], 'yyyy-MM-dd HH:mm:ss', $null) -ge $secondStart.AddSeconds(-1) }).Count
            $inUse = ($detail -like '*in use*') -and ($detail -like '*Navisworks 2026*') -and ($faultLogged -ge 1)
            $result.phases['isolation.secondRoamer'] = [ordered]@{ exit = $(if ($inUse) { 0 } else { 1 }); summary = "status window: '$detail'; 'could not create pipe' log lines: $faultLogged" }
            Write-Host "  second Roamer: status '$detail'; fault logged x$faultLogged"
            if (-not $inUse) { $allOk = $false }
        }
        finally {
            Stop-Navisworks $second
            $script:navisPid = $firstPid
            $script:bridgeWindow = $null
        }
        $firstAlive = -not $first.HasExited -and (Test-Path "\\.\pipe\$script:PipeName")
        $result.phases['isolation.firstStillServing'] = [ordered]@{ exit = $(if ($firstAlive) { 0 } else { 1 }); summary = "first Roamer alive with the pipe: $firstAlive" }
        if (-not $firstAlive) { $allOk = $false }
    }

    $bridgeLog = Get-BridgeLogTail 600 | Where-Object { $_ -match '^(\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2})' -and [datetime]::ParseExact($Matches[1], 'yyyy-MM-dd HH:mm:ss', $null) -ge $logStart.AddSeconds(-1) }
    $result.bridgeLog = [ordered]@{
        serverConnected = @($bridgeLog | Where-Object { $_ -like '*MCP server connected*' }).Count
        unauthorized = @($bridgeLog | Where-Object { $_ -like '*UnauthorizedAccess*' }).Count
        errors = @($bridgeLog | Where-Object { $_ -match ' \[(ERR|FTL)\] ' }).Count
    }
    Write-Host "bridge log: server connected x$($result.bridgeLog.serverConnected), UnauthorizedAccess x$($result.bridgeLog.unauthorized), ERR/FTL x$($result.bridgeLog.errors)"
    if ($result.bridgeLog.unauthorized -gt 0 -or $result.bridgeLog.serverConnected -lt 1) { $allOk = $false }
}
catch {
    Write-Host "live verify failed: $($_.Exception.Message)"
    $result.error = $_.Exception.Message
    $allOk = $false
}
finally {
    try { $null = Set-OptIn 'AllowHeavy' $false } catch { }
    Stop-Navisworks $proc
}

if ($WithNoDoc) {
    Write-Host "`n--- phase nodoc (own Roamer without a model) ---"
    $nd = $null
    try {
        $nd = Start-BridgeOn ''
        if (-not (Set-OptIn 'AllowExecution' $true)) { throw 'could not tick Allow AI code execution' }
        $null = Invoke-Phase 'nodoc'
    }
    catch { Write-Host "nodoc failed: $($_.Exception.Message)"; $result.phases['nodoc'] = [ordered]@{ exit = 1; summary = $_.Exception.Message }; $allOk = $false }
    finally { Stop-Navisworks $nd }
}

if ($IncludeIsolation) {
    # our plugin folder renamed away: a Roamer start must not write a single HPNavis bridge line (proves the log
    # lines above came from this plugin, and that removing the folder uninstalls it). Restored whatever happens.
    Write-Host "`n--- isolation: plugin folder removed ---"
    $parked = $script:PluginDir + '.parked'
    if (Test-Path $parked) { throw "a parked plugin folder already exists: $parked - restore it by hand before running the isolation check" }
    $before = (Get-BridgeLogTail 1 | Select-Object -First 1)
    $app = $null
    try {
        Rename-Item $script:PluginDir $parked
        $app = Start-NavisworksWithModel ''
        Start-Sleep -Seconds 5
        $after = (Get-BridgeLogTail 1 | Select-Object -First 1)
        $pipeUp = Test-Path "\\.\pipe\$script:PipeName"
        $silent = ($after -eq $before) -and -not $pipeUp
        $result.phases['isolation.pluginRemoved'] = [ordered]@{ exit = $(if ($silent) { 0 } else { 1 }); summary = "no new bridge log line and no pipe with the plugin folder parked: $silent" }
        Write-Host "  plugin removed: silent = $silent"
        if (-not $silent) { $allOk = $false }
    }
    catch { Write-Host "plugin-removed check failed: $($_.Exception.Message)"; $result.phases['isolation.pluginRemoved'] = [ordered]@{ exit = 1; summary = $_.Exception.Message }; $allOk = $false }
    finally {
        Stop-Navisworks $app
        try { if (Test-Path $parked) { Rename-Item $parked $script:PluginDir } } catch { Write-Host "restore failed: $($_.Exception.Message)" }
        if (-not (Test-Path (Join-Path $script:PluginDir 'HPNavis.McpBridge.dll'))) {
            # a run that leaves the plugin uninstalled must never report PASS
            Write-Host "FAIL: plugin folder not restored - move $parked back to $($script:PluginDir) by hand"
            $result.phases['isolation.pluginRestored'] = [ordered]@{ exit = 1; summary = "plugin folder missing after the check: $($script:PluginDir)" }
            $allOk = $false
        }
    }
}

$result.seconds = [int]$clock.Elapsed.TotalSeconds
$result.ok = $allOk
$result | ConvertTo-Json -Depth 5 | Set-Content (Join-Path $outDir 'run-summary.json') -Encoding UTF8
Write-Host "`nlive verify => $(if ($allOk) { 'PASS' } else { 'FAIL' }) in $($result.seconds) s  (log: $logPath)"
foreach ($k in $result.phases.Keys) { Write-Host ("  {0,-28} exit={1} {2}" -f $k, $result.phases[$k].exit, $result.phases[$k].summary) }
exit $(if ($allOk) { 0 } else { 1 })
