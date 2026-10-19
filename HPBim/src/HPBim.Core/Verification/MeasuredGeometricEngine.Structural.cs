namespace HPBim.Core.Verification;

using System;
using System.Collections.Generic;
using HPBim.Core.Coordination;
using HPBim.Core.Execution;
using HPBim.Core.Geometry;

public sealed partial class MeasuredGeometricEngine
{
    private ElementGeometricAudit AuditColumn(ElementPlanItem item, ActualElementGeometry actual)
    {
        var discrepancies = new List<string>();
        double deltaPos = CheckColumnPosition(item, actual, discrepancies);
        double deltaWidth = CheckDimension(actual.WidthMm, GetExpectedDimension(item, "b", "Width", "width"), "Column width", discrepancies);
        double deltaHeight = CheckDimension(actual.HeightMm, GetExpectedDimension(item, "h", "Height", "depth", "Depth"), "Column depth", discrepancies);

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
            DeltaWidthMm = deltaWidth,
            DeltaHeightMm = deltaHeight,
            IsPositionOutOfTolerance = deltaPos > _tolerances.PlanPositionMm,
            IsDimensionOutOfTolerance = deltaWidth > _tolerances.SectionDimensionMm || deltaHeight > _tolerances.SectionDimensionMm,
            DefectReason = hasDefects ? string.Join("; ", discrepancies) : null,
            Discrepancies = discrepancies
        };
    }

    private double CheckColumnPosition(ElementPlanItem item, ActualElementGeometry actual, List<string> discrepancies)
    {
        if (item.Geometry is not PointGeometrySpec ptSpec || !actual.Centroid.HasValue) return 0.0;
        double deltaPos = new Point2D(ptSpec.Location.X, ptSpec.Location.Y)
            .DistanceTo(new Point2D(actual.Centroid.Value.X, actual.Centroid.Value.Y));
        if (deltaPos > _tolerances.PlanPositionMm || !IsFiniteNumber(deltaPos))
        {
            discrepancies.Add($"Column center offset ({deltaPos:F2}mm) exceeds tolerance ({_tolerances.PlanPositionMm:F2}mm).");
        }
        return deltaPos;
    }

    private ElementGeometricAudit AuditLinearStructure(ElementPlanItem item, ActualElementGeometry actual)
    {
        var discrepancies = new List<string>();
        var (deltaStart, deltaEnd) = CheckEndpoints(item, actual, discrepancies);
        double deltaElev = CheckLinearElevation(item, actual, discrepancies);
        double deltaWidth = CheckDimension(actual.WidthMm, GetExpectedDimension(item, "b", "Width", "width"), "Beam width", discrepancies);
        double deltaHeight = CheckDimension(actual.HeightMm, GetExpectedDimension(item, "h", "Height", "depth", "Depth"), "Beam height", discrepancies);

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
            DeltaCurveStartMm = deltaStart,
            DeltaCurveEndMm = deltaEnd,
            DeltaElevationMm = deltaElev,
            DeltaWidthMm = deltaWidth,
            DeltaHeightMm = deltaHeight,
            IsPositionOutOfTolerance = deltaStart > _tolerances.PlanPositionMm || deltaEnd > _tolerances.PlanPositionMm,
            IsElevationOutOfTolerance = deltaElev > _tolerances.ElevationMm,
            IsDimensionOutOfTolerance = deltaWidth > _tolerances.SectionDimensionMm || deltaHeight > _tolerances.SectionDimensionMm,
            DefectReason = hasDefects ? string.Join("; ", discrepancies) : null,
            Discrepancies = discrepancies
        };
    }

    private (double deltaStart, double deltaEnd) CheckEndpoints(ElementPlanItem item, ActualElementGeometry actual, List<string> discrepancies)
    {
        if (item.Geometry is not LineGeometrySpec lnSpec || !actual.CurveStart.HasValue || !actual.CurveEnd.HasValue)
        {
            return (0.0, 0.0);
        }

        double deltaStart = Distance3D(lnSpec.Start, actual.CurveStart.Value);
        double deltaEnd = Distance3D(lnSpec.End, actual.CurveEnd.Value);
        if (deltaStart > _tolerances.PlanPositionMm || deltaEnd > _tolerances.PlanPositionMm || !IsFiniteNumber(deltaStart) || !IsFiniteNumber(deltaEnd))
            discrepancies.Add($"Beam endpoints offset (start: {deltaStart:F2}mm, end: {deltaEnd:F2}mm) exceeds tolerance ({_tolerances.PlanPositionMm:F2}mm).");
        return (deltaStart, deltaEnd);
    }

    private double CheckLinearElevation(ElementPlanItem item, ActualElementGeometry actual, List<string> discrepancies)
    {
        if (item.Geometry is not LineGeometrySpec lnSpec || !actual.TopElevationMm.HasValue) return 0.0;
        double deltaElev = Math.Abs(actual.TopElevationMm.Value - lnSpec.ElevationMm);
        if (deltaElev > _tolerances.ElevationMm || !IsFiniteNumber(deltaElev))
            discrepancies.Add($"Beam elevation error ({deltaElev:F2}mm) exceeds tolerance ({_tolerances.ElevationMm:F2}mm).");
        return deltaElev;
    }

    private ElementGeometricAudit AuditPile(ElementPlanItem item, ActualElementGeometry actual)
    {
        var discrepancies = new List<string>();
        double deltaPos = 0.0;
        double deltaElev = 0.0;

        if (item.Geometry is PointGeometrySpec ptSpec && actual.Centroid.HasValue)
        {
            deltaPos = new Point2D(ptSpec.Location.X, ptSpec.Location.Y).DistanceTo(new Point2D(actual.Centroid.Value.X, actual.Centroid.Value.Y));
            if (deltaPos > _tolerances.PileCenterMm || !IsFiniteNumber(deltaPos))
                discrepancies.Add($"Pile center offset ({deltaPos:F2}mm) exceeds pile tolerance ({_tolerances.PileCenterMm:F2}mm).");

            if (actual.TopElevationMm.HasValue)
            {
                deltaElev = Math.Abs(actual.TopElevationMm.Value - ptSpec.OffsetZMm);
                if (deltaElev > _tolerances.ElevationMm || !IsFiniteNumber(deltaElev))
                    discrepancies.Add($"Pile cutoff elevation error ({deltaElev:F2}mm) exceeds tolerance ({_tolerances.ElevationMm:F2}mm).");
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
            DeltaCenterLineMm = deltaPos,
            DeltaElevationMm = deltaElev,
            IsPositionOutOfTolerance = deltaPos > _tolerances.PileCenterMm || !IsFiniteNumber(deltaPos),
            IsElevationOutOfTolerance = deltaElev > _tolerances.ElevationMm || !IsFiniteNumber(deltaElev),
            DefectReason = hasDefects ? string.Join("; ", discrepancies) : null,
            Discrepancies = discrepancies
        };
    }

    private ElementGeometricAudit AuditWall(ElementPlanItem item, ActualElementGeometry actual)
    {
        var discrepancies = new List<string>();
        double deltaStart = 0.0;
        double deltaEnd = 0.0;

        if (item.Geometry is LineGeometrySpec lnSpec && actual.CurveStart.HasValue && actual.CurveEnd.HasValue)
        {
            deltaStart = Distance3D(lnSpec.Start, actual.CurveStart.Value);
            deltaEnd = Distance3D(lnSpec.End, actual.CurveEnd.Value);
            if (deltaStart > _tolerances.PlanPositionMm || deltaEnd > _tolerances.PlanPositionMm || !IsFiniteNumber(deltaStart) || !IsFiniteNumber(deltaEnd))
                discrepancies.Add($"Wall location line error exceeds tolerance ({_tolerances.PlanPositionMm:F2}mm).");
        }

        double expThick = GetExpectedDimension(item, "thickness", "Thickness", "Width", "b");
        double deltaThick = CheckDimension(actual.ThicknessMm, expThick, "Wall thickness", discrepancies);

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
            DeltaWidthMm = deltaThick,
            IsPositionOutOfTolerance = deltaStart > _tolerances.PlanPositionMm || deltaEnd > _tolerances.PlanPositionMm || !IsFiniteNumber(deltaStart) || !IsFiniteNumber(deltaEnd),
            IsDimensionOutOfTolerance = deltaThick > _tolerances.SectionDimensionMm || !IsFiniteNumber(deltaThick),
            DefectReason = hasDefects ? string.Join("; ", discrepancies) : null,
            Discrepancies = discrepancies
        };
    }

    private static bool HasRequiredPileMeasurements(ActualElementGeometry actual, out string missingReason)
    {
        if (!actual.Centroid.HasValue || HasNaN(actual.Centroid))
        {
            missingReason = "Rule 0: Pile thiếu thuộc tính Centroid từ Revit API.";
            return false;
        }

        if (!actual.TopElevationMm.HasValue || HasNaN(actual.TopElevationMm))
        {
            missingReason = "Rule 0: Pile thiếu thuộc tính Cutoff Elevation (TopElevationMm) từ Revit API.";
            return false;
        }

        missingReason = string.Empty;
        return true;
    }

    private static bool HasRequiredColumnMeasurements(ElementPlanItem item, ActualElementGeometry actual, out string missingReason)
    {
        if (!actual.Centroid.HasValue || HasNaN(actual.Centroid))
        {
            missingReason = "Rule 0: Column thiếu thuộc tính Centroid từ Revit API.";
            return false;
        }

        if (HasNaN(actual.WidthMm) || HasNaN(actual.HeightMm))
        {
            missingReason = "Rule 0: Column chứa giá trị kích thước NaN không hợp lệ từ Revit API.";
            return false;
        }

        double expW = GetExpectedDimension(item, "b", "Width", "width");
        double expH = GetExpectedDimension(item, "h", "Height", "depth", "Depth");
        bool hasColDim = expW > 0 || expH > 0;
        if ((hasColDim || actual.WidthMm.HasValue || actual.HeightMm.HasValue) &&
            (!actual.WidthMm.HasValue || !actual.HeightMm.HasValue))
        {
            missingReason = "Rule 0: Column thiếu thuộc tính Width/Height từ Revit API.";
            return false;
        }

        missingReason = string.Empty;
        return true;
    }

    private static bool HasRequiredBeamMeasurements(CoordinationCategory category, ActualElementGeometry actual, out string missingReason)
    {
        if (!actual.CurveStart.HasValue || !actual.CurveEnd.HasValue || HasNaN(actual.CurveStart) || HasNaN(actual.CurveEnd))
        {
            missingReason = $"Rule 0: {category} thiếu thuộc tính CurveStart/CurveEnd từ Revit API.";
            return false;
        }

        if (!actual.TopElevationMm.HasValue || HasNaN(actual.TopElevationMm))
        {
            missingReason = $"Rule 0: {category} thiếu thuộc tính TopElevation từ Revit API.";
            return false;
        }

        missingReason = string.Empty;
        return true;
    }

    private static bool HasRequiredWallMeasurements(ActualElementGeometry actual, out string missingReason)
    {
        if (!actual.CurveStart.HasValue || !actual.CurveEnd.HasValue || HasNaN(actual.CurveStart) || HasNaN(actual.CurveEnd))
        {
            missingReason = "Rule 0: Wall thiếu thuộc tính CurveStart/CurveEnd từ Revit API.";
            return false;
        }

        if (HasNaN(actual.ThicknessMm))
        {
            missingReason = "Rule 0: Wall chứa giá trị ThicknessMm NaN không hợp lệ từ Revit API.";
            return false;
        }

        missingReason = string.Empty;
        return true;
    }
}
