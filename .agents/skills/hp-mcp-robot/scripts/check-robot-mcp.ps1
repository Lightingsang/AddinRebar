param(
    [string]$PipeName = "hprobot-mcp-2026"
)

Write-Host "=== HPRobot MCP Diagnostic Check ===" -ForegroundColor Cyan

# 1. Check Robot 2026 Process
$robotProc = Get-Process -Name "robot" -ErrorAction SilentlyContinue
if ($robotProc) {
    Write-Host "[OK] Robot Structural Analysis 2026 is running (PID: $($robotProc.Id))" -ForegroundColor Green
} else {
    Write-Host "[WARN] Robot process is not currently running." -ForegroundColor Yellow
}

# 2. Check Bridge Process
$bridgeProc = Get-Process -Name "HPRobot.McpBridge" -ErrorAction SilentlyContinue
if ($bridgeProc) {
    Write-Host "[OK] HPRobot.McpBridge is running (PID: $($bridgeProc.Id))" -ForegroundColor Green
} else {
    Write-Host "[WARN] HPRobot.McpBridge process is not currently running." -ForegroundColor Yellow
}

# 3. Check Named Pipe
$pipePath = "\\.\pipe\$PipeName"
if ([System.IO.File]::Exists($pipePath)) {
    Write-Host "[OK] Named Pipe '$PipeName' is active and listening." -ForegroundColor Green
} else {
    Write-Host "[INFO] Named Pipe '$PipeName' not detected (Bridge listener may be stopped)." -ForegroundColor Yellow
}

Write-Host "Diagnostic complete." -ForegroundColor Cyan
