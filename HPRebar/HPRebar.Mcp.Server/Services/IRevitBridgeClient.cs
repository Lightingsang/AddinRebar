using HPRebar.Mcp.Contracts.Messages;

namespace HPRebar.Mcp.Server.Services;

/// <summary>
///     What the tools need from the bridge: send one request, get one typed answer, optionally hear
///     progress in between. Tests substitute this with an in-memory implementation.
/// </summary>
public interface IRevitBridgeClient
{
    bool IsConnected { get; }

    string PipeName { get; }

    /// <summary>Last `revit.status` the bridge pushed, or null before the first one.</summary>
    StatusParams? LastStatus { get; }

    /// <exception cref="BridgeUnavailableException">No bridge on the pipe.</exception>
    /// <exception cref="BridgeTimeoutException">No answer within <paramref name="timeout"/>.</exception>
    /// <exception cref="BridgeErrorException">The bridge answered with a JSON-RPC error.</exception>
    Task<T> SendAsync<T>(
        string method,
        object? parameters,
        TimeSpan timeout,
        IProgress<ProgressParams>? progress,
        CancellationToken cancellationToken);
}
