# Deploys HPDynamo.McpBridge into Revit 2026 and Dynamo 3.7 user folders.
# Usage:
#   pwsh HPDynamo/tools/deploy-bridge.ps1 [-RevitVersion 2026] [-Configuration Debug]

param(
    [int]$RevitVersion = 2026,
    [string]$Configuration = 'Release'
)

$ErrorActionPreference = 'Stop'
$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path

# 1. Target folders
$revitAddinsDir = Join-Path $env:APPDATA "Autodesk\Revit\Addins\$RevitVersion"
$bridgeTargetDir = Join-Path $revitAddinsDir 'HPDynamo.McpBridge'
$dynamoViewExtDir = Join-Path $env:APPDATA 'Dynamo\Dynamo Revit\3.7\viewExtensions'

if (-not (Test-Path $revitAddinsDir)) { New-Item -ItemType Directory -Path $revitAddinsDir -Force | Out-Null }
if (-not (Test-Path $bridgeTargetDir)) { New-Item -ItemType Directory -Path $bridgeTargetDir -Force | Out-Null }
if (-not (Test-Path $dynamoViewExtDir)) { New-Item -ItemType Directory -Path $dynamoViewExtDir -Force | Out-Null }

# 2. Deploy Revit Add-in Manifest
$sourceManifest = Join-Path $repoRoot 'HPDynamo\HPDynamo.McpBridge\Manifests\HPDynamo.McpBridge.addin'
Copy-Item -Path $sourceManifest -Destination (Join-Path $revitAddinsDir 'HPDynamo.McpBridge.addin') -Force
Write-Host "[OK] Deployed Revit Addin Manifest -> $revitAddinsDir\HPDynamo.McpBridge.addin" -ForegroundColor Green

# 3. Deploy Bridge Binaries
$binSourceDir = Join-Path $repoRoot "HPDynamo\HPDynamo.McpBridge\bin\$Configuration\net10.0-windows7.0"
if (-not (Test-Path $binSourceDir)) {
    throw "Built binaries not found at $binSourceDir. Run 'dotnet build HPDynamo/HPDynamo.slnx -c $Configuration' first."
}
Copy-Item -Path "$binSourceDir\*" -Destination $bridgeTargetDir -Recurse -Force
Write-Host "[OK] Deployed Bridge Binaries -> $bridgeTargetDir" -ForegroundColor Green

# 4. Deploy Dynamo ViewExtension Manifest
$sourceViewExtXml = Join-Path $repoRoot 'HPDynamo\HPDynamo.McpBridge\Manifests\HPDynamo_ViewExtensionDefinition.xml'
Copy-Item -Path $sourceViewExtXml -Destination (Join-Path $dynamoViewExtDir 'HPDynamo_ViewExtensionDefinition.xml') -Force
Write-Host "[OK] Deployed Dynamo ViewExtension XML -> $dynamoViewExtDir\HPDynamo_ViewExtensionDefinition.xml" -ForegroundColor Green

Write-Host "`nDeployment completed successfully! Restart Revit $RevitVersion to load HPDynamo." -ForegroundColor Cyan
