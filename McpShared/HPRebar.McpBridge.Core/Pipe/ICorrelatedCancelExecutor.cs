namespace HPRebar.McpBridge.Core.Pipe;

using System;
using System.Threading;
using System.Threading.Tasks;
using HPRebar.Mcp.Contracts.Messages;
using HPRebar.McpBridge.Core.Model;

/// <summary>
///     Optional extension of <see cref="IBridgeExecutor"/> for bridges that track and correlate
///     cancellation signals to specific incoming JSON-RPC request IDs.
/// </summary>
public interface ICorrelatedCancelExecutor : IBridgeExecutor
{
    /// <summary>
    ///     Executes a script with its correlated JSON-RPC request identifier.
    /// </summary>
    Task<ExecuteResult> ExecuteAsync(
        long requestId,
        ExecuteRequest request,
        IProgress<ScriptProgress>? progress,
        CancellationToken cancellationToken);

    /// <summary>
    ///     Signals cancellation to the running script.
    ///     If <paramref name="targetRequestId"/> is specified, cancellation is only triggered if the
    ///     currently running script matches that ID. If <paramref name="targetRequestId"/> is null or 0,
    ///     unconditional cancellation is triggered.
    /// </summary>
    CancelResult Cancel(long? targetRequestId);

    /// <summary>
    ///     Signals cancellation to the running script scoped to a specific session and request ID.
    /// </summary>
    CancelResult Cancel(long? targetRequestId, string? sessionId);
}
