namespace HPBim.Core.Coordination;

using System;

/// <summary>
/// Independent QA/QC audit record measuring deviations and specifying defect routing.
/// </summary>
public sealed record AuditRecord
{
    public AuditStatus Status { get; init; } = AuditStatus.ChuaDuCoSo;
    public int AuditStep { get; init; } = 1;
    public double DeltaDimensionMm { get; init; }
    public double DeltaElevationMm { get; init; }
    public double DeltaPositionMm { get; init; }
    public DefectRoute DefectRoute { get; init; } = DefectRoute.None;
    public string Notes { get; init; } = string.Empty;
    public DateTime? AuditedAt { get; init; }
}
