string destinationUnits = args.Str("targetRhinoUnits", "Meters");
bool organizeByLevel = args.Bool("groupByLevel", true);

const double feetPerMeter = 3.280839895013123;
double scaleFactor = 1.0 / feetPerMeter;
if (string.Equals(destinationUnits, "Millimeters", StringComparison.OrdinalIgnoreCase))
{
    scaleFactor = 1000.0 / feetPerMeter;
}
else if (string.Equals(destinationUnits, "Feet", StringComparison.OrdinalIgnoreCase))
{
    scaleFactor = 1.0;
}

IReadOnlyList<string> categoriesToScan = args.Has("categories")
    ? args.Strings("categories")
    : new[] { "OST_Columns", "OST_Framing", "OST_Floors", "OST_Walls" };

var generatedLayers = new List<object>();
int totalExportedCount = 0;

var projectLevels = new Autodesk.Revit.DB.FilteredElementCollector(revitDoc)
    .OfClass(typeof(Autodesk.Revit.DB.Level))
    .Cast<Autodesk.Revit.DB.Level>()
    .OrderBy(lvl => lvl.Elevation)
    .ToList();

for (int categoryIndex = 0; categoryIndex < categoriesToScan.Count; categoryIndex++)
{
    string categoryName = categoriesToScan[categoryIndex];
    string cleanCategoryName = categoryName.Replace("OST_", "");
    bool hasBuiltInCategory = Enum.TryParse<Autodesk.Revit.DB.BuiltInCategory>(categoryName, out var builtInCategory);

    if (!organizeByLevel || projectLevels.Count == 0)
    {
        var flatCollector = hasBuiltInCategory
            ? new Autodesk.Revit.DB.FilteredElementCollector(revitDoc).OfCategory(builtInCategory).WhereElementIsNotElementType()
            : null;
        int flatCount = flatCollector?.GetElementCount() ?? 0;

        generatedLayers.Add(new
        {
            layerPath = cleanCategoryName,
            parentLayer = (string?)null,
            leafName = cleanCategoryName,
            colorRgb = new[] { 120 + (categoryIndex * 30), 140, 180 },
            unitScale = scaleFactor,
            elementsFound = flatCount
        });
        totalExportedCount += flatCount;
        continue;
    }

    for (int levelIndex = 0; levelIndex < projectLevels.Count; levelIndex++)
    {
        var levelItem = projectLevels[levelIndex];
        string fullLayerPath = $"{levelItem.Name}::{cleanCategoryName}";

        var levelCollector = hasBuiltInCategory
            ? new Autodesk.Revit.DB.FilteredElementCollector(revitDoc).OfCategory(builtInCategory).WhereElementIsNotElementType().WherePasses(new Autodesk.Revit.DB.ElementLevelFilter(levelItem.Id))
            : null;
        int levelCount = levelCollector?.GetElementCount() ?? 0;

        generatedLayers.Add(new
        {
            layerPath = fullLayerPath,
            parentLayer = levelItem.Name,
            leafName = cleanCategoryName,
            colorRgb = new[] { 100 + (categoryIndex * 30), 120 + (levelIndex * 20), 160 },
            unitScale = scaleFactor,
            elementsFound = levelCount
        });
        totalExportedCount += levelCount;
    }
}

return new
{
    success = true,
    targetUnits = destinationUnits,
    layerCount = generatedLayers.Count,
    estimatedExportCount = totalExportedCount,
    levelsIncluded = projectLevels.Select(lvl => lvl.Name).ToList(),
    layers = generatedLayers
};
