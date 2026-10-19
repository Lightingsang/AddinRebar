namespace HPBim.Core.Execution;

using System;

/// <summary>
/// Operational parameters controlling plan execution in BimPipelineEngine and Revit bridge.
/// </summary>
public sealed record ExecutionOptions
{
    /// <summary>True for Preview (rollback transaction); false for Apply (commit transaction).</summary>
    public bool DryRun { get; init; }

    /// <summary>When true, any unresolved symbol or level immediately aborts (Golden Rule / Fail-Fast).</summary>
    public bool StrictMode { get; init; } = true;

    /// <summary>Timeout ceiling in seconds for Revit script execution.</summary>
    public int TimeoutSeconds { get; init; } = 120;

    /// <summary>Whether to create associated FloorPlan views for Level items.</summary>
    public bool CreatePlanViews { get; init; } = true;

    /// <summary>Instructs Revit bridge to capture all FailureSeverity.Warning messages into the response.</summary>
    public bool CaptureWarnings { get; init; } = true;

    /// <summary>Label attached to the Revit TransactionGroup.</summary>
    public string TransactionLabel { get; init; } = "HPBim Execution Plan";

    /// <summary>Unique operation tracking ID recorded in IntentJournal and Extensible Storage receipt.</summary>
    public Guid OperationId { get; init; } = Guid.NewGuid();
}
