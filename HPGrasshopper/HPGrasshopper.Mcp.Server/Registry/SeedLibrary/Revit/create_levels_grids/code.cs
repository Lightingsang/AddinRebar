var elevationList = args.Doubles("elevations");
var spacingList = args.Doubles("gridSpacings");
double lineLength = args.Double("gridLength", 30.0);

const double feetPerMeter = 3.280839895013123;
var createdLevels = new List<long>();
var createdGrids = new List<long>();

for (int levelIndex = 0; levelIndex < elevationList.Count; levelIndex++)
{
    double elevationFeet = elevationList[levelIndex] * feetPerMeter;
    var levelInstance = Autodesk.Revit.DB.Level.Create(revitDoc, elevationFeet);
    levelInstance.Name = $"Level_{levelIndex + 1}_{(int)(elevationList[levelIndex] * 1000)}mm";
    createdLevels.Add(levelInstance.Id.Value);
}

double currentXPosition = 0.0;
for (int gridIndex = 0; gridIndex < spacingList.Count; gridIndex++)
{
    double startYFeet = -lineLength * 0.5 * feetPerMeter;
    double endYFeet = lineLength * 0.5 * feetPerMeter;
    double xFeet = currentXPosition * feetPerMeter;

    var startPoint = new Autodesk.Revit.DB.XYZ(xFeet, startYFeet, 0.0);
    var endPoint = new Autodesk.Revit.DB.XYZ(xFeet, endYFeet, 0.0);
    var gridCurve = Autodesk.Revit.DB.Line.CreateBound(startPoint, endPoint);

    var gridInstance = Autodesk.Revit.DB.Grid.Create(revitDoc, gridCurve);
    gridInstance.Name = $"Axis_{gridIndex + 1}";
    createdGrids.Add(gridInstance.Id.Value);

    currentXPosition += spacingList[gridIndex];
}

return new
{
    success = true,
    levelsCount = createdLevels.Count,
    gridsCount = createdGrids.Count,
    levelIds = createdLevels,
    gridIds = createdGrids
};
