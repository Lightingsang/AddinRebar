var targetIdList = args.Longs("elementIds");
string requestedDetail = args.Str("detailLevel", "Fine");
bool extractFaces = args.Bool("includeFaces", true);

const double feetPerMeter = 3.280839895013123;
double feetSquaredPerSquareMeter = feetPerMeter * feetPerMeter;
double feetCubedPerCubicMeter = feetPerMeter * feetPerMeter * feetPerMeter;

(int solidCount, double totalVolume, double totalArea) ComputeSolids(Autodesk.Revit.DB.GeometryElement? geometry)
{
    if (geometry == null) return (0, 0.0, 0.0);
    int count = 0;
    double volume = 0.0;
    double area = 0.0;
    foreach (var geometryObject in geometry)
    {
        if (geometryObject is Autodesk.Revit.DB.Solid solidItem && solidItem.Volume > 0.0)
        {
            volume += solidItem.Volume;
            area += solidItem.SurfaceArea;
            count++;
        }
    }
    return (count, volume, area);
}

var geometryOptions = new Autodesk.Revit.DB.Options
{
    ComputeReferences = true,
    DetailLevel = Autodesk.Revit.DB.ViewDetailLevel.Fine
};

if (string.Equals(requestedDetail, "Coarse", StringComparison.OrdinalIgnoreCase))
{
    geometryOptions.DetailLevel = Autodesk.Revit.DB.ViewDetailLevel.Coarse;
}
else if (string.Equals(requestedDetail, "Medium", StringComparison.OrdinalIgnoreCase))
{
    geometryOptions.DetailLevel = Autodesk.Revit.DB.ViewDetailLevel.Medium;
}

var queriedElements = new List<object>();

for (int index = 0; index < targetIdList.Count; index++)
{
    long elementIdValue = targetIdList[index];
    var sourceElement = revitDoc.GetElement(new Autodesk.Revit.DB.ElementId(elementIdValue));
    if (sourceElement == null) continue;

    var geometryElement = sourceElement.get_Geometry(geometryOptions);
    var (solidCount, totalVolumeCubicFeet, totalAreaSquareFeet) = ComputeSolids(geometryElement);

    var boundingBox = sourceElement.get_BoundingBox(null);
    object? boundingBoxData = null;
    if (boundingBox != null)
    {
        boundingBoxData = new
        {
            minX = Math.Round(boundingBox.Min.X / feetPerMeter, 3),
            minY = Math.Round(boundingBox.Min.Y / feetPerMeter, 3),
            minZ = Math.Round(boundingBox.Min.Z / feetPerMeter, 3),
            maxX = Math.Round(boundingBox.Max.X / feetPerMeter, 3),
            maxY = Math.Round(boundingBox.Max.Y / feetPerMeter, 3),
            maxZ = Math.Round(boundingBox.Max.Z / feetPerMeter, 3)
        };
    }

    queriedElements.Add(new
    {
        elementId = elementIdValue,
        category = sourceElement.Category?.Name ?? "Unassigned",
        name = sourceElement.Name,
        solidCount,
        volumeCubicMeters = Math.Round(totalVolumeCubicFeet / feetCubedPerCubicMeter, 3),
        surfaceAreaSquareMeters = extractFaces ? Math.Round(totalAreaSquareFeet / feetSquaredPerSquareMeter, 3) : 0.0,
        bounds = boundingBoxData
    });
}

return new
{
    success = true,
    queriedCount = queriedElements.Count,
    elements = queriedElements
};
