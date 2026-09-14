Point3d P(HPRebar.McpBridge.Core.Scripting.ScriptArgs point, string key)
{
    if (!point.Has("x") || !point.Has("y")) throw new ArgumentException($"{key} must be an object {{x, y}} in millimetres.");
    return new Point3d(units.ToDrawing(point.Double("x")), units.ToDrawing(point.Double("y")), 0);
}
var center = P(args.Obj("center"), "center");
double radiusMm = args.Double("radiusMm");
if (radiusMm <= 0) throw new ArgumentException("radiusMm must be greater than 0.");
string layer = args.Str("layer");
void RequireLayer(string layerName)
{
    var table = (LayerTable)tr.GetObject(db.LayerTableId, OpenMode.ForRead);
    if (!table.Has(layerName)) throw new ArgumentException($"Layer '{layerName}' does not exist in this drawing; run create_layer first or omit layer.");
}

var circle = new Circle(center, Vector3d.ZAxis, units.ToDrawing(radiusMm));
if (!string.IsNullOrWhiteSpace(layer)) { RequireLayer(layer); circle.Layer = layer; }

var space = (BlockTableRecord)tr.GetObject(db.CurrentSpaceId, OpenMode.ForWrite);
space.AppendEntity(circle);
tr.AddNewlyCreatedDBObject(circle, true);

log($"circle {circle.Handle}: r = {radiusMm} mm");
return new { handle = circle.Handle.ToString(), radiusMm, areaMm2 = Math.Round(circle.Area * units.MmPerUnit * units.MmPerUnit, 0) };
