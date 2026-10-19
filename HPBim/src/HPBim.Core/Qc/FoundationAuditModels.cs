namespace HPBim.Core.Qc;

using System;
using System.Collections.Generic;
using System.Linq;
using HPBim.Core.Coordination;
using HPBim.Core.Geometry;

public enum FoundationStepStatus
{
    Passed = 1,
    Failed = 2,
    Inconclusive = 3
}

public sealed record GridAuditItem(
    string Name,
    double P0X,
    double P0Y,
    double P1X,
    double P1Y,
    bool Pinned);

public sealed record LevelAuditItem(
    string Name,
    double ElevationMm,
    bool Pinned);

public sealed record PileAuditItem(
    long Id,
    string Mark,
    double X,
    double Y,
    double TopElevationMm,
    string SymbolName);

public sealed record FootingAuditItem(
    long Id,
    string Mark,
    string TypeName,
    double TopElevationMm,
    double BottomElevationMm,
    double ThicknessMm,
    Point2D Centroid,
    Polygon2D Polygon);

public sealed record BeamAuditItem(
    long Id,
    string Mark,
    string SymbolName,
    double WidthMm,
    double HeightMm,
    double TopElevationMm,
    Point2D StartPoint,
    Point2D EndPoint);

public sealed record FoundationStepResult(
    int StepNumber,
    string StepName,
    FoundationStepStatus Status,
    string Summary,
    IReadOnlyList<string> Errors)
{
    public bool Passed => Status == FoundationStepStatus.Passed;
    public bool IsInconclusive => Status == FoundationStepStatus.Inconclusive;

    public FoundationStepResult(int stepNumber, string stepName, bool passed, string summary, IReadOnlyList<string> errors)
        : this(stepNumber, stepName, passed ? FoundationStepStatus.Passed : FoundationStepStatus.Failed, summary, errors)
    {
    }
}

public sealed record FoundationAuditReport(
    bool AllPassed,
    IReadOnlyList<FoundationStepResult> Steps,
    DateTime AuditedAtUtc,
    FoundationStepStatus OverallStatus)
{
    public int PassedStepsCount => Steps.Count(s => s.Status == FoundationStepStatus.Passed);
    public int FailedStepsCount => Steps.Count(s => s.Status == FoundationStepStatus.Failed);
    public int InconclusiveStepsCount => Steps.Count(s => s.Status == FoundationStepStatus.Inconclusive);

    public FoundationAuditReport(bool allPassed, IReadOnlyList<FoundationStepResult> steps, DateTime auditedAtUtc)
        : this(allPassed, steps, auditedAtUtc, allPassed ? FoundationStepStatus.Passed : (steps.Any(s => s.Status == FoundationStepStatus.Failed) ? FoundationStepStatus.Failed : FoundationStepStatus.Inconclusive))
    {
    }
}

public sealed record FoundationAuditContext(
    IReadOnlyList<GridAuditItem> Grids,
    IReadOnlyList<LevelAuditItem> Levels,
    IReadOnlyList<PileAuditItem> Piles,
    IReadOnlyList<FootingAuditItem> Footings,
    IReadOnlyList<BeamAuditItem> Beams,
    IReadOnlyDictionary<string, double>? ExpectedXGrids = null,
    IReadOnlyDictionary<string, double>? ExpectedYGrids = null,
    IReadOnlyDictionary<string, double>? ExpectedLevels = null,
    IReadOnlyDictionary<long, Point2D>? GroundTruthPiles = null,
    IReadOnlyDictionary<string, Point2D>? GroundTruthFootings = null,
    BimCoordinationLedger? Ledger = null);
