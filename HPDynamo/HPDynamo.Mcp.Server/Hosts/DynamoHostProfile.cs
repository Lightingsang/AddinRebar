namespace HPDynamo.Mcp.Server.Hosts;

using System.Reflection;
using HPRebar.Mcp.Contracts;
using HPRebar.Mcp.Contracts.JsonRpc;
using HPRebar.Mcp.Server.Hosts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ModelContextProtocol.Server;

/// <summary>
///     Dynamo host profile configuration for HPDynamo.Mcp.Server.
///     Enforces 4 Core Tools, 8 Registry Meta Tools, and suppresses inspect_type
///     to maintain the strict 24-tool surface invariant.
/// </summary>
public static class DynamoHostProfile
{
    public const string ExecuteCodeToolName = "execute_dynamo_code";
    public const string ExecuteDesignScriptToolName = "execute_designscript";
    public const string ContextToolName = "get_dynamo_context";
    public const string CancelToolName = "cancel_execution";
    public const string SuppressedEngineToolName = "inspect_type";

    public const int DefaultVersion = 2026;
    public const int HeavyMaxTimeoutSeconds = HostScriptContracts.DynamoHeavyMaxTimeoutSeconds;

    public static readonly HostProfile Instance = new HostProfile
    {
        HostId = PipeNaming.DynamoHost,
        DisplayName = "Dynamo",
        ServerName = "HPDynamo MCP",
        ProductFolder = "HPDynamo",
        EnvPrefix = "HPDYNAMO_MCP_",
        DefaultVersion = DefaultVersion,
        ValidVersions = new[] { DefaultVersion },
        MethodPrefix = JsonRpcMethods.DynamoPrefix,
        ExecuteToolName = ExecuteCodeToolName,
        ContextToolName = ContextToolName,
        ResourceScheme = PipeNaming.DynamoHost,
        Categories = new[]
        {
            "Graph", "Workspace", "Library", "Node", "Nodes", "Wire", "Data", "Export",
            "CodeBlock", "Geometry", "Revit", "Python", "Package", "Generic"
        },
        CoreToolNames = new[]
        {
            ExecuteCodeToolName,
            ExecuteDesignScriptToolName,
            ContextToolName,
            CancelToolName
        },
        ScriptImports = HostScriptContracts.DynamoImports,
        ScriptContractSummary =
            "Globals: dynamo (DynamoModel/DynamoViewModel), workspace (WorkspaceModel), doc (Document), ct, log(string), progress(cur,total,msg), args. " +
            "Dual-host execution: headless via RevitDynamoModel or live canvas inspection/authoring. " +
            "transaction: auto when the code modifies Revit elements or Dynamo workspace; none when reading; manual if managing its own Transaction.",
        MaxTimeoutSeconds = HeavyMaxTimeoutSeconds,
        HostAssembly = typeof(DynamoHostProfile).Assembly,
        CliExecutable = "HPDynamo.Mcp.Server.exe",
        BridgeNotConnectedHint = "Open Revit 2026 with Dynamo and ensure HPDynamo MCP Bridge is running (pipe hpdynamo-mcp-2026).",
        TimeoutSemanticsHint = "The script or graph execution may still be running in Dynamo/Revit; check Dynamo status before retrying.",
    };

    /// <summary>
    /// Enforces the strict 24-tool surface by removing and suppressing inspect_type
    /// from both builder.Services and McpServerOptions.ToolCollection.
    /// </summary>
    public static void EnforceToolSurface(HostApplicationBuilder builder)
    {
        for (int i = builder.Services.Count - 1; i >= 0; i--)
        {
            var d = builder.Services[i];
            if (d.ServiceType == typeof(McpServerTool) && d.ImplementationFactory?.Target != null)
            {
                var target = d.ImplementationFactory.Target;
                var field = target.GetType().GetField("toolMethod", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                var method = field?.GetValue(target) as MethodInfo;
                if (method?.Name == "InspectAsync" || method?.DeclaringType?.Name == "InspectTypeTool")
                {
                    builder.Services.RemoveAt(i);
                }
            }
        }

        builder.Services.PostConfigure<McpServerOptions>(options =>
        {
            if (options.ToolCollection != null)
            {
                var toRemove = options.ToolCollection.FirstOrDefault(t =>
                    string.Equals(t.ProtocolTool.Name, SuppressedEngineToolName, StringComparison.OrdinalIgnoreCase));
                if (toRemove != null)
                {
                    options.ToolCollection.Remove(toRemove);
                }
            }
        });
    }
}
