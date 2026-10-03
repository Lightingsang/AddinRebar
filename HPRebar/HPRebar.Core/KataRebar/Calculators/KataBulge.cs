using System;
using System.Collections.Generic;
using HPRebar.Core.KataRebar.Models;

namespace HPRebar.Core.KataRebar.Calculators;

/// <summary>A polyline with AutoCAD bulges as points: each arc segment split into short chords.</summary>
public static class KataBulge
{
    /// <summary>Largest angle one chord of an arc covers.</summary>
    private const double MaxStep = Math.PI / 24.0;

    public static IReadOnlyList<(double X, double Z)> Points(IReadOnlyList<KataBulgeVertex> vertices)
    {
        if (vertices is null) throw new ArgumentNullException(nameof(vertices));
        var points = new List<(double X, double Z)>(vertices.Count * 2);
        for (int i = 0; i < vertices.Count; i++)
        {
            var a = vertices[i];
            points.Add((a.X, a.Z));
            if (i + 1 >= vertices.Count || Math.Abs(a.Bulge) < 1e-9) continue;
            AddArc(points, a, vertices[i + 1]);
        }

        return points;
    }

    /// <summary>
    /// The inside points of the arc from <paramref name="a"/> to <paramref name="b"/>: it sweeps 4 atan(bulge),
    /// counter-clockwise when the bulge is positive, its centre left of the chord (right when the arc passes 180°).
    /// </summary>
    private static void AddArc(List<(double X, double Z)> points, KataBulgeVertex a, KataBulgeVertex b)
    {
        double dx = b.X - a.X, dz = b.Z - a.Z, chord = Math.Sqrt(dx * dx + dz * dz);
        if (chord < 1e-9) return;

        double sweep = 4.0 * Math.Atan(a.Bulge);
        double offset = chord / 2.0 * (1.0 - a.Bulge * a.Bulge) / (2.0 * a.Bulge);
        double cx = (a.X + b.X) / 2.0 - dz / chord * offset, cz = (a.Z + b.Z) / 2.0 + dx / chord * offset;
        double radius = Math.Sqrt((a.X - cx) * (a.X - cx) + (a.Z - cz) * (a.Z - cz));
        double start = Math.Atan2(a.Z - cz, a.X - cx);
        int steps = Math.Max(2, (int)Math.Ceiling(Math.Abs(sweep) / MaxStep));
        for (int k = 1; k < steps; k++)
        {
            double t = start + sweep * k / steps;
            points.Add((cx + radius * Math.Cos(t), cz + radius * Math.Sin(t)));
        }
    }
}
