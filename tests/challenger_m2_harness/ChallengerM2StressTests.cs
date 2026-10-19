using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using HPGrasshopper.Mcp.Server.Hosts;
using HPGrasshopper.Mcp.Server.Tests.Fixtures;
using HPGrasshopper.Mcp.Server.Tools;
using HPRebar.Mcp.Contracts;
using HPRebar.Mcp.Contracts.JsonRpc;
using HPRebar.Mcp.Contracts.Messages;
using HPRebar.Mcp.Server.Bootstrap;
using HPRebar.Mcp.Server.Models;
using HPRebar.Mcp.Server.Registry;
using HPRebar.Mcp.Server.Registry.Model;
using HPRebar.Mcp.Server.Services;
using HPRebar.McpBridge.Core.Scripting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using Xunit;

namespace ChallengerM2Harness;

public sealed class ChallengerM2StressTests
{
    #region Area 1: Tool Surface Listing

    [Fact]
    public void Surface_CoreToolsListing_ContainsAllRequiredCoreTools()
    {
        var builder = McpServerHost.CreateBuilder([], GrasshopperHostProfile.Instance);
        GrasshopperHostProfile.EnforceToolSurface(builder);
        using var host = builder.Build();

        var tools = host.Services.GetServices<McpServerTool>()
            .Select(t => t.ProtocolTool.Name)
            .ToHashSet(StringComparer.Ordinal);

        string[] requiredCore =
        [
            "read_grasshopper_canvas",
            "run_grasshopper_player",
            "batch_edit_grasshopper_canvas",
            "execute_grasshopper_code",
            "export_grasshopper_canvas_image",
            "get_grasshopper_context",
            "cancel_execution"
        ];

        foreach (var req in requiredCore)
        {
            Assert.Contains(req, tools);
        }
    }

    [Fact]
    public void Surface_DynamicRegistryMetaTools_ContainsAllEightTools()
    {
        var builder = McpServerHost.CreateBuilder([], GrasshopperHostProfile.Instance);
        GrasshopperHostProfile.EnforceToolSurface(builder);
        using var host = builder.Build();

        var tools = host.Services.GetServices<McpServerTool>()
            .Select(t => t.ProtocolTool.Name)
            .ToHashSet(StringComparer.Ordinal);

        string[] requiredRegistry =
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

        foreach (var req in requiredRegistry)
        {
            Assert.Contains(req, tools);
        }
    }

    [Fact]
    public void Surface_InspectType_IsSuppressedAcrossAllContainers()
    {
        var builder = McpServerHost.CreateBuilder([], GrasshopperHostProfile.Instance);
        GrasshopperHostProfile.EnforceToolSurface(builder);
        using var host = builder.Build();

        var tools = host.Services.GetServices<McpServerTool>()
            .Select(t => t.ProtocolTool.Name)
            .ToList();

        Assert.DoesNotContain("inspect_type", tools, StringComparer.OrdinalIgnoreCase);

        var options = host.Services.GetService<IOptions<McpServerOptions>>()?.Value;
        if (options?.ToolCollection != null)
        {
            Assert.DoesNotContain(options.ToolCollection,
                t => string.Equals(t.ProtocolTool.Name, "inspect_type", StringComparison.OrdinalIgnoreCase));
        }
    }

    [Fact]
    public void Surface_EmbeddedSeeds_ContainsExactlyTwelveSeedsWithIntegrity()
    {
        var seeds = SeedInstaller.LoadSeeds(typeof(GrasshopperHostProfile).Assembly);
        Assert.Equal(12, seeds.Count);

        string[] expectedSeedNames =
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

        var seedNames = seeds.Select(s => s.Name).ToHashSet(StringComparer.Ordinal);
        foreach (var expected in expectedSeedNames)
        {
            Assert.Contains(expected, seedNames);
        }

        foreach (var seed in seeds)
        {
            Assert.False(string.IsNullOrWhiteSpace(seed.Checksum));
            Assert.Equal(64, seed.Checksum.Length);
            Assert.False(string.IsNullOrWhiteSpace(seed.Code));
            Assert.False(string.IsNullOrWhiteSpace(seed.ToolJson));
            Assert.False(string.IsNullOrWhiteSpace(seed.ExamplesJson));
        }
    }

