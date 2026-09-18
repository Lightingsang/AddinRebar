string pointGroup = args.Str("pointGroup");
string descriptionPattern = args.Str("descriptionPattern", "*");
long numberFrom = args.Long("numberFrom", -1);
long numberTo = args.Long("numberTo", -1);
int limit = args.Int("limit", 200);
int offset = args.Int("offset", 0);
if (limit <= 0 || limit > 300) throw new ArgumentException("limit must be 1–300.");
if (offset < 0) throw new ArgumentException("offset must be >= 0.");
if (numberFrom >= 0 && numberTo >= 0 && numberTo < numberFrom) throw new ArgumentException("numberTo must be >= numberFrom.");
if (civil == null) throw new InvalidOperationException("The active drawing has no Civil document.");
if (!string.IsNullOrWhiteSpace(pointGroup) && !civil.PointGroups.Contains(pointGroup)) throw new ArgumentException($"No point group named '{pointGroup}' in this drawing.");
bool Like(string text, string pattern) => string.IsNullOrEmpty(pattern) || pattern == "*" || System.Text.RegularExpressions.Regex.IsMatch(text ?? "",
    "^" + System.Text.RegularExpressions.Regex.Escape(pattern).Replace("\\*", ".*").Replace("\\?", ".") + "$", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
var errors = new List<object>();
void Fail(string code, string message, string handle) => errors.Add(new { code, message, handle });
string Classify(Exception ex) => ex is Autodesk.AutoCAD.Runtime.Exception acad ? acad.ErrorStatus.ToString() : ex.GetType().Name;

var groupNames = new Dictionary<ObjectId, string>();
string GroupName(ObjectId id)
{
    if (id.IsNull) return null;
    if (!groupNames.TryGetValue(id, out var name)) { try { name = ((PointGroup)tr.GetObject(id, OpenMode.ForRead)).Name; } catch { name = null; } groupNames[id] = name; }
    return name;
}
// a narrow number range is looked up by number; otherwise every point is scanned (the collection has no index by anything else)
IEnumerable<ObjectId> Candidates()
{
    if (numberFrom >= 0 && numberTo >= 0 && numberTo - numberFrom <= 5000)
    {
        for (long n = numberFrom; n <= numberTo; n++) if (civil.CogoPoints.Contains((uint)n)) yield return civil.CogoPoints.GetPointByPointNumber((uint)n);
    }
    else foreach (ObjectId id in civil.GetAllPointIds()) yield return id;
}
var rows = new List<CogoPoint>();
foreach (ObjectId id in Candidates())
{
    ct.ThrowIfCancellationRequested();
    try
    {
        var p = (CogoPoint)tr.GetObject(id, OpenMode.ForRead);
        if (numberFrom >= 0 && p.PointNumber < numberFrom) continue;
        if (numberTo >= 0 && p.PointNumber > numberTo) continue;
        if (!Like(p.FullDescription, descriptionPattern)) continue;
        if (!string.IsNullOrWhiteSpace(pointGroup) && !string.Equals(GroupName(p.PrimaryPointGroupId), pointGroup, StringComparison.OrdinalIgnoreCase)) continue;
        rows.Add(p);
    }
    catch (Exception ex) { Fail(Classify(ex), ex.Message, id.Handle.ToString()); }
}
var items = rows.OrderBy(p => p.PointNumber).Skip(offset).Take(limit).Select(p => new
{
    handle = p.Handle.ToString(), number = p.PointNumber, name = p.PointName, x = Math.Round(units.ToMm(p.Easting), 1), y = Math.Round(units.ToMm(p.Northing), 1), elevation = Math.Round(p.Elevation, 4),
    rawDescription = p.RawDescription, fullDescription = p.FullDescription, pointGroup = GroupName(p.PrimaryPointGroupId),
}).ToList();
log($"{items.Count} of {rows.Count} matching COGO points (total {civil.CogoPoints.Count})");
return new { success = errors.Count == 0 || items.Count > 0, summary = $"{items.Count} of {rows.Count} matching COGO points, {civil.CogoPoints.Count} in the drawing", count = items.Count, total = (int)civil.CogoPoints.Count, offset, truncated = rows.Count > offset + items.Count, drawingUnit = units.Label, lengthUnit = "mm", items, warnings = new List<string>(), errors };
