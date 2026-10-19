namespace HPBim.Core.Algorithms;

using System;
using System.Collections.Generic;
using System.Linq;
using HPBim.Core.Geometry;
using HPBim.Core.Models;

public sealed record WallSegment(
    Point2D StartPoint,
    Point2D EndPoint,
    double StartOffsetMm,
    double EndOffsetMm,
    double ThicknessMm,
    Polygon2D Polygon)
{
    public double LengthMm => EndOffsetMm - StartOffsetMm;
}

public sealed class WallSegmenter
{
    private const double Epsilon = 1e-4;

    public IReadOnlyList<WallSegment> Segment(WallDef wall)
    {
        if (wall == null) throw new ArgumentNullException(nameof(wall));
        double wallLength = wall.LengthMm;
        if (wallLength < Epsilon)
        {
            throw new ArgumentException("Wall length must be greater than zero.", nameof(wall));
        }

        Vector2D u = wall.Direction;
        Vector2D n = wall.Normal;
        double halfT = wall.ThicknessMm / 2.0;
        Point2D p1 = wall.StartPoint;

        var intervals = GetNormalizedOpeningIntervals(wall.Openings, wallLength);
        var solidSpans = BuildSolidSpans(intervals, wallLength);

        var segments = new List<WallSegment>(solidSpans.Count);
        foreach (var (a, b) in solidSpans)
        {
            Point2D segStart = p1 + u * a;
            Point2D segEnd = p1 + u * b;

            var polyVertices = new[]
            {
                segStart + n * halfT,
                segEnd + n * halfT,
                segEnd - n * halfT,
                segStart - n * halfT
            };

            segments.Add(new WallSegment(
                segStart, segEnd, a, b,
                wall.ThicknessMm,
                new Polygon2D(polyVertices)));
        }

        return segments;
    }

    private static List<(double Start, double End)> BuildSolidSpans(
        List<(double Start, double End)> intervals, double wallLength)
    {
        var solidSpans = new List<(double Start, double End)>();
        double current = 0.0;

        foreach (var (start, end) in intervals)
        {
            if (start > current + Epsilon)
            {
                solidSpans.Add((current, start));
            }
            current = Math.Max(current, end);
        }

        if (current < wallLength - Epsilon)
        {
            solidSpans.Add((current, wallLength));
        }

        return solidSpans;
    }

    private static List<(double Start, double End)> GetNormalizedOpeningIntervals(
        IReadOnlyList<OpeningDef> openings, double maxSpan)
    {
        if (openings == null || openings.Count == 0)
        {
            return new List<(double, double)>();
        }

        var raw = openings
            .Select(o => (
                Start: Math.Max(0.0, o.DistanceAlongWallMm),
                End: Math.Min(maxSpan, o.DistanceAlongWallMm + o.WidthMm)))
            .Where(i => i.End > i.Start + Epsilon)
            .OrderBy(i => i.Start)
            .ToList();

        var merged = new List<(double Start, double End)>();
        foreach (var interval in raw)
        {
            if (merged.Count == 0)
            {
                merged.Add(interval);
            }
            else
            {
                var last = merged[merged.Count - 1];
                if (interval.Start <= last.End + Epsilon)
                {
                    merged[merged.Count - 1] = (last.Start, Math.Max(last.End, interval.End));
                }
                else
                {
                    merged.Add(interval);
                }
            }
        }

        return merged;
    }
}
