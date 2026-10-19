# PowerShell Runner for HPDynamo MCP Acceptance Test Suite (Tiers 1-4)
param(
    [ValidateSet("All", "Tier1", "Tier2", "Tier3", "Tier4")]
    [string]$Tier = "All"
)

$ErrorActionPreference = "Stop"
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$projectPath = "$scriptDir\HPDynamo.E2E.Tests\HPDynamo.E2E.Tests.csproj"
$binPath = "$scriptDir\HPDynamo.E2E.Tests\bin\Debug\net10.0\HPDynamo.E2E.Tests.exe"

Push-Location $scriptDir
try {
    Write-Host "============================================================" -ForegroundColor Cyan
    Write-Host "  HPDynamo MCP Subsystem Acceptance Test Suite (Revit 2026) " -ForegroundColor Cyan
    Write-Host "============================================================" -ForegroundColor Cyan

    if (-not (Test-Path $binPath)) {
        Write-Host "Building test project: $projectPath..." -ForegroundColor Yellow
        dotnet build $projectPath -m:1 -nodeReuse:false
        if ($LASTEXITCODE -ne 0) {
            Write-Host "Build failed with exit code $LASTEXITCODE" -ForegroundColor Red
            exit $LASTEXITCODE
        }
    }

    $filterArg = switch ($Tier) {
        "All"   { @() }
        "Tier1" { @("--filter-class", "*Tier1*") }
        "Tier2" { @("--filter-class", "*Tier2*") }
        "Tier3" { @("--filter-class", "*Tier3*") }
        "Tier4" { @("--filter-class", "*Tier4*") }
    }

    Write-Host "Running Acceptance Tier: $Tier" -ForegroundColor Yellow
    if ($filterArg.Length -gt 0) {
        & $binPath $filterArg
    } else {
        & $binPath
    }

    if ($LASTEXITCODE -eq 0) {
        Write-Host "------------------------------------------------------------" -ForegroundColor Green
        Write-Host "  Result: SUCCESS (100% Passed)                             " -ForegroundColor Green
        Write-Host "------------------------------------------------------------" -ForegroundColor Green
    } else {
        Write-Host "------------------------------------------------------------" -ForegroundColor Red
        Write-Host "  Result: FAILED (Exit Code: $LASTEXITCODE)                 " -ForegroundColor Red
        Write-Host "------------------------------------------------------------" -ForegroundColor Red
        exit $LASTEXITCODE
    }
}
finally {
    Pop-Location
}
