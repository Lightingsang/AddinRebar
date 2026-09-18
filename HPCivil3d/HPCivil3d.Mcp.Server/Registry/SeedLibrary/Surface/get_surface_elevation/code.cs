string key = args.Require("surface");
var points = args.List("points");
if (points.Count == 0 || points.Count > 500) throw new ArgumentException("points needs 1–500 entries of {x, y} in millimetres.");
if (civil == null) throw new InvalidOperationException("The active drawing has no Civil document.");
ObjectId Resolve(string key, ObjectIdCollection ids, string what)
{
    if (string.IsNullOrWhiteSpace(key)) throw new ArgumentException($"{what} is required: give its name or handle.");
    foreach (ObjectId id in ids)
    {
        if (string.Equals(id.Handle.ToString(), key, StringComparison.OrdinalIgnoreCase)) return id;
        var entity = tr.GetObject(id, OpenMode.ForRead) as Autodesk.Civil.DatabaseServices.Entity;
        if (entity != null && string.Equals(entity.Name, key, StringComparison.OrdinalIgnoreCase)) return id;
    }
    throw new ArgumentException($"No {what} named '{key}' (name or handle) in this drawing.");
}

var id = Resolve(key, civil.GetSurfaceIds(), "surface");
var surface = (Autodesk.Civil.DatabaseServices.Surface)tr.GetObject(id, OpenMode.ForRead);
var items = new List<object>();
int okCount = 0, outsideCount = 0, failedCount = 0;
for (int i = 0; i < points.Count; i++)
{
    ct.ThrowIfCancellationRequested();
    if (!points[i].Has("x") || !points[i].Has("y")) throw new ArgumentException($"points[{i}] must be an object {{x, y}} in millimetres.");
    double x = points[i].Double("x"), y = points[i].Double("y");
    try
    {
        double elevation = surface.FindElevationAtXY(units.ToDrawing(x), units.ToDrawing(y));
        okCount++;
        items.Add(new { index = i, x, y, ok = true, elevation = Math.Round(elevation, 4) });
    }
    catch (PointNotOnEntityException ex)
    {
        outsideCount++;
        items.Add(new { index = i, x, y, ok = false, error = "OUTSIDE_SURFACE", message = ex.Message });
    }
    catch (Exception ex)
    {
        failedCount++;
        items.Add(new { index = i, x, y, ok = false, error = ex.GetType().Name, message = ex.Message });
    }
}
log($"{surface.Name}: {okCount} on the surface, {outsideCount} outside, {failedCount} failed");
return new { success = failedCount == 0, summary = $"{surface.Name}: {okCount}/{points.Count} points on the surface, {outsideCount} outside", surface = new { handle = id.Handle.ToString(), name = surface.Name }, count = items.Count, okCount, outsideCount, failedCount, drawingUnit = units.Label, lengthUnit = "mm", items, warnings = new List<string>(), errors = new List<object>() };
