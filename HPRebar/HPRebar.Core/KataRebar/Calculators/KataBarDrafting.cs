using System;
using System.Collections.Generic;
using System.Linq;

namespace HPRebar.Core.KataRebar.Calculators;

/// <summary>
/// A bar drawn on the elevation the way Kata's polylines on layer "kata_thep chu" are: its own centre line, a short
/// slash at each end (<see cref="KataTagStyle.TickAlong"/> back along the bar, <see cref="KataTagStyle.TickAcross"/>
/// toward its inside — down for a top bar, up for the others, toward the bar's body at the tip of a hook leg) and the
/// square bends of its hooks rounded (<see cref="KataTagStyle.BendRadius"/>); a crank keeps its sharp corners.
/// </summary>
public static class KataBarDrafting
{
    private const int ArcSegments = 6;

    /// <param name="points">The bar's centre line in elevation: X along the beam, Z up (mm).</param>
    /// <param name="topBar">A top bar: its straight ends tick downwards.</param>
    public static IReadOnlyList<(double X, double Z)> Outline(IReadOnlyList<(double X, double Z)> points, bool topBar)
    {
        if (points is null) throw new ArgumentNullException(nameof(points));
        var p = new List<(double X, double Z)>();
        foreach (var q in points)
            if (p.Count == 0 || Math.Abs(p[p.Count - 1].X - q.X) > 1e-6 || Math.Abs(p[p.Count - 1].Z - q.Z) > 1e-6) p.Add(q);
        if (p.Count < 2) return p;

        double meanX = p.Average(q => q.X);
        var outline = new List<(double X, double Z)> { Tick(p[0], p[1], topBar, meanX), p[0] };
        for (int i = 1; i + 1 < p.Count; i++) outline.AddRange(Bend(p[i - 1], p[i], p[i + 1]));
        outline.Add(p[p.Count - 1]);
        outline.Add(Tick(p[p.Count - 1], p[p.Count - 2], topBar, meanX));
        return outline;
    }

    /// <summary>The slash at end <paramref name="end"/> whose neighbour is <paramref name="next"/>.</summary>
    private static (double X, double Z) Tick((double X, double Z) end, (double X, double Z) next, bool topBar, double meanX)
    {
        var (ax, az) = Unit(next.X - end.X, next.Z - end.Z);
        // On an end segment shorter than the slash, the slash stops at its far end.
        double along = Math.Min(KataTagStyle.TickAlong, Math.Sqrt(Sq(next.X - end.X) + Sq(next.Z - end.Z)));
        double ix, iz;
        if (Math.Abs(az) < 0.5)
        {
            // A straight end: toward the inside of the beam.
            ix = 0.0;
            iz = topBar ? -1.0 : 1.0;
        }
        else
        {
            // The tip of a hook leg: toward the bar's body.
            ix = meanX >= end.X ? 1.0 : -1.0;
            iz = 0.0;
        }

        return (end.X + along * ax + KataTagStyle.TickAcross * ix,
                end.Z + along * az + KataTagStyle.TickAcross * iz);
    }

    /// <summary>A square bend rounded; any other corner (a crank) as it is.</summary>
    private static IEnumerable<(double X, double Z)> Bend((double X, double Z) a, (double X, double Z) corner, (double X, double Z) b)
    {
        double lin = Math.Sqrt(Sq(corner.X - a.X) + Sq(corner.Z - a.Z)), lout = Math.Sqrt(Sq(b.X - corner.X) + Sq(b.Z - corner.Z));
        var (ux, uz) = Unit(corner.X - a.X, corner.Z - a.Z);
        var (vx, vz) = Unit(b.X - corner.X, b.Z - corner.Z);
        double r = KataTagStyle.BendRadius;
        if (Math.Abs(ux * vx + uz * vz) > 0.2 || lin < 2.0 * r || lout < 2.0 * r)
        {
            yield return corner;
            yield break;
        }

        // Tangent points r before and after the corner; the centre is r from the first along the outgoing direction.
        double sx = corner.X - r * ux, sz = corner.Z - r * uz;
        double cx = sx + r * vx, cz = sz + r * vz;
        double a0 = Math.Atan2(sz - cz, sx - cx);
        double a1 = Math.Atan2(corner.Z + r * vz - cz, corner.X + r * vx - cx);
        double sweep = a1 - a0;
        if (sweep > Math.PI) sweep -= 2.0 * Math.PI;
        if (sweep < -Math.PI) sweep += 2.0 * Math.PI;
        for (int k = 0; k <= ArcSegments; k++)
        {
            double t = a0 + sweep * k / ArcSegments;
            yield return (cx + r * Math.Cos(t), cz + r * Math.Sin(t));
        }
    }

    private static (double X, double Z) Unit(double x, double z)
    {
        double l = Math.Sqrt(x * x + z * z);
        return l < 1e-9 ? (1.0, 0.0) : (x / l, z / l);
    }

    private static double Sq(double v) => v * v;
}
