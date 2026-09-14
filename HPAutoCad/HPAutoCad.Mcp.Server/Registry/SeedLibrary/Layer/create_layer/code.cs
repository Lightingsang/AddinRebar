string name = args.Str("name");
if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("name is required.");
int colorIndex = args.Int("colorIndex", 7);
if (colorIndex < 1 || colorIndex > 255) throw new ArgumentException("colorIndex must be between 1 and 255.");
int lineweight = args.Int("lineweight", -1);
string ifExists = (args.Str("ifExists", "skip") ?? "skip").Trim().ToLowerInvariant();
if (ifExists != "skip" && ifExists != "update") throw new ArgumentException("ifExists must be skip or update.");
if (lineweight >= 0 && !Enum.IsDefined(typeof(LineWeight), lineweight)) throw new ArgumentException($"lineweight {lineweight} is not a valid AutoCAD lineweight.");

var color = Color.FromColorIndex(ColorMethod.ByAci, (short)colorIndex);
var table = (LayerTable)tr.GetObject(db.LayerTableId, OpenMode.ForRead);
var created = false;
var updated = false;

if (table.Has(name))
{
    if (ifExists == "update")
    {
        var existing = (LayerTableRecord)tr.GetObject(table[name], OpenMode.ForWrite);
        existing.Color = color;
        if (lineweight >= 0) existing.LineWeight = (LineWeight)lineweight;
        updated = true;
    }
}
else
{
    table.UpgradeOpen();
    var record = new LayerTableRecord { Name = name, Color = color };
    if (lineweight >= 0) record.LineWeight = (LineWeight)lineweight;
    table.Add(record);
    tr.AddNewlyCreatedDBObject(record, true);
    created = true;
}

log($"layer {name}: created={created} updated={updated}");
return new { name, created, updated, colorIndex, lineweight = lineweight >= 0 ? (int?)lineweight : null };
