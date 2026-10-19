$coreAsmPath = "g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\McpShared\HPRebar.Mcp.Server.Core\bin\Debug\net10.0\HPRebar.Mcp.Server.Core.dll"
$serverAsmPath = "g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPDynamo\HPDynamo.Mcp.Server\bin\Debug\net10.0\HPDynamo.Mcp.Server.dll"

[System.Reflection.Assembly]::LoadFrom($coreAsmPath) | Out-Null
$serverAsm = [System.Reflection.Assembly]::LoadFrom($serverAsmPath)

$seedInstallerType = [Type]::GetType("HPRebar.Mcp.Server.Registry.SeedInstaller, HPRebar.Mcp.Server.Core")
$loadSeedsMethod = $seedInstallerType.GetMethod("LoadSeeds", [System.Reflection.BindingFlags]"Public,Static", $null, @([System.Reflection.Assembly]), $null)

$seeds = $loadSeedsMethod.Invoke($null, @($serverAsm))
Write-Host "SeedInstaller.LoadSeeds count:" $seeds.Count
foreach ($s in $seeds) {
    Write-Host "  Loaded: Category=$($s.Category), Name=$($s.Name), Checksum=$($s.Checksum)"
}
