try {
    $robot = New-Object -ComObject "Robot.Application"
    $robot.Interactive = 0
    $robot.Visible = 0

    $robot.Project.New(4) # I_PT_FRAME_3D
    $unitMngr = $robot.Project.Preferences.Units

    Write-Host "Current Force Unit: $($unitMngr.Get(7).Name)"
    Write-Host "Current StructureDim Unit: $($unitMngr.Get(1).Name)"
    Write-Host "Current Moment Unit: $($unitMngr.Get(8).Name)"

    # Test setting to Metric:
    # 1: StructureDim -> 'm'
    $uData1 = $unitMngr.Get(1)
    $uData1.Name = "m"
    $unitMngr.Set(1, $uData1)

    # 7: Force -> 'kN'
    $uData7 = $unitMngr.Get(7)
    $uData7.Name = "kN"
    $unitMngr.Set(7, $uData7)

    # 8: Moment -> 'kN*m'
    $uData8 = $unitMngr.Get(8)
    $uData8.Name = "kN*m"
    $unitMngr.Set(8, $uData8)

    $unitMngr.Refresh()

    Write-Host "`nAfter setting:"
    Write-Host "New Force Unit: $($unitMngr.Get(7).Name)"
    Write-Host "New StructureDim Unit: $($unitMngr.Get(1).Name)"
    Write-Host "New Moment Unit: $($unitMngr.Get(8).Name)"

    # Check UseMetricAsDefault toggle
    Write-Host "Setting UseMetricAsDefault = true..."
    $unitMngr.UseMetricAsDefault = $true
    $unitMngr.Refresh()
    Write-Host "After UseMetricAsDefault = true:"
    Write-Host "Force Unit: $($unitMngr.Get(7).Name)"
    Write-Host "StructureDim Unit: $($unitMngr.Get(1).Name)"
    Write-Host "Moment Unit: $($unitMngr.Get(8).Name)"
    Write-Host "Stress Unit: $($unitMngr.Get(9).Name)"

    $robot.Project.Close()
    $robot.Quit(1)
    Write-Host "Done!"
}
catch {
    Write-Host "ERROR: $_"
    if ($robot) { $robot.Quit(1) }
}
