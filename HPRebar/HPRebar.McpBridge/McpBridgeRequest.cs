using Autodesk.Revit.UI;

namespace HPRebar.McpBridge;

/// <summary>
///     One unit of work waiting for Revit's API thread. The pipe thread parks the work here and awaits
///     <see cref="Completion"/>; the external-event handler runs <see cref="Work"/> inside Execute and
///     completes it. Continuations run asynchronously so nothing resumes on Revit's thread by accident.
/// </summary>
public sealed class McpBridgeRequest
{
    public McpBridgeRequest(string name, Func<UIApplication, CancellationToken, object> work, CancellationToken cancellationToken)
    {
        Name = name;
        Work = work;
        CancellationToken = cancellationToken;
    }

    /// <summary>Short label for logs and the busy indicator, e.g. "execute: count walls".</summary>
    public string Name { get; }

    /// <summary>Runs on Revit's API thread; the token is the script's cooperative cancellation.</summary>
    public Func<UIApplication, CancellationToken, object> Work { get; }

    public CancellationToken CancellationToken { get; }

    public TaskCompletionSource<object> Completion { get; } =
        new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
}
