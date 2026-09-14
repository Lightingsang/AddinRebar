string text = args.Str("text");
if (string.IsNullOrEmpty(text)) throw new ArgumentException("text is required.");
Point3d P(HPRebar.McpBridge.Core.Scripting.ScriptArgs point, string key)
{
    if (!point.Has("x") || !point.Has("y")) throw new ArgumentException($"{key} must be an object {{x, y}} in millimetres.");
    return new Point3d(units.ToDrawing(point.Double("x")), units.ToDrawing(point.Double("y")), 0);
}
var point = P(args.Obj("position"), "position");
double heightMm = args.Double("heightMm");
if (heightMm <= 0) throw new ArgumentException("heightMm must be greater than 0.");
double rotation = args.Double("rotationDeg", 0) * Math.PI / 180.0;
string layer = args.Str("layer");
bool mtext = args.Bool("mtext", false);
double widthMm = args.Double("widthMm", 0);
void RequireLayer(string layerName)
{
    var table = (LayerTable)tr.GetObject(db.LayerTableId, OpenMode.ForRead);
    if (!table.Has(layerName)) throw new ArgumentException($"Layer '{layerName}' does not exist in this drawing; run create_layer first or omit layer.");
}

Entity entity;
if (mtext)
{
    var m = new MText { Contents = text, Location = point, TextHeight = units.ToDrawing(heightMm), Rotation = rotation };
    if (widthMm > 0) m.Width = units.ToDrawing(widthMm);
    entity = m;
}
else
{
    entity = new DBText { TextString = text, Position = point, Height = units.ToDrawing(heightMm), Rotation = rotation };
}
if (!string.IsNullOrWhiteSpace(layer)) { RequireLayer(layer); entity.Layer = layer; }

var space = (BlockTableRecord)tr.GetObject(db.CurrentSpaceId, OpenMode.ForWrite);
space.AppendEntity(entity);
tr.AddNewlyCreatedDBObject(entity, true);

log($"{entity.GetType().Name} {entity.Handle}: '{text}'");
return new { handle = entity.Handle.ToString(), type = entity.GetType().Name, heightMm };
