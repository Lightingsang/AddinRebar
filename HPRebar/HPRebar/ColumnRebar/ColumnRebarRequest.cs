using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HPRebar.ColumnRebar.Model;

namespace HPRebar.ColumnRebar;

/// <summary>
///     One queued build, handed from the modeless window to the external event handler. The window cannot
///     touch the document on its own thread, so it parks the settings here and waits on
///     <see cref="Completion"/>, which the handler finishes once Revit has run it.
/// </summary>
public sealed class ColumnRebarRequest
{
    public ColumnRebarRequest(IReadOnlyList<ColumnRebarSpec> specs, IProgress<int>? progress)
    {
        Specs = specs;
        Progress = progress;
    }

    /// <summary>What the user asked to build, one entry per column segment.</summary>
    public IReadOnlyList<ColumnRebarSpec> Specs { get; }

    /// <summary>Where to report element counts as they are created. Null when nobody is listening.</summary>
    public IProgress<int>? Progress { get; }

    /// <summary>
    ///     Completed by the handler on Revit's API thread: with the result on success, or with the
    ///     exception that stopped it. Continuations run asynchronously so the handler never resumes the
    ///     window inside <c>Execute</c>, while Revit still owns the thread.
    /// </summary>
    public TaskCompletionSource<OrchestratorResult> Completion { get; } =
        new TaskCompletionSource<OrchestratorResult>(TaskCreationOptions.RunContinuationsAsynchronously);
}
