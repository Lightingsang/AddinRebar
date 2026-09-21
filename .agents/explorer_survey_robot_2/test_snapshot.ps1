try {
    $robot = New-Object -ComObject "Robot.Application"
    $robot.Interactive = 0
    $robot.Visible = 0

    $robot.Project.New(4) # I_PT_FRAME_3D
    
    $nodes = $robot.Project.Structure.Nodes
    $nodes.Create(1, 0.0, 0.0, 0.0)
    $nodes.Create(2, 0.0, 0.0, 3.0)

    $bars = $robot.Project.Structure.Bars
    $bars.Create(1, 1, 2)

    $tempFile = [System.IO.Path]::Combine([System.IO.Path]::GetTempPath(), "test_robot_model.rtd")
    if (Test-Path $tempFile) { Remove-Item $tempFile -Force }

    Write-Host "Saving model to: $tempFile"
    $robot.Project.SaveAs($tempFile)

    Write-Host "File created: $(Test-Path $tempFile)"
    if (Test-Path $tempFile) {
        $fi = Get-Item $tempFile
        Write-Host "File size: $($fi.Length) bytes"
        Remove-Item $tempFile -Force
        Write-Host "Temp file deleted cleanly."
    }

    $robot.Project.Close()
    $robot.Quit(1)
    Write-Host "Done!"
}
catch {
    Write-Host "ERROR: $_"
    if ($robot) { $robot.Quit(1) }
}
