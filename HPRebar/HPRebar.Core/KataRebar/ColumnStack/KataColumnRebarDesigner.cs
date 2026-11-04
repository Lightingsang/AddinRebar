using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace HPRebar.Core.KataRebar.ColumnStack;

/// <summary>A main bar of the design with the number it carries (identical bars share one).</summary>
public sealed record KataColumnNumberedBar(KataColumnBarRun Run, int Number);

/// <summary>Ties of one storey: their zone along the storey (mm above its base) and each tie with its number.</summary>
public sealed record KataColumnTieSet(KataColumnHoopZone Zone, IReadOnlyList<(KataColumnTie Tie, int Number)> Ties);

/// <summary>What one storey gets besides its main bars: the section the bars come from, hoops and ties.</summary>
public sealed record KataColumnStoreyDesign(
    int Storey,
    string Name,
    KataColumnSection Section,
    IReadOnlyList<KataColumnHoopZone> HoopZones,
    int HoopNumber,
    KataColumnTieSet? TieSet);

/// <summary>Everything Kata Column Rebar creates, or why it cannot.</summary>
public sealed record KataColumnRebarDesign(
    IReadOnlyList<KataColumnNumberedBar> Bars,
    IReadOnlyList<KataColumnStoreyDesign> Storeys,
    IReadOnlyList<string> Notes,
    IReadOnlyList<string> Errors);

/// <summary>
/// Puts the bar plan, the hoop zones and the ties of every storey together and numbers them for the schedule: main
/// bars of one shape and Ø share a number (Kata numbers identical bars alike), then hoops by size, then ties by shape.
/// </summary>
public static class KataColumnRebarDesigner
{
    private const double EndGapMm = 50;
    private const double ShapeRoundingMm = 5;

    public static KataColumnRebarDesign Design(KataColumnSpec spec, IReadOnlyList<KataColumnStoreyGeometry> storeys, KataColumnBarRules rules)
    {
        var plan = KataColumnBarPlanner.Plan(spec, storeys, rules);
        if (plan.Errors.Count > 0)
        {
            return new KataColumnRebarDesign(Array.Empty<KataColumnNumberedBar>(), Array.Empty<KataColumnStoreyDesign>(), plan.Notes, plan.Errors);
        }

        var numbers = new Dictionary<string, int>();
        int Next(string key) => numbers.TryGetValue(key, out int n) ? n : numbers[key] = numbers.Count + 1;

        var bars = plan.Bars.Select(b => new KataColumnNumberedBar(b, Next(BarKey(b)))).ToList();
        var designs = new List<KataColumnStoreyDesign>();
        for (int k = 0; k < storeys.Count; k++)
        {
            var row = KataColumnSectionPlanner.OnFaces(spec.Rows[k], storeys[k]);
            var section = KataColumnSectionPlanner.Plan(row, spec);
            var zones = KataColumnHoopPlanner.Zones(row, spec);
            int hoop = Next($"hoop|{row.IsRound}|{Round(row.WidthMm)}|{Round(row.DepthMm)}|{spec.Hoops.DiameterMm}");
            designs.Add(new KataColumnStoreyDesign(k, row.Name, section, zones, hoop, TieSet(row, section, spec, Next)));
        }

        return new KataColumnRebarDesign(bars, designs, plan.Notes, Array.Empty<string>());
    }

    private static KataColumnTieSet? TieSet(KataColumnStoreyRow row, KataColumnSection section, KataColumnSpec spec, Func<string, int> next)
    {
        if (section.Ties.Count == 0)
        {
            return null;
        }

        double clear = row.HeightMm - row.BeamDepthMm;
        if (clear - EndGapMm <= EndGapMm)
        {
            return null;
        }

        var zone = new KataColumnHoopZone(EndGapMm, clear - EndGapMm, spec.Ties.SpacingMm);
        var ties = section.Ties
            .Select(t => (t, next($"tie|{t.Kind}|{t.AlongB}|{t.SecondSlot - t.FirstSlot}|{Round(row.WidthMm)}|{Round(row.DepthMm)}|{spec.Ties.DiameterMm}")))
            .ToList();
        return new KataColumnTieSet(zone, ties);
    }

    /// <summary>Shape of a bar for numbering: kind, Ø and its segment lengths and bends, rounded to 5 mm.</summary>
    private static string BarKey(KataColumnBarRun run)
    {
        var segments = run.Points.Zip(run.Points.Skip(1), (a, b) =>
            $"{Round(Math.Sqrt((b.X - a.X) * (b.X - a.X) + (b.Y - a.Y) * (b.Y - a.Y)))}:{Round(b.Z - a.Z)}");
        return $"bar|{run.Kind}|{run.DiameterMm.ToString(CultureInfo.InvariantCulture)}|{string.Join(";", segments)}";
    }

    private static string Round(double mm) =>
        (Math.Round(mm / ShapeRoundingMm, MidpointRounding.AwayFromZero) * ShapeRoundingMm).ToString(CultureInfo.InvariantCulture);
}
