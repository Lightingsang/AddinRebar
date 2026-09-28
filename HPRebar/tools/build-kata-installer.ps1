<#
.SYNOPSIS
    Builds the standalone MSI installer for HPRebar containing only the Kata Export tool.

.DESCRIPTION
    Compiles HPRebar for Revit 2026 with the KATA_ONLY conditional compilation symbol,
    ensures WiX Toolset is configured, and builds SingleUser & MultiUser .msi packages.

.PARAMETER Version
    The version string for the installer. Defaults to today's date (yyyy.M.d): the Kata-only MSI shares its upgrade
    code with the full HPRebar MSI (same add-in files and AddInId, so the two never coexist); a date version keeps
    it from being refused as a downgrade on a machine with a recent HPRebar.

.PARAMETER Configuration
    The MSBuild configuration to build (default: Release.R26).

.PARAMETER ProductName
    The product name for the installer (default: HPRebar-KataExport).

.EXAMPLE
    .\build-kata-installer.ps1
    .\build-kata-installer.ps1 -Version 2026.1.0
#>
[CmdletBinding()]
param(
    [string]$Version = (Get-Date -Format "yyyy.M.d"),
    [string]$Configuration = "Release.R26",
    [string]$ProductName = "HPRebar-KataExport"
)

$ErrorActionPreference = "Stop"

$ToolsDir = $PSScriptRoot
$HpRebarDir = (Resolve-Path "$ToolsDir\..").Path
$RepoRoot = (Resolve-Path "$HpRebarDir\..").Path

Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host " Building $ProductName MSI Installer ($Configuration)" -ForegroundColor Cyan
Write-Host " Version: $Version" -ForegroundColor Cyan
Write-Host "==========================================================" -ForegroundColor Cyan

# 1. Ensure WiX Toolset is available
$WixExe = Get-Command "wix.exe" -ErrorAction SilentlyContinue | Select-Object -ExpandProperty Source -ErrorAction SilentlyContinue
$LocalWixDir = Join-Path $HpRebarDir ".wix"

if (-not $WixExe) {
    if (Test-Path "$LocalWixDir\wix.exe") {
        $WixExe = "$LocalWixDir\wix.exe"
    } else {
        Write-Host "--> Installing WiX Toolset locally into $LocalWixDir..." -ForegroundColor Yellow
        New-Item -ItemType Directory -Force -Path $LocalWixDir | Out-Null
        dotnet tool install wix --tool-path $LocalWixDir
        $WixExe = "$LocalWixDir\wix.exe"
    }
}

$WixDir = Split-Path $WixExe -Parent
$env:PATH = "$WixDir;$env:PATH"

Write-Host "--> WiX tool located at: $WixExe" -ForegroundColor Green
& $WixExe eula accept wix7 2>$null
& $WixExe extension add -g "WixToolset.UI.wixext/7.0.0" 2>$null

# 2. Build HPRebar with KataOnly=true and DeployAddin=false
Write-Host "`n--> Compiling HPRebar ($Configuration, KataOnly=true)..." -ForegroundColor Yellow
$HpRebarProj = Join-Path $HpRebarDir "HPRebar\HPRebar.csproj"
dotnet build $HpRebarProj -c $Configuration -p:KataOnly=true -p:DeployAddin=false
if ($LASTEXITCODE -ne 0) {
    throw "Failed to compile HPRebar ($Configuration) with KataOnly=true"
}

# 3. Build Installer Generator project
Write-Host "`n--> Compiling Installer generator project..." -ForegroundColor Yellow
$InstallerProj = Join-Path $HpRebarDir "install\Installer.csproj"
dotnet build $InstallerProj -c Release
if ($LASTEXITCODE -ne 0) {
    throw "Failed to compile Installer project"
}

# 4. Generate MSI installers
Write-Host "`n--> Packaging MSI installers via WiX / WixSharp..." -ForegroundColor Yellow
$InstallerExe = Join-Path $HpRebarDir "install\bin\Release\net10.0-windows\Installer.exe"
$PublishDir = Join-Path $HpRebarDir "HPRebar\bin\$Configuration\publish"

if (-not (Test-Path $PublishDir)) {
    throw "Publish directory not found: $PublishDir"
}

Push-Location $HpRebarDir
try {
    & $InstallerExe "--name=$ProductName" $Version $PublishDir
    if ($LASTEXITCODE -ne 0) {
        throw "Failed to generate MSI installers"
    }
} finally {
    Pop-Location
}

# 5. Report generated artifacts
$OutputDir = Join-Path $HpRebarDir "output"
$MsiFiles = Get-ChildItem -Path $OutputDir -Filter "$ProductName-*.msi" | Sort-Object LastWriteTime -Descending

Write-Host "`n==========================================================" -ForegroundColor Green
Write-Host " Build & Packaging Completed Successfully!" -ForegroundColor Green
Write-Host " Output MSI Files:" -ForegroundColor Green
foreach ($file in $MsiFiles) {
    $sizeMb = [math]::Round($file.Length / 1MB, 2)
    Write-Host " - $($file.Name) ($sizeMb MB)" -ForegroundColor Cyan
    Write-Host "   Path: $($file.FullName)" -ForegroundColor DarkGray
}
Write-Host "==========================================================" -ForegroundColor Green
