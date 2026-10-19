namespace HPBim.Client.Pipeline;

using System.Collections.Generic;

/// <summary>
///     Options controlling the execution of the 8-stage BIM generation pipeline.
/// </summary>
public sealed record BimPipelineOptions
{
    /// <summary>
    ///     When true, performs execution without committing changes to the Revit Document.
    /// </summary>
    public bool DryRun { get; init; }

    /// <summary>
    ///     When true and DryRun is active, executes all stages in a single atomic transaction group
    ///     via CompositeBimGenerator so all dependent entities exist in-memory before rollback.
    /// </summary>
    public bool AtomicDryRun { get; init; } = true;

    /// <summary>
    ///     Execution timeout per stage in seconds.
    /// </summary>
    public int TimeoutSecondsPerStage { get; init; } = 120;

    /// <summary>
    ///     Optional subset of stage indexes (1 to 8) to execute. If null, all 8 stages are executed.
    /// </summary>
    public IReadOnlyList<int>? SelectedStages { get; init; }

    /// <summary>
    ///     Custom label describing this pipeline execution.
    /// </summary>
    public string Label { get; init; } = "HPBim Pipeline";

    /// <summary>
    ///     When true, executes using the unified BimExecutionPlan IR instead of legacy staged generation.
    /// </summary>
    public bool UseExecutionPlan { get; init; } = false;

    /// <summary>
    ///     Identity snapshot of the target Revit document. If null and UseExecutionPlan is true,
    ///     the pipeline automatically queries the active document via Named Pipe.
    /// </summary>
    public HPBim.Core.Execution.DocumentIdentitySnapshot? TargetDoc { get; init; }
}
