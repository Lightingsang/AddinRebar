using System.Collections.Generic;
using HPRebar.Core.KataRebar.Models;

namespace HPRebar.Core.KataRebar.Calculators;

/// <summary>
/// The lines of Kata's section (T2-DY7.dwg): slab and beam outline with a break line through the slab each side, the
/// hoop with its corners bent round the corner bars and both 135° hooks at the top right, and the ties — round an
/// inner layer their straight part under the bars and the tails over them, round side bars the other way up.
/// </summary>
internal static class KataSectionLines
{
    public static IEnumerable<KataSectionPolyline> Outline(double width, double depth, double slab)
    {
        double face = width / 2.0, edge = face + KataSectionStyle.SlabReach;
        if (slab <= 0.0 || slab >= depth)
        {
            yield return Line(KataDrawingPen.Outline, (-face, 0.0), (face, 0.0), (face, -depth), (-face, -depth), (-face, 0.0));
            yield break;
        }

        yield return Line(KataDrawingPen.Outline, (-edge, 0.0), (edge, 0.0));
        yield return Line(KataDrawingPen.Outline, (-edge, -slab), (-face, -slab), (-face, -depth), (face, -depth), (face, -slab), (edge, -slab));
        yield return Break(-edge, slab);
        yield return Break(edge, slab);
    }

    /// <summary>A zigzag across the slab, the same shape both sides (it does not mirror in Kata's drawing).</summary>
    private static KataSectionPolyline Break(double x, double slab)
    {
        double k = KataSectionStyle.BreakSize, mid = -slab / 2.0;
        return Line(KataDrawingPen.Thin, (x, -slab - k), (x, mid - k), (x + k, mid - k), (x - k, mid + k), (x, mid + k), (x, k));
    }

    /// <summary>The hoop, starting and ending with its hooks at the top-right corner.</summary>
    public static KataSectionPolyline Hoop(KataSectionBars bars)
    {
        double xc = bars.HoopX, c = bars.Cover, top = -c, bottom = -bars.Depth + c;
        double r = bars.CornerRadius(KataSectionFace.Top), rb = bars.CornerRadius(KataSectionFace.Bottom);
        double hook = KataSectionStyle.HoopHook;
        double bend = KataSectionStyle.CornerBulge;
        return new KataSectionPolyline(KataDrawingPen.Stirrup, new[]
        {
            new KataBulgeVertex(xc - r - hook, top - hook),
            new KataBulgeVertex(xc - r, top, KataSectionStyle.HookBulge),
            new KataBulgeVertex(xc, top - r),
            new KataBulgeVertex(xc, bottom + rb, bend),
            new KataBulgeVertex(xc - rb, bottom),
            new KataBulgeVertex(-xc + rb, bottom, bend),
            new KataBulgeVertex(-xc, bottom + rb),
            new KataBulgeVertex(-xc, top - r, bend),
            new KataBulgeVertex(-xc + r, top),
            new KataBulgeVertex(xc - r, top, bend),
            new KataBulgeVertex(xc, top - r),
            new KataBulgeVertex(xc - hook, top - r - hook)
        });
    }

    /// <summary>
    /// A tie across the section at <paramref name="z"/>: a half circle at each end, its centre one bend radius inside
    /// the hoop's line, the straight part on one side of the bars and the tails on the other.
    /// </summary>
    /// <param name="straightAbove">Straight part over the bars (side bars) or under them (an inner layer).</param>
    public static KataSectionPolyline Tie(KataSectionBars bars, double z, bool straightAbove)
    {
        double r = KataSectionStyle.TieBendDiameters * bars.Stirrup;
        double x = bars.HoopX - r, tail = x - KataSectionStyle.TieTailDiameters * bars.Stirrup;
        double s = straightAbove ? r : -r;
        if (straightAbove)
            return Arcs((tail, z - s), (x, z - s), (x, z + s), (-x, z + s), (-x, z - s), (-tail, z - s));
        return Arcs((-tail, z - s), (-x, z - s), (-x, z + s), (x, z + s), (x, z - s), (tail, z - s));
    }

    /// <summary>
    /// An inner U as B01 section 2-2 draws it (Ø10 round bars 3-4: legs ±59, bottom on the hoop's, top two diameters
    /// under the hoop's, each leg ending in a 180° hook over its bar — a half circle four diameters across, bulge 1 —
    /// and a tail five and a half diameters down inside).
    /// </summary>
    public static KataSectionPolyline InnerU(KataSectionBars bars, double left, double right)
    {
        double ds = bars.Stirrup, top = -bars.Cover - 2.0 * ds, bottom = -bars.Depth + bars.Cover;
        double turn = 4.0 * ds, drop = 5.5 * ds, r = 2.0 * ds;
        return new KataSectionPolyline(KataDrawingPen.Stirrup, new[]
        {
            new KataBulgeVertex(left + turn, top - drop), new KataBulgeVertex(left + turn, top, KataSectionStyle.HalfCircleBulge),
            new KataBulgeVertex(left, top),
            new KataBulgeVertex(left, bottom + r, -KataSectionStyle.CornerBulge), new KataBulgeVertex(left + r, bottom),
            new KataBulgeVertex(right - r, bottom, -KataSectionStyle.CornerBulge), new KataBulgeVertex(right, bottom + r),
            new KataBulgeVertex(right, top, KataSectionStyle.HalfCircleBulge), new KataBulgeVertex(right - turn, top),
            new KataBulgeVertex(right - turn, top - drop)
        });
    }

    /// <summary>
    /// An inner C as B01 section 2-2 draws it (bar 2 at −124.5: long leg at −144.5, 180° hooks over the top bar and
    /// under the bottom bar — half circles four diameters across, top and bottom two diameters inside the hoop's — and
    /// tails five and a half diameters toward each other at −104.5) — always open to the right.
    /// </summary>
    public static KataSectionPolyline InnerC(KataSectionBars bars, double x)
    {
        double ds = bars.Stirrup, top = -bars.Cover - 2.0 * ds, bottom = -bars.Depth + bars.Cover + 2.0 * ds;
        double leg = x - 2.0 * ds, open = x + 2.0 * ds, tail = 5.5 * ds, half = KataSectionStyle.HalfCircleBulge;
        return new KataSectionPolyline(KataDrawingPen.Stirrup, new[]
        {
            new KataBulgeVertex(open, top - tail), new KataBulgeVertex(open, top, half), new KataBulgeVertex(leg, top),
            new KataBulgeVertex(leg, bottom, half), new KataBulgeVertex(open, bottom), new KataBulgeVertex(open, bottom + tail)
        });
    }

    /// <summary>Six points, a half circle (bulge 1) from the second to the third and from the fourth to the fifth.</summary>
    private static KataSectionPolyline Arcs(params (double X, double Z)[] p) =>
        new(KataDrawingPen.Stirrup, new[]
        {
            new KataBulgeVertex(p[0].X, p[0].Z), new KataBulgeVertex(p[1].X, p[1].Z, 1.0), new KataBulgeVertex(p[2].X, p[2].Z),
            new KataBulgeVertex(p[3].X, p[3].Z, 1.0), new KataBulgeVertex(p[4].X, p[4].Z), new KataBulgeVertex(p[5].X, p[5].Z)
        });

    public static KataSectionPolyline Line(KataDrawingPen pen, params (double X, double Z)[] points)
    {
        var vertices = new KataBulgeVertex[points.Length];
        for (int i = 0; i < points.Length; i++) vertices[i] = new KataBulgeVertex(points[i].X, points[i].Z);
        return new KataSectionPolyline(pen, vertices);
    }
}
