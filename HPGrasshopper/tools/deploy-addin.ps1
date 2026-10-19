# Deploy HPGrasshopper.McpBridge to Revit 2026 Addins directory and harmonize McpBridge.Core
$appData = [System.Environment]::GetFolderPath([System.Environment+SpecialFolder]::ApplicationData)
$revitAddins = Join-Path $appData "Autodesk\Revit\Addins\2026"
$targetDir = Join-Path $revitAddins "HPGrasshopper.McpBridge"

if (!(Test-Path $targetDir)) {
    New-Item -ItemType Directory -Path $targetDir -Force | Out-Null
}

$sourceBin = Join-Path $PSScriptRoot "..\HPGrasshopper.McpBridge\bin\Release\net10.0-windows7.0\*"
Copy-Item -Path $sourceBin -Destination $targetDir -Recurse -Force

$manifestSource = Join-Path $PSScriptRoot "..\HPGrasshopper.McpBridge\Host\GrasshopperManifest.addin"
$manifestTarget = Join-Path $revitAddins "HPGrasshopper.McpBridge.addin"
Copy-Item -Path $manifestSource -Destination $manifestTarget -Force

Write-Host "HPGrasshopper.McpBridge successfully deployed to $targetDir"
Write-Host "Manifest deployed to $manifestTarget"

# Harmonize McpShared assemblies in HPDynamo.McpBridge to prevent TypeLoadException
$dynamoBridgeDir = Join-Path $revitAddins "HPDynamo.McpBridge"
if (Test-Path $dynamoBridgeDir) {
    $sharedCore = Join-Path $targetDir "HPRebar.McpBridge.Core.dll"
    $sharedContracts = Join-Path $targetDir "HPRebar.Mcp.Contracts.dll"
    
    if (Test-Path $sharedCore) {
        Copy-Item -Path $sharedCore -Destination $dynamoBridgeDir -Force
        $corePdb = Join-Path $targetDir "HPRebar.McpBridge.Core.pdb"
        if (Test-Path $corePdb) { Copy-Item -Path $corePdb -Destination $dynamoBridgeDir -Force }
    }
    if (Test-Path $sharedContracts) {
        Copy-Item -Path $sharedContracts -Destination $dynamoBridgeDir -Force
        $contractsPdb = Join-Path $targetDir "HPRebar.Mcp.Contracts.pdb"
        if (Test-Path $contractsPdb) { Copy-Item -Path $contractsPdb -Destination $dynamoBridgeDir -Force }
    }
    Write-Host "Harmonized McpBridge.Core & Mcp.Contracts into $dynamoBridgeDir"
}