    [Fact]
    public void Surface_CombinedCatalogCompleteness_ExposesExactlyTwentySevenTools()
    {
        var builder = McpServerHost.CreateBuilder([], GrasshopperHostProfile.Instance);
        GrasshopperHostProfile.EnforceToolSurface(builder);
        using var host = builder.Build();

        var staticTools = host.Services.GetServices<McpServerTool>()
            .Select(t => t.ProtocolTool.Name)
            .Distinct(StringComparer.Ordinal)
            .ToHashSet();

        var seeds = SeedInstaller.LoadSeeds(typeof(GrasshopperHostProfile).Assembly)
            .Select(s => s.Name)
            .Distinct(StringComparer.Ordinal)
            .ToHashSet();

        var combined = staticTools.Union(seeds, StringComparer.Ordinal).ToHashSet();

        Assert.Equal(15, staticTools.Count);
        Assert.Equal(12, seeds.Count);
        Assert.Equal(27, combined.Count);
    }

    [Fact]
    public void Surface_CoreTools_HaveCorrectSecurityAnnotations()
    {
        var builder = McpServerHost.CreateBuilder([], GrasshopperHostProfile.Instance);
        GrasshopperHostProfile.EnforceToolSurface(builder);
        using var host = builder.Build();

        var tools = host.Services.GetServices<McpServerTool>().ToList();

        void Verify(string name, bool expectedReadOnly, bool expectedDestructive)
        {
            var match = tools.Single(t => t.ProtocolTool.Name == name).ProtocolTool;
            bool isReadOnly = match.Annotations?.ReadOnlyHint ?? false;
            bool isDestructive = match.Annotations?.DestructiveHint ?? false;

            Assert.Equal(expectedReadOnly, isReadOnly);
            Assert.Equal(expectedDestructive, isDestructive);
        }

        Verify("execute_grasshopper_code", false, true);
        Verify("batch_edit_grasshopper_canvas", false, true);
        Verify("run_grasshopper_player", false, true);
        Verify("read_grasshopper_canvas", true, false);
        Verify("export_grasshopper_canvas_image", true, false);
        Verify("get_grasshopper_context", true, false);
        Verify("cancel_execution", false, false);
    }

    #endregion

    #region Area 2: Dynamic Registry Lifecycle State Transitions

    private const string SampleCode = """
        int count = args.Int("count", 3);
        double spacing = args.Double("spacing", 100.0);
        return count * spacing;
        """;

    private static readonly JsonElement SampleSchema = JsonSerializer.Deserialize<JsonElement>("""
        {
            "type": "object",
            "properties": {
                "count": { "type": "integer", "default": 3 },
                "spacing": { "type": "number", "default": 100.0 }
            }
        }
        """);

    private static ProposeInput CreateInput(string name = "stress_truss_span") => new(
        name,
        "Stress Truss Span",
        "Calculates the total length of a truss span given panel count and spacing.",
        "Generic",
        ["geometry", "truss"],
        SampleSchema,
        SampleCode,
        [
            new ToolExample { Title = "defaults", Args = JsonSerializer.Deserialize<JsonElement>("{}") },
            new ToolExample { Title = "custom", Args = JsonSerializer.Deserialize<JsonElement>("""{"count": 5, "spacing": 200.0}""") }
        ],
        "none",
        30,
        null,
        false);

    [Fact]
    public async Task Registry_FullLifecycleTransition_DraftToTestedToPendingToPublishedToDeprecated()
    {
        await using var fixture = new GrasshopperRegistryFixture();
        var lifecycle = new ToolLifecycleService(fixture.Manager, fixture.Bridge, NullLogger<ToolLifecycleService>.Instance);

        const string toolName = "lifecycle_full_tool";

        // 1. Propose -> Draft
        var proposed = await lifecycle.ProposeAsync(CreateInput(toolName), CancellationToken.None);
        Assert.True(proposed.Accepted, string.Join("; ", proposed.Report.Errors));
        Assert.NotNull(proposed.Record);
        Assert.Equal(ToolStatus.Draft, proposed.Record.Status);

        // 2. Test -> Tested
        fixture.Executor.ExecuteHandler = req => new ExecuteResult { DurationMs = 5, RolledBack = req.DryRun };
        var tested = await lifecycle.TestAsync(toolName, null, false, CancellationToken.None);
        Assert.Equal(2, tested.Passed);
        Assert.Equal("tested", tested.Status);

        // 3. Publish (manual policy) -> PendingApproval
        var publishedOutcome = lifecycle.Publish(toolName);
        Assert.Equal("pending_approval", publishedOutcome.Status);
        Assert.NotNull(publishedOutcome.ReviewFile);
        Assert.True(File.Exists(publishedOutcome.ReviewFile));

        // 4. Approve -> Published
        var approved = lifecycle.Approve(toolName, "qa_lead");
        Assert.Equal(ToolStatus.Published, approved.Status);
        Assert.Equal("qa_lead", approved.ApprovedBy);

        // 5. Manage deprecate (retire) -> Deprecated (Retired)
        var deprecated = lifecycle.Manage(toolName, "deprecate", "End of life cycle test", "qa_lead");
        Assert.Equal(ToolStatus.Deprecated, deprecated.Status);

        var recordInDb = fixture.Manager.Get(toolName);
        Assert.NotNull(recordInDb);
        Assert.Equal(ToolStatus.Deprecated, recordInDb.Record.Status);
    }

