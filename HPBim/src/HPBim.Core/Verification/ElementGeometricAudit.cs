namespace HPBim.Core.Verification;

using System;
using System.Collections.Generic;
using HPBim.Core.Coordination;
using HPBim.Core.Execution;

public sealed record ElementGeometricAudit
{
    public ElementPlanItem? Planned { get; init; }
    public ActualElementGeometry? Actual { get; init; }
    public CompositeSourceKey SourceKey { get; init; } = CompositeSourceKey.Empty;
    public CoordinationCategory Category { get; init; }
    public string Mark { get; init; } = string.Empty;
    public GeometricVerificationStatus Status { get; init; } = GeometricVerificationStatus.Inconclusive;
    public string? TargetUniqueId { get; init; }
    public long? TargetElementId { get; init; }

    // Measured deviations in millimeters
    public double DeltaCenterLineMm { get; init; }
    public double DeltaElevationMm { get; init; }
    public double DeltaWidthMm { get; init; }
    public double DeltaHeightMm { get; init; }
    public double DeltaCurveStartMm { get; init; }
    public double DeltaCurveEndMm { get; init; }

    // Tolerance violation flags
    public bool IsPositionOutOfTolerance { get; init; }
    public bool IsElevationOutOfTolerance { get; init; }
    public bool IsDimensionOutOfTolerance { get; init; }

    public string? DefectReason { get; init; }
    public IReadOnlyList<string> Discrepancies { get; init; } = Array.Empty<string>();

    public bool IsVerified => Status == GeometricVerificationStatus.Verified;

    public AuditRecord ToCoordinationAuditRecord(int auditStep = 4) => new()
    {
        Status = Status switch
        {
            GeometricVerificationStatus.Verified => AuditStatus.Dung,
            GeometricVerificationStatus.Defect => AuditStatus.SaiCanSua,
            _ => AuditStatus.ChuaDuCoSo
        },
        AuditStep = auditStep,
        DeltaDimensionMm = Math.Max(Math.Abs(DeltaWidthMm), Math.Abs(DeltaHeightMm)),
        DeltaElevationMm = Math.Abs(DeltaElevationMm),
        DeltaPositionMm = Math.Abs(DeltaCenterLineMm),
        DefectRoute = Status == GeometricVerificationStatus.Defect ? DefectRoute.RevitFix : DefectRoute.None,
        Notes = DefectReason ?? (Status == GeometricVerificationStatus.Verified ? "Đạt nghiệm thu hình học 100%" : "Chưa đủ cơ sở để kết luận"),
        AuditedAt = DateTime.UtcNow
    };
}
