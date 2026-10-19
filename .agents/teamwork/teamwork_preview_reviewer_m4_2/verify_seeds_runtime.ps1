$coreDll = "G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\McpShared\HPRebar.Mcp.Server.Core\bin\Debug\net10.0\HPRebar.Mcp.Server.Core.dll"
$serverDll = "G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPDynamo\HPDynamo.Mcp.Server\bin\Debug\net10.0\HPDynamo.Mcp.Server.dll"

Write-Host "Loading assemblies in pwsh..."
Add-Type -Path $coreDll
Add-Type -Path $serverDll

$serverAssembly = [Reflection.Assembly]::LoadFrom($serverDll)
$seeds = [HPRebar.Mcp.Server.Registry.SeedInstaller]::LoadSeeds($serverAssembly)

Write-Host "Loaded seeds count: $($seeds.Count)"
foreach ($s in $seeds) {
    Write-Host "Seed: [$($s.Category)] $($s.Name) | tool.json: $($s.ToolJson.Length) chars | code.cs: $($s.Code.Length) chars | examples.json: $($s.ExamplesJson.Length) chars"
    if ($s.ToolJson.Length -eq 0 -or $s.Code.Length -eq 0 -or $s.ExamplesJson.Length -eq 0) {
        Write-Error "Seed $($s.Name) has empty component!"
        exit 1
    }
}

if ($seeds.Count -ne 12) {
    Write-Error "Expected exactly 12 seeds, but got $($seeds.Count)"
    exit 1
}

Write-Host "SUCCESS: All 12 seeds loaded directly from HPDynamo.Mcp.Server manifest resources!"
exit 0
