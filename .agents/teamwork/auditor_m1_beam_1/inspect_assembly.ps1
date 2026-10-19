using namespace System.Reflection.Metadata
using namespace System.Reflection.PortableExecutable

$dllPath = "G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPRebar\HPRebar\bin\Debug.R26\HPRebar.dll"
$stream = [System.IO.File]::OpenRead($dllPath)
$peReader = [PEReader]::new($stream)
$metaReader = $peReader.GetMetadataReader()

$helixTypes = [System.Collections.Generic.List[string]]::new()

foreach ($handle in $metaReader.TypeDefinitions) {
    $typeDef = $metaReader.GetTypeDefinition($handle)
    $ns = $metaReader.GetString($typeDef.Namespace)
    $name = $metaReader.GetString($typeDef.Name)
    $fullName = if ($ns) { "$ns.$name" } else { $name }
    if ($fullName -like "*HelixToolkit*") {
        $helixTypes.Add($fullName)
    }
}

$stream.Close()

Write-Output "PEReader Metadata Analysis:"
Write-Output "Total TypeDefinitions in HPRebar.dll: $($metaReader.TypeDefinitions.Count)"
Write-Output "Total HelixToolkit TypeDefinitions in HPRebar.dll: $($helixTypes.Count)"
Write-Output "Sample HelixToolkit types in HPRebar.dll:"
$helixTypes | Select-Object -First 20
