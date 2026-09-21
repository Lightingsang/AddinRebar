$interopPath = "C:\Program Files\Autodesk\Robot Structural Analysis Professional 2026\Exe\Interop.RobotOM.dll"
$asm = [System.Reflection.Assembly]::Load([System.IO.File]::ReadAllBytes($interopPath))

$t = $asm.GetType("RobotOM.IRobotComponentType")
if ($t) {
    Write-Host "=== ENUM: IRobotComponentType ==="
    [System.Enum]::GetNames($t) | ForEach-Object {
        $val = [int][System.Enum]::Parse($t, $_)
        Write-Host "  $_ = $val"
    }
}

$cf = $asm.GetType("RobotOM.IRobotComponentFactory")
if ($cf) {
    Write-Host "`n=== INTERFACE: IRobotComponentFactory ==="
    foreach ($m in $cf.GetMethods()) {
        Write-Host "$($m.ReturnType.Name) $($m.Name)"
    }
}
