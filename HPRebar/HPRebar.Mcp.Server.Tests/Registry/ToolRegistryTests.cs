using System.Text.Json;
using HPRebar.Mcp.Contracts.Messages;
using HPRebar.Mcp.Server.Models;
using HPRebar.Mcp.Server.Registry;
using HPRebar.Mcp.Server.Registry.Model;
using HPRebar.Mcp.Server.Services;
using HPRebar.Mcp.Server.Tests.Fakes;
using HPRebar.McpBridge.Core.Model;
using HPRebar.McpBridge.Core.Pipe;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Server;
using Xunit;

namespace HPRebar.Mcp.Server.Tests.Registry;

/// <summary>Temp library + temp database + the real pipe with a fake executor: the registry without Revit.</summary>
public sealed class RegistryFixture : IAsyncDisposable
{
    /// <summary>Seeds are embedded in the Revit exe, not in the engine assembly the fixture exercises.</summary>
    public static readonly System.Reflection.Assembly RevitSeeds = typeof(Hosts.Revit.ExecuteRevitCodeTool).Assembly;

    public readonly string Root = Path.Combine(Path.GetTempPath(), "hprebar-registry-" + Guid.NewGuid().ToString("N"));
    public readonly FakeRevitExecutor Executor = new();
    public readonly BridgeSettings Settings = new() { ExecutionEnabled = true };
    public readonly RegistryOptions Options;
    public readonly ToolLibraryStore Store;
    public readonly ToolRegistryDb Db;
    public readonly ToolManager Manager;
    private readonly PipeListener _listener;
    private readonly RevitBridgeClient _client;

    public IRevitBridgeClient Bridge => _client;

    public RegistryFixture(Action<RegistryOptions>? configure = null)
    {
        Options = new RegistryOptions { LibraryPath = Path.Combine(Root, "lib"), DbPath = Path.Combine(Root, "registry.db"), WatchLibrary = false };
        configure?.Invoke(Options);

        var pipe = "hprebar-mcp-test-" + Guid.NewGuid().ToString("N");
        _listener = new PipeListener(pipe, new RequestDispatcher(Executor, Settings, "2026"));
        _listener.Start();
        _client = new RevitBridgeClient(Microsoft.Extensions.Options.Options.Create(new BridgeOptions { PipeName = pipe, ConnectTimeoutMs = 3000, PingIntervalSeconds = 60 }), NullLogger<RevitBridgeClient>.Instance);

        Store = new ToolLibraryStore(Options.LibraryPath, NullLogger<ToolLibraryStore>.Instance);
        Store.EnsureRoot();
        Db = new ToolRegistryDb(Options.DbPath, NullLogger<ToolRegistryDb>.Instance);
        Db.Initialize();
        Manager = new ToolManager(Microsoft.Extensions.Options.Options.Create(Options), Microsoft.Extensions.Options.Options.Create(new BridgeOptions()), Store, Db, _client, NullLogger<ToolManager>.Instance);
    }

    public async ValueTask DisposeAsync()
    {
        await _client.DisposeAsync();
        await _listener.StopAsync();
        _listener.Dispose();
        Store.Dispose();
        Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
        try { Directory.Delete(Root, true); } catch (IOException) { /* best effort */ }
    }

    public static ToolRecord NewRecord(string name, ToolStatus status = ToolStatus.Published, string transaction = "auto") => new()
    {
        Name = name,
        Title = name.Replace('_', ' '),
        Description = $"Test tool {name} that counts things",
        Category = "Generic",
        Tags = ["test"],
        Status = status,
        Transaction = transaction,
        TimeoutSeconds = 30,
        Destructive = transaction != "none",
        InputSchema = JsonSerializer.Deserialize<JsonElement>("""{"type":"object","properties":{"count":{"type":"integer"}},"required":["count"]}"""),
        Code = "return args.Int(\"count\") * 2;",
        Examples = [new ToolExample { Title = "two", Args = JsonSerializer.Deserialize<JsonElement>("""{"count":2}""") }],
        Author = "test",
    };
}

