string namePattern = args.Str("namePattern", "*");
int limit = args.Int("limit", 100);
if (limit <= 0 || limit > 500) throw new ArgumentException("limit must be 1–500.");
if (civil == null) throw new InvalidOperationException("The active drawing has no Civil document.");
bool Like(string text, string pattern) => string.IsNullOrEmpty(pattern) || pattern == "*" || System.Text.RegularExpressions.Regex.IsMatch(text ?? "",
    "^" + System.Text.RegularExpressions.Regex.Escape(pattern).Replace("\\*", ".*").Replace("\\?", ".") + "$", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
var errors = new List<object>();
void Fail(string code, string message, string handle) => errors.Add(new { code, message, handle });
string Classify(Exception ex) => ex is Autodesk.AutoCAD.Runtime.Exception acad ? acad.ErrorStatus.ToString() : ex.GetType().Name;

var items = new List<object>();
int matched = 0;
foreach (ObjectId id in civil.GetSurfaceIds())
{
    ct.ThrowIfCancellationRequested();
    try
    {
        var s = (Autodesk.Civil.DatabaseServices.Surface)tr.GetObject(id, OpenMode.ForRead);
        if (!Like(s.Name, namePattern)) continue;
        matched++;
        if (items.Count >= limit) continue;
        var g = s.GetGeneralProperties();
        object area2D = null, area3D = null, slopeMax = null;
        if (s is TinSurface tin)
        {
            try { var t = tin.GetTerrainProperties(); area2D = t.SurfaceArea2D; area3D = t.SurfaceArea3D; slopeMax = t.MaximumGradeOrSlope; }
            catch (Exception ex) { Fail(Classify(ex), "terrain properties: " + ex.Message, id.Handle.ToString()); }
        }
        items.Add(new
        {
            handle = id.Handle.ToString(), name = s.Name, surfaceType = s.GetType().Name, description = s.Description, isOutOfDate = s.IsOutOfDate, autoRebuild = s.AutoRebuild, style = s.StyleName,
            pointCount = g.NumberOfPoints, elevationMin = g.MinimumElevation, elevationMax = g.MaximumElevation, elevationMean = g.MeanElevation,
            boundsMm = new { minX = Math.Round(units.ToMm(g.MinimumCoordinateX), 1), minY = Math.Round(units.ToMm(g.MinimumCoordinateY), 1), maxX = Math.Round(units.ToMm(g.MaximumCoordinateX), 1), maxY = Math.Round(units.ToMm(g.MaximumCoordinateY), 1) },
            area2D, area3D, slopeMax,
        });
    }
    catch (Exception ex) { Fail(Classify(ex), ex.Message, id.Handle.ToString()); }
}
log($"{items.Count} surfaces");
return new { success = errors.Count == 0 || items.Count > 0, summary = $"{items.Count} of {matched} matching surfaces (areas in {units.Label}²)", count = items.Count, truncated = matched > items.Count, drawingUnit = units.Label, lengthUnit = "mm", items, warnings = new List<string>(), errors };
