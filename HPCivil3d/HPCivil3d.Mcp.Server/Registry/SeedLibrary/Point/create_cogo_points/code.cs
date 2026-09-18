var points = args.List("points");
string defaultDescription = args.Str("description", "MCP");
if (points.Count == 0 || points.Count > 500) throw new ArgumentException("points needs 1–500 entries of {x, y, elevation?} (x/y in millimetres, elevation in drawing units).");
if (civil == null) throw new InvalidOperationException("The active drawing has no Civil document.");
var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
for (int i = 0; i < points.Count; i++)
{
    if (!points[i].Has("x") || !points[i].Has("y")) throw new ArgumentException($"points[{i}] must be an object {{x, y}} in millimetres (elevation optional, drawing units).");
    string n = points[i].Str("name");
    if (!string.IsNullOrWhiteSpace(n) && !names.Add(n)) throw new ArgumentException($"points[{i}]: the name '{n}' is used twice in this batch.");
}

var items = new List<object>();
var affectedHandles = new List<string>();
uint firstNumber = 0;
for (int i = 0; i < points.Count; i++)
{
    ct.ThrowIfCancellationRequested();
    var p = points[i];
    var location = new Point3d(units.ToDrawing(p.Double("x")), units.ToDrawing(p.Double("y")), p.Double("elevation", 0));
    string description = p.Str("description", defaultDescription);
    ObjectId id = civil.CogoPoints.Add(location, description, true);
    string name = p.Str("name");
    if (!string.IsNullOrWhiteSpace(name)) civil.CogoPoints.SetPointName(id, name);
    var created = (CogoPoint)tr.GetObject(id, OpenMode.ForRead);
    if (firstNumber == 0) firstNumber = created.PointNumber;
    items.Add(new { index = i, handle = id.Handle.ToString(), number = created.PointNumber, name = created.PointName });
    affectedHandles.Add(id.Handle.ToString());
}
log($"{items.Count} COGO points created (numbers from {firstNumber})");
return new { success = true, summary = $"{items.Count} COGO points created, elevations in {units.Label}", createdCount = items.Count, modifiedCount = 0, deletedCount = 0, drawingUnit = units.Label, lengthUnit = "mm", items, affectedHandles, warnings = new List<string>(), errors = new List<object>() };