public sealed class ToolLibraryStoreTests
{
    [Fact]
    public async Task Write_then_read_round_trips_and_checksums()
    {
        await using var f = new RegistryFixture();
        var record = RegistryFixture.NewRecord("count_things");

        var folder = f.Store.Write(record);
        var read = Assert.Single(f.Store.ReadAll());

        Assert.Equal(Path.Combine(f.Store.Root, "Generic", "count_things"), folder);
        Assert.Equal("count_things", read.Name);
        Assert.Equal(ToolStatus.Published, read.Status);
        Assert.Equal(record.Code.Trim(), read.Code.Trim());
        Assert.Single(read.Examples);
        Assert.Equal(2, read.Examples[0].Args.GetProperty("count").GetInt32());
        Assert.Equal(record.Checksum, read.Checksum);
        Assert.Contains("\"status\": \"published\"", File.ReadAllText(Path.Combine(folder, "tool.json")));
        Assert.True(f.Store.Exists("count_things"));
        Assert.Null(f.Store.FindFolder("nope"));
    }

    [Fact]
    public async Task Moving_category_removes_the_old_folder()
    {
        await using var f = new RegistryFixture();
        var record = RegistryFixture.NewRecord("mover");
        var oldFolder = f.Store.Write(record);

        record.Category = "Structure";
        var newFolder = f.Store.Write(record);

        Assert.False(Directory.Exists(oldFolder));
        Assert.True(File.Exists(Path.Combine(newFolder, "tool.json")));
        Assert.Single(f.Store.ReadAll());
    }

    [Fact]
    public async Task Seeds_install_once_and_never_overwrite()
    {
        await using var f = new RegistryFixture();

        var first = SeedInstaller.Install(f.Store, NullLogger.Instance, RegistryFixture.RevitSeeds);
        Assert.Equal(21, first.Count);
        Assert.Equal(21, f.Store.ReadAll().Count);

        var marker = Path.Combine(f.Store.FindFolder("create_grid")!, "code.cs");
        File.WriteAllText(marker, "return 1;");
        var second = SeedInstaller.Install(f.Store, NullLogger.Instance, RegistryFixture.RevitSeeds);

        Assert.Empty(second);
        Assert.Equal("return 1;", File.ReadAllText(marker));
        Assert.True(File.Exists(Path.Combine(f.Store.Root, SeedInstaller.ManifestFile)));
    }

    [Fact]
    public async Task Untouched_seed_is_upgraded_when_the_shipped_version_changes()
    {
        await using var f = new RegistryFixture();
        SeedInstaller.Install(f.Store, NullLogger.Instance, RegistryFixture.RevitSeeds);
        var manifestPath = Path.Combine(f.Store.Root, SeedInstaller.ManifestFile);
        var manifest = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(manifestPath), RegistryJson.Options)!;

        // simulate an older install: the folder holds "old" code and the manifest recorded exactly that
        var folder = f.Store.FindFolder("create_grid")!;
        File.WriteAllText(Path.Combine(folder, "code.cs"), "return \"old\";");
        manifest["create_grid"] = f.Store.TryRead(folder)!.Checksum;
        File.WriteAllText(manifestPath, System.Text.Json.JsonSerializer.Serialize(manifest, RegistryJson.Options));

        var written = SeedInstaller.Install(f.Store, NullLogger.Instance, RegistryFixture.RevitSeeds);

        Assert.Equal(["create_grid"], written);
        Assert.DoesNotContain("return \"old\";", File.ReadAllText(Path.Combine(folder, "code.cs")));

        // a user edit on top of the recorded install is never overwritten
        File.WriteAllText(Path.Combine(folder, "code.cs"), "return \"mine\";");
        Assert.Empty(SeedInstaller.Install(f.Store, NullLogger.Instance, RegistryFixture.RevitSeeds));
        Assert.Equal("return \"mine\";", File.ReadAllText(Path.Combine(folder, "code.cs")));
    }

    [Theory]
    [InlineData("Create Grid", "Create Grid")]
    [InlineData("a/b\\c:d", "a_b_c_d")]
    [InlineData("..", "_")]
    public void Sanitize_keeps_folder_names_inside_the_root(string input, string expected) => Assert.Equal(expected, ToolLibraryStore.Sanitize(input));
}

