# Smoke test of the published stdio server against the real bridge: starts Roamer.exe with the sample model,
# brings the pipe up and ticks the execution opt-in (same steps as run-bridge-unattended.ps1), then runs
# server-smoke.py — one stdio session with the published HPNavis.Mcp.Server.exe on an isolated registry root:
# initialize, tools/list, get_navis_context, read-only execute, dry-run edit, heavy refusal, inspect_type,
# search_tools. This is the net10 pipe client (PipeOptions.CurrentUserOnly) meeting the net48 listener
# (PipeSecurity owner SID); the bridge log is checked for UnauthorizedAccess afterwards.
#
#   powershell.exe -ExecutionPolicy Bypass -File HPNavis/tools/harness/run-server-smoke.ps1 [-Exe <path>] [-Model <.nwd>]
param(
    [string]$Exe = '',
    [string]$Model = ''
)

if ($PSVersionTable.PSEdition -ne 'Desktop') {
    $ps = Join-Path $env:SystemRoot 'System32\WindowsPowerShell\v1.0\powershell.exe'
    $argList = @('-NoProfile', '-ExecutionPolicy', 'Bypass', '-File', $PSCommandPath)
    if ($Exe) { $argList += @('-Exe', $Exe) }
    if ($Model) { $argList += @('-Model', $Model) }
    & $ps @argList
    exit $LASTEXITCODE
}

$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'harness-common.ps1')

$repo = (Resolve-Path (Join-Path $PSScriptRoot '..\..\..')).Path
if (-not $Exe) { $Exe = Join-Path $repo 'HPNavis\output\HPNavis.Mcp.Server\HPNavis.Mcp.Server.exe' }
if (-not (Test-Path $Exe)) { throw "server exe not found: $Exe - publish first (see HPNavis/README.md)" }
if (-not $Model) { $Model = Join-Path $script:NavisDir 'Samples\gatehouse\gatehouse_pub.nwd' }
$smoke = Join-Path $PSScriptRoot 'server-smoke.py'
$outDir = Join-Path $repo 'HPNavis\output\spike'
New-Item -ItemType Directory -Force $outDir | Out-Null
$iso = Join-Path $env:TEMP ('hpnavis-smoke-' + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Force $iso | Out-Null
$env:PYTHONIOENCODING = 'utf-8'

Assert-NoNavisworksRunning
Assert-PluginDeployed

$ok = $false
$proc = $null
$logPath = Join-Path $outDir 'server-smoke.log'
try {
    $proc = Start-NavisworksWithModel $Model
    Start-Sleep -Seconds 3
    if (-not (Find-BridgeWindow 12)) { throw 'bridge window not found' }
    if (-not (Test-Path "\\.\pipe\$script:PipeName")) { Invoke-BridgeButton 'ToggleListener' | Out-Null }
    if (-not (Wait-Pipe 30)) { throw 'pipe never appeared' }
    if (-not (Set-OptIn 'AllowExecution' $true)) { throw 'could not tick Allow AI code execution' }
    $logStart = Get-Date

    $ErrorActionPreference = 'Continue'
    $out = & python $smoke $Exe --registry $iso 2>&1 | ForEach-Object { "$_" }
    $exit = $LASTEXITCODE
    $ErrorActionPreference = 'Stop'
    $out | Set-Content -Path $logPath -Encoding UTF8
    $out | ForEach-Object { Write-Host "  $_" }

    $bridgeLog = Get-BridgeLogTail 400 | Where-Object { $_ -match '^(\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2})' -and [datetime]::ParseExact($Matches[1], 'yyyy-MM-dd HH:mm:ss', $null) -ge $logStart.AddSeconds(-1) }
    $unauthorized = @($bridgeLog | Where-Object { $_ -like '*UnauthorizedAccess*' }).Count
    $connected = @($bridgeLog | Where-Object { $_ -like '*MCP server connected*' }).Count
    Write-Host "bridge log: server connected x$connected, UnauthorizedAccess x$unauthorized"
    $ok = ($exit -eq 0) -and ($unauthorized -eq 0) -and ($connected -ge 1)
}
catch {
    Write-Host "smoke failed: $($_.Exception.Message)"
    $ok = $false
}
finally {
    Stop-Navisworks $proc
    Remove-Item -Recurse -Force $iso -ErrorAction SilentlyContinue
}

Write-Host "`nserver smoke => $(if ($ok) { 'PASS' } else { 'FAIL' })  (log: $logPath)"
exit $(if ($ok) { 0 } else { 1 })
