namespace HPBim.Core.Verification;

using System;
using System.Collections.Generic;
using HPBim.Core.Coordination;
using HPBim.Core.Execution;
using HPBim.Core.Geometry;

public sealed partial class MeasuredGeometricEngine
{
    private readonly CoordinationTolerances _tolerances;

    public MeasuredGeometricEngine(CoordinationTolerances? tolerances = null)
    {
        _tolerances = tolerances ?? new CoordinationTolerances();
    }

    public GeometricAuditReport AuditPlan(
        BimExecutionPlan plan,
        IReadOnlyDictionary<string, ActualElementGeometry> actuals)
    {
        if (plan == null) throw new ArgumentNullException(nameof(plan));
        if (actuals == null) throw new ArgumentNullException(nameof(actuals));

        var audits = new List<ElementGeometricAudit>(plan.Items.Count);
        foreach (var item in plan.Items)
        {
            audits.Add(AuditItem(item, actuals));
        }

        return GeometricAuditReport.Create(plan.PlanId, plan.PlanHash, audits);
    }

    public GeometricAuditReport AuditPlan(
        BimExecutionPlan plan,
        IReadOnlyList<ActualElementGeometry> actualsList)
    {
        if (plan == null) throw new ArgumentNullException(nameof(plan));
        if (actualsList == null) throw new ArgumentNullException(nameof(actualsList));

        var dict = new Dictionary<string, ActualElementGeometry>(StringComparer.OrdinalIgnoreCase);
        foreach (var act in actualsList)
        {
            if (!string.IsNullOrEmpty(act.Key)) dict[act.Key] = act;
            if (!string.IsNullOrEmpty(act.UniqueId)) dict[act.UniqueId!] = act;
        }

        return AuditPlan(plan, dict);
    }

    public ElementGeometricAudit AuditItem(
        ElementPlanItem item,
        IReadOnlyDictionary<string, ActualElementGeometry> actuals)
    {
        if (item == null) throw new ArgumentNullException(nameof(item));

        ActualElementGeometry? actual = FindActual(item, actuals);
        if (actual == null)
        {
            return CreateMissingAudit(item);
        }

        if (item.Geometry == null)
        {
            return CreateInconclusiveAudit(item, actual, $"Rule 0: ElementPlanItem '{item.Mark}' lacks planned geometry specification.");
        }

        if (!actual.HasGeometry)
        {
            return CreateInconclusiveAudit(item, actual, "Rule 0: Revit element exists but physical geometry was unmeasurable or degenerated.");
        }

        if (!HasRequiredMeasurements(item.Category, item, actual, out string missingReason))
        {
            return CreateInconclusiveAudit(item, actual, missingReason);
        }

        return item.Category switch
        {
            CoordinationCategory.Column => AuditColumn(item, actual),
            CoordinationCategory.Beam or CoordinationCategory.GradeBeam => AuditLinearStructure(item, actual),
            CoordinationCategory.Wall => AuditWall(item, actual),
            CoordinationCategory.Pile => AuditPile(item, actual),
            CoordinationCategory.Footing or CoordinationCategory.Slab => AuditProfileStructure(item, actual),
            CoordinationCategory.Level => AuditLevel(item, actual),
            CoordinationCategory.Grid => AuditGrid(item, actual),
            _ => AuditGeneric(item, actual)
        };
    }

    private static ElementGeometricAudit AuditGeneric(ElementPlanItem item, ActualElementGeometry actual) => new()
    {
        Planned = item,
        Actual = actual,
        SourceKey = item.SourceKey,
        Category = item.Category,
        Mark = item.Mark,
        Status = GeometricVerificationStatus.Inconclusive,
        TargetUniqueId = actual.UniqueId,
        TargetElementId = actual.ElementId,
        DefectReason = $"Rule 0: Category '{item.Category}' lacks empirical measurement rules; cannot auto-verify without actual physical measurements.",
        Discrepancies = new[] { $"Unmeasured category [{item.Category}] marked Inconclusive under Rule 0." }
    };

    private static ActualElementGeometry? FindActual(ElementPlanItem item, IReadOnlyDictionary<string, ActualElementGeometry> actuals)
    {
        if (!string.IsNullOrEmpty(item.TargetUniqueId) && actuals.TryGetValue(item.TargetUniqueId!, out var actByUid))
        {
            return actByUid;
        }

        if (item.SourceKey != null)
        {
            string formattedKey = item.SourceKey.ToString();
            if (actuals.TryGetValue(formattedKey, out var act)) return act;

            string plainKey = $"{item.SourceKey.DocIdentifier}#{item.SourceKey.Category}:{item.SourceKey.PrimaryHandle}";
            if (actuals.TryGetValue(plainKey, out act)) return act;

            if (!string.IsNullOrEmpty(item.SourceKey.PrimaryHandle) && actuals.TryGetValue(item.SourceKey.PrimaryHandle, out act)) return act;
        }

        if (!string.IsNullOrEmpty(item.Mark) && actuals.TryGetValue(item.Mark, out var actByMark)) return actByMark;

        return null;
    }

