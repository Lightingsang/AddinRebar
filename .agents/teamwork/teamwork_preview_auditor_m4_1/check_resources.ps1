$asmPath = "g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPDynamo\HPDynamo.Mcp.Server\bin\Debug\net10.0\HPDynamo.Mcp.Server.dll"
$asm = [System.Reflection.Assembly]::LoadFrom($asmPath)
$resources = $asm.GetManifestResourceNames() | Where-Object { $_ -like "SeedLibrary/*" }
Write-Host "Total embedded SeedLibrary resources in HPDynamo.Mcp.Server.dll:" $resources.Count
$resources | Sort-Object | ForEach-Object { Write-Host "  $_" }
