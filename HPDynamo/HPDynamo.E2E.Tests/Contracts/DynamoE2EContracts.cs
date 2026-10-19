namespace HPDynamo.E2E.Tests.Contracts;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

/// <summary>
/// Authoritative wire and interface contracts for the HPDynamo MCP subsystem
/// according to ORIGINAL_REQUEST.md (lines 285-365) and PROJECT.md.
/// </summary>
public static class DynamoE2EContracts
{
    public const string HostKey = "dynamo";
    public const int DefaultHostVersion = 2026;
    public const string ExpectedPipeName = "hpdynamo-mcp-2026";
    public const string MethodPrefix = "dynamo.";

    public const int OptInDisabledErrorCode = -32001;
    public const string OptInDisabledMessageFragment = "Allow AI Dynamo execution";

    public static readonly string[] RequiredImports =
    {
        "System",
        "System.Linq",
        "System.Collections.Generic",
        "Autodesk.Revit.DB",
        "Dynamo.Graph.Nodes",
        "Dynamo.Graph.Workspaces",
        "Dynamo.Models",
        "Autodesk.DesignScript.Geometry",
        "RevitServices.Persistence",
        "RevitServices.Transactions",
        "HPRebar.McpBridge.Core.Scripting"
    };

    public static readonly string[] RequiredGlobals =
    {
        "dynamo", "workspace", "doc", "ct", "log", "progress", "args"
    };

    public static readonly IReadOnlyList<string> CoreTools = new ReadOnlyCollection<string>(new[]
    {
        "execute_dynamo_code",
        "execute_designscript",
        "get_dynamo_context",
        "cancel_execution"
    });

    public static readonly IReadOnlyList<string> RegistryMetaTools = new ReadOnlyCollection<string>(new[]
    {
        "search_tools",
        "get_tool",
        "run_tool",
        "get_run",
        "propose_tool",
        "test_tool",
        "publish_tool",
        "manage_tool"
    });

    public static readonly IReadOnlyList<string> EmbeddedSeeds = new ReadOnlyCollection<string>(new[]
    {
        "run_dyn_file",
        "get_active_graph_info",
        "search_nodes_library",
        "inspect_node_type",
        "add_node_to_workspace",
        "connect_node_ports",
        "disconnect_node_ports",
        "set_node_properties",
        "get_node_output_values",
        "export_graph_to_image",
        "validate_dyn_file",
        "create_code_block_node",
        "batch_edit_graph"
    });

    public static readonly string SuppressedEngineTool = "inspect_type";

    public static readonly IReadOnlyList<string> DeniedNamespaces = new ReadOnlyCollection<string>(new[]
    {
        "System.IO",
        "System.Net",
        "System.Diagnostics.Process",
        "System.Reflection"
    });

    public static string ResolvePipeName(string host, int version)
    {
        if (string.IsNullOrWhiteSpace(host)) throw new ArgumentException("host is required", nameof(host));
        string key = host.Trim().ToLowerInvariant();
        return key switch
        {
            "dynamo" => "hpdynamo-mcp-" + version,
            _ => "hp" + key + "-mcp-" + version
        };
    }
}
