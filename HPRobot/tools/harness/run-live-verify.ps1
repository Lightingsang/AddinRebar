# Live verification driver for Robot Structural Analysis MCP (powershell.exe)
param(
    [ValidateSet('detached', 'spike', 'bridge', 'seeds', 'full', 'all')][string]$Phase = 'detached',
    [string]$Exe = '',
    [string]$BridgeExe = '',
    [int]$Runs = 1
)

$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'harness-common.ps1')

$repo = (Resolve-Path (Join-Path $PSScriptRoot '..\..\..')).Path
if (-not $Exe) { $Exe = Join-Path $repo 'HPRobot\HPRobot.Mcp.Server\bin\Debug\net10.0\HPRobot.Mcp.Server.exe' }
if (-not $BridgeExe) { $BridgeExe = Join-Path $repo 'HPRobot\HPRobot.McpBridge\bin\Debug\net8.0-windows\HPRobot.McpBridge.exe' }

if (-not (Test-Path $Exe)) { throw "Server exe not found: $Exe" }
if (-not (Test-Path $BridgeExe)) { throw "Bridge exe not found: $BridgeExe" }

$verifyPy = Join-Path $PSScriptRoot 'live-verify.py'

try {
    Write-Host "=== Running Robot Live Verification Harness ===" -ForegroundColor Cyan
    Write-Host "Server Exe: $Exe"
    Write-Host "Bridge Exe: $BridgeExe"
    Write-Host "Phase:      $Phase"
    
    # Start Bridge
    $bPid = Start-BridgeProcess -ExePath $BridgeExe -Args "--autorun"
    Start-Sleep -Seconds 3

    # Run Python harness
    $pyArgs = @($verifyPy, "--exe", $Exe, "--phase", $Phase)
    & python @pyArgs
    $exitCode = $LASTEXITCODE
    if ($exitCode -ne 0) {
        Write-Host "[FAIL] Python harness returned exit code $exitCode" -ForegroundColor Red
        exit $exitCode
    } else {
        Write-Host "[PASS] Phase $Phase completed successfully!" -ForegroundColor Green
    }
}
finally {
    Stop-BridgeProcess
}
