namespace HPBim.Core.Qc;

using System;
using System.Collections.Generic;
using HPBim.Core.Geometry;

public sealed record PileContainmentViolation(
    long PileId,
    string PileMark,
    Point2D PileLocation,
    string? NearestFootingMark,
    double DistanceToFootingMm,
    string Reason);

public sealed record PileContainmentSummary(
    int TotalPiles,
    int ContainedPiles,
    IReadOnlyList<PileContainmentViolation> Violations)
{
    public bool AllContained => Violations.Count == 0 && ContainedPiles == TotalPiles;
}

public sealed class PileContainmentChecker
{
    private readonly double _minEdgeClearanceMm;
    private readonly double _toleranceMm;

    public PileContainmentChecker(double minEdgeClearanceMm = 150.0, double toleranceMm = 1.0)
    {
        _minEdgeClearanceMm = minEdgeClearanceMm;
        _toleranceMm = toleranceMm;
    }

    public PileContainmentSummary CheckAll(
        IReadOnlyList<PileAuditItem> piles,
        IReadOnlyList<FootingAuditItem> footings)
    {
        if (piles == null) throw new ArgumentNullException(nameof(piles));
        if (footings == null) throw new ArgumentNullException(nameof(footings));

        var violations = new List<PileContainmentViolation>();
        int containedCount = 0;

        foreach (var pile in piles)
        {
            var pt = new Point2D(pile.X, pile.Y);
            bool isInside = false;
            FootingAuditItem? enclosingFooting = null;

            foreach (var ft in footings)
            {
                if (ft.Polygon != null && ft.Polygon.Contains(pt))
                {
                    isInside = true;
                    enclosingFooting = ft;
                    break;
                }
            }

            if (isInside && enclosingFooting != null)
            {
                containedCount++;
                double dist = enclosingFooting.Polygon.DistanceToBoundary(pt);
                if (dist < (_minEdgeClearanceMm - _toleranceMm))
                {
                    violations.Add(new PileContainmentViolation(
                        pile.Id, pile.Mark, pt, enclosingFooting.Mark, dist,
                        $"Khoảng cách từ mép cọc đến biên đài {enclosingFooting.Mark} là {dist:F1}mm (< {_minEdgeClearanceMm:F1}mm)."));
                }
            }
            else
            {
                var (nearest, minDist) = FindNearestFooting(pt, footings);
                violations.Add(new PileContainmentViolation(
                    pile.Id, pile.Mark, pt, nearest?.Mark, minDist,
                    $"Cọc nằm ngoài toàn bộ đài móng! Khoảng cách tới đài gần nhất {nearest?.Mark ?? "N/A"} là {minDist:F1}mm."));
            }
        }

        return new PileContainmentSummary(piles.Count, containedCount, violations);
    }

    private static (FootingAuditItem? Nearest, double MinDist) FindNearestFooting(
        Point2D pt, IReadOnlyList<FootingAuditItem> footings)
    {
        FootingAuditItem? nearest = null;
        double minDist = double.PositiveInfinity;

        foreach (var ft in footings)
        {
            if (ft.Polygon == null || ft.Polygon.VertexCount < 3) continue;
            double d = ft.Polygon.DistanceToBoundary(pt);
            if (d < minDist)
            {
                minDist = d;
                nearest = ft;
            }
        }

        return (nearest, minDist);
    }
}
