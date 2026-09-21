try {
    $robot = New-Object -ComObject "Robot.Application"
    $robot.Interactive = 0
    $robot.Visible = 0

    $robot.Project.New(1) # I_PT_FRAME_2D

    $cases = $robot.Project.Structure.Cases
    $c1 = $cases.FreeNumber
    Write-Host "FreeNumber: $c1"
    
    # 0 = I_CN_PERMANENT, 1 = I_CAT_STATIC_LINEAR
    $sc = $cases.CreateSimple($c1, "Dead load", 0, 1)
    Write-Host "CreateSimple returned: $($sc -ne $null)"
    Write-Host "Name: $($sc.Name)"
    Write-Host "Number: $($sc.Number)"
    Write-Host "Records Count: $($sc.Records.Count)"

    # Add dead load record
    $rec = $sc.Records.Create(7) # 7 = I_LRT_DEAD
    Write-Host "Record created: $($rec -ne $null)"
    $rec.SetValue(15, 1.0) # 15 = I_DRV_ENTIRE_STRUCTURE
    Write-Host "SetValue done. Records Count now: $($sc.Records.Count)"

    $robot.Project.Close()
    $robot.Quit(1)
    Write-Host "Success!"
}
catch {
    Write-Host "ERROR: $_"
    if ($robot) { $robot.Quit(1) }
}
