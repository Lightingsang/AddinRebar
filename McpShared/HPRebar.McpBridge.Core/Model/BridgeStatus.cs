namespace HPRebar.McpBridge.Core.Model;

/// <summary>Where the bridge is in its life: what the status window shows and what `revit.status` reports.</summary>
public enum BridgeStatus
{
    /// <summary>Listener off; the server will get "bridge not connected".</summary>
    Stopped,

    /// <summary>Pipe created, waiting for the MCP server to connect.</summary>
    Listening,

    /// <summary>A server is connected and idle.</summary>
    Connected,

    /// <summary>A script is running on the Revit thread.</summary>
    Busy,

    /// <summary>Listener could not start or died; see the status message.</summary>
    Error,
}
