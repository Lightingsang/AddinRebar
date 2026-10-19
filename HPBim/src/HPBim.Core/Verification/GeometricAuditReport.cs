namespace HPBim.Core.Verification;

using System;
using System.Collections.Generic;
using System.Linq;

public sealed record GeometricAuditReport
{
    public Guid PlanId { get; init; }
    public string PlanHash { get; init; } = string.Empty;
    public bool OverallPassed { get; init; }
    public int TotalPlannedElements { get; init; }
    public int TotalMeasuredElements { get; init; }
    public int VerifiedCount { get; init; }
    public int DefectCount { get; init; }
    public int InconclusiveCount { get; init; }
    public int MissingCount { get; init; }

    public IReadOnlyList<ElementGeometricAudit> Audits { get; init; } = Array.Empty<ElementGeometricAudit>();
    public IReadOnlyList<string> SummaryDiscrepancies { get; init; } = Array.Empty<string>();
    public DateTime AuditedAtUtc { get; init; } = DateTime.UtcNow;

    public static GeometricAuditReport Create(
        Guid planId,
        string planHash,
        IReadOnlyList<ElementGeometricAudit> audits)
    {
        int totalPlanned = audits.Count;
        int verified = audits.Count(a => a.Status == GeometricVerificationStatus.Verified);
        int defects = audits.Count(a => a.Status == GeometricVerificationStatus.Defect);
        int inconclusive = audits.Count(a => a.Status == GeometricVerificationStatus.Inconclusive);
        int missing = audits.Count(a => a.Status == GeometricVerificationStatus.Missing);

        bool overallPassed = totalPlanned > 0 && verified == totalPlanned && defects == 0 && inconclusive == 0 && missing == 0;

        var discrepancies = audits
            .Where(a => a.Status != GeometricVerificationStatus.Verified)
            .SelectMany(a => a.Discrepancies)
            .ToList();

        return new GeometricAuditReport
        {
            PlanId = planId,
            PlanHash = planHash,
            OverallPassed = overallPassed,
            TotalPlannedElements = totalPlanned,
            TotalMeasuredElements = audits.Count(a => a.Status != GeometricVerificationStatus.Missing),
            VerifiedCount = verified,
            DefectCount = defects,
            InconclusiveCount = inconclusive,
            MissingCount = missing,
            Audits = audits,
            SummaryDiscrepancies = discrepancies
        };
    }
}
