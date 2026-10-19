namespace HPBim.Client.Pipeline;

using System;
using System.Threading;
using System.Threading.Tasks;
using HPBim.Core.Execution;
using HPBim.Core.Models;

/// <summary>
///     Contract for orchestrating the execution of the 8-stage BIM model creation pipeline.
/// </summary>
public interface IBimPipelineEngine
{
    /// <summary>
    ///     Event raised whenever an individual stage finishes execution.
    /// </summary>
    event Action<StageResult>? StageCompleted;

    /// <summary>
    ///     Executes the BIM pipeline against the connected Revit pipe.
    /// </summary>
    Task<BimPipelineResult> ExecuteAsync(
        BimModel model,
        BimPipelineOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Executes the unified BimExecutionPlan IR uniformly for Preview (dryRun rollback) or Apply (commit).
    /// </summary>
    Task<BimPipelineResult> ExecutePlanAsync(
        BimExecutionPlan plan,
        CancellationToken cancellationToken = default);
}
