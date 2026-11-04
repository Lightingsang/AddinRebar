using System;
using System.Collections.Generic;
using System.Linq;
using HPRebar.Core.KataRebar.Calculators;

namespace HPRebar.Core.KataRebar.ColumnStack;

/// <summary>
/// The stack seen along b as Kata's elevation shows it, in elevation mm: X along b measured from
/// <see cref="CentreXMm" /> (the lowest storey's centre in the column frame), Z absolute.
/// </summary>
/// <param name="Chains">Vertical chains: group A laps left of the stack, storey levels, group B laps and hoop zones right.</param>
public sealed record KataColumnElevation(double CentreXMm, double LeftMm, double RightMm, double BottomMm, double TopMm, IReadOnlyList<KataDimChain> Chains);

public static class KataColumnElevationDims
{
    /// <summary>Between two dimension lines and between the stack and the first one (mm, model).</summary>
    public const double GapMm = 300;

    private const double MergeMm = 1;

    public static KataColumnElevation Build(KataColumnSpec spec, IReadOnlyList<KataColumnStoreyGeometry> storeys, KataColumnRebarDesign design)
    {
        double cx = (storeys[0].MinXMm + storeys[0].MaxXMm) / 2;
        double left = storeys.Min(s => s.MinXMm) - cx, right = storeys.Max(s => s.MaxXMm) - cx;
        double bottom = storeys[0].BaseMm - spec.PileCapHeightMm, top = storeys[^1].TopMm;
        var chains = new List<KataDimChain>();
        if (design.Errors.Count == 0)
        {
            var levels = storeys.Select(s => (s.BaseMm, s.MaxXMm - cx)).Append((top, storeys[^1].MaxXMm - cx));
            chains.Add(Chain(right + GapMm, levels));
            if (Laps(design, KataColumnBarGroup.A, cx, leftmost: true) is { } a)
            {
                chains.Add(Chain(left - GapMm, a));
            }

            if (Laps(design, KataColumnBarGroup.B, cx, leftmost: false) is { } b)
            {
                chains.Add(Chain(right + 2 * GapMm, b));
            }

            var zones = design.Storeys.SelectMany(d => d.HoopZones.SelectMany(z => new[]
            {
                (storeys[d.Storey].BaseMm + z.FromMm, storeys[d.Storey].MaxXMm - cx),
                (storeys[d.Storey].BaseMm + z.ToMm, storeys[d.Storey].MaxXMm - cx),
            }));
            chains.Add(Chain(right + 3 * GapMm, zones));
        }

        return new KataColumnElevation(cx, left, right, bottom, top, chains.Where(c => c.Stations.Count >= 2).ToList());
    }

    /// <summary>The storey a bar is drawn with in the elevation: −1 for the starters in the pile cap.</summary>
    public static int StoreyKey(KataColumnBarRun run) => run.Kind == KataColumnBarKind.Starter ? -1 : run.Storey;

    /// <summary>
    /// The bar the elevation shows for a set (one storey, group and layer): the leftmost for the face on the left
    /// (group A), the rightmost for the right one (group B); the lap dimensions measure the same bar.
    /// </summary>
    public static T Outermost<T>(IEnumerable<T> bars, Func<T, KataColumnBarRun> run, bool leftmost) =>
        leftmost
            ? bars.OrderBy(b => run(b).Points.Min(p => p.X)).First()
            : bars.OrderByDescending(b => run(b).Points.Max(p => p.X)).First();

    /// <summary>
    /// The laps of one group on one face: its outermost layer-1 bar of every storey (the starters first); each lap is
    /// the stretch where a bar starts below the end of the bar it laps with. Null when the group has under two bars.
    /// </summary>
    private static IEnumerable<(double Z, double X)>? Laps(KataColumnRebarDesign design, KataColumnBarGroup group, double cx, bool leftmost)
    {
        var chain = design.Bars
            .Select(b => b.Run)
            .Where(r => r.Group == group && r.Layer == 1)
            .GroupBy(StoreyKey)
            .OrderBy(g => g.Key)
            .Select(g => Outermost(g, r => r, leftmost))
            .ToList();
        if (chain.Count < 2)
        {
            return null;
        }

        var stations = new List<(double Z, double X)>();
        for (int i = 1; i < chain.Count; i++)
        {
            var below = chain[i - 1];
            var above = chain[i];
            double start = above.Points.Min(p => p.Z), end = below.Points.Max(p => p.Z);
            if (start < end)
            {
                stations.Add((start, above.Points[0].X - cx));
                stations.Add((end, below.Points[^1].X - cx));
            }
        }

        return stations;
    }

    private static KataDimChain Chain(double lineAt, IEnumerable<(double Z, double X)> stations)
    {
        var kept = new List<KataDimStation>();
        foreach (var (z, x) in stations.OrderBy(s => s.Z))
        {
            if (kept.Count == 0 || z - kept[^1].Along > MergeMm)
            {
                kept.Add(new KataDimStation(z, x));
            }
        }

        return new KataDimChain(true, lineAt, kept);
    }
}
