using Autodesk.Navisworks.Api;
using Autodesk.Navisworks.Api.Clash;
using HPNavis.BIMCoordinator.ClashTests;
using HPNavis.BIMCoordinator.Rules;

namespace HPNavis.BIMCoordinator.Reports;

/// <summary>
///     Reads the HP clash tests of one design phase and every result into a <see cref="ClashSnapshot" />. Read-only.
///     A result's items are solids; the coordinator wants the Revit element above them (name, category, Id) and its level
///     (live: <c>Item &gt; Layer</c> of the solid holds the Revit level name, the element node carries <c>Element</c>).
///     Navisworks main thread only.
/// </summary>
public static class NavisClashSnapshotReader
{
    private const string ElementTab = "LcRevitData_Element";

    public static ClashSnapshot Read(Document doc, ClashMatrix matrix, string? lod, double mmPerUnit, DateTime exportedAtUtc, CancellationToken ct)
    {
        var tests = NavisClashTestCompiler.AllTests(doc).ToList();
        var phase = ClashSnapshotRules.ResolvePhase(lod, tests.Select(t => t.DisplayName));
        var rules = matrix.Rules.ToDictionary(r => r.Id, StringComparer.Ordinal);
        var testRows = new List<ClashSnapshotTest>();
        var rows = new List<ClashSnapshotRow>();
        var truncated = false;
        var grids = doc.Grids.Systems.ToList();
        foreach (var test in tests.OrderBy(t => t.DisplayName, StringComparer.Ordinal))
        {
            ct.ThrowIfCancellationRequested();
            if (ClashTestNaming.Parse(test.DisplayName) is not { } parsed || ClashMatrix.PhaseOf(parsed.Lod) != phase || !rules.TryGetValue(parsed.RuleId, out var rule)) continue;
            var results = Results(test, null).ToList();
            testRows.Add(new ClashSnapshotTest(test.DisplayName, rule.Id, rule.Priority, rule.DisciplinePair, rule.Left, rule.Right,
                test.Status.ToString(), test.LastRun?.ToUniversalTime().ToString("o"), results.Count,
                results.GroupBy(r => r.Result.Status.ToString()).OrderBy(g => g.Key, StringComparer.Ordinal).ToDictionary(g => g.Key, g => g.Count())));
            foreach (var (result, group) in results)
            {
                if (rows.Count >= ClashSnapshotRules.MaxRows) { truncated = true; break; }
                var item1 = ItemInfo(result.Item1);
                var item2 = ItemInfo(result.Item2);
                var center = result.Center;
                rows.Add(new ClashSnapshotRow(test.DisplayName, rule.Id, rule.Priority, rule.DisciplinePair, result.Guid.ToString(), result.DisplayName, group,
                    result.Status.ToString(), Math.Round(result.Distance * mmPerUnit, 1),
                    new[] { Math.Round(center.X * mmPerUnit), Math.Round(center.Y * mmPerUnit), Math.Round(center.Z * mmPerUnit) },
                    ClashSnapshotRules.LevelOf(item1, item2), item1, item2, ClashSnapshotRules.FoundAtText(result.CreatedTime),
                    GridLocation: GridLocation(grids, center, mmPerUnit, item1, item2)));
            }
        }

        if (testRows.Count == 0) throw new ArgumentException($"the model holds no HP clash test of phase {phase}");
        return new ClashSnapshot(ClashSnapshotRules.SchemaVersion, exportedAtUtc.ToString("o"), Path.GetFileName(doc.FileName), phase,
            matrix.ToleranceMmFor(phase), testRows, rows, truncated);
    }

    /// <summary>Every result of a test with the name of the result group it sits in (Clash Detective groups nest results).</summary>
    private static IEnumerable<(ClashResult Result, string? Group)> Results(SavedItem item, string? group) =>
        item switch
        {
            ClashResult result => new[] { (result, group) },
            ClashResultGroup resultGroup => resultGroup.Children.SelectMany(child => Results(child, resultGroup.DisplayName)),
            GroupItem parent => parent.Children.SelectMany(child => Results(child, group)),
            _ => Enumerable.Empty<(ClashResult, string?)>(),
        };

    private static ClashItemInfo ItemInfo(ModelItem? item)
    {
        if (item is null) return new ClashItemInfo("", "", "", "", "");
        var element = item.AncestorsAndSelf.FirstOrDefault(node => node.PropertyCategories.FindCategoryByName(ElementTab) is not null);
        var named = element ?? item;
        var level = Text(item.PropertyCategories.FindPropertyByName("LcOaNode", "LcOaNodeLayer"));
        if (string.IsNullOrWhiteSpace(level) && element is not null) level = Text(element.PropertyCategories.FindPropertyByDisplayName("Element", "Level"));
        return new ClashItemInfo(
            named.DisplayName,
            element is null ? item.ClassDisplayName : Text(element.PropertyCategories.FindPropertyByName(ElementTab, "LcRevitPropertyElementCategory")),
            element is null ? "" : ElementText(element, "Id"),
            Text(item.PropertyCategories.FindPropertyByName("LcOaNode", "LcOaNodeSourceFile")),
            level,
            element is null ? "" : ElementText(element, "Family"),
            element is null ? "" : ElementText(element, "Type"),
            element is null ? "" : FirstNonEmpty(ElementText(element, "Size"), ElementText(element, "Overall Size")),
            element is null ? "" : ClashElementFacts.SystemText(ElementText(element, "System Classification"), ElementText(element, "System Name"),
                Text(element.PropertyCategories.FindPropertyByDisplayName("Distribution System", "Name"))),
            element is null ? "" : FirstNonEmpty(ElementText(element, "Mark"), Text(element.PropertyCategories.FindPropertyByDisplayName("Custom", "STR_ElementName"))));
    }

    private static string ElementText(ModelItem element, string property) =>
        Text(element.PropertyCategories.FindPropertyByDisplayName("Element", property));

    private static string FirstNonEmpty(params string[] values) => values.FirstOrDefault(v => !string.IsNullOrWhiteSpace(v)) ?? "";

    /// <summary>
    ///     "Y.1a(120)-15.1a(-35)" read on the grid system of the first element's own file (else the second's), offsets in
    ///     mm; null when neither file brought a grid system.
    /// </summary>
    private static string? GridLocation(IReadOnlyList<GridSystem> grids, Point3D center, double mmPerUnit, ClashItemInfo item1, ClashItemInfo item2)
    {
        var names = grids.Select(g => g.DisplayName).ToList();
        var name = ClashElementFacts.GridSystemFor(item1.SourceFile, names) ?? ClashElementFacts.GridSystemFor(item2.SourceFile, names);
        if (name is null) return null;
        var intersection = grids.First(g => g.DisplayName == name).ClosestIntersection(center);
        var text = intersection?.FormatIntersectionDisplayString(center, mmPerUnit);
        return string.IsNullOrWhiteSpace(text) ? null : text;
    }

    /// <summary>A property value as text; <c>ToDisplayString</c> throws on anything but a display string, so each kind is read by type.</summary>
    private static string Text(DataProperty? property)
    {
        var value = property?.Value;
        if (value is null || value.IsNone) return "";
        if (value.IsDisplayString) return value.ToDisplayString();
        if (value.IsIdentifierString) return value.ToIdentifierString();
        if (value.IsNamedConstant) return value.ToNamedConstant().DisplayName;
        if (value.IsInt32) return value.ToInt32().ToString(System.Globalization.CultureInfo.InvariantCulture);
        if (value.IsDouble) return value.ToDouble().ToString(System.Globalization.CultureInfo.InvariantCulture);
        return value.ToString();
    }
}
