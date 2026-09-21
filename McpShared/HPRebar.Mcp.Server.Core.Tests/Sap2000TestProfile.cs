using System.Text.Json;
using HPRebar.Mcp.Contracts;
using HPRebar.Mcp.Contracts.JsonRpc;
using HPRebar.Mcp.Server.Hosts;
using HPRebar.Mcp.Server.Models;
using HPRebar.Mcp.Server.Registry.Model;

namespace HPRebar.Mcp.Server.Tests;

/// <summary>
///     The SAP2000 host profile the engine tests use (the real one lives in the HPSap2000 server exe) plus the
///     message texts that exe is expected to supply, shared by the profile and the bridge-message tests.
/// </summary>
internal static class Sap2000TestProfile
{
    public const string DisabledText = "Code execution is disabled. Ask the user to tick 'Allow AI code execution' in the HPSap2000 MCP Bridge window (a separate app, not inside SAP2000).";
    public const string NotConnectedHint = "Start HPSap2000.McpBridge.exe beside SAP2000 27, click Attach and tick 'Allow AI code execution' (pipe hpsap2000-mcp-27).";
    public const string TimeoutHint = "SAP2000 may still be running the call; changes made before the timeout persisted (no rollback) — check the snapshot named in the bridge window before retrying.";

    public static HostProfile Sap2000(int maxTimeout = 600, string? notConnected = null, string? timeoutHint = null) => new HostProfile
    {
        HostId = PipeNaming.Sap2000Host, DisplayName = "SAP2000", ServerName = "test", ProductFolder = "HPSap2000Test", EnvPrefix = "X_",
        DefaultVersion = 27, ValidVersions = new[] { 24, 25, 26, 27 }, MethodPrefix = JsonRpcMethods.Sap2000Prefix,
        ExecuteToolName = "execute_sap2000_code", ContextToolName = "get_sap2000_context", ResourceScheme = "sap2000",
        Categories = new[] { "Model", "Geometry", "Property", "Load", "Analysis", "Results", "Table", "Data", "Generic" },
        CoreToolNames = new[] { "execute_sap2000_code", "get_sap2000_context", "inspect_type", "cancel_execution" },
        ScriptImports = HostScriptContracts.Sap2000Imports, ScriptContractSummary = "test", HostAssembly = typeof(Sap2000TestProfile).Assembly,
        MaxTimeoutSeconds = maxTimeout, BridgeNotConnectedHint = notConnected, TimeoutSemanticsHint = timeoutHint,
    };

    public static ToolRecord Candidate(int timeoutSeconds, string transaction = "none") => new ToolRecord
    {
        Name = "get_model_info", Title = "Model info", Description = "Reads the model file name, units and lock state.",
        Category = "Model", Transaction = transaction, TimeoutSeconds = timeoutSeconds, Host = "sap2000",
        Code = "return sapModel.GetModelFilename();",
        InputSchema = JsonSerializer.Deserialize<JsonElement>("""{"type":"object","properties":{}}"""),
        Examples = [new ToolExample { Title = "default", Args = JsonSerializer.Deserialize<JsonElement>("{}") }],
    };

    public static BridgeOptions PipeOptions(string pipe) => new BridgeOptions { PipeName = pipe, ConnectTimeoutMs = 3000, PingIntervalSeconds = 60 };

    public static string NewPipe() => "hpsap2000-mcp-test-" + Guid.NewGuid().ToString("N");
}
