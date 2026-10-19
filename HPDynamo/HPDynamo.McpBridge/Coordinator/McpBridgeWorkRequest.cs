namespace HPDynamo.McpBridge.Coordinator;

using System;
using System.Threading;
using System.Threading.Tasks;
using Autodesk.Revit.UI;

/// <summary>
///     Encapsulates a work request dispatched to the Revit UI thread.
/// </summary>
internal sealed record McpBridgeWorkRequest(
    string Name,
    Func<UIApplication, CancellationToken, object?> Work,
    CancellationToken CancellationToken)
{
    public TaskCompletionSource<object?> Completion { get; } =
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    public override string ToString() => $"McpBridgeWorkRequest({Name})";
}
