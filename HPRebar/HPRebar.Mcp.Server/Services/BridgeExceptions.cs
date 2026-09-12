namespace HPRebar.Mcp.Server.Services;

/// <summary>No bridge on the pipe: Revit is closed, the add-in is not loaded, or its listener is off.</summary>
public sealed class BridgeUnavailableException(string message, Exception? inner = null) : Exception(message, inner);

/// <summary>The bridge accepted the request but did not answer within the agreed window.</summary>
public sealed class BridgeTimeoutException(string message) : Exception(message);

/// <summary>The bridge answered with a JSON-RPC error; <see cref="Code"/> decides whether the AI can act on it.</summary>
public sealed class BridgeErrorException(int code, string message) : Exception(message)
{
    public int Code { get; } = code;
}
