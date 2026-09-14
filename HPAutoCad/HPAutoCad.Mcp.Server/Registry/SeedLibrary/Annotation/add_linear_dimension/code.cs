Point3d P(HPRebar.McpBridge.Core.Scripting.ScriptArgs point, string key)
{
    if (!point.Has("x") || !point.Has("y")) throw new ArgumentException($"{key} must be an object {{x, y}} in millimetres.");
    return new Point3d(units.ToDrawing(point.Double("x")), units.ToDrawing(point.Double("y")), 0);
}
void RequireLayer(string layerName)
{
    var table = (LayerTable)tr.GetObject(db.LayerTableId, OpenMode.ForRead);
    if (!table.Has(layerName)) throw new ArgumentException($"Layer '{layerName}' does not exist in this drawing; run create_layer first or omit layer.");
}

var p1 = P(args.Obj("p1"), "p1");
var p2 = P(args.Obj("p2"), "p2");
var dimLinePoint = P(args.Obj("dimLinePoint"), "dimLinePoint");
bool aligned = args.Bool("aligned", false);
double rotation = args.Double("rotationDeg", 0) * Math.PI / 180.0;
string dimStyle = args.Str("dimStyle");
string layer = args.Str("layer");

var styleId = db.Dimstyle;
if (!string.IsNullOrWhiteSpace(dimStyle))
{
    var styles = (DimStyleTable)tr.GetObject(db.DimStyleTableId, OpenMode.ForRead);
    if (!styles.Has(dimStyle)) throw new ArgumentException($"Dimension style '{dimStyle}' does not exist in this drawing.");
    styleId = styles[dimStyle];
}

Dimension dimension = aligned
    ? new AlignedDimension(p1, p2, dimLinePoint, "", styleId)
    : new RotatedDimension(rotation, p1, p2, dimLinePoint, "", styleId);
if (!string.IsNullOrWhiteSpace(layer)) { RequireLayer(layer); dimension.Layer = layer; }

var space = (BlockTableRecord)tr.GetObject(db.CurrentSpaceId, OpenMode.ForWrite);
space.AppendEntity(dimension);
tr.AddNewlyCreatedDBObject(dimension, true);

var measurementMm = Math.Round(units.ToMm(dimension.Measurement), 1);
log($"{dimension.GetType().Name} {dimension.Handle}: {measurementMm} mm");
return new { handle = dimension.Handle.ToString(), type = dimension.GetType().Name, measurementMm };