public sealed class ToolRegistryDbTests
{
    [Fact]
    public async Task Upsert_search_and_runs()
    {
        await using var f = new RegistryFixture();
        foreach (var name in new[] { "create_grid_system", "color_beams_by_type", "export_room_data" })
        {
            var r = RegistryFixture.NewRecord(name);
            r.Description = name switch
            {
                "create_grid_system" => "Create a rectangular grid of axes with spacing and labels",
                "color_beams_by_type" => "Colour structural framing beams in the view by their type name",
                _ => "Export rooms with area and level",
            };
            f.Db.UpsertTool(r);
        }

        Assert.True(f.Db.HasFullTextSearch, "bundled e_sqlite3 should ship FTS5");
        var hits = f.Db.Search("grid spacing", 5);
        Assert.Equal("create_grid_system", hits[0].Name);
        Assert.Contains(f.Db.Search("beams colour", 5), h => h.Name == "color_beams_by_type");
        Assert.Empty(f.Db.Search("!!!", 5));
        Assert.Equal(3, f.Db.ToolNames().Count);

        var id = f.Db.InsertRun(new RunRecord { ToolName = "create_grid_system", Version = 1, Kind = RunRecord.KindTool, Success = true, DurationMs = 12 });
        f.Db.InsertRun(new RunRecord { ToolName = "create_grid_system", Version = 1, Kind = RunRecord.KindTool, Success = false, Error = "boom" });
        f.Db.InsertRun(new RunRecord { ToolName = "create_grid_system", Version = 1, Kind = RunRecord.KindTest, Success = false, Error = "test failure does not count" });

        Assert.True(id > 0);
        var stats = f.Db.Stats("create_grid_system", 50);
        Assert.Equal(2, stats.Runs);
        Assert.Equal(1, stats.Successes);
        Assert.Equal("boom", stats.LastError);
        Assert.Equal(2, f.Db.AllStats(50)["create_grid_system"].Runs);
        Assert.Equal(3, f.Db.RecentRuns("create_grid_system", 10).Count);
        Assert.Equal(RunStats.Empty, f.Db.Stats("export_room_data", 50));

        Assert.Equal(1, f.Db.RemoveToolsNotIn(["create_grid_system", "color_beams_by_type"]));
        Assert.Equal(2, f.Db.ToolNames().Count);
    }

    [Fact]
    public async Task Adhoc_code_is_pruned_beyond_the_keep_limit()
    {
        await using var f = new RegistryFixture();
        for (var i = 0; i < 5; i++) f.Db.InsertRun(new RunRecord { Kind = RunRecord.KindAdhoc, Success = true, Code = "return " + i + ";", Timestamp = DateTimeOffset.UtcNow.AddSeconds(i) });

        Assert.Equal(3, f.Db.PruneAdhocCode(2));
        Assert.Null(f.Db.GetRun(1)!.Code);
        Assert.Equal("return 4;", f.Db.GetRun(5)!.Code);
    }

    [Theory]
    [InlineData("tạo lưới trục A-B", new[] { "tạo", "lưới", "trục" })]
    [InlineData("Color (beams) by \"type\"!", new[] { "color", "beams", "by", "type" })]
    [InlineData("a b", new string[0])]
    public void Tokenize_strips_operators(string query, string[] expected) => Assert.Equal(expected, ToolRegistryDb.Tokenize(query));
}

public sealed class StabilityScorerTests
{
    [Theory]
    [InlineData(0, 0, 0)]
    [InlineData(2, 2, 0.2)]
    [InlineData(10, 10, 1)]
    [InlineData(20, 15, 0.75)]
    public void Score_ramps_with_run_count(int runs, int successes, double expected) => Assert.Equal(expected, StabilityScorer.Score(new RunStats(runs, successes, null, null)));

