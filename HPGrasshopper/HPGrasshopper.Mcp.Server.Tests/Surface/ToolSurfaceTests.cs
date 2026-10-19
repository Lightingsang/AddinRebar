namespace HPGrasshopper.Mcp.Server.Tests.Surface;

using System;
using System.Linq;
using System.Text.Json;
using HPGrasshopper.Mcp.Server.Hosts;
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
        "read_grasshopper_canvas",
        "run_grasshopper_player",
        "batch_edit_grasshopper_canvas",
        "execute_grasshopper_code",
        "export_grasshopper_canvas_image"
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
        "create_levels_grids",
        "generate_columns_beams",
        "create_floors_slabs",
        "generate_rebar_curves",
        "generate_stirrup_distribution",
        "facade_panelization",
        "curtain_grid_layout",
        "create_wall_openings",
        "directshape_from_geometry",
        "query_revit_geometry",
        "evaluate_solar_radiation",
        "export_model_to_rhino"
    ];

    [Fact]
    public void ToolSurface_ContainsAllRequiredFiveCoreTools()
    {
        var builder = McpServerHost.CreateBuilder([], GrasshopperHostProfile.Instance);
        GrasshopperHostProfile.EnforceToolSurface(builder);
        using var host = builder.Build();

        var toolNames = host.Services.GetServices<McpServerTool>()
            .Select(t => t.ProtocolTool.Name)
            .ToHashSet(StringComparer.Ordinal);

        foreach (var expected in ExpectedCoreTools)
        {
            Assert.Contains(expected, toolNames);
        }
    }

    [Fact]
    public void ToolSurface_ContainsAllEightRegistryMetaTools()
    {
        var builder = McpServerHost.CreateBuilder([], GrasshopperHostProfile.Instance);
        GrasshopperHostProfile.EnforceToolSurface(builder);
        using var host = builder.Build();

        var toolNames = host.Services.GetServices<McpServerTool>()
            .Select(t => t.ProtocolTool.Name)
            .ToHashSet(StringComparer.Ordinal);

        foreach (var expected in ExpectedRegistryTools)
        {
            Assert.Contains(expected, toolNames);
        }
    }

    [Fact]
    public void ToolSurface_InspectTypeTool_IsSuppressed()
    {
        var builder = McpServerHost.CreateBuilder([], GrasshopperHostProfile.Instance);
        GrasshopperHostProfile.EnforceToolSurface(builder);
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
    public void ToolSurface_EmbeddedSeeds_ContainsAllTwelveSeeds()
    {
        var seeds = SeedInstaller.LoadSeeds(typeof(GrasshopperHostProfile).Assembly)
            .Select(s => s.Name)
            .ToHashSet(StringComparer.Ordinal);

        Assert.Equal(12, seeds.Count);
        foreach (var expected in ExpectedSeedTools)
        {
            Assert.Contains(expected, seeds);
        }
    }

    [Fact]
    public void SchemaValidation_AllToolsInCatalog_HaveValidSchemas()
    {
        var builder = McpServerHost.CreateBuilder([], GrasshopperHostProfile.Instance);
        GrasshopperHostProfile.EnforceToolSurface(builder);
        using var host = builder.Build();

        var staticTools = host.Services.GetServices<McpServerTool>().ToList();
        var seeds = SeedInstaller.LoadSeeds(typeof(GrasshopperHostProfile).Assembly).ToList();

        var totalDistinctCount = staticTools.Select(t => t.ProtocolTool.Name)
            .Union(seeds.Select(s => s.Name))
            .Distinct()
            .Count();

        Assert.True(totalDistinctCount >= 24, $"Expected at least 24 tools, found {totalDistinctCount}");

        foreach (var tool in staticTools)
        {
            var p = tool.ProtocolTool;
            Assert.False(string.IsNullOrWhiteSpace(p.Name), "Tool name cannot be empty");
            Assert.False(string.IsNullOrWhiteSpace(p.Description), $"Tool {p.Name} description cannot be empty");
            Assert.Equal(JsonValueKind.Object, p.InputSchema.ValueKind);
            Assert.Equal("object", p.InputSchema.GetProperty("type").GetString());
        }

        foreach (var seed in seeds)
        {
            var doc = JsonDocument.Parse(seed.ToolJson);
            var root = doc.RootElement;
            Assert.Equal(seed.Name, root.GetProperty("name").GetString());
            Assert.True(root.GetProperty("description").GetString()!.Length >= 20);
            Assert.Equal("object", root.GetProperty("inputSchema").GetProperty("type").GetString());
        }
    }

    [Fact]
    public void CoreTools_HaveCorrectSecurityAnnotations()
    {
        var builder = McpServerHost.CreateBuilder([], GrasshopperHostProfile.Instance);
        GrasshopperHostProfile.EnforceToolSurface(builder);
        using var host = builder.Build();

        var tools = host.Services.GetServices<McpServerTool>().ToList();

        var exec = tools.Single(t => t.ProtocolTool.Name == "execute_grasshopper_code").ProtocolTool;
        Assert.True(exec.Annotations?.DestructiveHint);
        Assert.False(exec.Annotations?.ReadOnlyHint ?? false);

        var read = tools.Single(t => t.ProtocolTool.Name == "read_grasshopper_canvas").ProtocolTool;
        Assert.True(read.Annotations?.ReadOnlyHint);
        Assert.False(read.Annotations?.DestructiveHint ?? false);

        var export = tools.Single(t => t.ProtocolTool.Name == "export_grasshopper_canvas_image").ProtocolTool;
        Assert.True(export.Annotations?.ReadOnlyHint);
        Assert.False(export.Annotations?.DestructiveHint ?? false);
    }
}
