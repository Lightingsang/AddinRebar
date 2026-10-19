long wallIdValue = args.Long("curtainWallElementId");
double horizontalSpacing = args.Double("uSpacingMeters", 1.5);
double verticalSpacing = args.Double("vSpacingMeters", 3.0);
string spacingRule = args.Str("layoutRule", "FixedDistance");

if (horizontalSpacing <= 0.0 || verticalSpacing <= 0.0)
{
    return new { success = false, message = "uSpacingMeters and vSpacingMeters must be greater than zero." };
}

const double feetPerMeter = 3.280839895013123;

var targetElement = revitDoc.GetElement(new Autodesk.Revit.DB.ElementId(wallIdValue));
if (targetElement == null)
{
    return new { success = false, message = $"Element {wallIdValue} not found in Revit document." };
}

if (targetElement is not Autodesk.Revit.DB.Wall curtainWall)
{
    return new { success = false, message = $"Element {wallIdValue} is not a Revit Wall." };
}

var curtainGrid = curtainWall.CurtainGrid;
if (curtainGrid == null)
{
    return new { success = false, message = $"Wall {wallIdValue} has no active CurtainGrid (not a curtain wall family)." };
}

double wallLengthFeet = 30.0;
if (curtainWall.Location is Autodesk.Revit.DB.LocationCurve curveLocation)
{
    wallLengthFeet = curveLocation.Curve.Length;
}

double wallLengthMeters = wallLengthFeet / feetPerMeter;
int calculatedULineCount = Math.Max(1, (int)Math.Floor(wallLengthMeters / horizontalSpacing));
var gridPositions = new List<double>();

for (int uIndex = 1; uIndex <= calculatedULineCount; uIndex++)
{
    double offsetMeters = uIndex * horizontalSpacing;
    if (offsetMeters < wallLengthMeters)
    {
        gridPositions.Add(Math.Round(offsetMeters, 3));
    }
}

return new
{
    success = true,
    curtainWallId = wallIdValue,
    wallLengthMeters = Math.Round(wallLengthMeters, 3),
    uSpacingMeters = horizontalSpacing,
    vSpacingMeters = verticalSpacing,
    rule = spacingRule,
    uGridLinesCount = gridPositions.Count,
    uGridPositionsMeters = gridPositions
};
