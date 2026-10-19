namespace HPGrasshopper.Mcp.Server.Tests.Registry;

using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using HPGrasshopper.Mcp.Server.Tests.Fixtures;
using HPRebar.Mcp.Contracts.Messages;
using HPRebar.Mcp.Server.Registry;
using HPRebar.Mcp.Server.Registry.Model;
using HPRebar.Mcp.Server.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Server;
using Xunit;

public sealed class RegistryLifecycleTests
{
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

    private static ProposeInput CreateInput(
        string name = "compute_truss_span",
        string code = SampleCode,
        string transaction = "none") => new(
        name,
        "Compute Truss Span",
        "Calculates the total length of a truss span given panel count and spacing.",
        "Generic",
        ["geometry", "truss"],
        SampleSchema,
        code,
        [
            new ToolExample { Title = "defaults", Args = JsonSerializer.Deserialize<JsonElement>("{}") },
            new ToolExample { Title = "custom", Args = JsonSerializer.Deserialize<JsonElement>("""{"count": 5, "spacing": 200.0}""") }
        ],
        transaction,
        30,
        null,
        false);

    private static ToolLifecycleService CreateLifecycle(GrasshopperRegistryFixture fixture) =>
        new(fixture.Manager, fixture.Bridge, NullLogger<ToolLifecycleService>.Instance);

    [Fact]
    public async Task Lifecycle_ProposeValidTool_CreatesDraftAndSavesMetadata()
    {
        await using var fixture = new GrasshopperRegistryFixture();
        var lifecycle = CreateLifecycle(fixture);

        var proposed = await lifecycle.ProposeAsync(CreateInput(), CancellationToken.None);

        Assert.True(proposed.Accepted, string.Join("; ", proposed.Report.Errors));
        Assert.NotNull(proposed.Record);
        Assert.Equal(ToolStatus.Draft, proposed.Record.Status);
        Assert.Equal(1, proposed.Record.Version);
        Assert.True(File.Exists(Path.Combine(proposed.Record.Folder!, "tool.json")));
    }

    [Fact]
    public async Task Lifecycle_TestTool_ExecutesDryRunAndTransitionsToTested()
    {
        await using var fixture = new GrasshopperRegistryFixture();
        var lifecycle = CreateLifecycle(fixture);
        await lifecycle.ProposeAsync(CreateInput("test_tool_candidate"), CancellationToken.None);

        fixture.Executor.ExecuteHandler = req => new ExecuteResult
        {
            Value = req.Args,
            DurationMs = 5,
            RolledBack = req.DryRun
        };

        var tested = await lifecycle.TestAsync("test_tool_candidate", null, false, CancellationToken.None);

        Assert.Equal(2, tested.Passed);
        Assert.Equal("tested", tested.Status);
        Assert.All(tested.Cases, c => Assert.True(c.DryRun));
        Assert.True(fixture.Executor.LastExecuteRequest!.DryRun);
    }

    [Fact]
    public async Task Lifecycle_PublishManualPolicy_TransitionsToPendingApprovalAndGeneratesReview()
    {
        await using var fixture = new GrasshopperRegistryFixture();
        var lifecycle = CreateLifecycle(fixture);
        await lifecycle.ProposeAsync(CreateInput("tool_for_manual_publish"), CancellationToken.None);

        fixture.Executor.ExecuteHandler = req => new ExecuteResult
        {
            Value = req.Args,
            DurationMs = 2,
            RolledBack = req.DryRun
        };
        await lifecycle.TestAsync("tool_for_manual_publish", null, false, CancellationToken.None);

        var published = lifecycle.Publish("tool_for_manual_publish");

        Assert.Equal("pending_approval", published.Status);
        Assert.NotNull(published.ReviewFile);
        Assert.True(File.Exists(published.ReviewFile));
    }

    [Fact]
    public async Task Lifecycle_ApproveTool_TransitionsToPublishedAndUpdatesToolSurface()
    {
        await using var fixture = new GrasshopperRegistryFixture();
        var lifecycle = CreateLifecycle(fixture);
        var registrar = new DynamicToolRegistrar(
            fixture.Manager,
            new ResultFormatter(),
            Options.Create(new McpServerOptions()),
            NullLogger<DynamicToolRegistrar>.Instance);
        fixture.Manager.Changed += () => registrar.Sync();

        await lifecycle.ProposeAsync(CreateInput("tool_to_approve"), CancellationToken.None);
        fixture.Executor.ExecuteHandler = req => new ExecuteResult { DurationMs = 2, RolledBack = req.DryRun };
        await lifecycle.TestAsync("tool_to_approve", null, false, CancellationToken.None);
        lifecycle.Publish("tool_to_approve");

        var approved = lifecycle.Approve("tool_to_approve", "reviewer_engineer");

        Assert.Equal(ToolStatus.Published, approved.Status);
        Assert.Equal("reviewer_engineer", approved.ApprovedBy);
        Assert.Contains("tool_to_approve", registrar.RegisteredNames);
    }

    [Fact]
    public async Task Lifecycle_PublishAutoPolicy_DirectlyPublishesTestedTool()
    {
        await using var fixture = new GrasshopperRegistryFixture(opt => opt.PublishPolicy = "auto");
        var lifecycle = CreateLifecycle(fixture);

        await lifecycle.ProposeAsync(CreateInput("tool_auto_publish"), CancellationToken.None);
        fixture.Executor.ExecuteHandler = req => new ExecuteResult { DurationMs = 2, RolledBack = req.DryRun };
        await lifecycle.TestAsync("tool_auto_publish", null, false, CancellationToken.None);

        var published = lifecycle.Publish("tool_auto_publish");

        Assert.Equal("published", published.Status);
        var tool = fixture.Manager.Get("tool_auto_publish");
        Assert.NotNull(tool);
        Assert.Equal(ToolStatus.Published, tool.Record.Status);
    }

    [Fact]
    public async Task Lifecycle_ProposeInvalidTool_ReturnsFailuresAndSavesNothing()
    {
        await using var fixture = new GrasshopperRegistryFixture();
        var lifecycle = CreateLifecycle(fixture);
        const string badCode = "System.IO.File.Delete(\"test.txt\"); return 1;";

        var proposed = await lifecycle.ProposeAsync(CreateInput("bad_tool", badCode), CancellationToken.None);

        Assert.False(proposed.Accepted);
        Assert.NotEmpty(proposed.Report.Errors);
        Assert.False(fixture.Manager.TryGet("bad_tool", out _));
    }
}
