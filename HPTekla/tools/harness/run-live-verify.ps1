# PowerShell wrapper to run Trimble Tekla Structures 2025.0 MCP live verification harness
[CmdletBinding(SupportsShouldProcess)]
param(
    [string]$Exe = '',
    [ValidatePattern('^[A-Fa-f,\s]+$')]
    [string]$Stages = 'A,B,C,D,E,F',
    [string]$Out = '',
    [switch]$Json,
    [switch]$SkipBuild
)

$ErrorActionPreference = 'Stop'

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..\..')).Path
if (-not $Exe) {
    $candidateDebug = Join-Path $repoRoot 'HPTekla\HPTekla.Mcp.Server\bin\Debug\net10.0\HPTekla.Mcp.Server.exe'
    $candidateRelease = Join-Path $repoRoot 'HPTekla\HPTekla.Mcp.Server\bin\Release\net10.0\HPTekla.Mcp.Server.exe'
    if (Test-Path $candidateDebug) {
        $Exe = $candidateDebug
    } elseif (Test-Path $candidateRelease) {
        $Exe = $candidateRelease
    } else {
        $Exe = $candidateDebug
    }
}

$verifyPy = Join-Path $PSScriptRoot 'live-verify.py'

if (-not $PSCmdlet.ShouldProcess("HPTekla MCP Server ($Exe)", "Execute live verification harness (Stages: $Stages)")) {
    Write-Host "[WHATIF] Would execute: python `"$verifyPy`" --exe `"$Exe`" --stages `"$Stages`""
    return
}

if (-not (Test-Path $Exe) -and -not $SkipBuild) {
    Write-Host "Building HPTekla.Mcp.Server..." -ForegroundColor Cyan
    dotnet build (Join-Path $repoRoot 'HPTekla\HPTekla.Mcp.Server\HPTekla.Mcp.Server.csproj') -c Debug
}

if (-not (Test-Path $Exe)) {
    throw "HPTekla.Mcp.Server.exe not found at '$Exe'. Build HPTekla.slnx or pass -Exe."
}

Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host "HPTekla MCP Live Verification Harness (Tekla Structures 2025)" -ForegroundColor Cyan
Write-Host "Server Exe: $Exe"
Write-Host "Stages:     $Stages"
Write-Host "==========================================================" -ForegroundColor Cyan

# Check Tekla Structures process status
$teklaProc = Get-Process "TeklaStructures" -ErrorAction SilentlyContinue
if (-not $teklaProc) {
    $teklaProc = Get-Process "tekla" -ErrorAction SilentlyContinue
}

if ($teklaProc) {
    Write-Host "[INFO] Detected active Tekla Structures process (PID: $($teklaProc.Id))" -ForegroundColor Green
} else {
    Write-Host "[WARN] TeklaStructures.exe is not currently running. Real bridge stages will test detached handling." -ForegroundColor Yellow
}

# Check Named Pipe status
$pipeName = "hptekla-mcp-2025"
$pipes = [System.IO.Directory]::GetFiles("\\.\pipe\", $pipeName)
if ($pipes.Length -gt 0) {
    Write-Host "[INFO] Named Pipe '$pipeName' is ACTIVE" -ForegroundColor Green
} else {
    Write-Host "[INFO] Named Pipe '$pipeName' is NOT active (Bridge detached mode)" -ForegroundColor Yellow
}

$pyArgs = @($verifyPy, "--exe", $Exe, "--stages", $Stages)
if ($Out) {
    $pyArgs += @("--out", $Out)
}
if ($Json) {
    $pyArgs += @("--json")
}

Write-Host "`nLaunching Python verification harness..." -ForegroundColor Cyan
& python @pyArgs
$exitCode = $LASTEXITCODE

if ($exitCode -eq 0) {
    Write-Host "`n[SUCCESS] All requested stages passed!" -ForegroundColor Green
    exit 0
} else {
    Write-Host "`n[FAILURE] Verification returned exit code $exitCode" -ForegroundColor Red
    exit $exitCode
}
