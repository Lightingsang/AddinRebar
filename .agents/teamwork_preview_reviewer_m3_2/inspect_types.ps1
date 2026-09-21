$teklaDir = 'C:\Program Files\Tekla Structures\2025.0\bin'
[System.Reflection.Assembly]::LoadFrom("$teklaDir\Tekla.Structures.dll") | Out-Null
$asmModel = [System.Reflection.Assembly]::LoadFrom("$teklaDir\Tekla.Structures.Model.dll")

Write-Host "=== 1. ProjectInfo properties ==="
$projInfo = $asmModel.GetType('Tekla.Structures.Model.ProjectInfo')
$projInfo.GetProperties() | ForEach-Object { Write-Host "$($_.Name) : $($_.PropertyType.Name)" }

Write-Host "`n=== 2. ModelObjectEnum REBAR / REINF ==="
$moEnum = $asmModel.GetType('Tekla.Structures.Model.ModelObject+ModelObjectEnum')
[System.Enum]::GetNames($moEnum) | Where-Object { $_ -like '*REBAR*' -or $_ -like '*REINF*' -or $_ -like '*BAR*' } | ForEach-Object { Write-Host $_ }

Write-Host "`n=== 3. Reinforcement properties ==="
$reinf = $asmModel.GetType('Tekla.Structures.Model.Reinforcement')
$reinf.GetProperties() | ForEach-Object { Write-Host "$($_.Name) : $($_.PropertyType.Name)" }

Write-Host "`n=== 4. Operation IFC Export methods and enums ==="
$op = $asmModel.GetType('Tekla.Structures.Model.Operations.Operation')
$op.GetMethods() | Where-Object { $_.Name -like '*IFC*' } | ForEach-Object { 
    Write-Host "$($_.Name)($([string]::Join(', ', ($_.GetParameters() | ForEach-Object { "$($_.ParameterType.Name) $($_.Name)" }))))" 
}

Write-Host "`n=== 5. Operation Nested Types ==="
$op.GetNestedTypes() | ForEach-Object { 
    Write-Host $_.Name
    if ($_.IsEnum) {
        [System.Enum]::GetNames($_) | ForEach-Object { Write-Host "   - $_" }
    }
}
