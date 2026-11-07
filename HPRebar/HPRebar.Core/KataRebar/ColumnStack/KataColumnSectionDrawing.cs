using System;
using System.Collections.Generic;
using System.Linq;

namespace HPRebar.Core.KataRebar.ColumnStack;

/// <summary>A bar where the section cuts it, section-local mm (origin = the storey's min corner, X along b).</summary>
public sealed record KataColumnSectionBar(double X, double Y, double DiameterMm, int Layer, int Number);

/// <summary>
/// An inner tie, section-local mm on its centre line: "closed" = the rectangle (X1, Y1)–(X2, Y2), "cross" = the straight
/// leg from (X1, Y1) to (X2, Y2).
/// </summary>
public sealed record KataColumnSectionTie(string Kind, double X1, double Y1, double X2, double Y2, int Number);

/// <summary>
/// One storey's section as Kata draws it: the storey's own bars where they run plumb (laps and cranks left out), the
/// hoop and the inner ties, with their numbers. <see cref="CutElevationMm" /> is where Revit's section view cuts: the
/// height nearest mid clear height where every bar passing is plumb and the fewest pass (staggered laps rarely leave a
/// height with none).
/// </summary>
/// <param name="CutElevationMm">Elevation of the cut in the column frame.</param>
/// <param name="WidthMm">b (the diameter when round).</param>
/// <param name="DepthMm">h (the diameter when round).</param>
/// <param name="HoopOffsetMm">From the face to the hoop's centre line: cover + hoop Ø / 2.</param>
/// <param name="HoopSpacingMm">The storey's densest hoop zone, the spacing Kata labels a hoop with.</param>
public sealed record KataColumnSectionDrawing(
    string Level,
    double CutElevationMm,
    bool IsRound,
    double WidthMm,
    double DepthMm,
    double HoopOffsetMm,
    double HoopDiameterMm,
    double HoopSpacingMm,
    int HoopNumber,
    double TieDiameterMm,
    double TieSpacingMm,
    IReadOnlyList<KataColumnSectionBar> Bars,
    IReadOnlyList<KataColumnSectionTie> Ties);

/// <summary>The extents of the bars an inner tie restrains (section-local mm) and how far the tie wraps around them.</summary>
public sealed record KataColumnTieBox(double MinX, double MaxX, double MinY, double MaxY, double Wrap);

public static class KataColumnTieGeometry
{
    /// <summary>
    /// A closed tie wraps the bars of its two grid lines plus their Ø; a crosstie runs across the section through the
    /// first grid line, hooked on its two face bars. Null when a grid line holds no bar.
    /// </summary>
    public static KataColumnTieBox? Box(KataColumnTie tie, IReadOnlyList<KataColumnBar> bars, double tieDiameterMm)
    {
        var first = bars.Where(b => (tie.AlongB ? b.Slot.Column : b.Slot.Row) == tie.FirstSlot).ToList();
        var second = bars.Where(b => (tie.AlongB ? b.Slot.Column : b.Slot.Row) == tie.SecondSlot).ToList();
        if (first.Count == 0 || second.Count == 0)
        {
            return null;
        }

        var line = first.Concat(second).ToList();
        double wrap = bars.Max(b => b.DiameterMm) / 2 + tieDiameterMm / 2;
        return new KataColumnTieBox(line.Min(b => b.X), line.Max(b => b.X), line.Min(b => b.Y), line.Max(b => b.Y), wrap);
    }

    /// <summary>The tie's centre line in section-local mm (see <see cref="KataColumnSectionTie" />).</summary>
    public static (double X1, double Y1, double X2, double Y2) Outline(KataColumnTie tie, KataColumnTieBox box)
    {
        if (tie.Kind == KataColumnTieKind.Crosstie)
        {
            return tie.AlongB
                ? (box.MinX, box.MinY - box.Wrap, box.MinX, box.MaxY + box.Wrap)
                : (box.MinX - box.Wrap, box.MinY, box.MaxX + box.Wrap, box.MinY);
        }

        return (box.MinX - box.Wrap, box.MinY - box.Wrap, box.MaxX + box.Wrap, box.MaxY + box.Wrap);
    }
}

public static class KataColumnSectionDrawings
{
    /// <summary>One drawing per storey of a valid design; none when the design was refused.</summary>
    public static IReadOnlyList<KataColumnSectionDrawing> Build(KataColumnSpec spec, IReadOnlyList<KataColumnStoreyGeometry> storeys, KataColumnRebarDesign design)
    {
        if (design.Errors.Count > 0)
        {
            return Array.Empty<KataColumnSectionDrawing>();
        }

        return design.Storeys.Select(s => Drawing(spec, storeys[s.Storey], s, design.Bars)).ToList();
    }

