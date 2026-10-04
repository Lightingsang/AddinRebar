using System.Collections.Generic;
using System.Linq;
using HPRebar.Core.BeamRebar.Models;
using HPRebar.Core.KataRebar.Models;

namespace HPRebar.Core.KataRebar.Calculators;

/// <summary>Cuts a longitudinal bar at a station, the cut end straight or bent by a leg, its fabrication data kept true.</summary>
public static class KataBarSplit
{
    /// <summary>The bar from its start to <paramref name="end"/>, bending there by its leg (<paramref name="legDirection"/> −1 down, +1 up).</summary>
    public static KataRebarCurve Until(KataRebarCurve bar, KataBarEnd end, int legDirection)
    {
        var points = Clip(bar.Polyline.Points, double.NegativeInfinity, end.X).ToList();
        var tail = points[points.Count - 1];
        if (end.IsBent) points.Add(new Point3(tail.X, tail.Y, tail.Z + legDirection * end.Leg));
        return bar with
        {
            Polyline = new Polyline3(points).Simplify(1.0),
            EndHookAngle = end.IsBent ? HookAngle.Hook90 : HookAngle.None,
            EndHookLength = end.Leg,
            DimA = end.X - points[0].X,
            DimC = end.Leg
        };
    }

    /// <summary>The bar from <paramref name="start"/> (bending there by its leg) to its end.</summary>
    public static KataRebarCurve From(KataRebarCurve bar, KataBarEnd start, int legDirection)
    {
        var points = Clip(bar.Polyline.Points, start.X, double.PositiveInfinity).ToList();
        var head = points[0];
        if (start.IsBent) points.Insert(0, new Point3(head.X, head.Y, head.Z + legDirection * start.Leg));
        return bar with
        {
            Polyline = new Polyline3(points).Simplify(1.0),
            StartHookAngle = start.IsBent ? HookAngle.Hook90 : HookAngle.None,
            StartHookLength = start.Leg,
            DimA = points[points.Count - 1].X - start.X,
            DimB = start.Leg
        };
    }

    /// <summary>The part of a polyline between two stations, with the crossing points interpolated.</summary>
    public static IEnumerable<Point3> Clip(IReadOnlyList<Point3> points, double from, double to)
    {
        for (int i = 0; i < points.Count; i++)
        {
            var p = points[i];
            bool inside = p.X >= from - 1e-6 && p.X <= to + 1e-6;
            if (i > 0)
            {
                var q = points[i - 1];
                foreach (double x in new[] { from, to })
                {
                    if (double.IsInfinity(x)) continue;
                    if ((q.X - x) * (p.X - x) < 0.0)
                    {
                        double f = (x - q.X) / (p.X - q.X);
                        yield return new Point3(x, q.Y + f * (p.Y - q.Y), q.Z + f * (p.Z - q.Z));
                    }
                }
            }

            if (inside) yield return p;
        }
    }

}
