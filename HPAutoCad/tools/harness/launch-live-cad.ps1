# Launches AutoCAD 2026 with HPAutoCad bridge, answers SECURELOAD, waits for named pipe,
# enables AI code execution, and leaves AutoCAD open for interactive MCP testing.
param([int]$TimeoutSec = 300)

. (Join-Path $PSScriptRoot 'harness-common.ps1')

$scr = Join-Path $PSScriptRoot 'bridge.scr'
Write-Host "Starting AutoCAD 2026 with bridge script: $scr..."

$p = Start-AcadWithBridge $scr $TimeoutSec

if ($pipeUp) {
    Write-Host "AutoCAD 2026 is UP! PID: $($p.Id). Named pipe \\.\pipe\hpautocad-mcp-2026 is ACTIVE."
    Start-Sleep -Seconds 3
    $optInSuccess = Set-OptIn $true
    Write-Host "AI Code Execution Opt-in enabled: $optInSuccess"
} else {
    Write-Host "ERROR: Named pipe did not come up within $TimeoutSec seconds."
    exit 1
}
