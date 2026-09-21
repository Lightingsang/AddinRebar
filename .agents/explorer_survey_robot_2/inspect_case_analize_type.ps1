$interopPath = "C:\Program Files\Autodesk\Robot Structural Analysis Professional 2026\Exe\Interop.RobotOM.dll"
$asm = [System.Reflection.Assembly]::Load([System.IO.File]::ReadAllBytes($interopPath))

$t = $asm.GetType("RobotOM.IRobotCaseAnalizeType")
[System.Enum]::GetNames($t) | ForEach-Object {
    $val = [int][System.Enum]::Parse($t, $_)
    Write-Host "  $_ = $val"
}
