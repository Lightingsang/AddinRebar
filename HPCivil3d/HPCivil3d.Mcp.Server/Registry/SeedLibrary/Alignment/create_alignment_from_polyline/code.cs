string polylineHandle = args.Require("polyline");
string name = args.Require("name");
string site = args.Str("site", "");
string layer = args.Str("layer", "");
string style = args.Str("style", "");
string labelSet = args.Str("labelSet", "");
bool addCurves = args.Bool("addCurvesBetweenTangents", false);
bool erasePolyline = args.Bool("erasePolyline", false);
if (civil == null) throw new InvalidOperationException("The active drawing has no Civil document.");
if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("name is required.");

ObjectId plineId;
try { plineId = db.GetObjectId(false, new Handle(Convert.ToInt64(polylineHandle, 16)), 0); }
catch (Exception) { throw new ArgumentException($"'{polylineHandle}' is not a valid handle in this drawing."); }
if (plineId.IsNull || plineId.IsErased || !(tr.GetObject(plineId, OpenMode.ForRead) is Polyline)) throw new ArgumentException($"'{polylineHandle}' is not a lightweight polyline (LWPOLYLINE).");

foreach (ObjectId id in civil.GetAlignmentIds())
    if (string.Equals(((Alignment)tr.GetObject(id, OpenMode.ForRead)).Name, name, StringComparison.OrdinalIgnoreCase)) throw new ArgumentException($"An alignment named '{name}' already exists.");

string FirstStyle(TreeNodeCollectionBase styles, string given, string what)
{
    if (!string.IsNullOrWhiteSpace(given)) { if (!styles.Contains(given)) throw new ArgumentException($"{what} '{given}' does not exist in this drawing."); return given; }
    foreach (ObjectId id in styles) return ((StyleBase)tr.GetObject(id, OpenMode.ForRead)).Name;
    throw new ArgumentException($"The drawing has no {what}; create one in Civil 3D first.");
}
string styleName = FirstStyle(civil.Styles.AlignmentStyles, style, "alignment style");
string labelSetName = FirstStyle(civil.Styles.LabelSetStyles.AlignmentLabelSetStyles, labelSet, "alignment label set");
if (string.IsNullOrWhiteSpace(layer)) layer = ((LayerTableRecord)tr.GetObject(db.Clayer, OpenMode.ForRead)).Name;
else if (!((LayerTable)tr.GetObject(db.LayerTableId, OpenMode.ForRead)).Has(layer)) throw new ArgumentException($"Layer '{layer}' does not exist in this drawing.");
if (!string.IsNullOrWhiteSpace(site))
{
    bool found = false;
    foreach (ObjectId sid in civil.GetSiteIds()) if (string.Equals(((Site)tr.GetObject(sid, OpenMode.ForRead)).Name, site, StringComparison.OrdinalIgnoreCase)) found = true;
    if (!found) throw new ArgumentException($"Site '{site}' does not exist; use \"\" for a siteless alignment.");
}

var options = new PolylineOptions { PlineId = plineId, AddCurvesBetweenTangents = addCurves, EraseExistingEntities = erasePolyline };
ObjectId alignmentId = Alignment.Create(civil, options, name, site, layer, styleName, labelSetName);
var a = (Alignment)tr.GetObject(alignmentId, OpenMode.ForRead);
log($"alignment {a.Name} ({alignmentId.Handle}) from polyline {polylineHandle}: {a.Entities.Count} entities, {Math.Round(units.ToMm(a.Length), 1)} mm");
return new
{
    success = true, summary = $"Alignment '{a.Name}' created: {a.Entities.Count} entities, {Math.Round(units.ToMm(a.Length) / 1000, 2)} m, stations {a.StartingStation}–{a.EndingStation} {units.Label}",
    createdCount = 1, modifiedCount = 0, deletedCount = erasePolyline ? 1 : 0, drawingUnit = units.Label, lengthUnit = "mm",
    alignment = new { handle = alignmentId.Handle.ToString(), name = a.Name, lengthMm = Math.Round(units.ToMm(a.Length), 1), startStation = a.StartingStation, endStation = a.EndingStation, entityCount = a.Entities.Count, site = a.IsSiteless ? "" : a.SiteName, style = a.StyleName, labelSet = labelSetName, layer },
    affectedHandles = erasePolyline ? new List<string> { alignmentId.Handle.ToString(), polylineHandle } : new List<string> { alignmentId.Handle.ToString() }, warnings = new List<string>(), errors = new List<object>(),
};
