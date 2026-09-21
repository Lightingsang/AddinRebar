$teklaDir = 'C:\Program Files\Tekla Structures\2025.0\bin'
[System.Reflection.Assembly]::LoadFrom("$teklaDir\Tekla.Structures.dll") | Out-Null
$asmModel = [System.Reflection.Assembly]::LoadFrom("$teklaDir\Tekla.Structures.Model.dll")

Write-Host "=== IFCExportFlags fields ==="
$flags = $asmModel.GetType('Tekla.Structures.Model.Operations.Operation+IFCExportFlags')
$flags.GetFields() | ForEach-Object { Write-Host "$($_.Name) : $($_.FieldType.Name)" }

Write-Host "`n=== SingleRebar Size ==="
$sr = $asmModel.GetType('Tekla.Structures.Model.SingleRebar')
$sr.GetProperty('Size') | ForEach-Object { Write-Host "$($_.Name) : $($_.PropertyType.Name)" }

Write-Host "`n=== RebarGroup Size ==="
$rg = $asmModel.GetType('Tekla.Structures.Model.RebarGroup')
$rg.GetProperty('Size') | ForEach-Object { Write-Host "$($_.Name) : $($_.PropertyType.Name)" }

Write-Host "`n=== BaseRebarGroup properties ==="
$brg = $asmModel.GetType('Tekla.Structures.Model.BaseRebarGroup')
$brg.GetProperties() | ForEach-Object { Write-Host "$($_.Name) : $($_.PropertyType.Name)" }