    [Fact]
    public async Task Registry_NegativeTransition_DraftCannotPublishDirectly()
    {
        await using var fixture = new GrasshopperRegistryFixture();
        var lifecycle = new ToolLifecycleService(fixture.Manager, fixture.Bridge, NullLogger<ToolLifecycleService>.Instance);

        const string toolName = "draft_skip_test_tool";
        await lifecycle.ProposeAsync(CreateInput(toolName), CancellationToken.None);

        var ex = Assert.Throws<ToolNotRunnableException>(() => lifecycle.Publish(toolName));
        Assert.Contains("not been tested", ex.Message);
    }

    [Fact]
    public async Task Registry_NegativeTransition_DraftCannotApproveWithoutForce()
    {
        await using var fixture = new GrasshopperRegistryFixture();
        var lifecycle = new ToolLifecycleService(fixture.Manager, fixture.Bridge, NullLogger<ToolLifecycleService>.Instance);

        const string toolName = "draft_unforced_approve_tool";
        await lifecycle.ProposeAsync(CreateInput(toolName), CancellationToken.None);

        Assert.Throws<ToolNotRunnableException>(() => lifecycle.Approve(toolName, "qa_reviewer", force: false));

        var forced = lifecycle.Approve(toolName, "qa_reviewer", force: true);
        Assert.Equal(ToolStatus.Published, forced.Status);
    }

    [Fact]
    public async Task Registry_NegativeTransition_DeprecatedToolCannotBeTestedOrPublished()
    {
        await using var fixture = new GrasshopperRegistryFixture();
        var lifecycle = new ToolLifecycleService(fixture.Manager, fixture.Bridge, NullLogger<ToolLifecycleService>.Instance);

        const string toolName = "deprecated_blocked_tool";
        await lifecycle.ProposeAsync(CreateInput(toolName), CancellationToken.None);
        fixture.Executor.ExecuteHandler = req => new ExecuteResult { DurationMs = 5, RolledBack = req.DryRun };
        await lifecycle.TestAsync(toolName, null, false, CancellationToken.None);
        lifecycle.Approve(toolName, "qa", force: true);
        lifecycle.Manage(toolName, "deprecate", "Retiring tool", "admin");

        await Assert.ThrowsAsync<ToolNotRunnableException>(() => lifecycle.TestAsync(toolName, null, false, CancellationToken.None));
        Assert.Throws<ToolNotRunnableException>(() => lifecycle.Publish(toolName));
    }

    [Fact]
    public async Task Registry_Restoration_DeprecatedRestoresToDraftAndRePublishes()
    {
        await using var fixture = new GrasshopperRegistryFixture();
        var lifecycle = new ToolLifecycleService(fixture.Manager, fixture.Bridge, NullLogger<ToolLifecycleService>.Instance);

        const string toolName = "restore_flow_tool";
        await lifecycle.ProposeAsync(CreateInput(toolName), CancellationToken.None);
        fixture.Executor.ExecuteHandler = req => new ExecuteResult { DurationMs = 5, RolledBack = req.DryRun };
        await lifecycle.TestAsync(toolName, null, false, CancellationToken.None);
        lifecycle.Approve(toolName, "admin", force: true);
        lifecycle.Manage(toolName, "deprecate", "Retired temporarily", "admin");

        // Restore -> Draft
        var restored = lifecycle.Manage(toolName, "restore", "Bring back", "admin");
        Assert.Equal(ToolStatus.Draft, restored.Status);

        // Re-test -> Tested
        await lifecycle.TestAsync(toolName, null, false, CancellationToken.None);
        var reTested = fixture.Manager.Get(toolName);
        Assert.Equal(ToolStatus.Tested, reTested?.Record.Status);

        // Re-approve -> Published
        var reApproved = lifecycle.Approve(toolName, "admin");
        Assert.Equal(ToolStatus.Published, reApproved.Status);
    }

