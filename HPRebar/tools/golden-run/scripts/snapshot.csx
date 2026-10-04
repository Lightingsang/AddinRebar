// Golden-run snapshot — read-only dump of what a rebar feature created, for before/after comparison.
// Run through the Revit MCP: execute_revit_code, transaction "none".
// args: runDir (the document must live under it — never a user model), scope = column | foundation | beam | all.
// Output rules: millimetres, lengths rounded to 0.1 mm, points to 1 mm, every list sorted, no element ids.

string runDir = args.Str("runDir", "");
string docPath = doc.PathName ?? "";
if (runDir.Length == 0 || !docPath.StartsWith(runDir, StringComparison.OrdinalIgnoreCase))
{
    throw new ArgumentException("Refusing to read '" + docPath + "': it is not under the golden-run folder '" + runDir + "'.");
}

string scope = args.Str("scope", "all");
string prefix = scope == "column" ? "C" : scope == "foundation" ? "FND-" : scope == "beam" ? "BR-" : "";

Func<double, double> mm = feet => UnitUtils.ConvertFromInternalUnits(feet, UnitTypeId.Millimeters);
Func<double, double> len = feet => Math.Round(mm(feet), 1);
Func<XYZ, double[]> pt = p => new[] { Math.Round(mm(p.X)), Math.Round(mm(p.Y)), Math.Round(mm(p.Z)) };
Func<Element, string> markOf = e => e?.get_Parameter(BuiltInParameter.ALL_MODEL_MARK)?.AsString() ?? "";
Func<ElementId, string> nameOf = id => id == null || id == ElementId.InvalidElementId ? "" : doc.GetElement(id)?.Name ?? "";

var rebars = new FilteredElementCollector(doc)
    .OfClass(typeof(Rebar))
    .Cast<Rebar>()
    .Select(r => new { Rebar = r, Host = markOf(doc.GetElement(r.GetHostId())) })
    .Where(x => x.Host.StartsWith(prefix, StringComparison.Ordinal))
    .ToList();

var elements = rebars
    .Select(x =>
    {
        var r = x.Rebar;
        var curves = r.GetCenterlineCurves(false, false, false, MultiplanarOption.IncludeOnlyPlanarCurves, 0);
        var start = curves.Count > 0 ? pt(curves[0].GetEndPoint(0)) : new double[0];
        var end = curves.Count > 0 ? pt(curves[curves.Count - 1].GetEndPoint(1)) : new double[0];
        return new
        {
            host = x.Host,
            partition = r.get_Parameter(BuiltInParameter.NUMBER_PARTITION_PARAM)?.AsString() ?? "",
            type = nameOf(r.GetTypeId()),
            shape = nameOf(r.GetShapeId()),
            freeForm = r.IsRebarFreeForm(),
            qty = r.Quantity,
            layout = r.LayoutRule.ToString(),
            spacingMm = r.LayoutRule == RebarLayoutRule.Single ? 0.0 : len(r.MaxSpacing),
            barLengthMm = len(r.get_Parameter(BuiltInParameter.REBAR_ELEM_LENGTH)?.AsDouble() ?? 0.0),
            hookStart = nameOf(r.GetHookTypeId(0)),
            hookEnd = nameOf(r.GetHookTypeId(1)),
            p0 = start,
            p1 = end,
            // every printed field takes part, so equal keys mean identical lines whatever order the collector yields
            key = string.Join("|", x.Host, nameOf(r.GetTypeId()), nameOf(r.GetShapeId()),
                string.Join(",", start), string.Join(",", end), r.Quantity,
                r.get_Parameter(BuiltInParameter.NUMBER_PARTITION_PARAM)?.AsString() ?? "", r.LayoutRule,
                r.LayoutRule == RebarLayoutRule.Single ? 0.0 : len(r.MaxSpacing),
                len(r.get_Parameter(BuiltInParameter.REBAR_ELEM_LENGTH)?.AsDouble() ?? 0.0),
                nameOf(r.GetHookTypeId(0)), nameOf(r.GetHookTypeId(1)), r.IsRebarFreeForm())
        };
    })
    .OrderBy(e => e.key, StringComparer.Ordinal)
    .ToList();

var byPartition = new SortedDictionary<string, int>(StringComparer.Ordinal);
foreach (var e in elements)
{
    var key = e.partition.Length == 0 ? "(none)" : e.partition;
    byPartition[key] = byPartition.TryGetValue(key, out var n) ? n + 1 : 1;
}

var byTypeShape = elements
    .GroupBy(e => e.type + "|" + e.shape)
    .OrderBy(g => g.Key, StringComparer.Ordinal)
    .Select(g => new
    {
        type = g.First().type,
        shape = g.First().shape,
        elements = g.Count(),
        bars = g.Sum(e => e.qty),
        totalLengthMm = Math.Round(g.Sum(e => e.barLengthMm * e.qty), 1)
    })
    .ToList();

var views = new FilteredElementCollector(doc)
    .OfClass(typeof(View))
    .Cast<View>()
    .Where(v => !v.IsTemplate)
    .Select(v => v.ViewType + ": " + v.Name)
    .OrderBy(s => s, StringComparer.Ordinal)
    .ToList();

Func<IEnumerable<Element>, SortedDictionary<string, int>> countByView = items =>
{
    var counts = new SortedDictionary<string, int>(StringComparer.Ordinal);
    foreach (var item in items)
    {
        var view = item.OwnerViewId == ElementId.InvalidElementId ? "(model)" : nameOf(item.OwnerViewId);
        counts[view] = counts.TryGetValue(view, out var n) ? n + 1 : 1;
    }

    return counts;
};

var dimensions = new FilteredElementCollector(doc).OfClass(typeof(Dimension)).ToElements();
var textNotes = new FilteredElementCollector(doc).OfClass(typeof(TextNote)).Cast<TextNote>().ToList();
var tags = new FilteredElementCollector(doc).OfClass(typeof(IndependentTag)).GetElementCount();

var warnings = new SortedDictionary<string, int>(StringComparer.Ordinal);
foreach (var warning in doc.GetWarnings())
{
    var text = warning.GetDescriptionText();
    warnings[text] = warnings.TryGetValue(text, out var n) ? n + 1 : 1;
}

return new
{
    schema = 1,
    scope,
    revitBuild = app.VersionBuild,
    docPath,
    rebar = new
    {
        count = elements.Count,
        byPartition,
        byTypeShape,
        elements = elements.Select(e => new
        {
            e.host, e.partition, e.type, e.shape, e.freeForm, e.qty, e.layout, e.spacingMm, e.barLengthMm,
            e.hookStart, e.hookEnd, e.p0, e.p1
        }).ToList(),
        otherReinforcement = new
        {
            inSystem = new FilteredElementCollector(doc).OfClass(typeof(RebarInSystem)).GetElementCount(),
            area = new FilteredElementCollector(doc).OfClass(typeof(AreaReinforcement)).GetElementCount(),
            path = new FilteredElementCollector(doc).OfClass(typeof(PathReinforcement)).GetElementCount()
        }
    },
    views = new { count = views.Count, names = views },
    dimensions = new { count = dimensions.Count, byView = countByView(dimensions) },
    textNotes = new
    {
        count = textNotes.Count,
        byView = countByView(textNotes),
        texts = textNotes.Select(t => t.Text.Trim()).OrderBy(s => s, StringComparer.Ordinal).ToList()
    },
    tags = new { count = tags },
    warnings = new { count = warnings.Values.Sum(), byText = warnings }
};
