using System.Collections.Generic;
using HPRebar.Core.BeamRebar.Models;

namespace HPRebar.Core.KataRebar.Calculators;

/// <summary>
/// The straight segments a bar is drawn from: vertices closer than the minimum length are merged, and a
/// closed loop (a stirrup) gets its closing side back even though simplifying drops the repeated vertex.
/// </summary>
public static class KataPolylineSegments
{
    public static IReadOnlyList<(Point3 From, Point3 To)> Of(Polyline3 polyline, double minSegmentLength = 1.0)
    {
        var points = polyline.Simplify(minSegmentLength).Points;
        var segments = new List<(Point3, Point3)>(points.Count);

        for (int i = 1; i < points.Count; i++)
            segments.Add((points[i - 1], points[i]));

        if (polyline.IsClosed && points.Count > 2 && points[points.Count - 1].DistanceTo(points[0]) >= minSegmentLength)
            segments.Add((points[points.Count - 1], points[0]));

        return segments;
    }
}
