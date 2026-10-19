using System.Text.Json;
using HPRebar.Mcp.Contracts;
using HPRebar.Mcp.Contracts.JsonRpc;
using HPRebar.Mcp.Server.Hosts;
using HPRebar.Mcp.Server.Models;
using HPRebar.Mcp.Server.Registry.Model;

namespace HPRebar.Mcp.Server.Tests;

/// <summary>
///     The Dynamo host profile the engine tests use (the real one lives in the HPDynamo server exe) plus the
///     message texts that exe is expected to supply, shared by the profile and the bridge-message tests.
/// </summary>
internal static class DynamoTestProfile
{
    public const string DisabledText = "Code execution is disabled. Ask the user to tick 'Allow AI Dynamo execution' in the HPDynamo MCP Bridge window inside Revit.";
    public const string NotConnectedHint = "Open Revit 2026 with Dynamo and ensure HPDynamo MCP Bridge is running (pipe hpdynamo-mcp-2026).";
    public const string TimeoutHint = "The script or graph execution may still be running in Dynamo/Revit; check Dynamo status before retrying.";

    public static HostProfile Dynamo(int maxTimeout = 600, string? notConnected = null, string? timeoutHint = null) => new HostProfile
    {
        HostId = PipeNaming.DynamoHost,
        DisplayName = "Dynamo",
        ServerName = "test",
        ProductFolder = "HPDynamoTest",
        EnvPrefix = "X_",
        DefaultVersion = 2026,
        ValidVersions = new[] { 2026 },
        MethodPrefix = JsonRpcMethods.DynamoPrefix,
        ExecuteToolName = "execute_dynamo_code",
        ContextToolName = "get_dynamo_context",
        ResourceScheme = "dynamo",
        Categories = new[] { "Nodes", "Wire", "Workspace", "Geometry", "Revit", "Python", "Package", "Generic" },
        CoreToolNames = new[] { "execute_dynamo_code", "execute_designscript", "get_dynamo_context", "cancel_execution" },
        ScriptImports = HostScriptContracts.DynamoImports,
        ScriptContractSummary = "test",
        HostAssembly = typeof(DynamoTestProfile).Assembly,
        MaxTimeoutSeconds = maxTimeout,
        BridgeNotConnectedHint = notConnected,
        TimeoutSemanticsHint = timeoutHint,
    };

    public static ToolRecord Candidate(int timeoutSeconds, string transaction = "none") => new ToolRecord
    {
        Name = "get_active_graph_info",
        Title = "Graph info",
        Description = "Reads the active Dynamo workspace name, path, and node count.",
        Category = "Workspace",
        Transaction = transaction,
        TimeoutSeconds = timeoutSeconds,
        Host = "dynamo",
        Code = "return workspace.Name;",
        InputSchema = JsonSerializer.Deserialize<JsonElement>("""{"type":"object","properties":{}}"""),
        Examples = [new ToolExample { Title = "default", Args = JsonSerializer.Deserialize<JsonElement>("{}") }],
    };

    public static BridgeOptions PipeOptions(string pipe) => new BridgeOptions { PipeName = pipe, ConnectTimeoutMs = 3000, PingIntervalSeconds = 60 };

    public static string NewPipe() => "hpdynamo-mcp-test-" + Guid.NewGuid().ToString("N");
}
