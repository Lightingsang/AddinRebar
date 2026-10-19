namespace HPDynamo.Mcp.Server.Tests.Registry;

using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using HPDynamo.Mcp.Server.Tests.Fixtures;
using HPRebar.Mcp.Contracts.Messages;
using HPRebar.Mcp.Server.Registry;
using HPRebar.Mcp.Server.Registry.Model;
using HPRebar.Mcp.Server.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Server;
using Xunit;

public sealed class RegistrySearchAndManageTests
{
    private static ToolLifecycleService CreateLifecycle(DynamoRegistryFixture fixture) =>
        new(fixture.Manager, fixture.Bridge, NullLogger<ToolLifecycleService>.Instance);

    private static async Task<string> SeedPublishedToolAsync(DynamoRegistryFixture fixture, string name = "custom_array_tool")
    {
        var lifecycle = CreateLifecycle(fixture);
        var schema = JsonSerializer.Deserialize<JsonElement>("""{"type":"object","properties":{"n":{"type":"integer"}}}""");
        var input = new ProposeInput(
            name, "Array Tool", "A custom array generator tool for testing search and manage.", "Generic",
            ["array", "custom"], schema, "return args.Int(\"n\", 1) * 10;",
            [
                new ToolExample { Title = "default", Args = JsonSerializer.Deserialize<JsonElement>("{}") },
                new ToolExample { Title = "three", Args = JsonSerializer.Deserialize<JsonElement>("""{"n": 3}""") }
            ],
            "none", 30, null, false);

        await lifecycle.ProposeAsync(input, CancellationToken.None);
        fixture.Executor.ExecuteHandler = req => new ExecuteResult { DurationMs = 1, RolledBack = req.DryRun };
        await lifecycle.TestAsync(name, null, false, CancellationToken.None);
        lifecycle.Publish(name);
        lifecycle.Approve(name, "lead");
        return name;
    }

    [Fact]
    public async Task Manage_DeprecateTool_RemovesFromActiveSurface()
    {
        await using var fixture = new DynamoRegistryFixture();
        var toolName = await SeedPublishedToolAsync(fixture, "tool_to_deprecate");
        var lifecycle = CreateLifecycle(fixture);
        var registrar = new DynamicToolRegistrar(
            fixture.Manager,
            new ResultFormatter(),
            Options.Create(new McpServerOptions()),
            NullLogger<DynamicToolRegistrar>.Instance);
        fixture.Manager.Changed += () => registrar.Sync();
        registrar.Sync();

        Assert.Contains(toolName, registrar.RegisteredNames);

        var deprecated = lifecycle.Manage(toolName, "deprecate", "Obsolete", "admin");
        Assert.Equal(ToolStatus.Deprecated, deprecated.Status);
        Assert.DoesNotContain(toolName, registrar.RegisteredNames);
    }

    [Fact]
    public async Task Manage_RestoreTool_TransitionsBackToDraft()
    {
        await using var fixture = new DynamoRegistryFixture();
        var toolName = await SeedPublishedToolAsync(fixture, "tool_to_restore");
        var lifecycle = CreateLifecycle(fixture);

        lifecycle.Manage(toolName, "deprecate", "Obsolete", "admin");
        var restored = lifecycle.Manage(toolName, "restore", null, "admin");

        Assert.Equal(ToolStatus.Draft, restored.Status);
    }

    [Fact]
    public async Task SearchTools_FindsMatchingPublishedTools()
    {
        await using var fixture = new DynamoRegistryFixture();
        var toolName = await SeedPublishedToolAsync(fixture, "searchable_array_tool");

        var results = fixture.Manager.Search("array", "Generic", 10, false);

        Assert.NotEmpty(results);
        Assert.Contains(results, r => r.Name == toolName);
    }

    [Fact]
    public async Task GetTool_ReturnsFullRecordWithSchemaAndExamples()
    {
        await using var fixture = new DynamoRegistryFixture();
        var toolName = await SeedPublishedToolAsync(fixture, "retrievable_tool");

        var tool = fixture.Manager.Get(toolName);

        Assert.NotNull(tool);
        Assert.Equal(toolName, tool.Record.Name);
        Assert.Equal(ToolStatus.Published, tool.Record.Status);
        Assert.Equal(2, tool.Record.Examples.Count);
        Assert.NotEmpty(tool.Record.Code);
    }
}
