string categoryName = args.Require("categoryName");
string parameterName = args.Require("parameterName");
var view = doc.ActiveView;

ElementFilter categoryFilter;
if (Enum.TryParse<BuiltInCategory>(categoryName, true, out var bic)) categoryFilter = new ElementCategoryFilter(bic);
else
{
    var cat = doc.Settings.Categories.Cast<Category>().FirstOrDefault(c => string.Equals(c.Name, categoryName, StringComparison.OrdinalIgnoreCase))
        ?? throw new ArgumentException($"Unknown category '{categoryName}'.");
    categoryFilter = new ElementCategoryFilter(cat.Id);
}
var elements = new FilteredElementCollector(doc, view.Id).WhereElementIsNotElementType().WherePasses(categoryFilter).ToElements();
if (elements.Count == 0) throw new InvalidOperationException($"No {categoryName} elements in view '{view.Name}'.");

string ValueOf(Element e)
{
    if (string.Equals(parameterName, "Type Name", StringComparison.OrdinalIgnoreCase)) return (doc.GetElement(e.GetTypeId()) as ElementType)?.Name ?? "(no type)";
    var p = e.LookupParameter(parameterName) ?? (doc.GetElement(e.GetTypeId()) as ElementType)?.LookupParameter(parameterName);
    if (p == null || !p.HasValue) return "(none)";
    return p.StorageType switch
    {
        StorageType.String => string.IsNullOrEmpty(p.AsString()) ? "(empty)" : p.AsString(),
        StorageType.ElementId => p.AsValueString() ?? p.AsElementId().Value.ToString(),
        _ => p.AsValueString() ?? p.AsDouble().ToString("0.###"),
    };
}

var groups = elements.GroupBy(ValueOf).OrderBy(g => g.Key, StringComparer.OrdinalIgnoreCase).ToList();
var custom = args.List("customColors").Select(c => (r: c.Int("r"), g: c.Int("g"), b: c.Int("b"))).ToList();
bool gradient = args.Bool("useGradient", false);
var random = new Random(12345);
(int r, int g, int b) ColorFor(int i, int n)
{
    if (i < custom.Count) return custom[i];
    if (gradient)
    {
        double t = n <= 1 ? 0 : (double)i / (n - 1);
        return ((int)(255 * t), (int)(255 * (1 - Math.Abs(2 * t - 1))), (int)(255 * (1 - t)));
    }
    return (random.Next(40, 230), random.Next(40, 230), random.Next(40, 230));
}

var solid = new FilteredElementCollector(doc).OfClass(typeof(FillPatternElement)).Cast<FillPatternElement>().FirstOrDefault(p => p.GetFillPattern().IsSolidFill);
var legend = new List<object>();
for (int i = 0; i < groups.Count; i++)
{
    ct.ThrowIfCancellationRequested();
    var (r, g, b) = ColorFor(i, groups.Count);
    var color = new Color((byte)Math.Clamp(r, 0, 255), (byte)Math.Clamp(g, 0, 255), (byte)Math.Clamp(b, 0, 255));
    var ogs = new OverrideGraphicSettings();
    ogs.SetProjectionLineColor(color); ogs.SetCutLineColor(color);
    ogs.SetSurfaceForegroundPatternColor(color); ogs.SetCutForegroundPatternColor(color);
    if (solid != null)
    {
        ogs.SetSurfaceForegroundPatternId(solid.Id); ogs.SetSurfaceForegroundPatternVisible(true);
        ogs.SetCutForegroundPatternId(solid.Id); ogs.SetCutForegroundPatternVisible(true);
    }
    foreach (var e in groups[i]) view.SetElementOverrides(e.Id, ogs);
    legend.Add(new { value = groups[i].Key, color = new { r, g, b }, count = groups[i].Count() });
}

return new { view = view.Name, category = categoryName, parameter = parameterName, elements = elements.Count, groups = legend.Count, legend };
