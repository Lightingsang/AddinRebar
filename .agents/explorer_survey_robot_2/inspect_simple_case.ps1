$interopPath = "C:\Program Files\Autodesk\Robot Structural Analysis Professional 2026\Exe\Interop.RobotOM.dll"
$asm = [System.Reflection.Assembly]::Load([System.IO.File]::ReadAllBytes($interopPath))

function Dump-Interface($typeName) {
    $t = $asm.GetType("RobotOM.$typeName")
    if (-not $t) {
        Write-Host "Type RobotOM.$typeName not found!"
        return
    }
    Write-Host "`n========================================================"
    Write-Host "INTERFACE: $typeName"
    Write-Host "========================================================"
    Write-Host "--- Properties ---"
    foreach ($p in ($t.GetProperties() | Sort-Object Name)) {
        Write-Host "$($p.PropertyType.Name) $($p.Name)"
    }
    Write-Host "`n--- Methods ---"
    foreach ($m in ($t.GetMethods() | Where-Object { -not $_.IsSpecialName } | Sort-Object Name)) {
        $pList = New-Object System.Collections.Generic.List[string]
        foreach ($param in $m.GetParameters()) {
            $pList.Add("$($param.ParameterType.Name) $($param.Name)")
        }
        $paramStr = [string]::Join(", ", $pList)
        Write-Host "$($m.ReturnType.Name) $($m.Name)($paramStr)"
    }
}

Dump-Interface "IRobotCase"
Dump-Interface "IRobotSimpleCase"
Dump-Interface "IRobotLoadRecordMngr"
