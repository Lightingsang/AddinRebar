using System;
using System.Linq;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using HPRebar.BeamRebar.Service;
using HPRebar.Core.BeamRebar.Models;

namespace HPRebar.KataRebar.Service;

/// <summary>
/// Revit puts a "to cover" constraint on the segments of a new bar and may pull the bar towards the host's cover
/// in the section plane: a layer-2 bar or the C tie under it moved 12-13 mm up in Revit 2026 (beams cut by floors),
/// so the tie no longer wrapped its bars. Moving the element back holds through later regenerations (measured).
/// The bar is compared by the midpoint of its longest straight segment, its own straight run in plan.
/// </summary>
internal static class KataRebarSectionFit
{
    /// <summary>A bar further than this from its planned place in the section is moved back (mm).</summary>
    private const double ToleranceMm = 0.05;

    /// <summary>A segment whose ends differ less than this in height is level (mm), as in the plan.</summary>
    private const double LevelToleranceMm = 1.0;

    /// <summary>An actual line this close to the planned segment's direction (cosine) is its counterpart.</summary>
    private const double ParallelCosine = 0.999;

    /// <summary>
    /// Moves <paramref name="rebar"/> so the midpoint of its longest straight segment sits on the planned one, in local
    /// Z only (<paramref name="acrossToo"/> false) or in local Y and Z. Returns the distance left over (mm).
    /// </summary>
    public static double Fit(Document doc, Rebar rebar, PointMapper mapper, Point3 plannedA, Point3 plannedB, bool acrossToo)
    {
        doc.Regenerate();
        var planned = Mid(plannedA, plannedB);
        var actual = ActualMid(rebar, mapper, plannedA, plannedB);
        if (actual is null) return 0.0;

        double dy = acrossToo ? planned.Y - actual.Value.Y : 0.0;
        double dz = planned.Z - actual.Value.Z;
        if (Math.Sqrt(dy * dy + dz * dz) <= ToleranceMm) return 0.0;

        var move = mapper.AxisY * (dy / 304.8) + mapper.AxisZ * (dz / 304.8);
        ElementTransformUtils.MoveElement(doc, rebar.Id, move);
        doc.Regenerate();

        var after = ActualMid(rebar, mapper, plannedA, plannedB);
        if (after is null) return 0.0;
        double ry = acrossToo ? planned.Y - after.Value.Y : 0.0;
        return Math.Sqrt(ry * ry + (planned.Z - after.Value.Z) * (planned.Z - after.Value.Z));
    }

    /// <summary>The longest level segment of a planned bar (its run along the beam), or the whole bar when none is level.</summary>
    public static (Point3 A, Point3 B) LongestLevelSegment(Polyline3 shape)
    {
        var points = shape.Points;
        (Point3 A, Point3 B) best = (points[0], points[points.Count - 1]);
        double longest = -1.0;
        for (int i = 1; i < points.Count; i++)
        {
            if (Math.Abs(points[i].Z - points[i - 1].Z) > LevelToleranceMm) continue;
            double length = points[i].DistanceTo(points[i - 1]);
            if (length <= longest) continue;
            longest = length;
            best = (points[i - 1], points[i]);
        }

        return best;
    }

    /// <summary>
    /// The midpoint of the bar's longest line running the way the planned segment runs (a hanger bar's slopes are
    /// longer than its level bottom: comparing a slope with the bottom moved the bar half its rise down, out of the
    /// beam); the longest line when none does.
    /// </summary>
    private static Point3? ActualMid(Rebar rebar, PointMapper mapper, Point3 plannedA, Point3 plannedB)
    {
        var lines = rebar.GetCenterlineCurves(false, false, false, MultiplanarOption.IncludeOnlyPlanarCurves, 0)
            .OfType<Line>()
            .Select(l => (A: mapper.ToLocal(l.GetEndPoint(0)), B: mapper.ToLocal(l.GetEndPoint(1))))
            .OrderByDescending(l => l.A.DistanceTo(l.B))
            .ToList();
        if (lines.Count == 0) return null;
        var line = lines.FirstOrDefault(l => Parallel(l.A, l.B, plannedA, plannedB));
        if (line == default) line = lines[0];
        return Mid(line.A, line.B);
    }

    private static bool Parallel(Point3 a, Point3 b, Point3 c, Point3 d)
    {
        double ab = a.DistanceTo(b), cd = c.DistanceTo(d);
        if (ab < 1e-9 || cd < 1e-9) return false;
        double dot = (b.X - a.X) * (d.X - c.X) + (b.Y - a.Y) * (d.Y - c.Y) + (b.Z - a.Z) * (d.Z - c.Z);
        return Math.Abs(dot) / (ab * cd) >= ParallelCosine;
    }

    private static Point3 Mid(Point3 a, Point3 b) => new((a.X + b.X) / 2.0, (a.Y + b.Y) / 2.0, (a.Z + b.Z) / 2.0);
}
