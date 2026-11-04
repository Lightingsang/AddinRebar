using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace HPRebar.Core.KataRebar.ColumnStack;

/// <summary>One storey as the Kata Column Rebar page lists it.</summary>
/// <param name="MainBars">"24Ø25 + 16Ø20".</param>
/// <param name="Hoops">"Ø10: a100 ×10, a200 ×3, a100 ×10".</param>
public sealed record KataColumnRebarSummaryRow(string Level, int BarCount, string MainBars, string Hoops, int TieCount, string Numbers);

/// <summary>What the page shows before anything is created.</summary>
/// <param name="Sections">One section drawing per storey (none while the sheet has errors).</param>
/// <param name="Elevation">The elevation along b the page draws beside the sections (null while the sheet has errors).</param>
public sealed record KataColumnRebarSummary(
    string Name,
    int StarterCount,
    int BarCount,
    IReadOnlyList<KataColumnRebarSummaryRow> Rows,
    IReadOnlyList<string> Notes,
    IReadOnlyList<string> Errors,
    IReadOnlyList<KataColumnSectionDrawing> Sections,
    KataColumnElevationDrawing? Elevation);

public static class KataColumnRebarSummaryMapper
{
    public static KataColumnRebarSummary Map(KataColumnSpec spec, KataColumnRebarDesign design, IReadOnlyList<KataColumnStoreyGeometry> storeys)
    {
        var rows = design.Storeys.Select(s => Row(s, design, spec.Hoops.DiameterMm)).ToList();
        var sections = KataColumnSectionDrawings.Build(spec, storeys, design);
        return new KataColumnRebarSummary(
            spec.Name,
            design.Bars.Count(b => b.Run.Kind == KataColumnBarKind.Starter),
            design.Bars.Count,
            rows,
            design.Notes,
            design.Errors,
            sections,
            KataColumnDrawingMapper.Map(spec, storeys, design, sections));
    }

    private static KataColumnRebarSummaryRow Row(KataColumnStoreyDesign storey, KataColumnRebarDesign design, double hoopDiameterMm)
    {
        var bars = design.Bars.Where(b => b.Run.Storey == storey.Storey && b.Run.Kind != KataColumnBarKind.Starter).ToList();
        string mainBars = string.Join(" + ", bars
            .GroupBy(b => b.Run.DiameterMm)
            .OrderByDescending(g => g.Key)
            .Select(g => $"{g.Count()}Ø{Mm(g.Key)}"));
        string hoops = $"Ø{Mm(hoopDiameterMm)}: " + string.Join(", ", storey.HoopZones.Select(z => $"a{Mm(z.SpacingMm)} ×{Count(z)}"));
        var numbers = bars.Select(b => b.Number).Append(storey.HoopNumber)
            .Concat(storey.TieSet?.Ties.Select(t => t.Number) ?? Enumerable.Empty<int>())
            .Distinct().OrderBy(n => n);
        return new KataColumnRebarSummaryRow(storey.Name, bars.Count, mainBars, hoops, storey.TieSet?.Ties.Count ?? 0, string.Join(", ", numbers));
    }

    /// <summary>Hoops in a zone: one at each end and one per spacing between.</summary>
    public static int Count(KataColumnHoopZone zone) =>
        (int)System.Math.Floor((zone.ToMm - zone.FromMm) / zone.SpacingMm + 1e-6) + 1;

    private static string Mm(double value) => value.ToString("0.#", CultureInfo.InvariantCulture);
}
