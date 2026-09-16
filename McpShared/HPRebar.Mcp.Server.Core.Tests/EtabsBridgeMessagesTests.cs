using System.Text.Json;
using HPRebar.Mcp.Contracts.JsonRpc;
using HPRebar.Mcp.Contracts.Messages;
using HPRebar.Mcp.Server.Models;
using HPRebar.Mcp.Server.Registry;
using HPRebar.Mcp.Server.Registry.Model;
using HPRebar.Mcp.Server.Services;
using HPRebar.Mcp.Server.Tests.Fakes;
using HPRebar.McpBridge.Core.Model;
using HPRebar.McpBridge.Core.Pipe;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;
using static HPRebar.Mcp.Server.Tests.EtabsTestProfile;

namespace HPRebar.Mcp.Server.Tests;

/// <summary>
///     The ETABS profile over a real pipe with the fake executor: the timeout ceiling reaches the clamp, the three
///     host-facing messages keep their historical text unless the host supplies its own, the context shape carries
///     the `etabs` block, a proposal's declared transaction reaches the bridge, and a refusal the bridge answers with
///     `-32001` never enters a tool's run history.
/// </summary>
public sealed class EtabsBridgeMessagesTests
{
    [Theory]
    [InlineData(600, 600, 600)]
    [InlineData(601, 600, 600)]
    [InlineData(600, 120, 120)]
    public async Task Execute_service_clamps_to_the_etabs_ceiling(int requested, int ceiling, int expected)
    {
        var pipe = NewPipe();
        var executor = new FakeRevitExecutor();
        using var listener = new PipeListener(pipe, new RequestDispatcher(executor, new BridgeSettings { ExecutionEnabled = true }, "22", "ETABS"));
        listener.Start();
        var options = Options.Create(PipeOptions(pipe));
        await using var client = new RevitBridgeClient(options, NullLogger<RevitBridgeClient>.Instance, Etabs(ceiling));
        var service = new ExecuteCodeService(client, new ResultFormatter(), options);

        var result = await service.ExecuteAsync("return 1;", "none", false, requested, "clamp", null, null, TestContext.Current.CancellationToken);

        Assert.False(result.IsError);
        Assert.Equal(expected, executor.LastExecuteRequest!.TimeoutSeconds);
        await listener.StopAsync();
    }

    [Fact]
    public async Task Execution_disabled_text_is_the_historical_one_unless_the_bridge_supplies_its_own()
    {
        var generic = await ExecuteWithOptInOffAsync(executionDisabledMessage: null);
        var etabs = await ExecuteWithOptInOffAsync(DisabledText);

        Assert.Equal(BridgeErrorCode.ExecutionDisabled, generic.Code);
        Assert.Equal("Code execution is disabled. Ask the user to tick 'Allow AI code execution' in the HP MCP Bridge window inside ETABS.", generic.Message);
        Assert.Equal(BridgeErrorCode.ExecutionDisabled, etabs.Code);
        Assert.Equal(DisabledText, etabs.Message);
    }

    private static async Task<BridgeErrorException> ExecuteWithOptInOffAsync(string? executionDisabledMessage)
    {
        var pipe = NewPipe();
        using var listener = new PipeListener(pipe, new RequestDispatcher(new FakeRevitExecutor(), new BridgeSettings(), "22", "ETABS", executionDisabledMessage));
        listener.Start();
        await using var client = new RevitBridgeClient(Options.Create(PipeOptions(pipe)), NullLogger<RevitBridgeClient>.Instance, Etabs());

        var error = await Assert.ThrowsAsync<BridgeErrorException>(() => client.SendAsync<ExecuteResult>(
            "etabs.execute", new ExecuteRequest("return 1;", "none", false, 10, "off", null), TimeSpan.FromSeconds(10), null, TestContext.Current.CancellationToken));
        await listener.StopAsync();
        return error;
    }

    [Fact]
    public async Task Bridge_not_connected_message_keeps_the_add_in_sentence_unless_the_profile_names_its_program()
    {
        var options = Options.Create(new BridgeOptions { HostId = "etabs", HostVersion = 22, PipeName = "hpetabs-mcp-nobody-" + Guid.NewGuid().ToString("N"), ConnectTimeoutMs = 200, PingIntervalSeconds = 60 });
        await using var generic = new RevitBridgeClient(options, NullLogger<RevitBridgeClient>.Instance, Etabs());
        await using var named = new RevitBridgeClient(options, NullLogger<RevitBridgeClient>.Instance, Etabs(notConnected: NotConnectedHint));

        var a = await Assert.ThrowsAsync<BridgeUnavailableException>(() => generic.SendAsync<ExecuteResult>("etabs.ping", new { }, TimeSpan.FromSeconds(5), null, TestContext.Current.CancellationToken));
        var b = await Assert.ThrowsAsync<BridgeUnavailableException>(() => named.SendAsync<ExecuteResult>("etabs.ping", new { }, TimeSpan.FromSeconds(5), null, TestContext.Current.CancellationToken));

        Assert.StartsWith("ETABS bridge not connected. Open ETABS 22 and enable the HP MCP Bridge (pipe hpetabs-mcp-nobody-", a.Message);
        Assert.Equal("ETABS bridge not connected. " + NotConnectedHint, b.Message);
    }