    [Fact]
    public async Task Registry_DynamicToolRegistrarSync_PublishesAndRemovesCorrectly()
    {
        await using var fixture = new GrasshopperRegistryFixture();
        var lifecycle = new ToolLifecycleService(fixture.Manager, fixture.Bridge, NullLogger<ToolLifecycleService>.Instance);
        var registrar = new DynamicToolRegistrar(
            fixture.Manager,
            new ResultFormatter(),
            Options.Create(new McpServerOptions()),
            NullLogger<DynamicToolRegistrar>.Instance);

        fixture.Manager.Changed += () => registrar.Sync();

        const string toolName = "registrar_sync_tool";
        await lifecycle.ProposeAsync(CreateInput(toolName), CancellationToken.None);
        Assert.DoesNotContain(toolName, registrar.RegisteredNames);

        fixture.Executor.ExecuteHandler = req => new ExecuteResult { DurationMs = 2, RolledBack = req.DryRun };
        await lifecycle.TestAsync(toolName, null, false, CancellationToken.None);
        Assert.DoesNotContain(toolName, registrar.RegisteredNames);

        lifecycle.Approve(toolName, "qa", force: true);
        Assert.Contains(toolName, registrar.RegisteredNames);

        // Quarantine removes
        lifecycle.Manage(toolName, "quarantine", "Bad behavior", "sec_team");
        Assert.DoesNotContain(toolName, registrar.RegisteredNames);

        // Restore -> Draft
        lifecycle.Manage(toolName, "restore", "Cleaned up", "sec_team");
        Assert.DoesNotContain(toolName, registrar.RegisteredNames);
    }

    [Fact]
    public async Task Registry_AutoPublishPolicy_PublishesTestedImmediately()
    {
        await using var fixture = new GrasshopperRegistryFixture(opt => opt.PublishPolicy = "auto");
        var lifecycle = new ToolLifecycleService(fixture.Manager, fixture.Bridge, NullLogger<ToolLifecycleService>.Instance);

        const string toolName = "auto_policy_tool";
        await lifecycle.ProposeAsync(CreateInput(toolName), CancellationToken.None);
        fixture.Executor.ExecuteHandler = req => new ExecuteResult { DurationMs = 2, RolledBack = req.DryRun };
        await lifecycle.TestAsync(toolName, null, false, CancellationToken.None);

        var pub = lifecycle.Publish(toolName);
        Assert.Equal("published", pub.Status);

        var tool = fixture.Manager.Get(toolName);
        Assert.Equal(ToolStatus.Published, tool?.Record.Status);
        Assert.Equal("policy:auto", tool?.Record.ApprovedBy);
    }

    #endregion

    #region Area 3: JSON Schema Validation

    [Fact]
    public void Schema_StaticToolsJsonSchema_ConformsStrictly()
    {
        var builder = McpServerHost.CreateBuilder([], GrasshopperHostProfile.Instance);
        GrasshopperHostProfile.EnforceToolSurface(builder);
        using var host = builder.Build();

        var tools = host.Services.GetServices<McpServerTool>().ToList();

        foreach (var t in tools)
        {
            var p = t.ProtocolTool;
            Assert.False(string.IsNullOrWhiteSpace(p.Name));
            Assert.True(p.Description?.Length >= 10);

            var schema = p.InputSchema;
            Assert.Equal(JsonValueKind.Object, schema.ValueKind);
            Assert.Equal("object", schema.GetProperty("type").GetString());

            if (schema.TryGetProperty("properties", out var properties))
            {
                Assert.Equal(JsonValueKind.Object, properties.ValueKind);
            }

            if (schema.TryGetProperty("required", out var required))
            {
                Assert.Equal(JsonValueKind.Array, required.ValueKind);
                foreach (var reqItem in required.EnumerateArray())
                {
                    var reqKey = reqItem.GetString();
                    Assert.NotNull(reqKey);
                    Assert.True(properties.TryGetProperty(reqKey, out _));
                }
            }
        }
    }

    [Fact]
    public void Schema_SeedsJsonSchema_ConformsStrictlyWithExplicitTypes()
    {
        var seeds = SeedInstaller.LoadSeeds(typeof(GrasshopperHostProfile).Assembly);

        foreach (var seed in seeds)
        {
            using var doc = JsonDocument.Parse(seed.ToolJson);
            var root = doc.RootElement;

            Assert.Equal(seed.Name, root.GetProperty("name").GetString());
            var desc = root.GetProperty("description").GetString();
            Assert.True(desc?.Length >= 20);

            var inputSchema = root.GetProperty("inputSchema");
            Assert.Equal("object", inputSchema.GetProperty("type").GetString());

            var properties = inputSchema.GetProperty("properties");
            foreach (var prop in properties.EnumerateObject())
            {
                Assert.True(prop.Value.TryGetProperty("type", out var propType));
                Assert.False(string.IsNullOrWhiteSpace(propType.GetString()));
            }

            if (inputSchema.TryGetProperty("required", out var required))
            {
                foreach (var req in required.EnumerateArray())
                {
                    var reqName = req.GetString();
                    Assert.True(properties.TryGetProperty(reqName!, out _));
                }
            }
        }
    }

