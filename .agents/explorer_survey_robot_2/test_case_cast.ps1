$interopPath = "C:\Program Files\Autodesk\Robot Structural Analysis Professional 2026\Exe\Interop.RobotOM.dll"
$asm = [System.Reflection.Assembly]::Load([System.IO.File]::ReadAllBytes($interopPath))

try {
    $robot = New-Object -ComObject "Robot.Application"
    $robot.Interactive = 0
    $robot.Visible = 0

    $robot.Project.New(1) # I_PT_FRAME_2D

    $cases = $robot.Project.Structure.Cases
    $c1 = $cases.FreeNumber
    $rawCase = $cases.CreateSimple($c1, "SelfWeight", 0, 0)
    Write-Host "rawCase type: $($rawCase.GetType().FullName)"
    
    # Try casting to IRobotSimpleCase
    $simpleCaseType = $asm.GetType("RobotOM.IRobotSimpleCase")
    $sc = [System.Runtime.InteropServices.Marshal]::CreateWrapperOfType($rawCase, $asm.GetType("RobotOM.RobotSimpleCaseClass"))
    Write-Host "Cast wrapper: $($sc.GetType().FullName)"
    Write-Host "sc.Records count: $($sc.Records.Count)"

    $robot.Project.Close()
    $robot.Quit(1)
}
catch {
    Write-Host "ERROR: $_"
    if ($robot) { $robot.Quit(1) }
}
