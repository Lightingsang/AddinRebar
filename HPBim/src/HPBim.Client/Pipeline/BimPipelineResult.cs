namespace HPBim.Client.Pipeline;

using System;
using System.Collections.Generic;
using System.Linq;
using HPBim.Core.Execution;

/// <summary>
/// Overall outcome of an 8-stage BIM model creation pipeline execution.
/// </summary>
public sealed record BimPipelineResult
{
    public bool Success { get; init; }
    public bool RolledBack { get; init; }
    public bool IsOutcomeUnknown { get; init; }
    public Guid? OperationId { get; init; }
    public long TotalDurationMs { get; init; }
    public IReadOnlyList<StageResult> StageResults { get; init; } = Array.Empty<StageResult>();
    public IReadOnlyList<ElementExecutionRecord> ElementRecords { get; init; } = Array.Empty<ElementExecutionRecord>();
    public BimExecutionPlan? ExecutionPlan { get; init; }
    public string? ErrorMessage { get; init; }
    public IReadOnlyList<string> Warnings { get; init; } = Array.Empty<string>();

    public int TotalElementsAdded => StageResults?.Sum(s => s.Changed?.Added ?? 0) ?? 0;
    public int TotalElementsModified => StageResults?.Sum(s => s.Changed?.Modified ?? 0) ?? 0;
    public int TotalElementsDeleted => StageResults?.Sum(s => s.Changed?.Deleted ?? 0) ?? 0;

    public int CreatedCount => ElementRecords?.Count(r => r.Status == ElementExecutionStatus.Created) ?? 0;
    public int UpdatedCount => ElementRecords?.Count(r => r.Status == ElementExecutionStatus.Updated) ?? 0;
    public int NoOpCount => ElementRecords?.Count(r => r.Status == ElementExecutionStatus.NoOp) ?? 0;
    public int FailedCount => ElementRecords?.Count(r => r.Status == ElementExecutionStatus.Failed) ?? 0;
    public int SkippedCount => ElementRecords?.Count(r => r.Status == ElementExecutionStatus.Skipped) ?? 0;
    public int OutcomeUnknownCount => ElementRecords?.Count(r => r.Status == ElementExecutionStatus.OutcomeUnknown) ?? 0;

    public static BimPipelineResult Failure(string message, IReadOnlyList<StageResult>? stages = null, long durationMs = 0) =>
        new()
        {
            Success = false,
            ErrorMessage = message,
            StageResults = stages ?? Array.Empty<StageResult>(),
            TotalDurationMs = durationMs
        };
}
