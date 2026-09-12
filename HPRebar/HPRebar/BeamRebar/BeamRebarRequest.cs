using System;
using System.Threading.Tasks;
using HPRebar.BeamRebar.Model;

namespace HPRebar.BeamRebar;

/// <summary>
///     One queued build, handed from the modeless window to the external event handler. The window cannot
///     touch the document on its own thread, so it parks the settings here and waits on
///     <see cref="Completion"/>, which the handler finishes once Revit has run it.
/// </summary>
public sealed class BeamRebarRequest
{
    public BeamRebarRequest(BeamRebarSpec spec, IProgress<int>? progress)
    {
        Spec = spec;
        Progress = progress;
    }

    /// <summary>What the user asked to build across the whole continuous stack.</summary>
    public BeamRebarSpec Spec { get; }

    /// <summary>Where to report element counts as they are created. Null when nobody is listening.</summary>
    public IProgress<int>? Progress { get; }

    /// <summary>
    ///     Completed by the handler on Revit's API thread: with the result on success, or with the
    ///     exception that stopped it. Continuations run asynchronously so the handler never resumes the
    ///     window inside <c>Execute</c>, while Revit still owns the thread.
    /// </summary>
    public TaskCompletionSource<BeamOrchestratorResult> Completion { get; } =
        new TaskCompletionSource<BeamOrchestratorResult>(TaskCreationOptions.RunContinuationsAsynchronously);
}
