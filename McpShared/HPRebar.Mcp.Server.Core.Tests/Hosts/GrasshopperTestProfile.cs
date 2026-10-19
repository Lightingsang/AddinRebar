using System.Text.Json;
using HPRebar.Mcp.Contracts;
using HPRebar.Mcp.Contracts.JsonRpc;
using HPRebar.Mcp.Server.Hosts;
using HPRebar.Mcp.Server.Models;
using HPRebar.Mcp.Server.Registry.Model;

namespace HPRebar.Mcp.Server.Tests;

/// <summary>
///     The Grasshopper host profile the engine tests use (the real one lives in the HPGrasshopper server exe) plus the
///     message texts that exe is expected to supply, shared by the profile and the bridge-message tests.
/// </summary>
internal static class GrasshopperTestProfile
{
    public const string DisabledText = "Code execution is disabled. Ask the user to tick 'Allow AI Grasshopper execution' in the HPGrasshopper MCP Bridge window inside Revit.";
    public const string NotConnectedHint = "Open Revit 2026 with Rhino.Inside.Revit and Grasshopper, ensure HPGrasshopper MCP Bridge is running (pipe hpgrasshopper-mcp-2026).";
    public const string TimeoutHint = "The script or definition solver may still be running in Grasshopper/Revit; check Grasshopper status before retrying.";

    public static HostProfile Grasshopper(int maxTimeout = 600, string? notConnected = null, string? timeoutHint = null) => new HostProfile
    {
        HostId = PipeNaming.GrasshopperHost,
        DisplayName = "Grasshopper",
        ServerName = "test",
        ProductFolder = "HPGrasshopperTest",
        EnvPrefix = "X_",
        DefaultVersion = 2026,
        ValidVersions = new[] { 2026 },
        MethodPrefix = JsonRpcMethods.GrasshopperPrefix,
        ExecuteToolName = "execute_grasshopper_code",
        ContextToolName = "get_grasshopper_context",
        ResourceScheme = "grasshopper",
        Categories = new[] { "Canvas", "Component", "Parameter", "Geometry", "Revit", "Python", "Player", "Generic" },
        CoreToolNames = new[]
        {
            "execute_grasshopper_code",
            "get_grasshopper_context",
            "read_grasshopper_canvas",
            "run_grasshopper_player",
            "batch_edit_grasshopper_canvas",
            "export_grasshopper_canvas_image",
            "cancel_execution",
        },
        ScriptImports = HostScriptContracts.GrasshopperImports,
        ScriptContractSummary = "test",
        HostAssembly = typeof(GrasshopperTestProfile).Assembly,
        MaxTimeoutSeconds = maxTimeout,
        BridgeNotConnectedHint = notConnected,
        TimeoutSemanticsHint = timeoutHint,
    };

    public static ToolRecord Candidate(int timeoutSeconds, string transaction = "none") => new ToolRecord
    {
        Name = "get_active_canvas_info",
        Title = "Canvas info",
        Description = "Reads the active Grasshopper definition name, path, and component count.",
        Category = "Canvas",
        Transaction = transaction,
        TimeoutSeconds = timeoutSeconds,
        Host = "grasshopper",
        Code = "return doc.FilePath;",
        InputSchema = JsonSerializer.Deserialize<JsonElement>("""{"type":"object","properties":{}}"""),
        Examples = [new ToolExample { Title = "default", Args = JsonSerializer.Deserialize<JsonElement>("{}") }],
    };

    public static BridgeOptions PipeOptions(string pipe) => new BridgeOptions { PipeName = pipe, ConnectTimeoutMs = 3000, PingIntervalSeconds = 60 };

    public static string NewPipe() => "hpgrasshopper-mcp-test-" + Guid.NewGuid().ToString("N");
}
