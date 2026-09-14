var center = args.Obj("center");
double radiusMm = args.Double("radiusMm");
if (radiusMm <= 0) throw new ArgumentException("radiusMm must be greater than 0.");
string layer = args.Str("layer");
void RequireLayer(string layerName)
{
    var table = (LayerTable)tr.GetObject(db.LayerTableId, OpenMode.ForRead);
    if (!table.Has(layerName)) throw new ArgumentException($"Layer '{layerName}' does not exist in this drawing; run create_layer first or omit layer.");
}

var circle = new Circle(new Point3d(units.ToDrawing(center.Double("x")), units.ToDrawing(center.Double("y")), 0), Vector3d.ZAxis, units.ToDrawing(radiusMm));
if (!string.IsNullOrWhiteSpace(layer)) { RequireLayer(layer); circle.Layer = layer; }

var space = (BlockTableRecord)tr.GetObject(db.CurrentSpaceId, OpenMode.ForWrite);
space.AppendEntity(circle);
tr.AddNewlyCreatedDBObject(circle, true);

log($"circle {circle.Handle}: r = {radiusMm} mm");
return new { handle = circle.Handle.ToString(), radiusMm, areaMm2 = Math.Round(circle.Area * units.MmPerUnit * units.MmPerUnit, 0) };
