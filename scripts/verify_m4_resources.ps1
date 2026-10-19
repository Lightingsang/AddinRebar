Add-Type -AssemblyName 'System.Reflection.Metadata'
$dllPath = "G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPDynamo\HPDynamo.Mcp.Server\bin\Debug\net10.0\HPDynamo.Mcp.Server.dll"
$fs = [System.IO.File]::OpenRead($dllPath)
$pe = [System.Reflection.PortableExecutable.PEReader]::new($fs)
$mr = $pe.GetMetadataReader()

Write-Host "=== MANIFEST RESOURCES ==="
$manifestCount = 0
$seedCount = 0
foreach ($rh in $mr.ManifestResources) {
    $res = $mr.GetManifestResource($rh)
    $name = $mr.GetString($res.Name)
    $manifestCount++
    if ($name -like "SeedLibrary/*") {
        $seedCount++
        Write-Host "  Seed: $name"
    } else {
        Write-Host "  Other: $name"
    }
}
Write-Host "Total Manifest Resources: $manifestCount"
Write-Host "SeedLibrary Resources: $seedCount"

Write-Host "`n=== TYPES DEFINED IN ASSEMBLY ==="
$typeCount = 0
$scriptTypes = @()
foreach ($th in $mr.TypeDefinitions) {
    $td = $mr.GetTypeDefinition($th)
    $name = $mr.GetString($td.Name)
    $ns = $mr.GetString($td.Namespace)
    $fullName = if ($ns) { "$ns.$name" } else { $name }
    $typeCount++
    Write-Host "  Type: $fullName"
    if ($name -like "*run_dyn_file*" -or $name -like "*node*" -or $name -like "*wire*" -or $fullName -like "*SeedLibrary*") {
        $scriptTypes += $fullName
    }
}
Write-Host "Total Types: $typeCount"
Write-Host "Script Types compiled in: $($scriptTypes.Count)"
if ($scriptTypes.Count -gt 0) {
    Write-Host "WARNING: Found script types: $($scriptTypes -join ', ')"
} else {
    Write-Host "Confirmed: ZERO script types compiled statically into DLL."
}

$fs.Dispose()
