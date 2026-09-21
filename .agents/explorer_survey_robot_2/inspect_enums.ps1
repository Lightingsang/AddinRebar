$interopPath = "C:\Program Files\Autodesk\Robot Structural Analysis Professional 2026\Exe\Interop.RobotOM.dll"
$asm = [System.Reflection.Assembly]::Load([System.IO.File]::ReadAllBytes($interopPath))

function Dump-Enum($enumName) {
    $t = $asm.GetType("RobotOM.$enumName")
    if (-not $t) {
        Write-Host "Enum RobotOM.$enumName not found!"
        return
    }
    Write-Host "`n=== ENUM: $enumName ==="
    [System.Enum]::GetNames($t) | ForEach-Object {
        $val = [int][System.Enum]::Parse($t, $_)
        Write-Host "  $_ = $val"
    }
}

Dump-Enum "IRobotUnitType"
Dump-Enum "IRobotLabelType"
Dump-Enum "IRobotProjectType"
Dump-Enum "IRobotObjectType"
Dump-Enum "IRobotCaseNature"
Dump-Enum "IRobotLoadRecordType"
