namespace HPBim.Client.Connection;

using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using HPRebar.Mcp.Contracts.Messages;

/// <summary>
///     Contract for communicating with the Revit MCP Named Pipe bridge.
/// </summary>
public interface IRevitPipeClient : IAsyncDisposable, IDisposable
{
    /// <summary>
    ///     Gets whether the client is currently connected to the pipe.
    /// </summary>
    bool IsConnected { get; }

    /// <summary>
    ///     Gets the active named pipe name.
    /// </summary>
    string PipeName { get; }

    /// <summary>
    ///     Connects to the named pipe server.
    /// </summary>
    Task ConnectAsync(CancellationToken cancellationToken = default);

    /// <summary>
    ///     Disconnects from the named pipe.
    /// </summary>
    Task DisconnectAsync();

    /// <summary>
    ///     Executes a C# script inside Revit via "revit.execute".
    /// </summary>
    Task<ExecuteResult> ExecuteAsync(ExecuteRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Checks bridge liveness via "revit.ping".
    /// </summary>
    Task<bool> PingAsync(CancellationToken cancellationToken = default);

    /// <summary>
    ///     Retrieves document and environment context via "revit.context".
    /// </summary>
    Task<JsonElement?> GetContextAsync(CancellationToken cancellationToken = default);

    /// <summary>
    ///     Cancels running script execution in the bridge via "revit.cancel".
    /// </summary>
    Task<bool> CancelAsync(CancellationToken cancellationToken = default);
}
