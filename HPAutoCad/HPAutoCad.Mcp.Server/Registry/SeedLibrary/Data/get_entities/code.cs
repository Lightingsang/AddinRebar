string type = args.Str("type");
string layer = args.Str("layer");
string space = (args.Str("space", "model") ?? "model").Trim().ToLowerInvariant();
int limit = Math.Clamp(args.Int("limit", 200), 1, 2000);
double Mm(double du) => Math.Round(units.ToMm(du), 1);

var filter = new List<TypedValue>();
if (!string.IsNullOrWhiteSpace(type)) filter.Add(new TypedValue((int)DxfCode.Start, type.Trim().ToUpperInvariant()));
if (!string.IsNullOrWhiteSpace(layer)) filter.Add(new TypedValue((int)DxfCode.LayerName, layer));
if (space == "model") filter.Add(new TypedValue((int)DxfCode.LayoutName, "Model"));
else if (space == "current") filter.Add(new TypedValue((int)DxfCode.LayoutName, LayoutManager.Current.CurrentLayout));
else if (space != "all") throw new ArgumentException("space must be model, current or all.");

var selection = filter.Count == 0 ? ed.SelectAll() : ed.SelectAll(new SelectionFilter(filter.ToArray()));
if (selection.Status != PromptStatus.OK || selection.Value == null)
{
    log("no entity matches");
    return new { count = 0, truncated = false, items = new List<object>() };
}

var ids = selection.Value.GetObjectIds();
var items = new List<object>();
foreach (var id in ids.Take(limit))
{
    ct.ThrowIfCancellationRequested();
    if (tr.GetObject(id, OpenMode.ForRead) is not Entity entity) continue;

    object bbox = null;
    try
    {
        var extents = entity.GeometricExtents;
        bbox = new
        {
            min = new { x = Mm(extents.MinPoint.X), y = Mm(extents.MinPoint.Y), z = Mm(extents.MinPoint.Z) },
            max = new { x = Mm(extents.MaxPoint.X), y = Mm(extents.MaxPoint.Y), z = Mm(extents.MaxPoint.Z) },
        };
    }
    catch (Autodesk.AutoCAD.Runtime.Exception)
    {
        // some entities (empty blocks, rays) have no finite extents
    }

    items.Add(new { handle = entity.Handle.ToString(), type = id.ObjectClass.DxfName ?? id.ObjectClass.Name, layer = entity.Layer, bboxMm = bbox });
}

log($"{ids.Length} entities match, returning {items.Count}");
return new { count = ids.Length, truncated = ids.Length > limit, items };
