namespace HPBim.Client.Recovery;

using System;
using System.Collections.Generic;

/// <summary>
/// Result of an auto-reconciliation attempt resolving an orphaned or unknown operation intent.
/// </summary>
public sealed record ReconcileResult
{
    public Guid OperationId { get; init; }
    public string PlanHash { get; init; } = string.Empty;
    public ReconcileStatus Status { get; init; }
    public string Message { get; init; } = string.Empty;
    public int MappedElementCount { get; init; }
    public IReadOnlyDictionary<string, string> RecoveredMappings { get; init; } = new Dictionary<string, string>();
    public DateTime ReconciledAtUtc { get; init; } = DateTime.UtcNow;

    public static ReconcileResult Committed(Guid operationId, string planHash, IReadOnlyDictionary<string, string> mappings) =>
        new()
        {
            OperationId = operationId,
            PlanHash = planHash,
            Status = ReconcileStatus.Committed,
            Message = $"Transaction committed in Revit. Recovered {mappings.Count} element mappings.",
            MappedElementCount = mappings.Count,
            RecoveredMappings = mappings
        };

    public static ReconcileResult RolledBack(Guid operationId, string planHash) =>
        new()
        {
            OperationId = operationId,
            PlanHash = planHash,
            Status = ReconcileStatus.RolledBack,
            Message = "Transaction receipt absent in Revit. Confirmed rolled back or uncommitted.",
            MappedElementCount = 0
        };

    public static ReconcileResult StillUnknown(Guid operationId, string planHash, string reason) =>
        new()
        {
            OperationId = operationId,
            PlanHash = planHash,
            Status = ReconcileStatus.StillUnknown,
            Message = $"Reconciliation inconclusive: {reason}",
            MappedElementCount = 0
        };

    public static ReconcileResult NoOp() =>
        new()
        {
            Status = ReconcileStatus.NoOp,
            Message = "No unresolved intents found."
        };
}
