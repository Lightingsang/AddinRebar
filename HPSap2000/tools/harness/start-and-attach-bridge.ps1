# Starts HPSap2000.McpBridge on WinSta0\Default, starts listener, ticks Allow AI execution,
# attaches to SAP2000, runs context verification, and stays running as a daemon.

param(
    [string]$BridgeExe = '',
    [string]$ServerExe = ''
)

$ErrorActionPreference = 'Stop'
$repo = (Resolve-Path (Join-Path $PSScriptRoot '..\..\..')).Path

if (-not $BridgeExe) {
    $BridgeExe = Join-Path $repo 'HPSap2000\HPSap2000.McpBridge\bin\Debug\net8.0-windows\HPSap2000.McpBridge.exe'
}
if (-not $ServerExe) {
    $ServerExe = Join-Path $repo 'HPSap2000\output\HPSap2000.Mcp.Server\HPSap2000.Mcp.Server.exe'
}

. (Join-Path $PSScriptRoot 'harness-common.ps1')

Write-Host "=== Step 1: Checking prerequisites ==="
Assert-Sap2000Running

# Clean any stale bridge processes
$stale = Get-Process HPSap2000.McpBridge -ErrorAction SilentlyContinue
if ($stale) {
    Write-Host "Stopping stale bridge process(es)..."
    $stale | Stop-Process -Force -ErrorAction SilentlyContinue
    Start-Sleep -Seconds 1
}

Write-Host "=== Step 2: Starting Bridge on Desktop ==="
$proc = Start-Bridge $BridgeExe
Write-Host "Bridge PID: $($proc.Id)"

# Check self-check text
$selfCheck = Wait-BridgeText 'SelfCheck' '.+' 15
Write-Host "SelfCheck: $selfCheck"

Write-Host "=== Step 3: Starting Named Pipe Listener ==="
if (-not (Test-Path "\\.\pipe\$script:PipeName")) {
    Invoke-BridgeButton 'ToggleListener' | Out-Null
}
if (-not (Wait-Pipe 30)) { throw "Pipe \\.\pipe\$script:PipeName did not appear" }

Write-Host "=== Step 4: Enabling 'Allow AI code execution' ==="
if (-not (Set-OptIn 'AllowExecution' $true)) { throw "Could not tick Allow AI code execution" }

Write-Host "=== Step 5: Attaching to SAP2000 ==="
if (-not (Invoke-BridgeButton 'Attach')) { throw "Attach button not available" }
$state = Wait-BridgeText 'AttachState' '^Attached to SAP2000' 40
Write-Host "Attach status: $state"
$warn = Read-BridgeText 'AttachWarning'
if ($warn) { Write-Host "Attach warning: $warn" }

if (-not $state) {
    throw "Failed to attach to SAP2000"
}

Write-Host "=== Step 6: Testing get_sap2000_context via MCP Server ==="
$mcpCallPy = Join-Path $repo 'McpShared\tools\mcp-call.py'
$env:PYTHONIOENCODING = 'utf-8'
$env:PYTHONUTF8 = '1'

$contextJson = & python $mcpCallPy $ServerExe tools/call '{"name":"get_sap2000_context","arguments":{}}' 2>&1
Write-Host "Context Result:"
Write-Host $contextJson

Write-Host "`n=== SUCCESS: HPSap2000 MCP Bridge is running, attached and ready for AI interaction! ==="
Write-Host "Bridge window is active on your desktop. Keeping daemon alive..."

while ($true) {
    Start-Sleep -Seconds 10
    $proc.Refresh()
    if ($proc.HasExited) {
        Write-Host "Bridge process exited by user (code $($proc.ExitCode)). Daemon ending."
        break
    }
}
