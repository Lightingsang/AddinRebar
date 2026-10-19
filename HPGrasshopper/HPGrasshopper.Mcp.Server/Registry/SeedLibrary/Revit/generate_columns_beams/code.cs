var columnPointElements = args.List("columnPoints");
var beamLineElements = args.List("beamLines");
double heightMeters = args.Double("columnHeight", 3.6);

const double feetPerMeter = 3.280839895013123;
var createdColumnIds = new List<long>();
var createdBeamIds = new List<long>();

var defaultLevel = new Autodesk.Revit.DB.FilteredElementCollector(revitDoc)
    .OfClass(typeof(Autodesk.Revit.DB.Level))
    .Cast<Autodesk.Revit.DB.Level>()
    .OrderBy(lvl => lvl.Elevation)
    .FirstOrDefault();

if (defaultLevel == null)
{
    defaultLevel = Autodesk.Revit.DB.Level.Create(revitDoc, 0.0);
}

var columnSymbol = new Autodesk.Revit.DB.FilteredElementCollector(revitDoc)
    .OfCategory(Autodesk.Revit.DB.BuiltInCategory.OST_StructuralColumns)
    .OfClass(typeof(Autodesk.Revit.DB.FamilySymbol))
    .Cast<Autodesk.Revit.DB.FamilySymbol>()
    .FirstOrDefault();

if (columnSymbol != null && !columnSymbol.IsActive)
{
    columnSymbol.Activate();
}

var beamSymbol = new Autodesk.Revit.DB.FilteredElementCollector(revitDoc)
    .OfCategory(Autodesk.Revit.DB.BuiltInCategory.OST_StructuralFraming)
    .OfClass(typeof(Autodesk.Revit.DB.FamilySymbol))
    .Cast<Autodesk.Revit.DB.FamilySymbol>()
    .FirstOrDefault();

if (beamSymbol != null && !beamSymbol.IsActive)
{
    beamSymbol.Activate();
}

if (columnSymbol != null)
{
    for (int pointIndex = 0; pointIndex < columnPointElements.Count; pointIndex++)
    {
        var item = columnPointElements[pointIndex];
        double pointX = item.Double("x") * feetPerMeter;
        double pointY = item.Double("y") * feetPerMeter;
        double pointZ = item.Double("z", 0.0) * feetPerMeter;

        var insertionPoint = new Autodesk.Revit.DB.XYZ(pointX, pointY, pointZ);
        var columnInstance = revitDoc.Create.NewFamilyInstance(
            insertionPoint,
            columnSymbol,
            defaultLevel,
            Autodesk.Revit.DB.Structure.StructuralType.Column);

        createdColumnIds.Add(columnInstance.Id.Value);
    }
}

if (beamSymbol != null)
{
    for (int lineIndex = 0; lineIndex < beamLineElements.Count; lineIndex++)
    {
        var item = beamLineElements[lineIndex];
        double startX = item.Double("startX") * feetPerMeter;
        double startY = item.Double("startY") * feetPerMeter;
        double startZ = item.Double("startZ", heightMeters) * feetPerMeter;
        double endX = item.Double("endX") * feetPerMeter;
        double endY = item.Double("endY") * feetPerMeter;
        double endZ = item.Double("endZ", heightMeters) * feetPerMeter;

        var startPoint = new Autodesk.Revit.DB.XYZ(startX, startY, startZ);
        var endPoint = new Autodesk.Revit.DB.XYZ(endX, endY, endZ);
        var centerLine = Autodesk.Revit.DB.Line.CreateBound(startPoint, endPoint);

        var beamInstance = revitDoc.Create.NewFamilyInstance(
            centerLine,
            beamSymbol,
            defaultLevel,
            Autodesk.Revit.DB.Structure.StructuralType.Beam);

        createdBeamIds.Add(beamInstance.Id.Value);
    }
}

return new
{
    success = true,
    columnsCreated = createdColumnIds.Count,
    beamsCreated = createdBeamIds.Count,
    columnIds = createdColumnIds,
    beamIds = createdBeamIds
};
