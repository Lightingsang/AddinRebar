using Autodesk.Navisworks.Api;
using HPNavis.BIMCoordinator.SearchSets;

namespace HPNavis.BIMCoordinator.Colors;

/// <summary>A set the painter could not use: missing from the document (<paramref name="Blocking" />) or pending in the registry.</summary>
public sealed record ColorSetSkip(string Code, string Name, string Reason, bool Blocking);

/// <summary>Read-back of one painted set: elements sampled and those whose permanent colour is not the expected RGB.</summary>
public sealed record ColorCheck(string Code, string Name, int Sampled, int Mismatched, int RepaintedByLater);

public sealed record ColorPaintOutcome(IReadOnlyList<ColorSetPaint> Sets, IReadOnlyList<ColorSetSkip> Skipped, IReadOnlyList<ColorCheck> Checks, int Painted, int Reset)
{
    /// <summary>The run did its job: at least one painting set was found and no painting set is missing from the document.</summary>
    public bool Usable => Sets.Any(s => s.Rgb is not null) && !Skipped.Any(s => s.Blocking);
}

/// <summary>
///     Paints the saved colour sets with permanent colours (<c>DocumentModels.OverridePermanentColor</c>) in sheet order and
///     reads a sample back through <c>ModelGeometry.PermanentColor</c>. Every collection goes to Navisworks whole: a saved
///     set's elements are never enumerated (live: 36 000 elements took 46 s to walk, painting 4 481 took 18 ms), only the
///     first elements of each set are sampled and tested against later sets with <c>ModelItemCollection.Contains</c>. Reset
///     removes the permanent materials of the painting sets' elements only — never <c>ResetAllPermanentMaterials</c>, so
///     colours the user set elsewhere survive. Only <c>preview</c> resolves the <c>&lt;Default&gt;</c> sets (their searches
///     are the broadest — every ARC/STR element — and they never paint). Works on the saved sets (what the user sees in
///     Sets). Main thread only.
/// </summary>
public static class NavisColorPainter
{
    private const int SamplePerSet = 25;

    public static ColorPaintOutcome Run(Document doc, BaseSetCatalog catalog, ColorSetCatalog colors, string mode, IReadOnlyCollection<string>? codes, CancellationToken ct)
    {
        if (mode is not ("preview" or "apply" or "verify" or "reset")) throw new ArgumentException($"mode '{mode}' is not preview, apply, verify or reset");
        var entries = codes is { Count: > 0 } ? codes.Select(colors.Set).Distinct().OrderBy(e => e.Order).ToList() : colors.Sets.OrderBy(s => s.Order).ToList();
        var skipped = new List<ColorSetSkip>();
        var resolved = new List<(ColorSetEntry Entry, ModelItemCollection Items)>();
        var path = new[] { catalog.Folder, colors.Folder };
        foreach (var entry in entries)
        {
            ct.ThrowIfCancellationRequested();
            if (entry.Pending is not null) { skipped.Add(new ColorSetSkip(entry.Code, entry.DisplayName, "pending: " + entry.Pending, false)); continue; }
            if (!entry.Paints && mode != "preview") continue;
            var saved = NavisSearchSetCompiler.FindSet(doc, SearchSetPlan.ForDefinition(catalog, entry.ToDefinition(), SetKind.Color, entry.Discipline, path));
            if (saved is null) { skipped.Add(new ColorSetSkip(entry.Code, entry.DisplayName, "not in the document — run bim_sync_color_sets apply first", entry.Paints)); continue; }
            resolved.Add((entry, saved.GetSelectedItems(doc)));
        }

        var painting = ColorPaintPlan.PaintOrder(resolved.Select(r => r.Entry)).Select(e => resolved.First(r => r.Entry == e)).ToList();
        var painted = 0;
        var reset = 0;
        if (mode == "apply")
        {
            foreach (var (entry, items) in painting)
            {
                ct.ThrowIfCancellationRequested();
                if (items.Count == 0) continue;
                doc.Models.OverridePermanentColor(items, Color.FromByteRGB((byte)entry.Rgb![0], (byte)entry.Rgb[1], (byte)entry.Rgb[2]));
                painted += items.Count;
            }
        }
        else if (mode == "reset")
        {
            foreach (var (_, items) in painting.Where(p => p.Items.Count > 0))
            {
                ct.ThrowIfCancellationRequested();
                doc.Models.ResetPermanentMaterials(items);
                reset += items.Count;
            }
        }

        var checks = mode is "apply" or "verify" ? Check(painting, ct) : Array.Empty<ColorCheck>();
        var rows = resolved.Select(r => new ColorSetPaint(r.Entry.Code, r.Entry.DisplayName, r.Entry.Rgb, r.Items.Count)).ToList();
        return new ColorPaintOutcome(rows, skipped, checks, painted, reset);
    }

    /// <summary>
    ///     Samples each painting set and compares the permanent colour of each sampled element's first geometry node with the
    ///     last set holding that node or one of its ancestors (a parent painted by one set, a nested child by a later one).
    /// </summary>
    private static IReadOnlyList<ColorCheck> Check(IReadOnlyList<(ColorSetEntry Entry, ModelItemCollection Items)> painting, CancellationToken ct)
    {
        var order = painting.Select(p => ((IReadOnlyList<int>)p.Entry.Rgb!, (Func<ModelItem, bool>)p.Items.Contains)).ToList();
        var checks = new List<ColorCheck>();
        foreach (var (entry, items) in painting)
        {
            ct.ThrowIfCancellationRequested();
            var sample = items.Take(SamplePerSet).ToList();
            var mismatched = 0;
            var repainted = 0;
            foreach (var item in sample)
            {
                var geometry = item.HasGeometry ? item : item.Descendants.FirstOrDefault(d => d.HasGeometry);
                var expected = (geometry is null ? null : ColorPaintPlan.ExpectedRgb(geometry.AncestorsAndSelf, order)) ?? entry.Rgb!;
                if (!ReferenceEquals(expected, entry.Rgb)) repainted++;
                var color = geometry?.Geometry.PermanentColor;
                if (color is null || !ColorPaintPlan.Matches(expected, color.R, color.G, color.B)) mismatched++;
            }

            checks.Add(new ColorCheck(entry.Code, entry.DisplayName, sample.Count, mismatched, repainted));
        }

        return checks;
    }
}
