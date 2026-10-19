long beamIdValue = args.Long("beamElementId");
double denseSpacing = args.Double("denseSpacingMm", 100.0);
double sparseSpacing = args.Double("sparseSpacingMm", 200.0);
double zoneRatio = args.Double("supportZoneRatio", 0.25);
double barDiameter = args.Double("stirrupDiameterMm", 8.0);

if (denseSpacing <= 0.0 || sparseSpacing <= 0.0)
{
    return new { success = false, message = "denseSpacingMm and sparseSpacingMm must be greater than zero." };
}
if (zoneRatio <= 0.0 || zoneRatio >= 0.5)
{
    return new { success = false, message = "supportZoneRatio must be between 0.0 and 0.5." };
}
if (barDiameter <= 0.0)
{
    return new { success = false, message = "stirrupDiameterMm must be greater than zero." };
}

const double feetPerMeter = 3.280839895013123;
const double feetPerMm = 0.003280839895013123;

var beamElement = revitDoc.GetElement(new Autodesk.Revit.DB.ElementId(beamIdValue));
if (beamElement == null)
{
    return new { success = false, message = $"Beam element {beamIdValue} not found." };
}

var availableBarTypes = new Autodesk.Revit.DB.FilteredElementCollector(revitDoc)
    .OfClass(typeof(Autodesk.Revit.DB.Structure.RebarBarType))
    .Cast<Autodesk.Revit.DB.Structure.RebarBarType>()
    .ToList();

if (availableBarTypes.Count == 0)
{
    return new { success = false, message = "No RebarBarType found in Revit project." };
}

Autodesk.Revit.DB.Structure.RebarBarType? matchedBarType = null;
double minDiameterDifference = double.MaxValue;
for (int typeIndex = 0; typeIndex < availableBarTypes.Count; typeIndex++)
{
    var candidateType = availableBarTypes[typeIndex];
    double candidateDiameterMm = candidateType.BarNominalDiameter / feetPerMm;
    double diameterDifference = Math.Abs(candidateDiameterMm - barDiameter);
    if (diameterDifference < minDiameterDifference)
    {
        minDiameterDifference = diameterDifference;
        matchedBarType = candidateType;
    }
}
if (matchedBarType == null)
{
    matchedBarType = availableBarTypes[0];
}

double beamSpanLength = 6.0;
if (beamElement.Location is Autodesk.Revit.DB.LocationCurve locationCurve)
{
    beamSpanLength = locationCurve.Curve.Length / feetPerMeter;
}

double supportZoneLength = beamSpanLength * zoneRatio;
double midSpanZoneLength = beamSpanLength - (2.0 * supportZoneLength);

int leftStirrupCount = Math.Max(2, (int)Math.Round((supportZoneLength * 1000.0) / denseSpacing));
int midStirrupCount = Math.Max(2, (int)Math.Round((midSpanZoneLength * 1000.0) / sparseSpacing));
int rightStirrupCount = leftStirrupCount;
int totalStirrups = leftStirrupCount + midStirrupCount + rightStirrupCount;

return new
{
    success = true,
    beamId = beamIdValue,
    calculatedSpanMeters = Math.Round(beamSpanLength, 3),
    targetBarDiameterMm = barDiameter,
    matchedBarTypeName = matchedBarType.Name,
    matchedBarDiameterMm = Math.Round(matchedBarType.BarNominalDiameter / feetPerMm, 2),
    zones = new
    {
        supportLeft = new { lengthMeters = Math.Round(supportZoneLength, 3), spacingMm = denseSpacing, count = leftStirrupCount },
        midSpan = new { lengthMeters = Math.Round(midSpanZoneLength, 3), spacingMm = sparseSpacing, count = midStirrupCount },
        supportRight = new { lengthMeters = Math.Round(supportZoneLength, 3), spacingMm = denseSpacing, count = rightStirrupCount }
    },
    totalStirrupCount = totalStirrups
};
