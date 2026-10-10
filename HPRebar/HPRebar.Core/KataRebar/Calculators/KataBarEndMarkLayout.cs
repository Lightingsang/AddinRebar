using System;
using System.Collections.Generic;
using System.Linq;
using HPRebar.Core.BeamRebar.Models;
using HPRebar.Core.KataRebar.Models;

namespace HPRebar.Core.KataRebar.Calculators;

/// <summary>
/// Where the cut marks go on the long section in Revit: one at every end of the main, additional, side and hanger bars,
/// on the bar's own centre line (a bent bar's mark on the tip of its leg); stirrups and ties get none. A mark leaves the
/// end back along the bar at <see cref="KataBarEndMark.AngleDegrees"/> and turns the way Kata's slash does
/// (<see cref="KataBarDrafting.Inward"/>: a straight end of a top bar down, of any other bar up, a leg tip toward the
/// bar's body). Bars side by side across the beam end at the same point of the elevation and share one mark.
/// </summary>
public static class KataBarEndMarkLayout
{
    private const double SamePointMm = 1.0;

    public static IReadOnlyList<KataBarEndMark> Build(KataRebarLayoutResult layout)
    {
        if (layout is null) throw new ArgumentNullException(nameof(layout));

        var marks = new List<KataBarEndMark>();
        foreach (var bar in layout.LongitudinalBars)
        {
            var points = Elevation(bar.Polyline);
            if (points.Count < 2) continue;

            bool top = bar.Role is KataBarRole.MainTop or KataBarRole.ExtraTop;
            double meanX = points.Average(p => p.X);
            Add(marks, Mark(points[0], points[1], top, meanX));
            Add(marks, Mark(points[points.Count - 1], points[points.Count - 2], top, meanX));
        }

        return marks;
    }

    /// <summary>The bar's vertices on the elevation (y dropped), with points that coincide there merged.</summary>
    private static List<(double X, double Z)> Elevation(Polyline3 polyline)
    {
        var points = new List<(double X, double Z)>();
        foreach (var p in polyline.Points)
        {
            if (points.Count > 0 && Math.Abs(points[points.Count - 1].X - p.X) < SamePointMm && Math.Abs(points[points.Count - 1].Z - p.Z) < SamePointMm) continue;
            points.Add((p.X, p.Z));
        }

        return points;
    }

    /// <summary>The mark at <paramref name="end"/>, whose neighbour on the bar is <paramref name="next"/>.</summary>
    private static KataBarEndMark Mark((double X, double Z) end, (double X, double Z) next, bool top, double meanX)
    {
        double length = Math.Sqrt((next.X - end.X) * (next.X - end.X) + (next.Z - end.Z) * (next.Z - end.Z));
        double ax = (next.X - end.X) / length, az = (next.Z - end.Z) / length;
        var (ix, iz) = KataBarDrafting.Inward(end, next, top, meanX);

        // The inward side has a component along the bar on a slanted end; keep only its part across the bar.
        double across = ix * -az + iz * ax;
        double nx = across >= 0.0 ? -az : az, nz = across >= 0.0 ? ax : -ax;

        double angle = KataBarEndMark.AngleDegrees * Math.PI / 180.0;
        return new KataBarEndMark(end.X, end.Z, ax * Math.Cos(angle) + nx * Math.Sin(angle), az * Math.Cos(angle) + nz * Math.Sin(angle));
    }

    private static void Add(List<KataBarEndMark> marks, KataBarEndMark mark)
    {
        bool same = marks.Any(m => Math.Abs(m.X - mark.X) < SamePointMm && Math.Abs(m.Z - mark.Z) < SamePointMm
                                   && Math.Abs(m.DirectionX - mark.DirectionX) < 1e-3 && Math.Abs(m.DirectionZ - mark.DirectionZ) < 1e-3);
        if (!same) marks.Add(mark);
    }
}
