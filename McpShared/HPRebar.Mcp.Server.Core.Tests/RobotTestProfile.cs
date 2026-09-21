using System.Text.Json;
using HPRebar.Mcp.Contracts;
using HPRebar.Mcp.Contracts.JsonRpc;
using HPRebar.Mcp.Server.Hosts;
using HPRebar.Mcp.Server.Models;
using HPRebar.Mcp.Server.Registry.Model;

namespace HPRebar.Mcp.Server.Tests;

/// <summary>
///     The Robot Structural Analysis host profile the engine tests use (the real one lives in the HPRobot server exe)
///     plus the message texts that exe is expected to supply, shared by the profile and the bridge-message tests.
/// </summary>
internal static class RobotTestProfile
{
    public const string DisabledText = "Code execution is disabled. Ask the user to tick 'Allow AI code execution' in the HPRobot MCP Bridge window (a separate app, not inside Robot).";
    public const string NotConnectedHint = "Start HPRobot.McpBridge.exe beside Robot Structural Analysis Professional 2026, click Attach and tick 'Allow AI code execution' (pipe hprobot-mcp-2026).";
    public const string TimeoutHint = "Robot Structural Analysis may still be running the call; changes made before the timeout persisted (no rollback) — check the snapshot named in the bridge window before retrying.";

    public static HostProfile Robot(int maxTimeout = 300, string? notConnected = null, string? timeoutHint = null) => new HostProfile
    {
        HostId = PipeNaming.RobotHost,
        DisplayName = "Robot Structural Analysis",
        ServerName = "test",
        ProductFolder = "HPRobotTest",
        EnvPrefix = "X_",
        DefaultVersion = 2026,
        ValidVersions = new[] { 2024, 2025, 2026 },
        MethodPrefix = JsonRpcMethods.RobotPrefix,
        ExecuteToolName = "execute_robot_code",
        ContextToolName = "get_robot_context",
        ResourceScheme = "robot",
        Categories = new[] { "Model", "Geometry", "Property", "Load", "Analysis", "Results", "Generic" },
        CoreToolNames = new[] { "execute_robot_code", "get_robot_context", "inspect_type", "cancel_execution" },
        ScriptImports = HostScriptContracts.RobotImports,
        ScriptContractSummary = "test",
        HostAssembly = typeof(RobotTestProfile).Assembly,
        MaxTimeoutSeconds = maxTimeout,
        BridgeNotConnectedHint = notConnected,
        TimeoutSemanticsHint = timeoutHint,
    };

    public static ToolRecord Candidate(int timeoutSeconds, string transaction = "none") => new ToolRecord
    {
        Name = "get_model_info",
        Title = "Model info",
        Description = "Reads the project filename, type, and preferences.",
        Category = "Model",
        Transaction = transaction,
        TimeoutSeconds = timeoutSeconds,
        Host = "robot",
        Code = "return robot.Project.FileName;",
        InputSchema = JsonSerializer.Deserialize<JsonElement>("""{"type":"object","properties":{}}"""),
        Examples = [new ToolExample { Title = "default", Args = JsonSerializer.Deserialize<JsonElement>("{}") }],
    };

    public static BridgeOptions PipeOptions(string pipe) => new BridgeOptions { PipeName = pipe, ConnectTimeoutMs = 3000, PingIntervalSeconds = 60 };

    public static string NewPipe() => "hprobot-mcp-test-" + Guid.NewGuid().ToString("N");
}
