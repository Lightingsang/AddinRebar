namespace HPRebar.Mcp.Contracts.JsonRpc;

/// <summary>Method names on the server ↔ bridge pipe. Requests flow server → bridge, notifications bridge → server.</summary>
public static class JsonRpcMethods
{
    public const string Ping = "revit.ping";
    public const string Context = "revit.context";
    public const string Inspect = "revit.inspect";
    public const string Execute = "revit.execute";
    public const string Cancel = "revit.cancel";
    public const string Analyze = "revit.analyze";

    public const string ProgressNotification = "revit.progress";
    public const string LogNotification = "revit.log";
    public const string StatusNotification = "revit.status";
}
