namespace HPBim.Core.Verification;

using System;
using System.Collections.Generic;
using HPBim.Core.Coordination;
using HPBim.Core.Execution;
using HPBim.Core.Geometry;

public sealed partial class MeasuredGeometricEngine
{
    private ElementGeometricAudit AuditProfileStructure(ElementPlanItem item, ActualElementGeometry actual)
    {
        var discrepancies = new List<string>();
        var (deltaPos, deltaElev, deltaThick) = CheckProfileGeometry(item, actual, discrepancies);
        bool hasDefects = discrepancies.Count > 0;
        return new ElementGeometricAudit
        {
            Planned = item,
            Actual = actual,
            SourceKey = item.SourceKey,
            Category = item.Category,
            Mark = item.Mark,
            Status = hasDefects ? GeometricVerificationStatus.Defect : GeometricVerificationStatus.Verified,
            TargetUniqueId = actual.UniqueId,
            TargetElementId = actual.ElementId,
            DeltaCenterLineMm = deltaPos,
            DeltaElevationMm = deltaElev,
            DeltaWidthMm = deltaThick,
            IsPositionOutOfTolerance = deltaPos > _tolerances.PlanPositionMm || !IsFiniteNumber(deltaPos),
            IsElevationOutOfTolerance = deltaElev > _tolerances.ElevationMm || !IsFiniteNumber(deltaElev),
            IsDimensionOutOfTolerance = deltaThick > _tolerances.SectionDimensionMm || !IsFiniteNumber(deltaThick),
            DefectReason = hasDefects ? string.Join("; ", discrepancies) : null,
            Discrepancies = discrepancies
        };
    }

    private (double deltaPos, double deltaElev, double deltaThick) CheckProfileGeometry(
        ElementPlanItem item, ActualElementGeometry actual, List<string> discrepancies)
    {
        double deltaElev = 0.0, deltaThick = 0.0, deltaPos = 0.0;
        if (item.Geometry is not ProfileGeometrySpec profSpec) return (0.0, 0.0, 0.0);

        if (actual.TopElevationMm.HasValue)
        {
            deltaElev = Math.Abs(actual.TopElevationMm.Value - profSpec.ElevationMm);
            if (deltaElev > _tolerances.ElevationMm || !IsFiniteNumber(deltaElev))
                discrepancies.Add($"Profile top elevation error ({deltaElev:F2}mm) exceeds tolerance ({_tolerances.ElevationMm:F2}mm).");
        }

        if (profSpec.ThicknessMm > 0 && actual.ThicknessMm.HasValue)
        {
            deltaThick = Math.Abs(actual.ThicknessMm.Value - profSpec.ThicknessMm);
            if (deltaThick > _tolerances.SectionDimensionMm || !IsFiniteNumber(deltaThick))
                discrepancies.Add($"Profile thickness error ({deltaThick:F2}mm) exceeds tolerance ({_tolerances.SectionDimensionMm:F2}mm).");
        }

        if (actual.Centroid.HasValue && profSpec.OuterLoop != null && profSpec.OuterLoop.Count > 0)
        {
            Point2D expCentroid = ComputeProfileCentroid(profSpec.OuterLoop);
            deltaPos = new Point2D(actual.Centroid.Value.X, actual.Centroid.Value.Y).DistanceTo(expCentroid);
            if (deltaPos > _tolerances.PlanPositionMm || !IsFiniteNumber(deltaPos))
                discrepancies.Add($"Profile centroid offset ({deltaPos:F2}mm) exceeds tolerance ({_tolerances.PlanPositionMm:F2}mm).");
        }

        return (deltaPos, deltaElev, deltaThick);
    }

    private static Point2D ComputeProfileCentroid(IReadOnlyList<Point3D> loop)
    {
        if (loop == null || loop.Count == 0) return Point2D.Zero;

        int count = loop.Count;
        if (count >= 2 &&
            Math.Abs(loop[0].X - loop[count - 1].X) < 1e-4 &&
            Math.Abs(loop[0].Y - loop[count - 1].Y) < 1e-4)
        {
            count--;
        }

        if (count >= 3)
        {
            try
            {
                var pts = new Point2D[count];
                for (int i = 0; i < count; i++)
                {
                    pts[i] = new Point2D(loop[i].X, loop[i].Y);
                }
                return new Polygon2D(pts).Centroid;
            }
            catch (ArgumentException)
            {
                // Fall back to vertex average if polygon construction fails on degenerate loop
            }
        }

        double sx = 0.0, sy = 0.0;
        int n = Math.Max(1, count);
        for (int i = 0; i < n; i++)
        {
            sx += loop[i].X;
            sy += loop[i].Y;
        }
        return new Point2D(sx / n, sy / n);
    }

    private ElementGeometricAudit AuditLevel(ElementPlanItem item, ActualElementGeometry actual)
    {
        var discrepancies = new List<string>();
        double deltaElev = 0.0;

        if (item.Geometry is LevelGeometrySpec lvlSpec && actual.TopElevationMm.HasValue)
        {
            double expElevMm = lvlSpec.ElevationM * 1000.0;
            deltaElev = Math.Abs(actual.TopElevationMm.Value - expElevMm);
            if (deltaElev > _tolerances.ElevationMm || !IsFiniteNumber(deltaElev))
            {
                discrepancies.Add($"Level elevation error ({deltaElev:F2}mm) exceeds tolerance ({_tolerances.ElevationMm:F2}mm).");
            }
        }

        bool hasDefects = discrepancies.Count > 0;
        return new ElementGeometricAudit
        {
            Planned = item,
            Actual = actual,
            SourceKey = item.SourceKey,
            Category = item.Category,
            Mark = item.Mark,
            Status = hasDefects ? GeometricVerificationStatus.Defect : GeometricVerificationStatus.Verified,
            TargetUniqueId = actual.UniqueId,
            TargetElementId = actual.ElementId,
            DeltaElevationMm = deltaElev,
            IsElevationOutOfTolerance = deltaElev > _tolerances.ElevationMm || !IsFiniteNumber(deltaElev),
            DefectReason = hasDefects ? string.Join("; ", discrepancies) : null,
            Discrepancies = discrepancies
        };
    }

