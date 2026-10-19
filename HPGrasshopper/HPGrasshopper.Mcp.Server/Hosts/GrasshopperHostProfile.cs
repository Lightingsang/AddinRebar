namespace HPGrasshopper.Mcp.Server.Hosts;

using System.Reflection;
using HPRebar.Mcp.Contracts;
using HPRebar.Mcp.Contracts.JsonRpc;
using HPRebar.Mcp.Server.Hosts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ModelContextProtocol.Server;

/// <summary>
///     Grasshopper host profile configuration for HPGrasshopper.Mcp.Server.
///     Enforces Core Tools, Dynamic Registry Meta Tools, and suppresses inspect_type.
/// </summary>
public static class GrasshopperHostProfile
{
    public const string ExecuteCodeToolName = "execute_grasshopper_code";
    public const string ContextToolName = "get_grasshopper_context";
    public const string ReadCanvasToolName = "read_grasshopper_canvas";
    public const string RunPlayerToolName = "run_grasshopper_player";
    public const string BatchEditToolName = "batch_edit_grasshopper_canvas";
    public const string ExportImageToolName = "export_grasshopper_canvas_image";
    public const string CancelToolName = "cancel_execution";
    public const string SuppressedEngineToolName = "inspect_type";

    public const int DefaultVersion = 2026;
    public const int HeavyMaxTimeoutSeconds = HostScriptContracts.GrasshopperHeavyMaxTimeoutSeconds;

    public static readonly HostProfile Instance = new HostProfile
    {
        HostId = PipeNaming.GrasshopperHost,
        DisplayName = "Grasshopper",
        ServerName = "HPGrasshopper MCP",
        ProductFolder = "HPGrasshopper",
        EnvPrefix = "HPGRASSHOPPER_MCP_",
        DefaultVersion = DefaultVersion,
        ValidVersions = new[] { DefaultVersion },
        MethodPrefix = JsonRpcMethods.GrasshopperPrefix,
        ExecuteToolName = ExecuteCodeToolName,
        ContextToolName = ContextToolName,
        ResourceScheme = "grasshopper",
        Categories = new[]
        {
            "Canvas", "Component", "Parameter", "Geometry", "Revit", "Python", "Player", "Generic"
        },
        CoreToolNames = new[]
        {
            ExecuteCodeToolName,
            ContextToolName,
            ReadCanvasToolName,
            RunPlayerToolName,
            BatchEditToolName,
            ExportImageToolName,
            CancelToolName
        },
        ScriptImports = HostScriptContracts.GrasshopperImports,
        ScriptContractSummary =
            "Globals: doc (GH_Document), canvas (GH_Canvas), rhinoDoc (RhinoDoc), revitDoc (Document), ct, log(string), progress(cur,total,msg), args. " +
            "Dual-host execution: Grasshopper canvas in Rhino.Inside.Revit 2026. " +
            "transaction: auto when modifying Revit elements or Grasshopper canvas; none when reading; manual if managing its own Transaction.",
        HostAssembly = typeof(GrasshopperHostProfile).Assembly,
        CliExecutable = "HPGrasshopper.Mcp.Server.exe",
        MaxTimeoutSeconds = HeavyMaxTimeoutSeconds,
        BridgeNotConnectedHint = "Open Revit 2026 with Rhino.Inside.Revit and Grasshopper, ensure HPGrasshopper MCP Bridge is running (pipe hpgrasshopper-mcp-2026).",
        TimeoutSemanticsHint = "The script or definition solver may still be running in Grasshopper/Revit; check Grasshopper status before retrying.",
    };

    /// <summary>
    ///     Suppresses inspect_type from both builder.Services and McpServerOptions.ToolCollection.
    /// </summary>
    public static void EnforceToolSurface(HostApplicationBuilder builder)
    {
        RemoveInspectTypeFromServices(builder.Services);
        ConfigureToolCollectionFilter(builder.Services);
    }

    private static void RemoveInspectTypeFromServices(IServiceCollection services)
    {
        for (int i = services.Count - 1; i >= 0; i--)
        {
            var descriptor = services[i];
            if (descriptor.ServiceType == typeof(McpServerTool) && descriptor.ImplementationFactory?.Target != null)
            {
                var target = descriptor.ImplementationFactory.Target;
                var field = target.GetType().GetField("toolMethod", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                var method = field?.GetValue(target) as MethodInfo;
                if (method?.Name == "InspectAsync" || method?.DeclaringType?.Name == "InspectTypeTool")
                {
                    services.RemoveAt(i);
                }
            }
        }
    }

    private static void ConfigureToolCollectionFilter(IServiceCollection services)
    {
        services.PostConfigure<McpServerOptions>(options =>
        {
            if (options.ToolCollection != null)
            {
                var toRemove = options.ToolCollection.FirstOrDefault(tool =>
                    string.Equals(tool.ProtocolTool.Name, SuppressedEngineToolName, StringComparison.OrdinalIgnoreCase));
                if (toRemove != null)
                {
                    options.ToolCollection.Remove(toRemove);
                }
            }
        });
    }
}
