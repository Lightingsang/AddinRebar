long wallIdNumber = args.Long("wallElementId");
var openingList = args.List("openings");

const double feetPerMeter = 3.280839895013123;
var createdOpeningIds = new List<long>();

var hostElement = revitDoc.GetElement(new Autodesk.Revit.DB.ElementId(wallIdNumber));
if (hostElement == null)
{
    return new { success = false, message = $"Wall element {wallIdNumber} not found." };
}

if (hostElement is not Autodesk.Revit.DB.Wall wallInstance)
{
    return new { success = false, message = $"Element {wallIdNumber} is not a Revit Wall." };
}

if (wallInstance.Location is not Autodesk.Revit.DB.LocationCurve locationCurve)
{
    return new { success = false, message = $"Wall {wallIdNumber} has no linear LocationCurve." };
}

var wallDirection = (locationCurve.Curve.GetEndPoint(1) - locationCurve.Curve.GetEndPoint(0)).Normalize();
var wallOrigin = locationCurve.Curve.GetEndPoint(0);

for (int openingIndex = 0; openingIndex < openingList.Count; openingIndex++)
{
    var item = openingList[openingIndex];
    double startMeters = item.Double("startX");
    double bottomMeters = item.Double("bottomZ");
    double widthMeters = item.Double("width");
    double heightMeters = item.Double("height");

    double startFeet = startMeters * feetPerMeter;
    double endFeet = (startMeters + widthMeters) * feetPerMeter;
    double bottomFeet = bottomMeters * feetPerMeter;
    double topFeet = (bottomMeters + heightMeters) * feetPerMeter;

    var pointLower = wallOrigin + (wallDirection * startFeet) + new Autodesk.Revit.DB.XYZ(0.0, 0.0, bottomFeet);
    var pointUpper = wallOrigin + (wallDirection * endFeet) + new Autodesk.Revit.DB.XYZ(0.0, 0.0, topFeet);

    var openingInstance = revitDoc.Create.NewOpening(wallInstance, pointLower, pointUpper);
    createdOpeningIds.Add(openingInstance.Id.Value);
}

return new
{
    success = true,
    wallId = wallIdNumber,
    openingsCreated = createdOpeningIds.Count,
    openingIds = createdOpeningIds
};
