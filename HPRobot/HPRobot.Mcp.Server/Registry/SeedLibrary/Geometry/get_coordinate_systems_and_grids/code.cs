var project = robot.Project;
var axisMngr = project.AxisMngr;
int count = axisMngr.Count;
var grids = new List<object>();

for (int i = 1; i <= count; i++)
{
    var grid = axisMngr.Get(i);
    string gridName = grid.Name;
    string gridType = grid.Type.ToString();

    if (grid is IRobotStructuralAxisGridCartesian cart)
    {
        var xAxes = new List<object>();
        for (int j = 1; j <= cart.X.AxisCount; j++)
        {
            xAxes.Add(new { position = cart.X.StartPosition });
        }
        grids.Add(new
        {
            name = gridName,
            type = gridType,
            xCount = cart.X.AxisCount,
            yCount = cart.Y.AxisCount,
            zCount = cart.Z.AxisCount,
            rotationAngle = cart.RotationAngle
        });
    }
    else
    {
        grids.Add(new { name = gridName, type = gridType });
    }
}

return new
{
    success = true,
    gridCount = count,
    grids
};
