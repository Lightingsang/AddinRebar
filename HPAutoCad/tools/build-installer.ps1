<#
.SYNOPSIS
    Builds standalone MSI installers (SingleUser and MultiUser) for HPAutoCad (AI MCP Bridge & KMZ / HPGeoLink).

.DESCRIPTION
    Compiles HPAutoCad for AutoCAD 2026 in Release configuration, publishes HPAutoCad.Mcp.Server as self-contained,
    stages the unified HPAutoCad.bundle structure, and generates SingleUser & MultiUser .msi packages via WiX / WixSharp.

.PARAMETER Version
    The version string for the installer (default: yyyy.M.d or semantic version).

.PARAMETER Configuration
    The MSBuild configuration to build (default: Release).

.PARAMETER ProductName
    The product name for the installer and Control Panel display (default: HPAutoCad).

.PARAMETER OutputDir
    The output directory for the generated .msi packages (default: HPAutoCad\output).

.EXAMPLE
    .\build-installer.ps1
    .\build-installer.ps1 -Version 0.3.0
    .\build-installer.ps1 -Version 2026.10.1
#>
[CmdletBinding()]
param(
    [string]$Version = "",
    [string]$Configuration = "Release",
    [string]$ProductName = "HPAutoCad",
    [string]$OutputDir = ""
)

$ErrorActionPreference = "Stop"

$ToolsDir = $PSScriptRoot
$HpAutoCadDir = (Resolve-Path "$ToolsDir\..").Path
$RepoRoot = (Resolve-Path "$HpAutoCadDir\..").Path

# Auto-detect version from PackageContents.xml if not specified
if ([string]::IsNullOrWhiteSpace($Version)) {
    $ManifestSource = Join-Path $HpAutoCadDir "HPAutoCad.Loader\Bundle\PackageContents.xml"
    if (Test-Path $ManifestSource) {
        $content = Get-Content $ManifestSource -Raw
        if ($content -match 'AppVersion="([^"]+)"') {
            $Version = $matches[1]
        }
    }
    if ([string]::IsNullOrWhiteSpace($Version)) {
        $Version = "0.3.0"
    }
}

if ([string]::IsNullOrWhiteSpace($OutputDir)) {
    $OutputDir = Join-Path $HpAutoCadDir "output"
} else {
    $OutputDir = [System.IO.Path]::GetFullPath($OutputDir)
}

Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host " Building $ProductName MSI Installer ($Configuration)" -ForegroundColor Cyan
Write-Host " Version: $Version" -ForegroundColor Cyan
Write-Host " Output:  $OutputDir" -ForegroundColor Cyan
Write-Host "==========================================================" -ForegroundColor Cyan

# 1. Ensure WiX Toolset is available
$WixExe = Get-Command "wix.exe" -ErrorAction SilentlyContinue | Select-Object -ExpandProperty Source -ErrorAction SilentlyContinue
$LocalWixDir = Join-Path $HpAutoCadDir ".wix"
$HpRebarWixExe = Join-Path $RepoRoot "HPRebar\.wix\wix.exe"

