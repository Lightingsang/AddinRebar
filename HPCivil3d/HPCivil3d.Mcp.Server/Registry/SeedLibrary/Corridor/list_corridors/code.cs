string namePattern = args.Str("namePattern", "*");
int limit = args.Int("limit", 50);
if (limit <= 0 || limit > 500) throw new ArgumentException("limit must be 1–500.");
if (civil == null) throw new InvalidOperationException("The active drawing has no Civil document.");
bool Like(string text, string pattern) => string.IsNullOrEmpty(pattern) || pattern == "*" || System.Text.RegularExpressions.Regex.IsMatch(text ?? "",
    "^" + System.Text.RegularExpressions.Regex.Escape(pattern).Replace("\\*", ".*").Replace("\\?", ".") + "$", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
var errors = new List<object>();
void Fail(string code, string message, string handle) => errors.Add(new { code, message, handle });
string Classify(Exception ex) => ex is Autodesk.AutoCAD.Runtime.Exception acad ? acad.ErrorStatus.ToString() : ex.GetType().Name;

string NameOf(ObjectId id) { if (id.IsNull) return null; try { return (tr.GetObject(id, OpenMode.ForRead) as Autodesk.Civil.DatabaseServices.Entity)?.Name; } catch { return null; } }
var items = new List<object>();
int matched = 0;
foreach (ObjectId id in civil.CorridorCollection)
{
    ct.ThrowIfCancellationRequested();
    try
    {
        var c = (Corridor)tr.GetObject(id, OpenMode.ForRead);
        if (!Like(c.Name, namePattern)) continue;
        matched++;
        if (items.Count >= limit) continue;
        var baselines = new List<object>();
        foreach (Baseline b in c.Baselines) baselines.Add(new { name = b.Name, alignment = NameOf(b.AlignmentId), profile = NameOf(b.ProfileId), regionCount = b.BaselineRegions.Count });
        var surfaces = new List<object>();
        foreach (CorridorSurface s in c.CorridorSurfaces) surfaces.Add(new { name = s.Name, surface = NameOf(s.SurfaceId) });
        items.Add(new { handle = id.Handle.ToString(), name = c.Name, isOutOfDate = c.IsOutOfDate, rebuildAutomatic = c.RebuildAutomatic, codeSetStyle = c.CodeSetStyleName, baselines, surfaces });
    }
    catch (Exception ex) { Fail(Classify(ex), ex.Message, id.Handle.ToString()); }
}
log($"{items.Count} corridors");
return new { success = errors.Count == 0 || items.Count > 0, summary = $"{items.Count} of {matched} matching corridors", count = items.Count, truncated = matched > items.Count, drawingUnit = units.Label, lengthUnit = "mm", items, warnings = new List<string>(), errors };
