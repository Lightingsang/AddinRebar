namespace HPBim.Core.Execution;

using System;
using System.Collections.Generic;
using HPBim.Core.Coordination;

/// <summary>
/// Granular execution outcome record for a single BIM element.
/// Guarantees zero-guesswork reporting with explicit status, Revit IDs, and error diagnostics.
/// </summary>
public sealed record ElementExecutionRecord
{
    public CompositeSourceKey SourceKey { get; init; } = CompositeSourceKey.Empty;
    public CoordinationCategory Category { get; init; }
    public string Mark { get; init; } = string.Empty;
    public ElementExecutionStatus Status { get; init; }
    public long? TargetElementId { get; init; }
    public string? TargetUniqueId { get; init; }
    public string? ErrorCode { get; init; }
    public string? ErrorReason { get; init; }
    public IReadOnlyList<string> Warnings { get; init; } = Array.Empty<string>();
    public long ExecutionDurationMs { get; init; }
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;

    public double? MeasuredWidthMm { get; init; }
    public double? MeasuredHeightMm { get; init; }
    public double? MeasuredElevationMm { get; init; }

    public bool IsSuccess => Status is ElementExecutionStatus.Created
                                    or ElementExecutionStatus.Updated
                                    or ElementExecutionStatus.NoOp;

    public static ElementExecutionRecord Created(
        CompositeSourceKey sourceKey,
        CoordinationCategory category,
        string mark,
        long targetElementId,
        string targetUniqueId,
        IReadOnlyList<string>? warnings = null,
        long durationMs = 0) => new()
    {
        SourceKey = sourceKey,
        Category = category,
        Mark = mark,
        Status = ElementExecutionStatus.Created,
        TargetElementId = targetElementId,
        TargetUniqueId = targetUniqueId,
        Warnings = warnings ?? Array.Empty<string>(),
        ExecutionDurationMs = durationMs
    };

    public static ElementExecutionRecord Updated(
        CompositeSourceKey sourceKey,
        CoordinationCategory category,
        string mark,
        long targetElementId,
        string targetUniqueId,
        IReadOnlyList<string>? warnings = null,
        long durationMs = 0) => new()
    {
        SourceKey = sourceKey,
        Category = category,
        Mark = mark,
        Status = ElementExecutionStatus.Updated,
        TargetElementId = targetElementId,
        TargetUniqueId = targetUniqueId,
        Warnings = warnings ?? Array.Empty<string>(),
        ExecutionDurationMs = durationMs
    };

    public static ElementExecutionRecord NoOp(
        CompositeSourceKey sourceKey,
        CoordinationCategory category,
        string mark,
        long targetElementId,
        string targetUniqueId) => new()
    {
        SourceKey = sourceKey,
        Category = category,
        Mark = mark,
        Status = ElementExecutionStatus.NoOp,
        TargetElementId = targetElementId,
        TargetUniqueId = targetUniqueId
    };

    public static ElementExecutionRecord Skipped(
        CompositeSourceKey sourceKey,
        CoordinationCategory category,
        string mark,
        string reason) => new()
    {
        SourceKey = sourceKey,
        Category = category,
        Mark = mark,
        Status = ElementExecutionStatus.Skipped,
        ErrorReason = reason
    };

    public static ElementExecutionRecord Failed(
        CompositeSourceKey sourceKey,
        CoordinationCategory category,
        string mark,
        string errorCode,
        string errorReason,
        long durationMs = 0) => new()
    {
        SourceKey = sourceKey,
        Category = category,
        Mark = mark,
        Status = ElementExecutionStatus.Failed,
        ErrorCode = errorCode,
        ErrorReason = errorReason,
        ExecutionDurationMs = durationMs
    };

    public static ElementExecutionRecord OutcomeUnknown(
        CompositeSourceKey sourceKey,
        CoordinationCategory category,
        string mark,
        string errorReason,
        string errorCode = "ERR_OUTCOME_UNKNOWN") => new()
    {
        SourceKey = sourceKey,
        Category = category,
        Mark = mark,
        Status = ElementExecutionStatus.OutcomeUnknown,
        ErrorCode = errorCode,
        ErrorReason = errorReason
    };
}
