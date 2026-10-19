namespace HPDynamo.Mcp.Server.Tests.Seeds;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using HPDynamo.Mcp.Server.Hosts;
using HPRebar.Mcp.Server.Bootstrap;
using HPRebar.Mcp.Server.Registry;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Server;
using Xunit;

/// <summary>
///     Verifies Milestone B requirement: The published tool surface exposes exactly
///     25 tools (4 core, 8 registry, 13 seeds), with all formerly quarantined tools recovered
///     and published with status='published'.
/// </summary>
public sealed class ProductionScriptCompilerParityTests
{
    private static readonly string[] ExpectedQuarantinedRecoveredTools =
    [
        "batch_edit_graph",
        "search_nodes_library",
        "get_active_graph_info"
    ];

    [Fact]
    public void ToolSurface_ReturnsExactly25Tools_WithAllSeedsPublished()
    {
        var builder = McpServerHost.CreateBuilder([], DynamoHostProfile.Instance);
        DynamoHostProfile.EnforceToolSurface(builder);
        using var host = builder.Build();

        var staticTools = host.Services.GetServices<McpServerTool>()
            .Select(t => t.ProtocolTool.Name)
            .Distinct(StringComparer.Ordinal)
            .ToList();

        var seeds = SeedInstaller.LoadSeeds(typeof(DynamoHostProfile).Assembly);
        var seedNames = seeds.Select(s => s.Name).Distinct(StringComparer.Ordinal).ToList();
        var combined = staticTools.Union(seedNames, StringComparer.Ordinal).ToList();

        Assert.Equal(12, staticTools.Count);
        Assert.Equal(13, seeds.Count);
        Assert.Equal(25, combined.Count);

        foreach (var seed in seeds)
        {
            using var doc = JsonDocument.Parse(seed.ToolJson);
            var status = doc.RootElement.GetProperty("status").GetString();
            Assert.Equal("published", status);
        }
    }

    [Fact]
    public void QuarantinedSeeds_RecoveredAndPublished()
    {
        var seeds = SeedInstaller.LoadSeeds(typeof(DynamoHostProfile).Assembly);
        var seedDict = seeds.ToDictionary(s => s.Name, StringComparer.OrdinalIgnoreCase);

        foreach (var toolName in ExpectedQuarantinedRecoveredTools)
        {
            Assert.True(seedDict.TryGetValue(toolName, out var seed), $"Recovered tool '{toolName}' not found in seeds.");
            using var doc = JsonDocument.Parse(seed!.ToolJson);
            var status = doc.RootElement.GetProperty("status").GetString();
            Assert.Equal("published", status);
        }
    }
}
