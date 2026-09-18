string namePattern = args.Str("namePattern", "*");
string site = args.Str("site");
bool filterSite = args.Has("site");
int limit = args.Int("limit", 100);
int offset = args.Int("offset", 0);
if (limit <= 0 || limit > 180) throw new ArgumentException("limit must be 1–180.");
if (offset < 0) throw new ArgumentException("offset must be >= 0.");
if (civil == null) throw new InvalidOperationException("The active drawing has no Civil document.");
bool Like(string text, string pattern) => string.IsNullOrEmpty(pattern) || pattern == "*" || System.Text.RegularExpressions.Regex.IsMatch(text ?? "",
    "^" + System.Text.RegularExpressions.Regex.Escape(pattern).Replace("\\*", ".*").Replace("\\?", ".") + "$", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
var errors = new List<object>();
void Fail(string code, string message, string handle) => errors.Add(new { code, message, handle });
string Classify(Exception ex) => ex is Autodesk.AutoCAD.Runtime.Exception acad ? acad.ErrorStatus.ToString() : ex.GetType().Name;

var items = new List<object>();
int matched = 0;
foreach (ObjectId id in civil.GetAlignmentIds())
{
    ct.ThrowIfCancellationRequested();
    try
    {
        var a = (Alignment)tr.GetObject(id, OpenMode.ForRead);
        if (!Like(a.Name, namePattern)) continue;
        if (filterSite && !string.Equals(a.IsSiteless ? "" : a.SiteName, site, StringComparison.OrdinalIgnoreCase)) continue;
        matched++;
        if (matched <= offset) continue;
        if (items.Count >= limit) break;
        items.Add(new
        {
            handle = id.Handle.ToString(), name = a.Name, type = a.AlignmentType.ToString(), lengthMm = Math.Round(units.ToMm(a.Length), 1),
            startStation = Math.Round(a.StartingStation, 4), endStation = Math.Round(a.EndingStation, 4),
            startStationLabel = a.GetStationStringWithEquations(a.StartingStation), endStationLabel = a.GetStationStringWithEquations(a.EndingStation),
            site = a.IsSiteless ? "" : a.SiteName, isSiteless = a.IsSiteless, style = a.StyleName, profileCount = a.GetProfileIds().Count, entityCount = a.Entities.Count, description = a.Description,
        });
    }
    catch (Exception ex) { Fail(Classify(ex), ex.Message, id.Handle.ToString()); }
}
bool truncated = matched > offset + items.Count;
log($"{items.Count} alignments (matched {matched}, total {civil.GetAlignmentIds().Count})");
return new { success = errors.Count == 0 || items.Count > 0, summary = $"{items.Count} of {matched} matching alignments", count = items.Count, total = civil.GetAlignmentIds().Count, offset, truncated, drawingUnit = units.Label, lengthUnit = "mm", items, warnings = new List<string>(), errors };
