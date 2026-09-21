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
    $t.GetProperties() | Sort-Object Name | ForEach-Object {
        Write-Host "$($_.PropertyType.Name) $($_.Name)"
    }
    Write-Host "`n--- Methods ---"
    $t.GetMethods() | Where-Object { -not $_.IsSpecialName } | Sort-Object Name | ForEach-Object {
        $params = $_.GetParameters() | ForEach-Object { "$($_.ParameterType.Name) $($_.Name)" }
        $paramStr = [string]::Join(", ", $params)
        Write-Host "$($_.ReturnType.Name) $($_.Name)($paramStr)"
    }
}

Dump-Interface "IRobotApplication"
Dump-Interface "IRobotProject"
Dump-Interface "IRobotStructure"
Dump-Interface "IRobotPreferences"
