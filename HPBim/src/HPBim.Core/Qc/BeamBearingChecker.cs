namespace HPBim.Core.Qc;

using System;
using System.Collections.Generic;
using HPBim.Core.Geometry;

public sealed record BeamBearingViolation(
    long BeamId,
    string BeamMark,
    string EndpointName,
    Point2D Point,
    double DistanceToSupportMm,
    string Reason);

public sealed record BeamBearingSummary(
    int TotalBeams,
    int ValidBeams,
    IReadOnlyList<BeamBearingViolation> Violations)
{
    public bool AllSupported => Violations.Count == 0 && ValidBeams == TotalBeams;
}

public sealed class BeamBearingChecker
{
    private readonly double _connectionToleranceMm;
    private readonly double _maxCutbackProjectionMm;

    public BeamBearingChecker(double connectionToleranceMm = 400.0, double maxCutbackProjectionMm = 350.0)
    {
        _connectionToleranceMm = connectionToleranceMm;
        _maxCutbackProjectionMm = maxCutbackProjectionMm;
    }

    public BeamBearingSummary CheckAll(
        IReadOnlyList<BeamAuditItem> beams,
        IReadOnlyList<FootingAuditItem> footings,
        IReadOnlyList<Point2D>? gridNodes = null)
    {
        if (beams == null) throw new ArgumentNullException(nameof(beams));
        if (footings == null) throw new ArgumentNullException(nameof(footings));

        var violations = new List<BeamBearingViolation>();
        int validCount = 0;

        for (int i = 0; i < beams.Count; i++)
        {
            var beam = beams[i];
            bool startOk = IsEndpointSupported(beam.StartPoint, beam.EndPoint, footings, beams, i, gridNodes);
            bool endOk = IsEndpointSupported(beam.EndPoint, beam.StartPoint, footings, beams, i, gridNodes);

            if (!startOk)
            {
                violations.Add(new BeamBearingViolation(
                    beam.Id, beam.Mark, "Start", beam.StartPoint, 0.0,
                    $"Điểm đầu ({beam.StartPoint.X:F1}, {beam.StartPoint.Y:F1}) không tựa vào đài móng, dầm liên kết hay nút cột."));
            }

            if (!endOk)
            {
                violations.Add(new BeamBearingViolation(
                    beam.Id, beam.Mark, "End", beam.EndPoint, 0.0,
                    $"Điểm cuối ({beam.EndPoint.X:F1}, {beam.EndPoint.Y:F1}) không tựa vào đài móng, dầm liên kết hay nút cột."));
            }

            if (startOk && endOk)
            {
                validCount++;
            }
        }

        return new BeamBearingSummary(beams.Count, validCount, violations);
    }

    private bool IsEndpointSupported(
        Point2D pt,
        Point2D otherEnd,
        IReadOnlyList<FootingAuditItem> footings,
        IReadOnlyList<BeamAuditItem> allBeams,
        int currentBeamIndex,
        IReadOnlyList<Point2D>? gridNodes)
    {
        if (IsPointInOrNearAnyFooting(pt, footings)) return true;
        if (IsPointNearAnyBeamEnd(pt, allBeams, currentBeamIndex)) return true;
        if (IsPointNearAnyBeamSegment(pt, allBeams, currentBeamIndex)) return true;
        if (gridNodes != null && IsPointNearAnyGridNode(pt, gridNodes)) return true;
        if (IsSupportedByCutbackProjection(pt, otherEnd, footings, allBeams, currentBeamIndex, gridNodes)) return true;
        return false;
    }

    private bool IsPointNearAnyGridNode(Point2D pt, IReadOnlyList<Point2D> gridNodes)
    {
        for (int i = 0; i < gridNodes.Count; i++)
        {
            if (pt.DistanceTo(gridNodes[i]) <= _connectionToleranceMm) return true;
        }
        return false;
    }

    private bool IsPointInOrNearAnyFooting(Point2D pt, IReadOnlyList<FootingAuditItem> footings)
    {
        foreach (var ft in footings)
        {
            if (ft.Polygon == null || ft.Polygon.VertexCount < 3) continue;
            if (ft.Polygon.Contains(pt)) return true;
            if (ft.Polygon.DistanceToBoundary(pt) <= _connectionToleranceMm) return true;
        }
        return false;
    }

    private bool IsSupportedByCutbackProjection(
        Point2D pt,
        Point2D otherEnd,
        IReadOnlyList<FootingAuditItem> footings,
        IReadOnlyList<BeamAuditItem> allBeams,
        int currentIdx,
        IReadOnlyList<Point2D>? gridNodes)
    {
        double dx = pt.X - otherEnd.X;
        double dy = pt.Y - otherEnd.Y;
        double len = Math.Sqrt((dx * dx) + (dy * dy));
        if (len < 1e-6) return false;

        double ux = dx / len;
        double uy = dy / len;

        // Check points along forward projection vector (Revit cutback)
        for (double step = 20.0; step <= _maxCutbackProjectionMm; step += 20.0)
        {
            var probe = new Point2D(pt.X + (ux * step), pt.Y + (uy * step));
            if (IsPointInOrNearAnyFooting(probe, footings)) return true;
            if (gridNodes != null && IsPointNearAnyGridNode(probe, gridNodes)) return true;
            if (IsPointNearAnyBeamEnd(probe, allBeams, currentIdx)) return true;
            if (IsPointNearAnyBeamSegment(probe, allBeams, currentIdx)) return true;
        }
        return false;
    }

    private bool IsPointNearAnyBeamEnd(Point2D pt, IReadOnlyList<BeamAuditItem> allBeams, int currentIdx)
    {
        for (int i = 0; i < allBeams.Count; i++)
        {
            if (i == currentIdx) continue;
            var b = allBeams[i];
            if (pt.DistanceTo(b.StartPoint) <= _connectionToleranceMm) return true;
            if (pt.DistanceTo(b.EndPoint) <= _connectionToleranceMm) return true;
        }
        return false;
    }

    private bool IsPointNearAnyBeamSegment(Point2D pt, IReadOnlyList<BeamAuditItem> allBeams, int currentIdx)
    {
        for (int i = 0; i < allBeams.Count; i++)
        {
            if (i == currentIdx) continue;
            var b = allBeams[i];
            double d = DistancePointToSegment(pt, b.StartPoint, b.EndPoint);
            if (d <= _connectionToleranceMm) return true;
        }
        return false;
    }

    private static double DistancePointToSegment(Point2D p, Point2D a, Point2D b)
    {
        double dx = b.X - a.X;
        double dy = b.Y - a.Y;
        double l2 = (dx * dx) + (dy * dy);
        if (l2 < 1e-12) return p.DistanceTo(a);
        double t = Math.Max(0.0, Math.Min(1.0, (((p.X - a.X) * dx) + ((p.Y - a.Y) * dy)) / l2));
        return p.DistanceTo(new Point2D(a.X + (t * dx), a.Y + (t * dy)));
    }
}
