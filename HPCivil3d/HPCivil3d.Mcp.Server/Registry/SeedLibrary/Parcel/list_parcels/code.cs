string site = args.Str("site");
string namePattern = args.Str("namePattern", "*");
int limit = args.Int("limit", 100);
if (limit <= 0 || limit > 250) throw new ArgumentException("limit must be 1–250.");
if (civil == null) throw new InvalidOperationException("The active drawing has no Civil document.");
bool Like(string text, string pattern) => string.IsNullOrEmpty(pattern) || pattern == "*" || System.Text.RegularExpressions.Regex.IsMatch(text ?? "",
    "^" + System.Text.RegularExpressions.Regex.Escape(pattern).Replace("\\*", ".*").Replace("\\?", ".") + "$", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
var errors = new List<object>();
void Fail(string code, string message, string handle) => errors.Add(new { code, message, handle });
string Classify(Exception ex) => ex is Autodesk.AutoCAD.Runtime.Exception acad ? acad.ErrorStatus.ToString() : ex.GetType().Name;

var items = new List<object>();
int matched = 0, sites = 0;
foreach (ObjectId siteId in civil.GetSiteIds())
{
    ct.ThrowIfCancellationRequested();
    var s = (Site)tr.GetObject(siteId, OpenMode.ForRead);
    if (!string.IsNullOrWhiteSpace(site) && !string.Equals(s.Name, site, StringComparison.OrdinalIgnoreCase)) continue;
    sites++;
    foreach (ObjectId id in s.GetParcelIds())
    {
        try
        {
            var p = (Parcel)tr.GetObject(id, OpenMode.ForRead);
            if (!Like(p.Name, namePattern)) continue;
            matched++;
            if (items.Count >= limit) continue;
            items.Add(new { handle = id.Handle.ToString(), site = s.Name, name = p.Name, number = p.Number, taxId = p.TaxId, address = p.Address, style = p.StyleName, description = p.Description,
                            centroid = new { x = Math.Round(units.ToMm(p.Centroid.X), 1), y = Math.Round(units.ToMm(p.Centroid.Y), 1) }, area = Math.Round(p.Area, 3) });
        }
        catch (Exception ex) { Fail(Classify(ex), ex.Message, id.Handle.ToString()); }
    }
}
if (!string.IsNullOrWhiteSpace(site) && sites == 0) throw new ArgumentException($"No site named '{site}' in this drawing.");
log($"{items.Count} parcels in {sites} site(s)");
return new { success = errors.Count == 0 || items.Count > 0, summary = $"{items.Count} of {matched} matching parcels in {sites} site(s) (area in {units.Label}²)", count = items.Count, truncated = matched > items.Count, drawingUnit = units.Label, areaUnit = units.Label + "²", lengthUnit = "mm", items, warnings = new List<string>(), errors };