    [Fact]
    public async Task Timeout_message_keeps_the_rollback_sentence_unless_the_profile_says_otherwise()
    {
        var generic = await TimeOutAsync(timeoutHint: null);
        var etabs = await TimeOutAsync(TimeoutHint);

        Assert.Contains("nothing has been committed until it does", generic.Message);
        Assert.StartsWith("ETABS did not answer within", etabs.Message);
        Assert.EndsWith(TimeoutHint, etabs.Message);
        Assert.DoesNotContain("nothing has been committed", etabs.Message);
    }

    private static async Task<BridgeTimeoutException> TimeOutAsync(string? timeoutHint)
    {
        var pipe = NewPipe();
        // The fake keeps reporting progress for ~1 s; the client gives up after 300 ms.
        var executor = new FakeRevitExecutor { ProgressSteps = 10, ProgressDelayMs = 100 };
        using var listener = new PipeListener(pipe, new RequestDispatcher(executor, new BridgeSettings { ExecutionEnabled = true }, "22", "ETABS"));
        listener.Start();
        await using var client = new RevitBridgeClient(Options.Create(PipeOptions(pipe)), NullLogger<RevitBridgeClient>.Instance, Etabs(timeoutHint: timeoutHint));

        var error = await Assert.ThrowsAsync<BridgeTimeoutException>(() => client.SendAsync<ExecuteResult>(
            "etabs.execute", new ExecuteRequest("return 1;", "none", false, 5, "slow", null), TimeSpan.FromMilliseconds(300), null, TestContext.Current.CancellationToken));
        await listener.StopAsync();
        return error;
    }

    [Fact]
    public async Task Context_shape_for_etabs_drops_revit_fields_and_keeps_the_etabs_block()
    {
        var pipe = NewPipe();
        var executor = new FakeRevitExecutor
        {
            ContextHandler = _ => new ContextResult
            {
                RevitVersion = "22", Host = "etabs", HostVersion = "22", DocTitle = "Tower.EDB", DocPath = "<path>",
                Etabs = new EtabsInfo(true, 1234, "2.10.0.0", false, "kN_mm_C", "kN_m_C", false, 120, 340, 60),
            },
        };
        using var listener = new PipeListener(pipe, new RequestDispatcher(executor, new BridgeSettings(), "22", "ETABS"));
        listener.Start();
        await using var client = new RevitBridgeClient(Options.Create(PipeOptions(pipe)), NullLogger<RevitBridgeClient>.Instance, Etabs());

        var text = await new ContextService(client, new ResultFormatter()).ReadAsync(false, TestContext.Current.CancellationToken);

        using var json = JsonDocument.Parse(text);
        var root = json.RootElement;
        Assert.False(root.TryGetProperty("revitVersion", out _));
        Assert.False(root.TryGetProperty("isFamily", out _));
        Assert.Equal("etabs", root.GetProperty("host").GetString());
        Assert.Equal(1234, root.GetProperty("etabs").GetProperty("attachedPid").GetInt32());
        Assert.Equal("kN_mm_C", root.GetProperty("etabs").GetProperty("presentUnits").GetString());
        Assert.False(root.TryGetProperty("navis", out _));
        Assert.False(root.TryGetProperty("autocad", out _));
        await listener.StopAsync();
    }

