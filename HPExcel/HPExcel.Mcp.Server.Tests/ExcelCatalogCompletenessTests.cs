using System.Reflection;
using System.Text.Json;
using HPExcel.Mcp.Server.Hosts;
using HPRebar.Mcp.Contracts;
using HPRebar.Mcp.Server.Bootstrap;
using HPRebar.Mcp.Server.Hosts;
using HPRebar.Mcp.Server.Registry;
using HPRebar.Mcp.Server.Registry.Model;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Server;
using Xunit;

namespace HPExcel.Mcp.Server.Tests;

/// <summary>
///     Comprehensive tool catalog completeness and metadata verification suite for HPExcel MCP.
///     Asserts that all 24 tools (4 core, 8 registry, 12 seeds) are present, properly categorized,
///     conform to strict JSON schema rules, and accurately declare safety annotations.
/// </summary>
public sealed class ExcelCatalogCompletenessTests
{
    private static readonly string[] ExpectedCoreTools =
    [
        "execute_excel_code",
        "get_excel_context",
        "inspect_type",
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
        "read_range",
        "read_worksheet_info",
        "find_cells",
        "read_table",
        "write_range",
        "format_range",
        "manage_worksheet",
        "create_table",
        "create_chart",
        "evaluate_formula",
        "export_worksheet",
        "run_macro"
    ];

    [Fact]
    public void Catalog_ContainsExactly24ToolsAcrossCoreRegistryAndSeeds()
    {
        using var host = McpServerHost.CreateBuilder([], ExcelHostProfile.Instance).Build();
        var staticTools = host.Services.GetServices<McpServerTool>()
            .Select(t => t.ProtocolTool.Name)
            .ToHashSet(StringComparer.Ordinal);

        var seeds = SeedInstaller.LoadSeeds(typeof(ExcelHostProfile).Assembly)
            .Select(s => s.Name)
            .ToHashSet(StringComparer.Ordinal);

        var combinedCatalog = new HashSet<string>(staticTools, StringComparer.Ordinal);
        foreach (var s in seeds) combinedCatalog.Add(s);

        Assert.Equal(24, combinedCatalog.Count);
        Assert.Equal(4, ExpectedCoreTools.Length);
        Assert.Equal(8, ExpectedRegistryTools.Length);
        Assert.Equal(12, ExpectedSeedTools.Length);

        foreach (var core in ExpectedCoreTools)
            Assert.Contains(core, combinedCatalog);

        foreach (var reg in ExpectedRegistryTools)
            Assert.Contains(reg, combinedCatalog);

        foreach (var seed in ExpectedSeedTools)
            Assert.Contains(seed, combinedCatalog);
    }

    [Fact]
    public void CoreTools_HaveExpectedSafetyAnnotations()
    {
        using var host = McpServerHost.CreateBuilder([], ExcelHostProfile.Instance).Build();
        var tools = host.Services.GetServices<McpServerTool>()
            .ToDictionary(t => t.ProtocolTool.Name, t => t.ProtocolTool, StringComparer.Ordinal);

        // execute_excel_code
        Assert.True(tools.TryGetValue("execute_excel_code", out var execTool));
        Assert.True(execTool.Annotations?.DestructiveHint);
        Assert.False(execTool.Annotations?.ReadOnlyHint);
        Assert.True(execTool.Description!.Length >= 50);

        // get_excel_context
        Assert.True(tools.TryGetValue("get_excel_context", out var ctxTool));
        Assert.True(ctxTool.Annotations?.ReadOnlyHint);
        Assert.False(ctxTool.Annotations?.DestructiveHint);
        Assert.True(ctxTool.Annotations?.IdempotentHint);
        Assert.True(ctxTool.Description!.Length >= 50);

        // inspect_type
        Assert.True(tools.TryGetValue("inspect_type", out var inspectTool));
        Assert.True(inspectTool.Annotations?.ReadOnlyHint);
        Assert.False(inspectTool.Annotations?.DestructiveHint);

        // cancel_execution
        Assert.True(tools.TryGetValue("cancel_execution", out var cancelTool));
        Assert.False(cancelTool.Annotations?.ReadOnlyHint);
    }

    [Fact]
    public void All12Seeds_HaveStrictJsonSchema_AndValidMetadata()
    {
        var seeds = SeedInstaller.LoadSeeds(typeof(ExcelHostProfile).Assembly);
        Assert.Equal(12, seeds.Count);

        var validCategories = ExcelHostProfile.Instance.Categories;

        foreach (var seed in seeds)
        {
            var record = RegistryJson.Deserialize<ToolRecord>(seed.ToolJson);
            Assert.NotNull(record);

            // Host & Category
            Assert.Equal("excel", record.Host);
            Assert.Contains(record.Category, validCategories);
            Assert.Contains("2026", record.HostVersions);

            // Title, description, notes
            Assert.False(string.IsNullOrWhiteSpace(record.Title), $"Seed {record.Name} title is empty.");
            Assert.True(record.Description.Length >= 20, $"Seed {record.Name} description is too short.");
            Assert.False(string.IsNullOrWhiteSpace(record.Notes), $"Seed {record.Name} notes is empty.");

            // Tags
            Assert.NotEmpty(record.Tags);

            // Transaction & Destructive alignment
            if (record.Transaction == "none")
            {
                Assert.False(record.Destructive, $"Read-only tool {record.Name} must not be destructive.");
                Assert.InRange(record.TimeoutSeconds, 5, 30);
            }
            else
            {
                Assert.True(record.Destructive, $"Mutating tool {record.Name} must be flagged as destructive.");
                Assert.InRange(record.TimeoutSeconds, 5, 120);
            }

            // Schema checks
            var schema = record.InputSchema;
            Assert.Equal(JsonValueKind.Object, schema.ValueKind);
            Assert.Equal("object", schema.GetProperty("type").GetString());

            var props = schema.GetProperty("properties");
            Assert.Equal(JsonValueKind.Object, props.ValueKind);

            if (schema.TryGetProperty("additionalProperties", out var addlProps))
            {
                Assert.False(addlProps.GetBoolean(), $"Tool {record.Name} should disallow additionalProperties.");
            }

            if (schema.TryGetProperty("required", out var reqArray))
            {
                var declaredNames = props.EnumerateObject().Select(p => p.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
                foreach (var reqItem in reqArray.EnumerateArray())
                {
                    Assert.Contains(reqItem.GetString()!, declaredNames);
                }
            }
        }
    }

    [Fact]
    public void RegistryTools_AllEightAreRegisteredWithEngine()
    {
        using var host = McpServerHost.CreateBuilder([], ExcelHostProfile.Instance).Build();
        var toolNames = host.Services.GetServices<McpServerTool>()
            .Select(t => t.ProtocolTool.Name)
            .ToHashSet(StringComparer.Ordinal);

        foreach (var regTool in ExpectedRegistryTools)
        {
            Assert.Contains(regTool, toolNames);
        }
    }
}
