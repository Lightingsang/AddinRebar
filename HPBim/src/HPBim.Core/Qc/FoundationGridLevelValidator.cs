namespace HPBim.Core.Qc;

using System;
using System.Collections.Generic;
using System.Linq;
using HPBim.Core.Geometry;

/// <summary>
/// Domain validator for grid spans, level elevations, and pile cutoff elevations.
/// Pure mathematical verification without heuristics.
/// </summary>
public static class FoundationGridLevelValidator
{
    public static void ValidateGridSpans(
        IReadOnlyList<GridAuditItem> grids,
        IReadOnlyDictionary<string, double>? expX,
        IReadOnlyDictionary<string, double>? expY,
        List<string> errors)
    {
        if (expX == null && expY == null) return;
        foreach (var g in grids)
        {
            double midX = (g.P0X + g.P1X) / 2.0;
            if (expX != null && expX.TryGetValue(g.Name, out double tx) && Math.Abs(midX - tx) > 1.0)
            {
                errors.Add($"Trục X {g.Name} lệch {Math.Abs(midX - tx):F2}mm (> 1.0mm).");
            }

            double midY = (g.P0Y + g.P1Y) / 2.0;
            if (expY != null && expY.TryGetValue(g.Name, out double ty) && Math.Abs(midY - ty) > 1.0)
            {
                errors.Add($"Trục Y {g.Name} lệch {Math.Abs(midY - ty):F2}mm (> 1.0mm).");
            }
        }
    }

    public static void ValidateLevelElevations(
        IReadOnlyList<LevelAuditItem> levels,
        IReadOnlyDictionary<string, double>? expectedLevels,
        List<string> errors)
    {
        if (expectedLevels == null) return;
        foreach (var l in levels)
        {
            if (expectedLevels.TryGetValue(l.Name, out double expZ) && Math.Abs(l.ElevationMm - expZ) > 1.0)
            {
                errors.Add($"Cao trình {l.Name} lệch {Math.Abs(l.ElevationMm - expZ):F2}mm (> 1.0mm).");
            }
        }
    }

    public static void AuditPileElevations(
        IReadOnlyList<PileAuditItem> piles,
        IReadOnlyList<FootingAuditItem> footings,
        List<string> errors)
    {
        foreach (var p in piles)
        {
            var pt = new Point2D(p.X, p.Y);
            var f = footings.FirstOrDefault(ft => ft.Polygon != null && ft.Polygon.Contains(pt));
            if (f != null)
            {
                double expZ = f.BottomElevationMm + 100.0;
                if (Math.Abs(p.TopElevationMm - expZ) > 1.0)
                {
                    errors.Add($"Cọc {p.Mark} đỉnh {p.TopElevationMm:F1}mm != đáy đài {f.Mark} ({f.BottomElevationMm:F1}mm) + 100mm = {expZ:F1}mm.");
                }
            }
            else if (p.TopElevationMm > -2400.0 || p.TopElevationMm < -2900.0)
            {
                errors.Add($"Cọc {p.Mark} COTE đầu cọc {p.TopElevationMm:F1}mm ngoài phạm vi ngàm đáy đài.");
            }
        }
    }
}
