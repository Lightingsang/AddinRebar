# PowerShell Runner for HPRebar E2E Acceptance Test Suite
param(
    [ValidateSet("All", "Tier1", "Tier2", "Tier3", "Tier4")]
    [string]$Tier = "All"
)

$ErrorActionPreference = "Stop"
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path

Push-Location $scriptDir
try {
    Write-Host "============================================================" -ForegroundColor Cyan
    Write-Host "  HPRebar Acceptance Test Suite (TCVN 5574:2018 & 3D Beam)  " -ForegroundColor Cyan
    Write-Host "============================================================" -ForegroundColor Cyan

    $filter = switch ($Tier) {
        "All"   { "--filter-namespace HPRebar.Core.Tests.E2E.Tiers" }
        "Tier1" { "--filter-class *Tier1*" }
        "Tier2" { "--filter-class *Tier2*" }
        "Tier3" { "--filter-class *Tier3*" }
        "Tier4" { "--filter-class *Tier4*" }
    }

    Write-Host "Running Target: $Tier with filter: $filter" -ForegroundColor Yellow
    dotnet test HPRebar.Core.Tests $filter.Split(' ')

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