    private ElementGeometricAudit AuditGrid(ElementPlanItem item, ActualElementGeometry actual)
    {
        var discrepancies = new List<string>();
        double deltaStart = 0.0;
        double deltaEnd = 0.0;

        if (item.Geometry is LineGeometrySpec lnSpec && actual.CurveStart.HasValue && actual.CurveEnd.HasValue)
        {
            deltaStart = Distance3D(lnSpec.Start, actual.CurveStart.Value);
            deltaEnd = Distance3D(lnSpec.End, actual.CurveEnd.Value);
            if (deltaStart > _tolerances.PlanPositionMm || deltaEnd > _tolerances.PlanPositionMm || !IsFiniteNumber(deltaStart) || !IsFiniteNumber(deltaEnd))
            {
                discrepancies.Add($"Grid line location error exceeds tolerance ({_tolerances.PlanPositionMm:F2}mm).");
            }
        }

        bool hasDefects = discrepancies.Count > 0;
        return new ElementGeometricAudit
        {
            Planned = item,
            Actual = actual,
            SourceKey = item.SourceKey,
            Category = item.Category,
            Mark = item.Mark,
            Status = hasDefects ? GeometricVerificationStatus.Defect : GeometricVerificationStatus.Verified,
            TargetUniqueId = actual.UniqueId,
            TargetElementId = actual.ElementId,
            DeltaCenterLineMm = Math.Max(deltaStart, deltaEnd),
            IsPositionOutOfTolerance = deltaStart > _tolerances.PlanPositionMm || deltaEnd > _tolerances.PlanPositionMm || !IsFiniteNumber(deltaStart) || !IsFiniteNumber(deltaEnd),
            DefectReason = hasDefects ? string.Join("; ", discrepancies) : null,
            Discrepancies = discrepancies
        };
    }

    private double CheckDimension(double? actualMm, double expectedMm, string dimName, List<string> discrepancies)
    {
        if (!actualMm.HasValue)
        {
            if (expectedMm > 0)
            {
                discrepancies.Add($"{dimName}: Expected {expectedMm:F1}mm, but actual parameter was unmeasurable (null)");
                return expectedMm;
            }
            return 0.0;
        }

        if (double.IsNaN(actualMm.Value))
        {
            discrepancies.Add($"{dimName} is NaN; invalid measurement value.");
            return double.NaN;
        }

        if (expectedMm <= 0) return 0.0;

        double delta = Math.Abs(actualMm.Value - expectedMm);
        if (delta > _tolerances.SectionDimensionMm || !IsFiniteNumber(delta))
        {
            discrepancies.Add($"{dimName} error ({delta:F2}mm, actual {actualMm.Value:F1} vs exp {expectedMm:F1}) exceeds tolerance ({_tolerances.SectionDimensionMm:F2}mm).");
        }
        return delta;
    }

    private static bool HasRequiredProfileMeasurements(
        CoordinationCategory category,
        ElementPlanItem item,
        ActualElementGeometry actual,
        out string missingReason)
    {
        if (!actual.TopElevationMm.HasValue || HasNaN(actual.TopElevationMm))
        {
            missingReason = $"Rule 0: {category} thiếu thuộc tính TopElevation từ Revit API.";
            return false;
        }

        if (HasNaN(actual.ThicknessMm))
        {
            missingReason = $"Rule 0: {category} chứa giá trị ThicknessMm NaN không hợp lệ từ Revit API.";
            return false;
        }

        if (item.Geometry is ProfileGeometrySpec prof && prof.ThicknessMm > 0 && !actual.ThicknessMm.HasValue)
        {
            missingReason = $"Rule 0: {category} thiếu thuộc tính Thickness từ Revit API.";
            return false;
        }

        if (!actual.Centroid.HasValue || HasNaN(actual.Centroid))
        {
            missingReason = $"Rule 0: {category} thiếu thuộc tính Centroid từ Revit API.";
            return false;
        }

        missingReason = string.Empty;
        return true;
    }

    private static bool HasRequiredLevelMeasurements(ActualElementGeometry actual, out string missingReason)
    {
        if (!actual.TopElevationMm.HasValue || HasNaN(actual.TopElevationMm))
        {
            missingReason = "Rule 0: Level thiếu thuộc tính TopElevation từ Revit API.";
            return false;
        }

        missingReason = string.Empty;
        return true;
    }

    private static bool HasRequiredGridMeasurements(ActualElementGeometry actual, out string missingReason)
    {
        if (!actual.CurveStart.HasValue || !actual.CurveEnd.HasValue || HasNaN(actual.CurveStart) || HasNaN(actual.CurveEnd))
        {
            missingReason = "Rule 0: Grid thiếu thuộc tính CurveStart/CurveEnd từ Revit API.";
            return false;
        }

        missingReason = string.Empty;
        return true;
    }
}
