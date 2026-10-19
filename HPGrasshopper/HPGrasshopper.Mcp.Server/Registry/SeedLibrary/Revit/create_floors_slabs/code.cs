var loopElements = args.List("boundaryLoops");
double targetElevation = args.Double("elevation", 0.0);
bool isStructuralFloor = args.Bool("structural", true);

const double feetPerMeter = 3.280839895013123;
var createdFloorIds = new List<long>();

var floorTypeElement = new Autodesk.Revit.DB.FilteredElementCollector(revitDoc)
    .OfClass(typeof(Autodesk.Revit.DB.FloorType))
    .Cast<Autodesk.Revit.DB.FloorType>()
    .FirstOrDefault();

if (floorTypeElement == null)
{
    return new { success = false, message = "No FloorType found in Revit project." };
}

var targetLevel = new Autodesk.Revit.DB.FilteredElementCollector(revitDoc)
    .OfClass(typeof(Autodesk.Revit.DB.Level))
    .Cast<Autodesk.Revit.DB.Level>()
    .OrderBy(lvl => Math.Abs(lvl.Elevation - (targetElevation * feetPerMeter)))
    .FirstOrDefault();

if (targetLevel == null)
{
    targetLevel = Autodesk.Revit.DB.Level.Create(revitDoc, targetElevation * feetPerMeter);
}

var curveLoopCollection = new List<Autodesk.Revit.DB.CurveLoop>();

for (int loopIndex = 0; loopIndex < loopElements.Count; loopIndex++)
{
    var rawElement = loopElements[loopIndex].Raw;
    if (!rawElement.HasValue || rawElement.Value.ValueKind != System.Text.Json.JsonValueKind.Array) continue;
    var rawVertices = rawElement.Value.EnumerateArray().ToList();
    if (rawVertices.Count < 3) continue;

    var loopInstance = new Autodesk.Revit.DB.CurveLoop();
    for (int vertexIndex = 0; vertexIndex < rawVertices.Count; vertexIndex++)
    {
        var currentVertex = rawVertices[vertexIndex];
        var nextVertex = rawVertices[(vertexIndex + 1) % rawVertices.Count];

        double startXFeet = currentVertex.GetProperty("x").GetDouble() * feetPerMeter;
        double startYFeet = currentVertex.GetProperty("y").GetDouble() * feetPerMeter;
        double endXFeet = nextVertex.GetProperty("x").GetDouble() * feetPerMeter;
        double endYFeet = nextVertex.GetProperty("y").GetDouble() * feetPerMeter;

        var pointOne = new Autodesk.Revit.DB.XYZ(startXFeet, startYFeet, 0.0);
        var pointTwo = new Autodesk.Revit.DB.XYZ(endXFeet, endYFeet, 0.0);
        loopInstance.Append(Autodesk.Revit.DB.Line.CreateBound(pointOne, pointTwo));
    }

    curveLoopCollection.Add(loopInstance);
}

if (curveLoopCollection.Count > 0)
{
    var floorInstance = Autodesk.Revit.DB.Floor.Create(
        revitDoc,
        curveLoopCollection,
        floorTypeElement.Id,
        targetLevel.Id);

    var structuralParam = floorInstance.get_Parameter(Autodesk.Revit.DB.BuiltInParameter.FLOOR_PARAM_IS_STRUCTURAL);
    if (structuralParam != null && !structuralParam.IsReadOnly)
    {
        structuralParam.Set(isStructuralFloor ? 1 : 0);
    }

    createdFloorIds.Add(floorInstance.Id.Value);
}

return new
{
    success = createdFloorIds.Count > 0,
    floorsCreated = createdFloorIds.Count,
    floorIds = createdFloorIds,
    levelUsed = targetLevel.Name,
    isStructural = isStructuralFloor
};