    [Fact]
    public async Task Analyze_forwards_the_declared_transaction_and_leaves_it_null_for_ad_hoc_callers()
    {
        var registry = new RegistrySandbox();
        var executor = new FakeRevitExecutor();
        using var listener = new PipeListener(registry.Pipe, new RequestDispatcher(executor, new BridgeSettings { ExecutionEnabled = true }, "22", "ETABS"));
        listener.Start();
        await using var client = new RevitBridgeClient(Options.Create(PipeOptions(registry.Pipe)), NullLogger<RevitBridgeClient>.Instance, Etabs());
        using var store = registry.Store();
        var db = registry.Db();
        var manager = registry.Manager(store, db, client);
        var lifecycle = new ToolLifecycleService(manager, client, NullLogger<ToolLifecycleService>.Instance);

        var adHoc = await lifecycle.AnalyzeAsync("return sapModel.GetModelFilename();", TestContext.Current.CancellationToken);
        Assert.NotNull(adHoc);
        Assert.Null(executor.LastAnalyzeRequest!.Transaction);

        var declared = await lifecycle.AnalyzeAsync("return sapModel.GetModelFilename();", TestContext.Current.CancellationToken, "none");
        Assert.NotNull(declared);
        Assert.Equal("none", executor.LastAnalyzeRequest!.Transaction);

        await listener.StopAsync();
        registry.Cleanup();
    }

    [Fact]
    public async Task A_bridge_refusal_with_execution_disabled_never_enters_the_tool_run_history()
    {
        // The ETABS bridge answers a destructive call while the second checkbox is off with this code; a refusal that never
        // ran must not count against the tool, or five "please tick the box" round trips would quarantine it.
        var registry = new RegistrySandbox();
        var executor = new FakeRevitExecutor
        {
            ExecuteHandler = _ => throw new BridgeRequestException(BridgeErrorCode.ExecutionDisabled,
                "Destructive operations are disabled. Ask the user to tick 'Allow destructive operations' in the HPEtabs MCP Bridge window."),
        };
        using var listener = new PipeListener(registry.Pipe, new RequestDispatcher(executor, new BridgeSettings { ExecutionEnabled = true }, "22", "ETABS"));
        listener.Start();
        await using var client = new RevitBridgeClient(Options.Create(PipeOptions(registry.Pipe)), NullLogger<RevitBridgeClient>.Instance, Etabs());
        using var store = registry.Store();
        var db = registry.Db();
        var manager = registry.Manager(store, db, client);
        var record = Candidate(600, transaction: "auto");
        record.Name = "run_analysis";
        record.Status = ToolStatus.Published;
        manager.Save(record, "published", "test");

        for (var attempt = 0; attempt < 6; attempt++)
        {
            var error = await Assert.ThrowsAsync<BridgeErrorException>(() => manager.RunAsync(record.Name, null, false, false, "run", TestContext.Current.CancellationToken));
            Assert.Equal(BridgeErrorCode.ExecutionDisabled, error.Code);
            Assert.Contains("Allow destructive operations", error.Message);
        }

        Assert.Equal(0, db.Stats(record.Name, registry.Options.RunWindow).Runs);
        Assert.True(manager.TryGet(record.Name, out var after));
        Assert.Equal(ToolStatus.Published, after!.Status);

        // Contrast: a script that ran and failed is a run the tool owns.
        executor.ExecuteHandler = _ => ExecuteResult.Failure("InvalidOperationException: ETABS returned 1 from RunAnalysis");
        var failed = await manager.RunAsync(record.Name, null, false, false, "run", TestContext.Current.CancellationToken);
        Assert.True(failed.IsError);
        Assert.Equal(1, db.Stats(record.Name, registry.Options.RunWindow).Runs);

        await listener.StopAsync();
        registry.Cleanup();
    }

    /// <summary>One throw-away registry root + pipe name per test; SQLite pools are cleared before the folder goes.</summary>
    private sealed class RegistrySandbox
    {
        private readonly string _root = Path.Combine(Path.GetTempPath(), "hpetabs-registry-" + Guid.NewGuid().ToString("N"));

        public string Pipe { get; } = NewPipe();

        public RegistryOptions Options => new RegistryOptions { LibraryPath = Path.Combine(_root, "lib"), DbPath = Path.Combine(_root, "registry.db"), WatchLibrary = false };

        public ToolLibraryStore Store()
        {
            var store = new ToolLibraryStore(Options.LibraryPath, NullLogger<ToolLibraryStore>.Instance);
            store.EnsureRoot();
            return store;
        }

        public ToolRegistryDb Db()
        {
            var db = new ToolRegistryDb(Options.DbPath, NullLogger<ToolRegistryDb>.Instance);
            db.Initialize();
            return db;
        }

        public ToolManager Manager(ToolLibraryStore store, ToolRegistryDb db, IRevitBridgeClient client) =>
            new ToolManager(Microsoft.Extensions.Options.Options.Create(Options), Microsoft.Extensions.Options.Options.Create(new BridgeOptions()), store, db, client, NullLogger<ToolManager>.Instance);

        public void Cleanup()
        {
            Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
            try { Directory.Delete(_root, true); } catch (IOException) { /* best effort */ }
        }
    }
}
