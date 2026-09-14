var points = args.List("points");
if (points.Count < 2) throw new ArgumentException("points needs at least 2 entries of {x, y} in millimetres.");
bool closed = args.Bool("closed", false);
string layer = args.Str("layer");
int colorIndex = args.Int("colorIndex", -1);
void RequireLayer(string layerName)
{
    var table = (LayerTable)tr.GetObject(db.LayerTableId, OpenMode.ForRead);
    if (!table.Has(layerName)) throw new ArgumentException($"Layer '{layerName}' does not exist in this drawing; run create_layer first or omit layer.");
}

var polyline = new Polyline();
for (var i = 0; i < points.Count; i++)
{
    if (!points[i].Has("x") || !points[i].Has("y")) throw new ArgumentException($"points[{i}] must be an object {{x, y}} in millimetres.");
    polyline.AddVertexAt(i, new Point2d(units.ToDrawing(points[i].Double("x")), units.ToDrawing(points[i].Double("y"))), 0, 0, 0);
}
polyline.Closed = closed;
if (!string.IsNullOrWhiteSpace(layer)) { RequireLayer(layer); polyline.Layer = layer; }
if (colorIndex >= 0) polyline.ColorIndex = colorIndex;

var space = (BlockTableRecord)tr.GetObject(db.CurrentSpaceId, OpenMode.ForWrite);
space.AppendEntity(polyline);
tr.AddNewlyCreatedDBObject(polyline, true);

var lengthMm = Math.Round(units.ToMm(polyline.Length), 1);
log($"polyline {polyline.Handle}: {polyline.NumberOfVertices} vertices, {lengthMm} mm");
return new { handle = polyline.Handle.ToString(), lengthMm, vertexCount = polyline.NumberOfVertices, closed };
