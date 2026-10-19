namespace HPDynamo.Mcp.Server.Tests.Surface;

using System;
using System.Linq;
using HPDynamo.Mcp.Server.Hosts;
using HPRebar.Mcp.Server.Bootstrap;
using HPRebar.Mcp.Server.Registry;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Server;
using Xunit;

public sealed class ToolSurfaceTests
{
    private static readonly string[] ExpectedCoreTools =
    [
        "execute_dynamo_code",
        "execute_designscript",
        "get_dynamo_context",
        "cancel_execution"
    ];

    private static readonly string[] ExpectedRegistryTools =
    [
        "search_tools",
        "get_tool",
        "run_tool",
        "get_run",
        "propose_tool",
        "test_tool",
        "publish_tool",
        "manage_tool"
    ];

    private static readonly string[] ExpectedSeedTools =
    [
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
    ];

    [Fact]
    public void ToolSurface_ContainsExpectedToolsAcrossCoreRegistryAndSeeds()
    {
        var builder = McpServerHost.CreateBuilder([], DynamoHostProfile.Instance);
        DynamoHostProfile.EnforceToolSurface(builder);
        using var host = builder.Build();

        var staticTools = host.Services.GetServices<McpServerTool>()
            .Select(t => t.ProtocolTool.Name)
            .Distinct(StringComparer.Ordinal)
            .ToList();

        var seeds = SeedInstaller.LoadSeeds(typeof(DynamoHostProfile).Assembly)
            .Select(s => s.Name)
            .Distinct(StringComparer.Ordinal)
            .ToList();

        var combined = staticTools.Union(seeds, StringComparer.Ordinal).ToList();

        Assert.Equal(12, staticTools.Count);
        Assert.Equal(13, seeds.Count);
        Assert.Equal(25, combined.Count);
    }

    [Fact]
    public void ToolSurface_InspectTypeTool_IsSuppressed()
    {
        var builder = McpServerHost.CreateBuilder([], DynamoHostProfile.Instance);
        DynamoHostProfile.EnforceToolSurface(builder);
        using var host = builder.Build();

        var toolNames = host.Services.GetServices<McpServerTool>()
            .Select(t => t.ProtocolTool.Name)
            .ToList();

        Assert.DoesNotContain("inspect_type", toolNames, StringComparer.OrdinalIgnoreCase);

        var options = host.Services.GetService<IOptions<McpServerOptions>>()?.Value;
        if (options?.ToolCollection != null)
        {
            Assert.DoesNotContain(options.ToolCollection,
                t => string.Equals(t.ProtocolTool.Name, "inspect_type", StringComparison.OrdinalIgnoreCase));
        }
    }

    [Fact]
    public void CoreTools_ContainsExpectedFourToolsWithCorrectAnnotations()
    {
        var builder = McpServerHost.CreateBuilder([], DynamoHostProfile.Instance);
        DynamoHostProfile.EnforceToolSurface(builder);
        using var host = builder.Build();

        var tools = host.Services.GetServices<McpServerTool>().ToList();

        foreach (var expected in ExpectedCoreTools)
        {
            var match = tools.FirstOrDefault(t => t.ProtocolTool.Name == expected);
            Assert.NotNull(match);
        }

        var executeCode = tools.Single(t => t.ProtocolTool.Name == "execute_dynamo_code").ProtocolTool;
        Assert.True(executeCode.Annotations?.DestructiveHint);
        Assert.False(executeCode.Annotations?.ReadOnlyHint ?? false);

        var executeDs = tools.Single(t => t.ProtocolTool.Name == "execute_designscript").ProtocolTool;
        Assert.True(executeDs.Annotations?.DestructiveHint);
        Assert.False(executeDs.Annotations?.ReadOnlyHint ?? false);

        var context = tools.Single(t => t.ProtocolTool.Name == "get_dynamo_context").ProtocolTool;
        Assert.True(context.Annotations?.ReadOnlyHint);
        Assert.False(context.Annotations?.DestructiveHint ?? false);
    }

    [Fact]
    public void RegistryMetaTools_ContainsAllEightRequiredTools()
    {
        var builder = McpServerHost.CreateBuilder([], DynamoHostProfile.Instance);
        DynamoHostProfile.EnforceToolSurface(builder);
        using var host = builder.Build();

        var registered = host.Services.GetServices<McpServerTool>()
            .Select(t => t.ProtocolTool.Name)
            .ToHashSet(StringComparer.Ordinal);

        foreach (var meta in ExpectedRegistryTools)
        {
            Assert.Contains(meta, registered);
        }
    }

    [Fact]
    public void Seeds_ContainsAllPublishedSeedTools()
    {
        var seeds = SeedInstaller.LoadSeeds(typeof(DynamoHostProfile).Assembly)
            .Select(s => s.Name)
            .ToHashSet(StringComparer.Ordinal);

        Assert.Equal(13, seeds.Count);
        foreach (var seed in ExpectedSeedTools)
        {
            Assert.Contains(seed, seeds);
        }
    }
}
