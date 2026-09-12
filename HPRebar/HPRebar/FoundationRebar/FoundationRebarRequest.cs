using System.Threading.Tasks;
using HPRebar.Core.FoundationRebar.Models;
using HPRebar.FoundationRebar.Model;

namespace HPRebar.FoundationRebar;

/// <summary>
///     One queued build, handed from the modeless window to the external event handler. The window cannot
///     open a transaction on its own thread, so it parks the session here and waits on
///     <see cref="Completion"/>, which the handler finishes once Revit has run it.
/// </summary>
public sealed class FoundationRebarRequest
{
    public FoundationRebarRequest(FoundationSession session) => Session = session;

    /// <summary>The slab, its geometry snapshot and the spec the user settled on.</summary>
    public FoundationSession Session { get; }

    /// <summary>
    ///     Completed by the handler on Revit's API thread: with the mesh that was built on success, or with
    ///     the exception that stopped it. Continuations run asynchronously so the handler never resumes the
    ///     window inside <c>Execute</c>, while Revit still owns the thread.
    /// </summary>
    public TaskCompletionSource<FoundationMeshResult> Completion { get; } =
        new TaskCompletionSource<FoundationMeshResult>(TaskCreationOptions.RunContinuationsAsynchronously);
}
