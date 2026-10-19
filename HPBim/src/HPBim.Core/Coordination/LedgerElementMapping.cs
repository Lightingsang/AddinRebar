namespace HPBim.Core.Coordination;

using System;
using System.Collections.Generic;
using HPBim.Core.Execution;

/// <summary>
/// Immutable mapping entry in the BimCoordinationLedger connecting
/// a source design entity (CompositeSourceKey) with its host BIM element (TargetUniqueId).
/// Tracks provenance, revision history, and execution status across runs.
/// </summary>
public sealed record LedgerElementMapping
{
    public CompositeSourceKey SourceKey { get; init; } = CompositeSourceKey.Empty;
    public string TargetUniqueId { get; init; } = string.Empty;
    public long? TargetElementId { get; init; }
    public CoordinationCategory Category { get; init; }
    public string Mark { get; init; } = string.Empty;
    public string PlanHash { get; init; } = string.Empty;
    public IReadOnlyList<string> ProvenanceHandles { get; init; } = Array.Empty<string>();
    public DateTime CreatedAtUtc { get; init; } = DateTime.UtcNow;
    public DateTime LastModifiedAtUtc { get; init; } = DateTime.UtcNow;
    public ElementExecutionStatus LastModifiedStatus { get; init; } = ElementExecutionStatus.Created;

    public static LedgerElementMapping Create(
        CompositeSourceKey sourceKey,
        string targetUniqueId,
        long? targetElementId,
        CoordinationCategory category,
        string mark,
        string planHash,
        IReadOnlyList<string>? provenanceHandles = null) => new()
    {
        SourceKey = sourceKey ?? throw new ArgumentNullException(nameof(sourceKey)),
        TargetUniqueId = targetUniqueId ?? throw new ArgumentNullException(nameof(targetUniqueId)),
        TargetElementId = targetElementId,
        Category = category,
        Mark = mark ?? string.Empty,
        PlanHash = planHash ?? string.Empty,
        ProvenanceHandles = provenanceHandles ?? Array.Empty<string>(),
        CreatedAtUtc = DateTime.UtcNow,
        LastModifiedAtUtc = DateTime.UtcNow,
        LastModifiedStatus = ElementExecutionStatus.Created
    };

    public LedgerElementMapping WithUpdate(
        string targetUniqueId,
        long? targetElementId,
        string planHash,
        ElementExecutionStatus status) => this with
    {
        TargetUniqueId = targetUniqueId,
        TargetElementId = targetElementId ?? TargetElementId,
        PlanHash = planHash,
        LastModifiedStatus = status,
        LastModifiedAtUtc = DateTime.UtcNow
    };
}
