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
    $nodes.Create(2, 6.0, 0.0, 0.0) # 6m beam

    # Bar
    $bars = $robot.Project.Structure.Bars
    $bars.Create(1, 1, 2)

    # Set support on nodes
    $node1 = $nodes.Get(1)
    $node1.SetLabel(0, "Fixed")
    $node2 = $nodes.Get(2)
    $node2.SetLabel(0, "Pinned")

    # Set section on bar
    $secNames = $robot.Project.Structure.Labels.GetAvailableNames(3) # 3 = I_LT_BAR_SECTION
    Write-Host "Available section count: $($secNames.Count)"
    if ($secNames.Count -gt 0) {
        $secName = $secNames.Get(1)
        Write-Host "Assigning section: $secName"
        $bar1 = $bars.Get(1)
        $bar1.SetLabel(3, $secName)
    }

    # Load case: Dead load
    $cases = $robot.Project.Structure.Cases
    $c1 = $cases.FreeNumber
    $case1 = $cases.CreateSimple($c1, "SelfWeight", 0, 0) # 0 = I_CN_PERMANENT, 0 = I_CAT_STATIC_LINEAR
    $recDead = $case1.Records.Create(7) # 7 = I_LRT_DEAD
    $recDead.SetValue(15, 1.0) # 15 = I_DRV_ENTIRE_STRUCTURE

    # Load case 2: Uniform load 10 kN/m = 10000 N/m downwards (PZ = -10000)
    $case2 = $cases.CreateSimple($c1 + 1, "LiveLoad", 1, 0) # 1 = I_CN_EXPLOATATION, 0 = I_CAT_STATIC_LINEAR
    $recUniform = $case2.Records.Create(5) # 5 = I_LRT_BAR_UNIFORM
    $recUniform.SetValue(2, -10000.0) # 2 = I_BURV_PZ (-10000 N/m)
    $recUniform.Objects.AddOne(1) # Bar 1

    # Run FEA calculation
    Write-Host "`nRunning Calculate()..."
    $status = $robot.Project.CalcEngine.Calculate()
    Write-Host "Calculate returned: $status"
    Write-Host "Results Available: $($robot.Project.Structure.Results.Available)"

    # Read Reactions
    $reactions = $robot.Project.Structure.Results.Nodes.Reactions
    $r1 = $reactions.Value(1, $c1 + 1)
    $r2 = $reactions.Value(2, $c1 + 1)
    Write-Host "`nReactions for LiveLoad (Case $($c1 + 1)):"
    Write-Host "Node 1: FX=$($r1.FX) N, FZ=$($r1.FZ) N, MY=$($r1.MY) N*m"
    Write-Host "Node 2: FX=$($r2.FX) N, FZ=$($r2.FZ) N, MY=$($r2.MY) N*m"

    # Read Bar Forces
    $barForces = $robot.Project.Structure.Results.Bars.Forces
    Write-Host "`nBar 1 Forces along length:"
    for ($pt = 0.0; $pt -le 1.0; $pt += 0.25) {
        $f = $barForces.Value(1, $c1 + 1, $pt)
        Write-Host "  x/L=$pt : FX=$([Math]::Round($f.FX, 2)) N, FZ=$([Math]::Round($f.FZ, 2)) N, MY=$([Math]::Round($f.MY, 2)) N*m"
    }

    $robot.Project.Close()
    $robot.Quit(1)
    Write-Host "`nDone successfully!"
}
catch {
    Write-Host "ERROR: $_"
    Write-Host "Line: $($_.InvocationInfo.Line)"
    Write-Host "StackTrace: $($_.ScriptStackTrace)"
    if ($robot) { $robot.Quit(1) }
}
