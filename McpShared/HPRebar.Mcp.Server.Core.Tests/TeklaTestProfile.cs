using System.Text.Json;
using HPRebar.Mcp.Contracts;
using HPRebar.Mcp.Contracts.JsonRpc;
using HPRebar.Mcp.Server.Hosts;
using HPRebar.Mcp.Server.Models;
using HPRebar.Mcp.Server.Registry.Model;

namespace HPRebar.Mcp.Server.Tests;

/// <summary>
///     The Tekla Structures host profile the engine tests use (the real one lives in the HPTekla server exe) plus the
///     message texts that exe is expected to supply, shared by the profile and the bridge-message tests.
/// </summary>
internal static class TeklaTestProfile
{
    public const string DisabledText = "Code execution is disabled. Ask the user to tick 'Allow AI code execution' in the HPTekla MCP Bridge extension inside Tekla Structures.";
    public const string NotConnectedHint = "Open Tekla Structures 2025 and ensure the HPTekla MCP Bridge extension is loaded and listening (pipe hptekla-mcp-2025).";
    public const string TimeoutHint = "The script may still be processing in Tekla; check the Tekla status bar before retrying.";

    public static HostProfile Tekla(int maxTimeout = 600, string? notConnected = null, string? timeoutHint = null) => new HostProfile
    {
        HostId = PipeNaming.TeklaHost,
        DisplayName = "Tekla Structures",
        ServerName = "test",
        ProductFolder = "HPTeklaTest",
        EnvPrefix = "X_",
        DefaultVersion = 2025,
        ValidVersions = new[] { 2025 },
        MethodPrefix = JsonRpcMethods.TeklaPrefix,
        ExecuteToolName = "execute_tekla_code",
        ContextToolName = "get_tekla_context",
        ResourceScheme = "tekla",
        Categories = new[] { "Model", "Geometry", "Reinforcement", "Drawing", "Export", "Generic" },
        CoreToolNames = new[] { "execute_tekla_code", "get_tekla_context", "inspect_type", "cancel_execution" },
        ScriptImports = HostScriptContracts.TeklaImports,
        ScriptContractSummary = "test",
        HostAssembly = typeof(TeklaTestProfile).Assembly,
        MaxTimeoutSeconds = maxTimeout,
        BridgeNotConnectedHint = notConnected,
        TimeoutSemanticsHint = timeoutHint,
    };

    public static ToolRecord Candidate(int timeoutSeconds, string transaction = "none") => new ToolRecord
    {
        Name = "get_model_info",
        Title = "Model info",
        Description = "Reads the Tekla model name, path, and project info.",
        Category = "Model",
        Transaction = transaction,
        TimeoutSeconds = timeoutSeconds,
        Host = "tekla",
        Code = "return model.GetInfo().ModelName;",
        InputSchema = JsonSerializer.Deserialize<JsonElement>("""{"type":"object","properties":{}}"""),
        Examples = [new ToolExample { Title = "default", Args = JsonSerializer.Deserialize<JsonElement>("{}") }],
    };

    public static BridgeOptions PipeOptions(string pipe) => new BridgeOptions { PipeName = pipe, ConnectTimeoutMs = 3000, PingIntervalSeconds = 60 };

    public static string NewPipe() => "hptekla-mcp-test-" + Guid.NewGuid().ToString("N");
}