if (-not $WixExe) {
    if (Test-Path "$LocalWixDir\wix.exe") {
        $WixExe = "$LocalWixDir\wix.exe"
    } elseif (Test-Path $HpRebarWixExe) {
        $WixExe = $HpRebarWixExe
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

# 2. Compile HPAutoCad unified bundle projects
Write-Host "`n--> Compiling HPAutoCad projects ($Configuration)..." -ForegroundColor Yellow
$LoaderProj = Join-Path $HpAutoCadDir "HPAutoCad.Loader\HPAutoCad.Loader.csproj"
dotnet build $LoaderProj -c $Configuration -p:DeployBundle=false
if ($LASTEXITCODE -ne 0) {
    throw "Failed to compile HPAutoCad.Loader ($Configuration)"
}

# 3. Publish HPAutoCad.Mcp.Server (self-contained win-x64)
Write-Host "`n--> Publishing HPAutoCad.Mcp.Server (self-contained, win-x64)..." -ForegroundColor Yellow
$ServerProj = Join-Path $HpAutoCadDir "HPAutoCad.Mcp.Server\HPAutoCad.Mcp.Server.csproj"
$ServerPublishDir = Join-Path $HpAutoCadDir "output\staging\server"
if (Test-Path $ServerPublishDir) {
    Remove-Item -Recurse -Force $ServerPublishDir
}
dotnet publish $ServerProj -c $Configuration -r win-x64 --self-contained true -p:PublishSingleFile=true -o $ServerPublishDir
if ($LASTEXITCODE -ne 0) {
    throw "Failed to publish HPAutoCad.Mcp.Server"
}

# 4. Assemble the Staging Bundle structure
Write-Host "`n--> Assembling staging bundle..." -ForegroundColor Yellow
$StagingBundle = Join-Path $HpAutoCadDir "output\staging\HPAutoCad.bundle"
if (Test-Path $StagingBundle) {
    Remove-Item -Recurse -Force $StagingBundle
}

$AppStaging = Join-Path $StagingBundle "Contents\App"
$BridgeStaging = Join-Path $StagingBundle "Contents\Bridge"
$ServerStaging = Join-Path $StagingBundle "Contents\Server"
$ContentsStaging = Join-Path $StagingBundle "Contents"

New-Item -ItemType Directory -Force -Path $AppStaging | Out-Null
New-Item -ItemType Directory -Force -Path $BridgeStaging | Out-Null
New-Item -ItemType Directory -Force -Path $ServerStaging | Out-Null

# 4.1 Copy Manifest and update version
$ManifestSource = Join-Path $HpAutoCadDir "HPAutoCad.Loader\Bundle\PackageContents.xml"
$ManifestDest = Join-Path $StagingBundle "PackageContents.xml"
$ManifestContent = Get-Content $ManifestSource -Raw
$ManifestContent = [regex]::Replace($ManifestContent, 'AppVersion="[^"]*"', "AppVersion=""$Version""")
Set-Content -Path $ManifestDest -Value $ManifestContent -Encoding utf8

# 4.2 Copy Loaders into Contents/
$LoaderBin = Join-Path $HpAutoCadDir "HPAutoCad.Loader\bin\$Configuration\net8.0-windows"
$BridgeLoaderBin = Join-Path $HpAutoCadDir "HPAutoCad.McpBridge.Loader\bin\$Configuration\net8.0-windows"

Copy-Item "$LoaderBin\HPAutoCad.Loader.dll" -Destination "$ContentsStaging\" -Force
if (Test-Path "$LoaderBin\HPAutoCad.Loader.pdb") {
    Copy-Item "$LoaderBin\HPAutoCad.Loader.pdb" -Destination "$ContentsStaging\" -Force
}

Copy-Item "$BridgeLoaderBin\HPAutoCad.McpBridge.Loader.dll" -Destination "$ContentsStaging\" -Force
if (Test-Path "$BridgeLoaderBin\HPAutoCad.McpBridge.Loader.pdb") {
    Copy-Item "$BridgeLoaderBin\HPAutoCad.McpBridge.Loader.pdb" -Destination "$ContentsStaging\" -Force
}

# 4.3 Copy App (HPGeoLink / KMZ payload)
$AppBin = Join-Path $HpAutoCadDir "HPAutoCad\bin\$Configuration\net8.0-windows"
Copy-Item "$AppBin\*" -Destination "$AppStaging\" -Recurse -Force

# 4.4 Copy Bridge (MCP Bridge payload)
$BridgeBin = Join-Path $HpAutoCadDir "HPAutoCad.McpBridge\bin\$Configuration\net8.0-windows"
Copy-Item "$BridgeBin\*" -Destination "$BridgeStaging\" -Recurse -Force

# 4.5 Copy Server (Self-contained stdio MCP server)
Copy-Item "$ServerPublishDir\*" -Destination "$ServerStaging\" -Recurse -Force

Write-Host "--> Staged bundle contents at: $StagingBundle" -ForegroundColor Green

# 5. Compile Installer Generator project
Write-Host "`n--> Compiling Installer generator project..." -ForegroundColor Yellow
$InstallerProj = Join-Path $HpAutoCadDir "install\Installer.csproj"
dotnet build $InstallerProj -c Release
if ($LASTEXITCODE -ne 0) {
    throw "Failed to compile Installer project"
}

# 6. Generate MSI installers via WiX / WixSharp
Write-Host "`n--> Packaging MSI installers via WiX / WixSharp..." -ForegroundColor Yellow
$InstallerExe = Join-Path $HpAutoCadDir "install\bin\Release\net10.0-windows\Installer.exe"

Push-Location $HpAutoCadDir
try {
    & $InstallerExe "--name=$ProductName" "--outdir=$OutputDir" $Version $StagingBundle
    if ($LASTEXITCODE -ne 0) {
        throw "Failed to generate MSI installers"
    }
} finally {
    Pop-Location
}

# 7. Clean up staging directory
if (Test-Path (Join-Path $HpAutoCadDir "output\staging")) {
    Remove-Item -Recurse -Force (Join-Path $HpAutoCadDir "output\staging") -ErrorAction SilentlyContinue
}

# 8. Report generated artifacts
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
