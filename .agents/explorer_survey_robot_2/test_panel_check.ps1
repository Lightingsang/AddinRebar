try {
    Write-Host "Creating Robot.Application..."
    $robot = New-Object -ComObject "Robot.Application"
    $robot.Interactive = 0
    $robot.Visible = 0

    Write-Host "Creating Shell 3D project..."
    $robot.Project.New(7) # 7 = I_PT_SHELL

    $objects = $robot.Project.Structure.Objects
    $freeNum = $objects.FreeNumber
    Write-Host "Free object number: $freeNum"

    # Create points array for 4x4m slab
    $pts = $robot.CmpntFactory.Create(101) # 101 or RobotPointsArray
    if (-not $pts) {
        Write-Host "Trying assembly type for RobotPointsArray..."
    }

    # Or let's check CmpntFactory types
    $robot.Project.Close()
    $robot.Quit(1)
    Write-Host "Done!"
}
catch {
    Write-Host "ERROR: $_"
    if ($robot) { $robot.Quit(1) }
}
