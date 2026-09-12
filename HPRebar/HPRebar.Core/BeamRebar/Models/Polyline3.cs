using System;
using System.Collections.Generic;

namespace HPRebar.Core.BeamRebar.Models;

/// <summary>
/// An ordered sequence of 3D points forming a continuous rebar centerline curve.
/// All coordinates are in local millimetres.
/// </summary>
public sealed record Polyline3
{
    public Polyline3()
    {
    }

    public Polyline3(IReadOnlyList<Point3> points, bool isClosed = false)
    {
        Points = points ?? Array.Empty<Point3>();
        IsClosed = isClosed;
    }

    public IReadOnlyList<Point3> Points { get; init; } = Array.Empty<Point3>();

    public bool IsClosed { get; init; }

    /// <summary>Total cumulative length of all polyline segments in millimetres.</summary>
    public double TotalLength
    {
        get
        {
            if (Points.Count < 2) return 0.0;
            double sum = 0.0;
            for (int i = 0; i < Points.Count - 1; i++)
            {
                sum += Points[i].DistanceTo(Points[i + 1]);
            }
            if (IsClosed && Points.Count > 2)
            {
                sum += Points[Points.Count - 1].DistanceTo(Points[0]);
            }
            return sum;
        }
    }

    /// <summary>
    /// Simplifies the polyline by merging consecutive vertices closer than <paramref name="minSegmentLength"/> mm.
    /// Crucial for preventing Revit CreateFromCurves exceptions on Application.ShortCurveTolerance (~0.78 mm).
    /// </summary>
    public Polyline3 Simplify(double minSegmentLength = 1.0)
    {
        if (Points.Count < 2) return this;

        var result = new List<Point3>(Points.Count) { Points[0] };
        for (int i = 1; i < Points.Count; i++)
        {
            if (Points[i].DistanceTo(result[result.Count - 1]) >= minSegmentLength)
            {
                result.Add(Points[i]);
            }
        }

        // Check closed polyline tail
        if (IsClosed && result.Count > 2 && result[result.Count - 1].DistanceTo(result[0]) < minSegmentLength)
        {
            result.RemoveAt(result.Count - 1);
        }

        return this with { Points = result };
    }

    /// <summary>Translates all vertices by the specified vector.</summary>
    public Polyline3 Translate(Vector3 offset)
    {
        var translated = new Point3[Points.Count];
        for (int i = 0; i < Points.Count; i++)
        {
            translated[i] = Points[i] + offset;
        }
        return this with { Points = translated };
    }
}