    [Fact]
    public void Quarantine_needs_min_runs_and_failure_rate()
    {
        Assert.False(StabilityScorer.ShouldQuarantine(new RunStats(4, 0, null, null), 5, 0.4));
        Assert.False(StabilityScorer.ShouldQuarantine(new RunStats(5, 3, null, null), 5, 0.4));
        Assert.True(StabilityScorer.ShouldQuarantine(new RunStats(5, 2, null, null), 5, 0.4));
    }

    [Fact]
    public void Published_outranks_draft_with_equal_text_score()
    {
        var stats = new RunStats(10, 10, null, null);
        Assert.True(StabilityScorer.Rank(1, ToolStatus.Published, stats) > StabilityScorer.Rank(1, ToolStatus.Draft, stats));
        Assert.Equal(0, StabilityScorer.Rank(1, ToolStatus.Quarantined, stats));
    }
}

public sealed class ToolManagerTests
{
    [Fact]
    public async Task Load_search_and_run_a_stored_tool_through_the_pipe()
    {
        await using var f = new RegistryFixture();
        SeedInstaller.Install(f.Store, NullLogger.Instance, RegistryFixture.RevitSeeds);
        var changes = 0;
        f.Manager.Changed += () => changes++;

        Assert.Equal(21, await f.Manager.LoadAllAsync(TestContext.Current.CancellationToken));
        Assert.Equal(1, changes);
        Assert.Equal(21, f.Db.ToolNames().Count);

        var hits = f.Manager.Search("grid spacing labels", null, 3, false);
        Assert.Equal("create_grid", hits[0].Name);
        Assert.Equal("published", hits[0].Status);
        Assert.Equal(JsonValueKind.Object, hits[0].InputSchema.ValueKind);
        Assert.All(f.Manager.Search(null, "Annotation", 10, false), h => Assert.Equal("Annotation", h.Category));

        f.Executor.ExecuteHandler = request => new ExecuteResult { Value = request.Args, ValueType = "echo", DurationMs = 7 };
        var args = JsonSerializer.SerializeToElement(new { limit = 3 });
        var result = await f.Manager.RunAsync("get_selected_elements", args, dryRun: false, allowUnpublished: false, RunRecord.KindTool, TestContext.Current.CancellationToken);

        Assert.False(result.IsError);
        var sent = f.Executor.LastExecuteRequest!;
        Assert.Equal(f.Manager.Get("get_selected_elements")!.Record.Code, sent.Code);
        Assert.Equal("none", sent.Transaction);
        Assert.Equal("get_selected_elements", sent.Label);
        Assert.Equal(3, sent.Args!.Value.GetProperty("limit").GetInt32());

        var details = f.Manager.Get("get_selected_elements")!;
        Assert.Equal(1, details.Stats.Runs);
        Assert.Equal(0.1, details.Stability);
        Assert.Equal("tool", details.RecentRuns[0].Kind);
    }

    [Fact]
    public async Task Tools_without_a_host_load_and_tools_of_another_host_are_skipped()
    {
        await using var f = new RegistryFixture();
        var legacy = RegistryFixture.NewRecord("legacy_tool");
        legacy.Host = null;                                  // every tool.json written before the field existed
        var foreign = RegistryFixture.NewRecord("foreign_tool");
        foreign.Host = "autocad";                            // copied by hand from another host's library
        f.Store.Write(legacy);
        f.Store.Write(foreign);

        Assert.Equal(1, await f.Manager.LoadAllAsync(TestContext.Current.CancellationToken));
        Assert.True(f.Manager.TryGet("legacy_tool", out var loaded));
        Assert.Equal("revit", loaded.Host);
        Assert.False(f.Manager.TryGet("foreign_tool", out _));
        // Loading never rewrites the file: a legacy tool.json stays without a host key until it is saved again.
        Assert.DoesNotContain("\"host\"", File.ReadAllText(Path.Combine(f.Store.FindFolder("legacy_tool")!, ToolLibraryStore.ToolFile)));
    }

