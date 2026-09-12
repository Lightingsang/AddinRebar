namespace HPRebar.Mcp.Contracts.JsonRpc;

/// <summary>
///     JSON-RPC error codes the bridge can return. The four standard codes signal a broken conversation
///     and surface to the AI as protocol errors; the bridge-specific ones (-32001…) describe a state the
///     AI can act on (ask the user to enable execution, wait, open a document) and are returned as
///     tool errors instead.
/// </summary>
public static class BridgeErrorCode
{
    public const int ParseError = -32700;
    public const int InvalidRequest = -32600;
    public const int MethodNotFound = -32601;
    public const int InternalError = -32000;

    public const int ExecutionDisabled = -32001;
    public const int Busy = -32002;
    public const int NoActiveDocument = -32003;

    /// <summary>True for the codes an AI can recover from without human intervention on the server side.</summary>
    public static bool IsActionable(int code) => code is ExecutionDisabled or Busy or NoActiveDocument;
}
