try {
    $robot = New-Object -ComObject "Robot.Application"
    $robot.Interactive = 0
    $robot.Visible = 0

    $robot.Project.New(1) # I_PT_FRAME_2D

    $nodes = $robot.Project.Structure.Nodes
    $nodes.Create(1, 0.0, 0.0, 0.0)
    $nodes.Create(2, 6.0, 0.0, 0.0)

    $bars = $robot.Project.Structure.Bars
    $bars.Create(1, 1, 2)

    Write-Host "Checking node 1..."
    $n1 = $nodes.Get(1)
    Write-Host "n1 is null: $($n1 -eq $null)"

    if ($n1) {
        Write-Host "Setting label on n1..."
        $n1.SetLabel(0, "Fixed")
        Write-Host "Node 1 label set!"
    }

    $robot.Project.Close()
    $robot.Quit(1)
}
catch {
    Write-Host "ERROR: $_"
    Write-Host "Line: $($_.InvocationInfo.Line)"
    Write-Host "StackTrace: $($_.ScriptStackTrace)"
    if ($robot) { $robot.Quit(1) }
}
