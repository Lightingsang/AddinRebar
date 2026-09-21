try {
    Write-Host "Checking if Robot.Application can be created via COM..."
    $robot = New-Object -ComObject "Robot.Application"
    Write-Host "Created successfully!"
    Write-Host "Visible: $($robot.Visible)"
    Write-Host "Version: $($robot.Version)"
    Write-Host "ProgramVersion: $($robot.ProgramVersion)"
    
    # Check Project
    $proj = $robot.Project
    Write-Host "Project IsActive: $($proj.IsActive)"
    
    # Close / Quit if we launched a new instance
    Write-Host "Quitting test instance..."
    $robot.Quit(1) # 1 = I_QO_DISCARD_CHANGES
    Write-Host "Quit done."
}
catch {
    Write-Host "Error: $_"
}
