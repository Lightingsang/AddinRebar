param(
    [string]$Configuration = "Debug",
    [switch]$SkipBuild,
    [bool]$InstallToUserAddins = $true
)

$ErrorActionPreference = "Stop"
$RepoRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$HPDceProRoot = Join-Path $RepoRoot "HPDcePro"
$SolutionPath = Join-Path $HPDceProRoot "HPDcePro.slnx"

if (-not $SkipBuild) {
    Write-Host "[HPDcePro] Building $SolutionPath ($Configuration)..." -ForegroundColor Cyan
    dotnet build $SolutionPath -c $Configuration --disable-build-servers -m:1
    if ($LASTEXITCODE -ne 0) {
        throw "Build failed with exit code $LASTEXITCODE"
    }
}

$AutoCadBin = Join-Path $HPDceProRoot "src\HPDcePro.AutoCad\bin\$Configuration\net8.0-windows"
$BundleRoot = Join-Path $PSScriptRoot "HPDcePro.bundle"
$BundleContents = Join-Path $BundleRoot "Contents"
New-Item -ItemType Directory -Path $BundleContents -Force | Out-Null

Copy-Item (Join-Path $AutoCadBin "*.dll") -Destination $BundleContents -Force
Copy-Item (Join-Path $AutoCadBin "*.json") -Destination $BundleContents -Force -ErrorAction SilentlyContinue
Copy-Item (Join-Path $HPDceProRoot "src\HPDcePro.AutoCad\HPDcePro.lsp") -Destination $BundleContents -Force

$RevitBin = Join-Path $HPDceProRoot "src\HPDcePro.Revit\bin\$Configuration\net8.0-windows"
$RevitDeployDir = Join-Path $PSScriptRoot "Revit2026\HPDcePro"
New-Item -ItemType Directory -Path $RevitDeployDir -Force | Out-Null
Copy-Item (Join-Path $RevitBin "*.dll") -Destination $RevitDeployDir -Force
Copy-Item (Join-Path $PSScriptRoot "HPDcePro.Revit.addin") -Destination (Join-Path $PSScriptRoot "Revit2026\HPDcePro.Revit.addin") -Force

$McpBin = Join-Path $HPDceProRoot "src\HPDcePro.Mcp\bin\$Configuration\net8.0"
$McpDeployDir = Join-Path $PSScriptRoot "McpServer"
New-Item -ItemType Directory -Path $McpDeployDir -Force | Out-Null
Copy-Item (Join-Path $McpBin "*") -Destination $McpDeployDir -Recurse -Force

$DesktopBin = Join-Path $HPDceProRoot "src\HPDcePro.Desktop\bin\$Configuration\net8.0-windows"
$DesktopDeployDir = Join-Path $PSScriptRoot "Desktop"
New-Item -ItemType Directory -Path $DesktopDeployDir -Force | Out-Null
Copy-Item (Join-Path $DesktopBin "*") -Destination $DesktopDeployDir -Recurse -Force

if ($InstallToUserAddins) {
    $AcadPluginsDir = Join-Path $env:APPDATA "Autodesk\ApplicationPlugins\HPDcePro.bundle"
    try {
        New-Item -ItemType Directory -Path $AcadPluginsDir -Force | Out-Null
        Copy-Item (Join-Path $BundleRoot "*") -Destination $AcadPluginsDir -Recurse -Force
        Write-Host "  - Installed AutoCAD Bundle -> $AcadPluginsDir" -ForegroundColor Green
    } catch {
        Write-Host "  - AutoCAD Bundle in use (skipped live copy): $($_.Exception.Message)" -ForegroundColor Yellow
    }

    $RevitAddins2026 = Join-Path $env:APPDATA "Autodesk\Revit\Addins\2026"
    if (Test-Path $RevitAddins2026) {
        try {
            $RevitTargetSub = Join-Path $RevitAddins2026 "HPDcePro"
            New-Item -ItemType Directory -Path $RevitTargetSub -Force | Out-Null
            Copy-Item (Join-Path $RevitDeployDir "*") -Destination $RevitTargetSub -Recurse -Force
            Copy-Item (Join-Path $PSScriptRoot "HPDcePro.Revit.addin") -Destination (Join-Path $RevitAddins2026 "HPDcePro.Revit.addin") -Force
            Write-Host "  - Installed Revit 2026 Add-In -> $RevitAddins2026" -ForegroundColor Green
        } catch {
            Write-Host "  - Revit 2026 Add-In in use (skipped live copy): $($_.Exception.Message)" -ForegroundColor Yellow
        }
    }
}

Write-Host "[HPDcePro] Packaging complete:" -ForegroundColor Green
Write-Host "  - Desktop App    : $(Join-Path $DesktopDeployDir 'HPDcePro.Desktop.exe')"
Write-Host "  - AutoCAD Bundle : $BundleContents"
Write-Host "  - Revit 2026     : $RevitDeployDir"
Write-Host "  - MCP Server     : $(Join-Path $McpDeployDir 'HPDcePro.Mcp.exe')"