    [Fact]
    public void Schema_SeedExamples_StructureAndParity()
    {
        var seeds = SeedInstaller.LoadSeeds(typeof(GrasshopperHostProfile).Assembly);

        foreach (var seed in seeds)
        {
            using var toolDoc = JsonDocument.Parse(seed.ToolJson);
            using var exDoc = JsonDocument.Parse(seed.ExamplesJson);

            var tool = toolDoc.RootElement;
            var examples = exDoc.RootElement;

            Assert.Equal(JsonValueKind.Array, examples.ValueKind);
            Assert.True(examples.GetArrayLength() >= 2);

            var schemaProps = tool.GetProperty("inputSchema")
                .GetProperty("properties")
                .EnumerateObject()
                .Select(p => p.Name)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var requiredProps = tool.GetProperty("inputSchema").TryGetProperty("required", out var req)
                ? req.EnumerateArray().Select(r => r.GetString()!).ToList()
                : new List<string>();

            var argHashes = new HashSet<string>();

            foreach (var ex in examples.EnumerateArray())
            {
                var title = ex.GetProperty("title").GetString();
                Assert.False(string.IsNullOrWhiteSpace(title));

                var args = ex.GetProperty("args");
                Assert.Equal(JsonValueKind.Object, args.ValueKind);

                argHashes.Add(JsonSerializer.Serialize(args));

                var argKeys = args.EnumerateObject().Select(p => p.Name).ToList();
                foreach (var k in argKeys)
                {
                    Assert.Contains(k, schemaProps);
                }

                foreach (var rk in requiredProps)
                {
                    Assert.Contains(rk, argKeys, StringComparer.OrdinalIgnoreCase);
                }
            }

            Assert.True(argHashes.Count >= 2);
        }
    }

    [Fact]
    public void Schema_SeedCodeArgs_MatchInputSchemaProperties()
    {
        var seeds = SeedInstaller.LoadSeeds(typeof(GrasshopperHostProfile).Assembly);

        foreach (var seed in seeds)
        {
            using var toolDoc = JsonDocument.Parse(seed.ToolJson);
            var tool = toolDoc.RootElement;

            var analysis = ScriptAnalyzer.Analyze(seed.Code, AnalyzerProfile.Grasshopper);
            var schemaProps = tool.GetProperty("inputSchema")
                .GetProperty("properties")
                .EnumerateObject()
                .Select(p => p.Name)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            foreach (var argUsage in analysis.ArgKeys)
            {
                Assert.Contains(argUsage.Key, schemaProps);
            }
        }
    }

    #endregion

    #region Area 4: Script Quality & ADR-0007

    [Fact]
    public void Quality_ScriptQualityWalker_AllSeedsZeroErrorsAndZeroWarnings()
    {
        var seeds = SeedInstaller.LoadSeeds(typeof(GrasshopperHostProfile).Assembly);

        var allFindings = new List<string>();

        foreach (var seed in seeds)
        {
            var findings = ScriptQuality.Find(seed.Code);
            foreach (var f in findings)
            {
                allFindings.Add($"[{f.Severity}] {seed.Category}/{seed.Name} {f.RuleId} ({f.Line}:{f.Column}): {f.Message}");
            }

            int lineCount = seed.Code.Split('\n').Length;
            if (lineCount > 300)
            {
                allFindings.Add($"[Error] {seed.Category}/{seed.Name} exceeds 300 lines ({lineCount} lines)");
            }
        }

        Assert.Empty(allFindings);
    }

    [Fact]
    public void Quality_DeniedNamespaces_NoForbiddenTokensInAnySeed()
    {
        var seeds = SeedInstaller.LoadSeeds(typeof(GrasshopperHostProfile).Assembly);

        string[] forbiddenTokens =
        [
            "System.IO.",
            "System.Net.",
            "System.Diagnostics.Process",
            "#r ",
            "#load "
        ];

        foreach (var seed in seeds)
        {
            foreach (var tok in forbiddenTokens)
            {
                Assert.DoesNotContain(tok, seed.Code);
            }
        }
    }

    #endregion
}
