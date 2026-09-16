using System.Text.Json;
using HPRebar.Mcp.Contracts;
using HPRebar.Mcp.Contracts.JsonRpc;
using HPRebar.Mcp.Server.Hosts;
using HPRebar.Mcp.Server.Models;
using HPRebar.Mcp.Server.Registry.Model;

namespace HPRebar.Mcp.Server.Tests;

/// <summary>
///     The ETABS host profile the engine tests use (the real one lives in the HPEtabs server exe) plus the
///     message texts that exe is expected to supply, shared by the profile and the bridge-message tests.
/// </summary>
internal static class EtabsTestProfile
{
    public const string DisabledText = "Code execution is disabled. Ask the user to tick 'Allow AI code execution' in the HPEtabs MCP Bridge window (a separate app, not inside ETABS).";
    public const string NotConnectedHint = "Start HPEtabs.McpBridge.exe beside ETABS 22, click Attach and tick 'Allow AI code execution' (pipe hpetabs-mcp-22).";
    public const string TimeoutHint = "ETABS may still be running the call; changes made before the timeout persisted (no rollback) — check the snapshot named in the bridge window before retrying.";

    public static HostProfile Etabs(int maxTimeout = 600, string? notConnected = null, string? timeoutHint = null) => new HostProfile
    {
        HostId = PipeNaming.EtabsHost, DisplayName = "ETABS", ServerName = "test", ProductFolder = "HPEtabsTest", EnvPrefix = "X_",
        DefaultVersion = 22, ValidVersions = new[] { 22 }, MethodPrefix = JsonRpcMethods.EtabsPrefix,
        ExecuteToolName = "execute_etabs_code", ContextToolName = "get_etabs_context", ResourceScheme = "etabs",
        Categories = new[] { "Model", "Geometry", "Property", "Load", "Analysis", "Results", "Table", "Data", "Generic" },
        CoreToolNames = new[] { "execute_etabs_code", "get_etabs_context", "inspect_type", "cancel_execution" },
        ScriptImports = HostScriptContracts.EtabsImports, ScriptContractSummary = "test", HostAssembly = typeof(EtabsTestProfile).Assembly,
        MaxTimeoutSeconds = maxTimeout, BridgeNotConnectedHint = notConnected, TimeoutSemanticsHint = timeoutHint,
    };

    public static ToolRecord Candidate(int timeoutSeconds, string transaction = "none") => new ToolRecord
    {
        Name = "get_model_info", Title = "Model info", Description = "Reads the model file name, units and lock state.",
        Category = "Model", Transaction = transaction, TimeoutSeconds = timeoutSeconds, Host = "etabs",
        Code = "return sapModel.GetModelFilename();",
        InputSchema = JsonSerializer.Deserialize<JsonElement>("""{"type":"object","properties":{}}"""),
        Examples = [new ToolExample { Title = "default", Args = JsonSerializer.Deserialize<JsonElement>("{}") }],
    };

    public static BridgeOptions PipeOptions(string pipe) => new BridgeOptions { PipeName = pipe, ConnectTimeoutMs = 3000, PingIntervalSeconds = 60 };

    public static string NewPipe() => "hpetabs-mcp-test-" + Guid.NewGuid().ToString("N");
}
