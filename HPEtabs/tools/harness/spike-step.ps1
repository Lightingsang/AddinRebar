# Step-wise driver for the attached spike phases, for a session where a person changes ETABS's state between steps
# (close the model, open a dialog, close ETABS) and the bridge must stay up in between. Each call is one action:
#   start            start the bridge exe, start the listener, tick the execution opt-in; writes the pid to output/live-verify/spike/bridge.pid
#   attach           click Attach and wait for "Attached to ETABS …"
#   phase <name>     run live-verify.py --phase <name> (spike | nomodel | modal | closed | detached | bridge | bridgedestructive | seeds | seedsdestructive)
#   destructive on|off  tick/untick "Allow destructive operations"
#   state            print the attach state / warning / self-check texts
#   stop             close the bridge started by `start`
# Windows PowerShell 5.1 (UIA); relaunches itself from pwsh. Never starts, stops or drives ETABS itself.
param(
    [Parameter(Mandatory)][ValidateSet('start', 'attach', 'phase', 'destructive', 'state', 'stop')][string]$Action,
    [string]$Arg = '',
    [string]$Exe = '',
    [string]$BridgeExe = ''
)

if ($PSVersionTable.PSEdition -ne 'Desktop') {
    $ps = Join-Path $env:SystemRoot 'System32\WindowsPowerShell\v1.0\powershell.exe'
    $argList = @('-NoProfile', '-ExecutionPolicy', 'Bypass', '-File', $PSCommandPath, '-Action', $Action)
    if ($Arg) { $argList += @('-Arg', $Arg) }
    if ($Exe) { $argList += @('-Exe', $Exe) }
    if ($BridgeExe) { $argList += @('-BridgeExe', $BridgeExe) }
    & $ps @argList
    exit $LASTEXITCODE
}

$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'harness-common.ps1')

$repo = (Resolve-Path (Join-Path $PSScriptRoot '..\..\..')).Path
if (-not $Exe) { $Exe = Join-Path $repo 'HPEtabs\HPEtabs.Mcp.Server\bin\Debug\net10.0\HPEtabs.Mcp.Server.exe' }
if (-not $BridgeExe) { $BridgeExe = Join-Path $repo 'HPEtabs\HPEtabs.McpBridge\bin\Debug\net8.0-windows\HPEtabs.McpBridge.exe' }
$outDir = Join-Path $repo 'HPEtabs\output\live-verify\spike'
New-Item -ItemType Directory -Force $outDir | Out-Null
$pidFile = Join-Path $outDir 'bridge.pid'
$registry = Join-Path $outDir 'registry'
$env:PYTHONIOENCODING = 'utf-8'
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8

function Use-StartedBridge {
    if (-not (Test-Path $pidFile)) { throw 'no bridge started by this script (run -Action start first)' }
    $script:bridgePid = [int](Get-Content $pidFile -Raw).Trim()
    $p = Get-Process -Id $script:bridgePid -ErrorAction SilentlyContinue
    if (-not $p -or $p.ProcessName -ne 'HPEtabs.McpBridge') { throw "bridge pid $script:bridgePid is not running (or the pid was reused by '$($p.ProcessName)')" }
    if (-not (Find-BridgeWindow 3)) { throw 'bridge window not found' }
}

switch ($Action) {
    'start' {
        Assert-NoBridgeRunning
        if (Test-Path $registry) { Remove-Item -Recurse -Force $registry }
        New-Item -ItemType Directory -Force $registry | Out-Null
        $proc = Start-Bridge $BridgeExe
        $proc.Id | Set-Content $pidFile
        Start-Sleep -Seconds 2
        "self-check: $(Read-BridgeText 'SelfCheck')"
        if (-not (Test-Path "\\.\pipe\$script:PipeName")) { Invoke-BridgeButton 'ToggleListener' | Out-Null }
        if (-not (Wait-Pipe 30)) { throw 'pipe never appeared' }
        if (-not (Set-OptIn 'AllowExecution' $true)) { throw 'could not tick Allow AI code execution' }
        "bridge pid $($proc.Id) ready on $script:PipeName"
    }
    'attach' {
        Use-StartedBridge
        Assert-EtabsRunning
        if (-not (Invoke-BridgeButton 'Attach')) { throw 'Attach button not available' }
        $state = Wait-BridgeText 'AttachState' '^Attached to ETABS' 40
        "attach: $state"
        "warning: $(Read-BridgeText 'AttachWarning')"
        if (-not $state) { exit 1 }
    }
    'phase' {
        Use-StartedBridge
        & python (Join-Path $PSScriptRoot 'live-verify.py') $Exe --registry $registry --phase $Arg --out $outDir 2>&1 | ForEach-Object { "$_" }
        exit $LASTEXITCODE
    }
    'destructive' {
        Use-StartedBridge
        $on = $Arg -eq 'on'
        if (-not (Set-OptIn 'AllowDestructive' $on)) { throw "could not set Allow destructive operations to $on" }
    }
    'state' {
        Use-StartedBridge
        "attach: $(Read-BridgeText 'AttachState')"
        "warning: $(Read-BridgeText 'AttachWarning')"
        "self-check: $(Read-BridgeText 'SelfCheck')"
    }
    'stop' {
        if (Test-Path $pidFile) {
            $proc = Get-Process -Id ([int](Get-Content $pidFile -Raw).Trim()) -ErrorAction SilentlyContinue
            if ($proc -and $proc.ProcessName -ne 'HPEtabs.McpBridge') { throw "pid file names '$($proc.ProcessName)', not the bridge (pid reused); not stopping it" }
            Get-BridgeLogTail 60 | Set-Content -Encoding UTF8 (Join-Path $outDir 'bridge-log-tail.txt')
            Stop-Bridge $proc
            Remove-Item $pidFile -ErrorAction SilentlyContinue
        } else { 'no bridge pid file' }
    }
}
