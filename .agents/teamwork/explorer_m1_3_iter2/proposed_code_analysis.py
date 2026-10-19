import re

# Proposed ParseSegments and validation loop for generate_rebar_curves/code.cs

parse_segments = """(List<Autodesk.Revit.DB.Curve>? Segments, int ZeroLengthIndex) ParseSegments(System.Text.Json.JsonElement element, double scale)
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
}"""

validation_loop = """for (int setIndex = 0; setIndex < curveSetElements.Count; setIndex++)
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
}"""

print(f"ParseSegments lines: {len(parse_segments.splitlines())}")
print(f"Validation loop lines: {len(validation_loop.splitlines())}")

# Check vague names
vague_names = {"data", "tmp", "temp", "obj", "res", "val", "foo", "bar", "stuff", "thing"}
tokens = re.findall(r'\b[a-zA-Z_][a-zA-Z0-9_]*\b', parse_segments + " " + validation_loop)
found_vague = [t for t in tokens if t.lower() in vague_names]
print(f"Vague names found: {found_vague}")

# Check boolean parameters
bool_params = re.findall(r'\bbool\s+[a-zA-Z_]', parse_segments)
print(f"Bool params: {bool_params}")