    private static KataColumnSectionDrawing Drawing(KataColumnSpec spec, KataColumnStoreyGeometry g, KataColumnStoreyDesign storey, IReadOnlyList<KataColumnNumberedBar> runs)
    {
        var row = spec.Rows[storey.Storey];
        double height = g.TopMm - g.BaseMm;
        double clear = height - row.BeamDepthMm;
        double cut = CutElevation(g, clear > 0 ? clear : height, storey.Storey, runs);

        var bars = runs
            .Where(r => r.Run.Storey == storey.Storey && r.Run.Kind != KataColumnBarKind.Starter)
            .Select(r => (Run: r, At: Plumb(r.Run.Points)))
            .Select(t => new KataColumnSectionBar(t.At.X - g.MinXMm, t.At.Y - g.MinYMm, t.Run.Run.DiameterMm, t.Run.Run.Layer, t.Run.Number))
            .ToList();

        var ties = new List<KataColumnSectionTie>();
        foreach (var (tie, number) in storey.TieSet?.Ties ?? Array.Empty<(KataColumnTie, int)>())
        {
            if (KataColumnTieGeometry.Box(tie, storey.Section.Bars, spec.Ties.DiameterMm) is not { } box)
            {
                continue;
            }

            var (x1, y1, x2, y2) = KataColumnTieGeometry.Outline(tie, box);
            ties.Add(new KataColumnSectionTie(tie.Kind == KataColumnTieKind.Crosstie ? "cross" : "closed", x1, y1, x2, y2, number));
        }

        return new KataColumnSectionDrawing(
            storey.Name,
            cut,
            row.IsRound,
            g.MaxXMm - g.MinXMm,
            g.MaxYMm - g.MinYMm,
            spec.CoverMm + spec.Hoops.DiameterMm / 2,
            spec.Hoops.DiameterMm,
            storey.HoopZones.Select(z => z.SpacingMm).DefaultIfEmpty(0).Min(),
            storey.HoopNumber,
            spec.Ties.DiameterMm,
            storey.TieSet?.Zone.SpacingMm ?? 0,
            bars,
            ties);
    }

    private const double CutStepMm = 25;

    private static double CutElevation(KataColumnStoreyGeometry g, double clear, int k, IReadOnlyList<KataColumnNumberedBar> runs)
    {
        double mid = g.BaseMm + clear / 2;
        double best = mid;
        int fewest = int.MaxValue;
        // The section shows the storey's own bars, so the cut must pass through every one of them.
        var own = runs.Where(r => r.Run.Storey == k && r.Run.Kind != KataColumnBarKind.Starter).ToList();
        for (double d = 0; d <= clear / 2; d += CutStepMm)
        {
            foreach (double z in new[] { mid + d, mid - d })
            {
                var crossing = runs.Where(r => Cross(r.Run.Points, z) is not null).ToList();
                if (crossing.Count < fewest && own.All(r => Cross(r.Run.Points, z) is not null) && crossing.All(r => IsPlumbAt(r.Run.Points, z)))
                {
                    (best, fewest) = (z, crossing.Count);
                }
            }
        }

        return best;
    }

    /// <summary>Plan position of the bar's longest plumb segment (its place in the storey's section).</summary>
    private static (double X, double Y) Plumb(IReadOnlyList<KataPoint3> points)
    {
        var segment = points.Zip(points.Skip(1), (a, b) => (A: a, B: b))
            .Where(s => Math.Abs(s.B.X - s.A.X) < 1e-6 && Math.Abs(s.B.Y - s.A.Y) < 1e-6)
            .OrderByDescending(s => Math.Abs(s.B.Z - s.A.Z))
            .Select(s => ((double X, double Y)?)(s.A.X, s.A.Y))
            .FirstOrDefault();
        // A bar with no plumb segment (fully cranked) is shown where it starts.
        return segment ?? (points[0].X, points[0].Y);
    }

    /// <summary>The bar runs straight up through the elevation (not on a crank or lap inset).</summary>
    private static bool IsPlumbAt(IReadOnlyList<KataPoint3> points, double z)
    {
        for (int i = 1; i < points.Count; i++)
        {
            var (a, b) = (points[i - 1], points[i]);
            if (z >= Math.Min(a.Z, b.Z) && z <= Math.Max(a.Z, b.Z) && Math.Abs(b.Z - a.Z) > 1e-6)
            {
                return Math.Abs(b.X - a.X) < 1e-6 && Math.Abs(b.Y - a.Y) < 1e-6;
            }
        }

        return false;
    }

    /// <summary>Where a bar's centre line passes the elevation (on a rising segment), or null when it does not reach it.</summary>
    private static (double X, double Y)? Cross(IReadOnlyList<KataPoint3> points, double z)
    {
        for (int i = 1; i < points.Count; i++)
        {
            var (a, b) = (points[i - 1], points[i]);
            double low = Math.Min(a.Z, b.Z), high = Math.Max(a.Z, b.Z);
            if (high - low < 1e-6 || z < low || z > high)
            {
                continue;
            }

            double t = (z - a.Z) / (b.Z - a.Z);
            return (a.X + (b.X - a.X) * t, a.Y + (b.Y - a.Y) * t);
        }

        return null;
    }
}
