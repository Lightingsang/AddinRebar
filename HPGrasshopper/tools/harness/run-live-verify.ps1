<#
.SYNOPSIS
    Automated Live Verification Harness for HPGrasshopper MCP Subsystem.
.DESCRIPTION
    Drives HPGrasshopper.Mcp.Server.exe over stdio against HPGrasshopper.McpBridge
    (or mock_bridge.py in headless/mock mode) across 7 stages (A-G), verifying
    24 tools, protocol handshake, registry lifecycle, dual safety, and error handling.
.PARAMETER Exe
    Explicit path to HPGrasshopper.Mcp.Server.exe.
.PARAMETER Stages
    Comma-separated list of stages to run (A,B,C,D,E,F,G).
.PARAMETER Mock
    Runs against in-memory mock bridge (for headless CI without Revit/Rhino).
.PARAMETER Live
    Forces live mode against running Revit 2026 + Rhino.Inside session.
.PARAMETER Out
    Directory for logs and JSON summaries. Defaults to HPGrasshopper/output/live-verify.
.PARAMETER SkipBuild
    Skips dotnet build step.
.PARAMETER TimeoutSec
    Execution timeout in seconds.
.EXAMPLE
    powershell.exe -ExecutionPolicy Bypass -File tools/harness/run-live-verify.ps1 -Mock
#>
[CmdletBinding()]
param(
    [string]$Exe = '',
    [string]$Stages = 'A,B,C,D,E,F,G',
    [switch]$Mock,
    [switch]$Live,
    [string]$Out = '',
    [switch]$SkipBuild,
    [int]$TimeoutSec = 300
)

$ErrorActionPreference = 'Stop'
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = Resolve-Path (Join-Path $scriptDir '..\..\..') | Select-Object -ExpandProperty Path
$grasshopperDir = Join-Path $repoRoot 'HPGrasshopper'

Write-Host "=================================================================" -ForegroundColor Cyan
Write-Host "  HPGrasshopper MCP Automated Live Verification Harness" -ForegroundColor Cyan
Write-Host "=================================================================" -ForegroundColor Cyan

# 1. Output directory & logs setup
if (-not $Out) {
    $Out = Join-Path $grasshopperDir 'output\live-verify'
}
New-Item -ItemType Directory -Force $Out | Out-Null
$registryDir = Join-Path $Out 'registry'
if (Test-Path $registryDir) {
    Remove-Item -Recurse -Force $registryDir -ErrorAction SilentlyContinue
}
New-Item -ItemType Directory -Force $registryDir | Out-Null

$logPath = Join-Path $Out 'live-verify.log'
if (Test-Path $logPath) {
    Remove-Item -Force $logPath -ErrorAction SilentlyContinue
}

# 2. Server binary resolution
if (-not $Exe) {
    $candidateRelease = Join-Path $grasshopperDir 'HPGrasshopper.Mcp.Server\bin\Release\net10.0\HPGrasshopper.Mcp.Server.exe'
    $candidateDebug = Join-Path $grasshopperDir 'HPGrasshopper.Mcp.Server\bin\Debug\net10.0\HPGrasshopper.Mcp.Server.exe'
    if (Test-Path $candidateRelease) {
        $Exe = $candidateRelease
    }
    elseif (Test-Path $candidateDebug) {
        $Exe = $candidateDebug
    }
}

if ((-not $Exe -or -not (Test-Path $Exe)) -and -not $SkipBuild) {
    Write-Host "Building HPGrasshopper solution in Release configuration..." -ForegroundColor Yellow
    dotnet build (Join-Path $grasshopperDir 'HPGrasshopper.slnx') -c Release
    $Exe = Join-Path $grasshopperDir 'HPGrasshopper.Mcp.Server\bin\Release\net10.0\HPGrasshopper.Mcp.Server.exe'
}

if (-not (Test-Path $Exe)) {
    throw "HPGrasshopper.Mcp.Server.exe not found at '$Exe'. Please build the solution first."
}

Write-Host "Using Server binary: $Exe" -ForegroundColor Green

# 3. Pipe detection & Mock Bridge orchestration
$pipeName = '\\.\pipe\hpgrasshopper-mcp-2026'
$pipeExists = [System.IO.Directory]::GetFiles('\\.\pipe\') -contains $pipeName

$mockProc = $null
if ($Mock -or (-not $pipeExists -and -not $Live)) {
    Write-Host "Starting mock bridge daemon on $pipeName..." -ForegroundColor Yellow
    $mockProc = Start-Process python -ArgumentList @('mock_bridge.py', '--pipe', $pipeName, '--timeout', $TimeoutSec) -WorkingDirectory $scriptDir -PassThru -NoNewWindow
    $waitSw = [System.Diagnostics.Stopwatch]::StartNew()
    while ($waitSw.ElapsedMilliseconds -lt 5000) {
        if ([System.IO.Directory]::GetFiles('\\.\pipe\') -contains $pipeName) {
            break
        }
        Start-Sleep -Milliseconds 200
    }
}

# 4. Invoke live-verify.py test driver
$verifyScript = Join-Path $scriptDir 'live-verify.py'
$env:PYTHONIOENCODING = 'utf-8'
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8

Write-Host "`nExecuting verification stages: $Stages" -ForegroundColor Cyan
$exitCode = 0
try {
    $pyArgs = @($verifyScript, $Exe, '--registry', $registryDir, '--out', $Out, '--stages', $Stages)
    & python @pyArgs 2>&1 | Tee-Object -FilePath $logPath
    $exitCode = $LASTEXITCODE
}
finally {
    if ($mockProc -and -not $mockProc.HasExited) {
        Write-Host "Stopping mock bridge daemon (PID $($mockProc.Id))..." -ForegroundColor Gray
        Stop-Process -Id $mockProc.Id -Force -ErrorAction SilentlyContinue
    }
}

# 5. Summary & exit reporting
Write-Host "`n-----------------------------------------------------------------" -ForegroundColor Cyan
$summaryJsonPath = Join-Path $Out 'run-summary.json'
if (Test-Path $summaryJsonPath) {
    $summary = Get-Content $summaryJsonPath -Raw | ConvertFrom-Json
    Write-Host "Summary: Passed=$($summary.passed), Failed=$($summary.failed.Count), Total=$($summary.total)" -ForegroundColor $(if ($exitCode -eq 0) { 'Green' } else { 'Red' })
    if ($summary.failed.Count -gt 0) {
        Write-Host "Failed checks: $($summary.failed -join ', ')" -ForegroundColor Red
    }
}

if ($exitCode -eq 0) {
    Write-Host "LIVE VERIFICATION PASSED (ExitCode: 0)" -ForegroundColor Green
}
else {
    Write-Host "LIVE VERIFICATION FAILED (ExitCode: $exitCode)" -ForegroundColor Red
}

exit $exitCode