    [Fact]
    public async Task Unpublished_and_deprecated_tools_are_gated()
    {
        await using var f = new RegistryFixture();
        f.Store.Write(RegistryFixture.NewRecord("draft_tool", ToolStatus.Draft));
        f.Store.Write(RegistryFixture.NewRecord("old_tool", ToolStatus.Deprecated));
        await f.Manager.LoadAllAsync(TestContext.Current.CancellationToken);

        await Assert.ThrowsAsync<ToolNotRunnableException>(() => f.Manager.RunAsync("draft_tool", null, false, false, RunRecord.KindTool, TestContext.Current.CancellationToken));
        await Assert.ThrowsAsync<ToolNotRunnableException>(() => f.Manager.RunAsync("old_tool", null, false, true, RunRecord.KindTool, TestContext.Current.CancellationToken));
        await Assert.ThrowsAsync<ToolNotFoundException>(() => f.Manager.RunAsync("missing", null, false, true, RunRecord.KindTool, TestContext.Current.CancellationToken));

        var ok = await f.Manager.RunAsync("draft_tool", null, false, true, RunRecord.KindTool, TestContext.Current.CancellationToken);
        Assert.False(ok.IsError);
        Assert.Empty(f.Manager.Search("count", null, 5, false));
        Assert.Single(f.Manager.Search("count", null, 5, true), h => h.Name == "draft_tool");
    }

