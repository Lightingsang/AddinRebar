$interopPath = "C:\Program Files\Autodesk\Robot Structural Analysis Professional 2026\Exe\Interop.RobotOM.dll"
$asm = [System.Reflection.Assembly]::Load([System.IO.File]::ReadAllBytes($interopPath))

try {
    Write-Host "Creating Robot.Application..."
    $robot = New-Object -ComObject "Robot.Application"
    $robot.Interactive = 0
    $robot.Visible = 0

    # Create new project
    Write-Host "Creating new Frame 3D project..."
    $robot.Project.New(4) # 4 = I_PT_FRAME_3D
    Write-Host "Project IsActive: $($robot.Project.IsActive)"
    Write-Host "Project Type: $($robot.Project.Type)"

    # Inspect Units
    $unitMngr = $robot.Project.Preferences.Units
    Write-Host "UseMetricAsDefault: $($unitMngr.UseMetricAsDefault)"

    $unitTypes = @{
        "StructureDim" = 1 # I_UT_STRUCTURE_DIMENSION
        "SectionDim"   = 2 # I_UT_SECTION_DIMENSION
        "Force"        = 7 # I_UT_FORCE
        "Moment"       = 8 # I_UT_MOMENT
        "Stress"       = 9 # I_UT_STRESS
        "Displacement" = 10 # I_UT_DISPLACEMENT
    }

    foreach ($entry in $unitTypes.GetEnumerator()) {
        $uType = $entry.Value
        $uName = $entry.Key
        $cnt = $unitMngr.Count($uType)
        $current = $unitMngr.Get($uType)
        Write-Host "`nUnit: $uName (Type $uType) - Current: '$($current.Name)', Precision: $($current.Precision), E: $($current.E)"
        Write-Host "Available unit names ($cnt count):"
        for ($i = 1; $i -le $cnt; $i++) {
            $name = $unitMngr.GetName($uType, $i)
            $coeff = $unitMngr.GetCoeff2($uType, $i)
            Write-Host "  [$i] '$name' (Coeff to SI: $coeff)"
        }
    }

    # Test Nodes creation
    Write-Host "`nCreating nodes..."
    $nodes = $robot.Project.Structure.Nodes
    $nodes.Create(1, 0.0, 0.0, 0.0)
    $nodes.Create(2, 0.0, 0.0, 3.5)
    Write-Host "Node 1: X=$($nodes.Get(1).X), Y=$($nodes.Get(1).Y), Z=$($nodes.Get(1).Z)"
    Write-Host "Node 2: X=$($nodes.Get(2).X), Y=$($nodes.Get(2).Y), Z=$($nodes.Get(2).Z)"

    # Test Bars creation
    Write-Host "`nCreating bar 1..."
    $bars = $robot.Project.Structure.Bars
    $bars.Create(1, 1, 2)
    $bar1 = $bars.Get(1)
    Write-Host "Bar 1: Start=$($bar1.StartNode), End=$($bar1.EndNode), Length=$($bar1.Length)"

    # Close project without saving
    Write-Host "`nClosing project..."
    $robot.Project.Close()
    Write-Host "Quitting Robot..."
    $robot.Quit(1) # I_QO_DISCARD_CHANGES
    Write-Host "Done!"
}
catch {
    Write-Host "ERROR: $_"
    if ($robot) {
        $robot.Quit(1)
    }
}
