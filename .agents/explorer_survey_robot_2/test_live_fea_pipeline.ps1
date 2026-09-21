try {
    Write-Host "Creating Robot.Application..."
    $robot = New-Object -ComObject "Robot.Application"
    $robot.Interactive = 0
    $robot.Visible = 0

    Write-Host "Creating Frame 2D project..."
    $robot.Project.New(1) # 1 = I_PT_FRAME_2D

    # Nodes
    $nodes = $robot.Project.Structure.Nodes
    $nodes.Create(1, 0.0, 0.0, 0.0)
    $nodes.Create(2, 6.0, 0.0, 0.0)

    # Bar
    $bars = $robot.Project.Structure.Bars
    $bars.Create(1, 1, 2)

    # Supports
    $nodes.Get(1).SetLabel(0, "Fixed")
    $nodes.Get(2).SetLabel(0, "Pinned")

    # Section
    $secNames = $robot.Project.Structure.Labels.GetAvailableNames(3) # 3 = I_LT_BAR_SECTION
    if ($secNames.Count -gt 0) {
        $secName = $secNames.Get(1)
        Write-Host "Assigning section: $secName"
        $bars.Get(1).SetLabel(3, $secName)
    }

    # Case 1: Dead load
    $cases = $robot.Project.Structure.Cases
    $c1 = $cases.FreeNumber
    $scDead = $cases.CreateSimple($c1, "Dead Load", 0, 1) # 0 = I_CN_PERMANENT, 1 = I_CAT_STATIC_LINEAR
    $recDead = $scDead.Records.Create(7) # 7 = I_LRT_DEAD
    $recDead.SetValue(15, 1.0) # 15 = I_DRV_ENTIRE_STRUCTURE

    # Case 2: Live load (Uniform 10 kN/m = 10,000 N/m downwards)
    $scLive = $cases.CreateSimple($c1 + 1, "Live Load", 1, 1) # 1 = I_CN_EXPLOATATION, 1 = I_CAT_STATIC_LINEAR
    $recLive = $scLive.Records.Create(5) # 5 = I_LRT_BAR_UNIFORM
    $recLive.SetValue(2, -10000.0) # 2 = I_BURV_PZ (-10000 N/m)
    $recLive.Objects.AddOne(1) # Bar 1

    # Solve
    Write-Host "Solving model via Calculate()..."
    $status = $robot.Project.CalcEngine.Calculate()
    Write-Host "Calculate status: $status"
    Write-Host "Results Available: $($robot.Project.Structure.Results.Available)"

    # Read Node Reactions (Case 2: Live Load)
    $reactions = $robot.Project.Structure.Results.Nodes.Reactions
    $r1 = $reactions.Value(1, $c1 + 1)
    $r2 = $reactions.Value(2, $c1 + 1)
    Write-Host "`nReactions for Live Load (6m beam under 10 kN/m uniform load):"
    Write-Host "Theoretical: FZ1 = 30 kN (30000 N), FZ2 = 30 kN (30000 N)"
    Write-Host "FEA Node 1: FX=$([Math]::Round($r1.FX, 1)) N, FZ=$([Math]::Round($r1.FZ, 1)) N, MY=$([Math]::Round($r1.MY, 1)) N*m"
    Write-Host "FEA Node 2: FX=$([Math]::Round($r2.FX, 1)) N, FZ=$([Math]::Round($r2.FZ, 1)) N, MY=$([Math]::Round($r2.MY, 1)) N*m"

    # Read Bar Internal Forces
    $forces = $robot.Project.Structure.Results.Bars.Forces
    Write-Host "`nBar 1 Internal Forces (MY: Bending Moment, FZ: Shear Force):"
    Write-Host "Theoretical Midspan Moment: qL^2 / 8 = 10 * 6^2 / 8 = 45 kNm (45,000 Nm)"
    for ($pt = 0.0; $pt -le 1.0; $pt += 0.25) {
        $f = $forces.Value(1, $c1 + 1, $pt)
        Write-Host "  x/L=$($pt.ToString('0.00')) ($($pt*6)m) -> Shear FZ=$([Math]::Round($f.FZ, 1)) N, Moment MY=$([Math]::Round($f.MY, 1)) N*m"
    }

    $robot.Project.Close()
    $robot.Quit(1)
    Write-Host "`nFEA Pipeline test completed successfully!"
}
catch {
    Write-Host "ERROR: $_"
    Write-Host "Line: $($_.InvocationInfo.Line)"
    Write-Host "StackTrace: $($_.ScriptStackTrace)"
    if ($robot) { $robot.Quit(1) }
}