    [Fact]
    public async Task Repeated_failures_quarantine_a_published_tool()
    {
        await using var f = new RegistryFixture(o => { o.QuarantineMinRuns = 3; o.QuarantineMaxFailureRate = 0.5; });
        f.Store.Write(RegistryFixture.NewRecord("fragile"));
        await f.Manager.LoadAllAsync(TestContext.Current.CancellationToken);
        var changes = 0;
        f.Manager.Changed += () => changes++;
        f.Executor.ExecuteHandler = _ => ExecuteResult.Failure("NullReferenceException: no room");

        for (var i = 0; i < 3; i++) await f.Manager.RunAsync("fragile", null, false, false, RunRecord.KindTool, TestContext.Current.CancellationToken);

        Assert.Equal(1, changes);
        Assert.Equal(ToolStatus.Quarantined, f.Manager.Get("fragile")!.Record.Status);
        Assert.Contains("\"status\": \"quarantined\"", File.ReadAllText(Path.Combine(f.Store.FindFolder("fragile")!, "tool.json")));
        await Assert.ThrowsAsync<ToolNotRunnableException>(() => f.Manager.RunAsync("fragile", null, false, false, RunRecord.KindTool, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Argument_errors_are_the_callers_and_never_quarantine_a_tool()
    {
        await using var f = new RegistryFixture(o => { o.QuarantineMinRuns = 3; o.QuarantineMaxFailureRate = 0.5; });
        f.Store.Write(RegistryFixture.NewRecord("strict"));
        await f.Manager.LoadAllAsync(TestContext.Current.CancellationToken);
        f.Executor.ExecuteHandler = _ => ExecuteResult.Failure("ArgumentException: Block 'NOPE' is not defined in this drawing.");

        for (var i = 0; i < 5; i++) await f.Manager.RunAsync("strict", null, false, false, RunRecord.KindTool, TestContext.Current.CancellationToken);

        Assert.Equal(ToolStatus.Published, f.Manager.Get("strict")!.Record.Status);
        Assert.Equal(RunStats.Empty, f.Db.Stats("strict", 50));                       // wrong calls are not evidence about the tool
        Assert.DoesNotContain("strict", f.Db.AllStats(50).Keys);
        Assert.Equal(5, f.Db.RecentRuns("strict", 10).Count);                       // but they stay in the history

        f.Executor.ExecuteHandler = _ => ExecuteResult.Failure("NullReferenceException: no room");
        for (var i = 0; i < 3; i++) await f.Manager.RunAsync("strict", null, false, false, RunRecord.KindTool, TestContext.Current.CancellationToken);
        Assert.Equal(ToolStatus.Quarantined, f.Manager.Get("strict")!.Record.Status);
    }

    [Fact]
    public async Task A_restored_tool_starts_with_a_clean_stability_window()
    {
        await using var f = new RegistryFixture(o => { o.QuarantineMinRuns = 3; o.QuarantineMaxFailureRate = 0.5; });
        f.Store.Write(RegistryFixture.NewRecord("flaky"));
        await f.Manager.LoadAllAsync(TestContext.Current.CancellationToken);
        var lifecycle = new ToolLifecycleService(f.Manager, f.Bridge, NullLogger<ToolLifecycleService>.Instance);
        f.Executor.ExecuteHandler = _ => ExecuteResult.Failure("NullReferenceException: no room");
        for (var i = 0; i < 3; i++) await f.Manager.RunAsync("flaky", null, false, false, RunRecord.KindTool, TestContext.Current.CancellationToken);
        Assert.Equal(ToolStatus.Quarantined, f.Manager.Get("flaky")!.Record.Status);

        await Task.Delay(5, TestContext.Current.CancellationToken); // events and runs share a millisecond clock
        lifecycle.Manage("flaky", "restore", "fixed the model", "tester");
        lifecycle.Approve("flaky", "tester", force: true);
        Assert.Equal(RunStats.Empty, f.Db.Stats("flaky", 50));                    // the old failures no longer count

        f.Executor.ExecuteHandler = _ => new ExecuteResult { Value = System.Text.Json.JsonSerializer.SerializeToElement(1) };
        await f.Manager.RunAsync("flaky", null, false, false, RunRecord.KindTool, TestContext.Current.CancellationToken);
        Assert.Equal(ToolStatus.Published, f.Manager.Get("flaky")!.Record.Status);
        Assert.Equal(1, f.Db.Stats("flaky", 50).Runs);
    }

    [Fact]
    public async Task A_status_edited_by_hand_in_tool_json_also_starts_a_clean_window()
    {
        await using var f = new RegistryFixture(o => { o.QuarantineMinRuns = 3; o.QuarantineMaxFailureRate = 0.5; });
        f.Store.Write(RegistryFixture.NewRecord("hand_edited"));
        await f.Manager.LoadAllAsync(TestContext.Current.CancellationToken);
        f.Executor.ExecuteHandler = _ => ExecuteResult.Failure("NullReferenceException: no room");
        for (var i = 0; i < 3; i++) await f.Manager.RunAsync("hand_edited", null, false, false, RunRecord.KindTool, TestContext.Current.CancellationToken);
        Assert.Equal(ToolStatus.Quarantined, f.Manager.Get("hand_edited")!.Record.Status);

        // The second documented approval path: a human sets "status": "published" in tool.json and the watcher reloads.
        await Task.Delay(5, TestContext.Current.CancellationToken);
        var toolJson = Path.Combine(f.Store.FindFolder("hand_edited")!, "tool.json");
        File.WriteAllText(toolJson, File.ReadAllText(toolJson).Replace("\"status\": \"quarantined\"", "\"status\": \"published\""));
        await f.Manager.LoadAllAsync(TestContext.Current.CancellationToken);

        Assert.Equal(ToolStatus.Published, f.Manager.Get("hand_edited")!.Record.Status);
        Assert.Equal(RunStats.Empty, f.Db.Stats("hand_edited", 50));
        f.Executor.ExecuteHandler = _ => new ExecuteResult { Value = JsonSerializer.SerializeToElement(1) };
        await f.Manager.RunAsync("hand_edited", null, false, false, RunRecord.KindTool, TestContext.Current.CancellationToken);
        Assert.Equal(ToolStatus.Published, f.Manager.Get("hand_edited")!.Record.Status);
    }

    [Fact]
    public async Task Adhoc_runs_are_remembered_with_code_only_on_success()
    {
        await using var f = new RegistryFixture();
        var ok = f.Manager.RecordAdhoc("return 1;", null, false, new ExecuteResult { DurationMs = 3 });
        var bad = f.Manager.RecordAdhoc("return x;", null, false, ExecuteResult.Failure("compile"));

        Assert.Equal("return 1;", f.Db.GetRun(ok)!.Code);
        Assert.Null(f.Db.GetRun(bad)!.Code);
        Assert.False(f.Db.GetRun(bad)!.Success);
    }
}

public sealed class DynamicToolRegistrarTests
{
    [Fact]
    public async Task Sync_registers_published_tools_only_and_tracks_status_changes()
    {
        await using var f = new RegistryFixture();
        f.Store.Write(RegistryFixture.NewRecord("pub_tool"));
        f.Store.Write(RegistryFixture.NewRecord("draft_tool", ToolStatus.Draft));
        await f.Manager.LoadAllAsync(TestContext.Current.CancellationToken);

        var serverOptions = new McpServerOptions();
        var registrar = new DynamicToolRegistrar(f.Manager, new ResultFormatter(), Microsoft.Extensions.Options.Options.Create(serverOptions), NullLogger<DynamicToolRegistrar>.Instance);
        var changed = 0;
        registrar.Collection.Changed += (_, _) => changed++;

        Assert.Equal(1, registrar.Sync());
        Assert.Equal(["pub_tool"], registrar.RegisteredNames);
        Assert.Equal(1, serverOptions.ToolCollection!.Count);
        Assert.Equal(1, changed);

        var record = f.Manager.Get("pub_tool")!.Record;
        record.Status = ToolStatus.Deprecated;
        f.Manager.Save(record, "deprecated", "test");
        Assert.Equal(0, registrar.Sync());
        Assert.Empty(serverOptions.ToolCollection);
        Assert.Equal(2, changed);
        Assert.Equal(0, registrar.Sync()); // no-op, no extra notification
        Assert.Equal(2, changed);
    }

    [Fact]
    public async Task Function_schema_adds_dry_run_for_writing_tools_and_invokes_the_manager()
    {
        await using var f = new RegistryFixture();
        f.Store.Write(RegistryFixture.NewRecord("writer"));
        f.Store.Write(RegistryFixture.NewRecord("reader", transaction: "none"));
        await f.Manager.LoadAllAsync(TestContext.Current.CancellationToken);
        var formatter = new ResultFormatter();

        var writer = new RegistryToolFunction(f.Manager.Get("writer")!.Record, f.Manager, formatter);
        var reader = new RegistryToolFunction(f.Manager.Get("reader")!.Record, f.Manager, formatter);
        Assert.True(writer.JsonSchema.GetProperty("properties").TryGetProperty("dryRun", out _));
        Assert.False(reader.JsonSchema.GetProperty("properties").TryGetProperty("dryRun", out _));
        Assert.Equal("count", writer.JsonSchema.GetProperty("required")[0].GetString());

        f.Executor.ExecuteHandler = request => new ExecuteResult { Value = JsonSerializer.SerializeToElement(new { dry = request.DryRun, count = request.Args!.Value.GetProperty("count").GetInt32() }) };
        var arguments = new AIFunctionArguments { ["count"] = JsonSerializer.SerializeToElement(4), ["dryRun"] = JsonSerializer.SerializeToElement(true) };
        var result = Assert.IsType<ModelContextProtocol.Protocol.CallToolResult>(await writer.InvokeAsync(arguments, TestContext.Current.CancellationToken));

        Assert.False(result.IsError);
        var text = ((ModelContextProtocol.Protocol.TextContentBlock)result.Content[0]).Text;
        Assert.Contains("\"dry\":true", text);
        Assert.Contains("\"count\":4", text);
        Assert.True(f.Executor.LastExecuteRequest!.DryRun);
        Assert.False(f.Executor.LastExecuteRequest.Args!.Value.TryGetProperty("dryRun", out _));
    }
}
