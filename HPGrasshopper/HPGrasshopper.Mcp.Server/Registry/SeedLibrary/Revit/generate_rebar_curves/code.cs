long hostIdNumber = args.Long("hostElementId");
var curveSetElements = args.List("rebarCurves");
double barDiameter = args.Double("barDiameterMm", 16.0);
double diameterTolerance = args.Double("diameterToleranceMm", 2.0);

if (barDiameter <= 0.0)
{
    return new { success = false, message = "barDiameterMm must be greater than zero." };
}
if (diameterTolerance < 0.0)
{
    return new { success = false, message = "diameterToleranceMm cannot be negative." };
}
if (curveSetElements.Count == 0)
{
    return new { success = false, message = "rebarCurves array must contain at least one curve set." };
}

const double feetPerMeter = 3.280839895013123;
const double feetPerMm = 0.003280839895013123;
var createdRebarIds = new List<long>();

var hostElement = revitDoc.GetElement(new Autodesk.Revit.DB.ElementId(hostIdNumber));
if (hostElement == null)
{
    return new { success = false, message = $"Host element {hostIdNumber} not found in document." };
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

if (matchedBarType == null || minDiameterDifference > diameterTolerance)
{
    string closestName = matchedBarType?.Name ?? "None";
    double closestMm = matchedBarType != null ? Math.Round(matchedBarType.BarNominalDiameter / feetPerMm, 2) : 0.0;
    return new
    {
        success = false,
        message = $"No RebarBarType matching diameter {barDiameter}mm found within tolerance ±{diameterTolerance}mm. Closest available is '{closestName}' ({closestMm}mm, diff: {Math.Round(minDiameterDifference, 2)}mm)."
    };
}

(List<Autodesk.Revit.DB.Curve>? Segments, int ZeroLengthIndex) ParseSegments(System.Text.Json.JsonElement element, double scale)
{
    var segments = new List<Autodesk.Revit.DB.Curve>();
    int segmentIndex = 0;
    foreach (var segmentItem in element.EnumerateArray())
    {
        double startX = segmentItem.GetProperty("startX").GetDouble() * scale;
        double startY = segmentItem.GetProperty("startY").GetDouble() * scale;
        double startZ = segmentItem.GetProperty("startZ").GetDouble() * scale;
        double endX = segmentItem.GetProperty("endX").GetDouble() * scale;
        double endY = segmentItem.GetProperty("endY").GetDouble() * scale;
        double endZ = segmentItem.GetProperty("endZ").GetDouble() * scale;
        var startPoint = new Autodesk.Revit.DB.XYZ(startX, startY, startZ);
        var endPoint = new Autodesk.Revit.DB.XYZ(endX, endY, endZ);
        if (startPoint.DistanceTo(endPoint) < 0.001)
        {
            return (null, segmentIndex);
        }
        segments.Add(Autodesk.Revit.DB.Line.CreateBound(startPoint, endPoint));
        segmentIndex++;
    }
    return (segments, -1);
}

Autodesk.Revit.DB.XYZ ComputeCurveNormal(List<Autodesk.Revit.DB.XYZ> tangents)
{
    for (int firstIdx = 0; firstIdx < tangents.Count - 1; firstIdx++)
    {
        for (int secondIdx = firstIdx + 1; secondIdx < tangents.Count; secondIdx++)
        {
            var crossProduct = tangents[firstIdx].CrossProduct(tangents[secondIdx]);
            if (crossProduct.GetLength() > 0.001) return crossProduct.Normalize();
        }
    }
    var primaryTangent = tangents[0];
    var referenceAxis = Math.Abs(primaryTangent.Z) < 0.85
        ? new Autodesk.Revit.DB.XYZ(0.0, 0.0, 1.0)
        : new Autodesk.Revit.DB.XYZ(1.0, 0.0, 0.0);
    return primaryTangent.CrossProduct(referenceAxis).Normalize();
}

bool CheckCoplanar(List<Autodesk.Revit.DB.Curve> segments, List<Autodesk.Revit.DB.XYZ> tangents, Autodesk.Revit.DB.XYZ normal)
{
    for (int checkIdx = 0; checkIdx < tangents.Count; checkIdx++)
    {
        if (Math.Abs(tangents[checkIdx].DotProduct(normal)) > 0.01) return false;
    }
    if (segments.Count > 0)
    {
        var origin = segments[0].GetEndPoint(0);
        for (int segIdx = 0; segIdx < segments.Count; segIdx++)
        {
            if (Math.Abs((segments[segIdx].GetEndPoint(0) - origin).DotProduct(normal)) > 0.01) return false;
            if (Math.Abs((segments[segIdx].GetEndPoint(1) - origin).DotProduct(normal)) > 0.01) return false;
        }
    }
    return true;
}

var validatedSets = new List<(Autodesk.Revit.DB.XYZ Normal, List<Autodesk.Revit.DB.Curve> Curves)>();

for (int setIndex = 0; setIndex < curveSetElements.Count; setIndex++)
{
    var rawItem = curveSetElements[setIndex].Raw;
    if (!rawItem.HasValue || rawItem.Value.ValueKind != System.Text.Json.JsonValueKind.Array)
    {
        return new { success = false, message = $"Rebar curve set {setIndex} must be an array of curve segments." };
    }
    var (curveSegments, zeroLengthIndex) = ParseSegments(rawItem.Value, feetPerMeter);
    if (zeroLengthIndex >= 0)
    {
        return new { success = false, message = $"Rebar curve set {setIndex} contains a zero-length segment at index {zeroLengthIndex}." };
    }
    if (curveSegments == null || curveSegments.Count == 0)
    {
        return new { success = false, message = $"Rebar curve set {setIndex} must contain at least one curve segment." };
    }
    for (int segIdx = 0; segIdx < curveSegments.Count - 1; segIdx++)
    {
        double gap = curveSegments[segIdx].GetEndPoint(1).DistanceTo(curveSegments[segIdx + 1].GetEndPoint(0));
        if (gap > 0.01)
        {
            return new { success = false, message = $"Rebar curve set {setIndex} is disconnected between segment {segIdx} and {segIdx + 1}." };
        }
    }
    var tangents = new List<Autodesk.Revit.DB.XYZ>();
    for (int segmentIdx = 0; segmentIdx < curveSegments.Count; segmentIdx++)
    {
        var direction = curveSegments[segmentIdx].GetEndPoint(1) - curveSegments[segmentIdx].GetEndPoint(0);
        if (direction.GetLength() > 0.0001) tangents.Add(direction.Normalize());
    }
    if (tangents.Count == 0)
    {
        return new { success = false, message = $"Rebar curve set {setIndex} has no valid tangent directions." };
    }
    var normalVector = ComputeCurveNormal(tangents);
    if (!CheckCoplanar(curveSegments, tangents, normalVector))
    {
        return new { success = false, message = $"Rebar curve set {setIndex} is non-planar; all segments must lie in a single plane." };
    }
    validatedSets.Add((normalVector, curveSegments));
}

for (int setIndex = 0; setIndex < validatedSets.Count; setIndex++)
{
    var (normalVector, curveSegments) = validatedSets[setIndex];
    try
    {
        var rebarInstance = Autodesk.Revit.DB.Structure.Rebar.CreateFromCurves(
            revitDoc,
            Autodesk.Revit.DB.Structure.RebarStyle.Standard,
            matchedBarType,
            null,
            null,
            hostElement,
            normalVector,
            curveSegments,
            Autodesk.Revit.DB.Structure.RebarHookOrientation.Right,
            Autodesk.Revit.DB.Structure.RebarHookOrientation.Right,
            true,
            true);

        if (rebarInstance == null)
        {
            throw new InvalidOperationException($"Revit returned null creating rebar for curve set {setIndex}.");
        }
        createdRebarIds.Add(rebarInstance.Id.Value);
    }
    catch (Exception ex)
    {
        throw new InvalidOperationException($"Failed creating rebar for curve set {setIndex}: {ex.Message}", ex);
    }
}

return new
{
    success = true,
    rebarCount = createdRebarIds.Count,
    rebarIds = createdRebarIds,
    hostId = hostIdNumber,
    resolvedBarTypeName = matchedBarType.Name,
    resolvedBarDiameterMm = Math.Round(matchedBarType.BarNominalDiameter / feetPerMm, 2)
};