    private static ElementGeometricAudit CreateMissingAudit(ElementPlanItem item) => new()
    {
        Planned = item,
        SourceKey = item.SourceKey,
        Category = item.Category,
        Mark = item.Mark,
        Status = GeometricVerificationStatus.Missing,
        DefectReason = $"Element [{item.Category}] Mark '{item.Mark}' was not found in active Revit model.",
        Discrepancies = new[] { $"Missing element: {item.SourceKey} ({item.Mark})" }
    };

    private static ElementGeometricAudit CreateInconclusiveAudit(ElementPlanItem item, ActualElementGeometry actual, string reason) => new()
    {
        Planned = item,
        Actual = actual,
        SourceKey = item.SourceKey,
        Category = item.Category,
        Mark = item.Mark,
        Status = GeometricVerificationStatus.Inconclusive,
        TargetUniqueId = actual.UniqueId,
        TargetElementId = actual.ElementId,
        DefectReason = reason,
        Discrepancies = new[] { reason }
    };

    private static double GetExpectedDimension(ElementPlanItem item, params string[] paramNames)
    {
        if (item.Resource?.DimensionParametersMm != null)
        {
            foreach (string name in paramNames)
            {
                if (item.Resource.DimensionParametersMm.TryGetValue(name, out double val)) return val;
            }
        }
        return 0.0;
    }

    private static double Distance3D(Point3D a, Point3D b)
    {
        double dx = a.X - b.X;
        double dy = a.Y - b.Y;
        double dz = a.Z - b.Z;
        return Math.Sqrt(dx * dx + dy * dy + dz * dz);
    }

    public static bool HasRequiredMeasurements(
        CoordinationCategory category,
        ElementPlanItem item,
        ActualElementGeometry actual,
        out string missingReason)
    {
        if (item == null) throw new ArgumentNullException(nameof(item));
        if (actual == null) throw new ArgumentNullException(nameof(actual));

        if (item.Geometry == null)
        {
            missingReason = $"Rule 0: ElementPlanItem '{item.Mark}' lacks planned geometry specification.";
            return false;
        }

        if (!IsMatchingGeometrySpec(category, item.Geometry, out string expectedType))
        {
            if (string.IsNullOrEmpty(expectedType))
            {
                return HasRequiredGenericMeasurements(category, out missingReason);
            }
            missingReason = $"Rule 0: ElementPlanItem '{item.Mark}' category '{category}' expects {expectedType} geometry specification.";
            return false;
        }

        return category switch
        {
            CoordinationCategory.Pile => HasRequiredPileMeasurements(actual, out missingReason),
            CoordinationCategory.Column => HasRequiredColumnMeasurements(item, actual, out missingReason),
            CoordinationCategory.Beam or CoordinationCategory.GradeBeam => HasRequiredBeamMeasurements(category, actual, out missingReason),
            CoordinationCategory.Wall => HasRequiredWallMeasurements(actual, out missingReason),
            CoordinationCategory.Footing or CoordinationCategory.Slab => HasRequiredProfileMeasurements(category, item, actual, out missingReason),
            CoordinationCategory.Level => HasRequiredLevelMeasurements(actual, out missingReason),
            CoordinationCategory.Grid => HasRequiredGridMeasurements(actual, out missingReason),
            _ => HasRequiredGenericMeasurements(category, out missingReason)
        };
    }

    internal static bool IsFiniteNumber(double val) => !double.IsNaN(val) && !double.IsInfinity(val);

    internal static bool HasNaN(double? val) => val.HasValue && double.IsNaN(val.Value);

    internal static bool HasNaN(Point3D? pt) => pt.HasValue && (double.IsNaN(pt.Value.X) || double.IsNaN(pt.Value.Y) || double.IsNaN(pt.Value.Z));

    private static bool IsMatchingGeometrySpec(CoordinationCategory category, ElementGeometrySpec spec, out string expectedType)
    {
        switch (category)
        {
            case CoordinationCategory.Column:
            case CoordinationCategory.Pile:
                expectedType = nameof(PointGeometrySpec);
                return spec is PointGeometrySpec;
            case CoordinationCategory.Beam:
            case CoordinationCategory.GradeBeam:
            case CoordinationCategory.Wall:
            case CoordinationCategory.Grid:
                expectedType = nameof(LineGeometrySpec);
                return spec is LineGeometrySpec;
            case CoordinationCategory.Footing:
            case CoordinationCategory.Slab:
                expectedType = nameof(ProfileGeometrySpec);
                return spec is ProfileGeometrySpec;
            case CoordinationCategory.Level:
                expectedType = nameof(LevelGeometrySpec);
                return spec is LevelGeometrySpec;
            default:
                expectedType = string.Empty;
                return false;
        }
    }

    private static bool HasRequiredGenericMeasurements(CoordinationCategory category, out string missingReason)
    {
        missingReason = $"Rule 0: Category '{category}' has no standardized measurement rules implemented.";
        return false;
    }
}
