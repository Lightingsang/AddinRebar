Add-Type -Path "G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPRebar\HPRebar\bin\Debug.R26\HPRebar.dll"

$previewEnumType = [HPRebar.BeamRebar.ViewModel.BeamPreviewMode]
Write-Host "Enum values: $([Enum]::GetNames($previewEnumType) -join ', ')"

$vmType = [HPRebar.BeamRebar.ViewModel.BeamRebarViewModel]
Write-Host "ViewModel type found: $($vmType.FullName)"

# Check properties exist
$props = @("PreviewMode", "IsTwoDMode", "IsThreeDMode", "IsSplitMode")
foreach ($p in $props) {
    $prop = $vmType.GetProperty($p)
    if ($prop -eq $null) {
        Write-Error "Missing property: $p"
        exit 1
    } else {
        Write-Host "Property OK: $p ($($prop.PropertyType.Name))"
    }
}

Write-Host "ALL REFLECTION CHECKS PASSED!"
