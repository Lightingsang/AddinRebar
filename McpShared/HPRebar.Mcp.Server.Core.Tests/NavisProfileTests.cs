using System.Text.Json;
using HPRebar.Mcp.Contracts;
using HPRebar.Mcp.Contracts.JsonRpc;
using HPRebar.Mcp.Contracts.Messages;
using HPRebar.Mcp.Server.Hosts;
using HPRebar.Mcp.Server.Models;
using HPRebar.Mcp.Server.Registry;
using HPRebar.Mcp.Server.Registry.Model;
using HPRebar.Mcp.Server.Services;
using HPRebar.Mcp.Server.Tests.Fakes;
using HPRebar.McpBridge.Core.Model;
using HPRebar.McpBridge.Core.Pipe;
using HPRebar.McpBridge.Core.Scripting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace HPRebar.Mcp.Server.Tests;

/// <summary>
///     What the engine gained for a third host (Navisworks) and what it must keep for the first two: the
///     `navis` constants, the Navis guard/analyzer profiles, the per-profile timeout ceiling that stays
///     120 for Revit and AutoCAD and reaches every clamp, and a context shape that stays clean.
/// </summary>
public sealed class NavisProfileTests
{
    private static HostProfile Navis(int maxTimeout = 600) => new HostProfile
    {
        HostId = PipeNaming.NavisHost, DisplayName = "Navisworks", ServerName = "test", ProductFolder = "HPNavisTest", EnvPrefix = "X_",
        DefaultVersion = 2026, ValidVersions = new[] { 2026 }, MethodPrefix = JsonRpcMethods.NavisPrefix,
        ExecuteToolName = "execute_navis_code", ContextToolName = "get_navis_context", ResourceScheme = "navis",
        Categories = new[] { "Model", "Search", "Selection", "Viewpoint", "Clash", "Timeliner", "Report", "Data", "Generic" },
        CoreToolNames = new[] { "execute_navis_code", "get_navis_context", "inspect_type", "cancel_execution" },
        ScriptImports = HostScriptContracts.NavisImports, ScriptContractSummary = "test", HostAssembly = typeof(NavisProfileTests).Assembly,
        MaxTimeoutSeconds = maxTimeout,
    };

    private static ToolRecord Candidate(int timeoutSeconds) => new ToolRecord
    {
        Name = "get_model_info", Title = "Model info", Description = "Lists the appended models and the document units.",
        Category = "Model", Transaction = "none", TimeoutSeconds = timeoutSeconds, Host = "navis",
        Code = "return doc.Title;",
        InputSchema = JsonSerializer.Deserialize<JsonElement>("""{"type":"object","properties":{}}"""),
        Examples = [new ToolExample { Title = "default", Args = JsonSerializer.Deserialize<JsonElement>("{}") }],
    };

    [Fact]
    public void Navis_constants_produce_the_pipe_and_prefix_the_generic_branch_already_did()
    {
        Assert.Equal("hpnavis-mcp-2026", PipeNaming.For(PipeNaming.NavisHost, 2026));
        Assert.Equal("hpnavis-mcp-2026", PipeNaming.For("NAVIS", 2026));
        Assert.Equal("navis.execute", JsonRpcMethods.For(JsonRpcMethods.NavisPrefix, JsonRpcMethods.ExecuteSuffix));
        Assert.Equal("execute", JsonRpcMethods.Suffix("navis.execute"));
        Assert.Equal("hpnavis-mcp-2026", new BridgeOptions { HostId = "navis", HostVersion = 2026 }.PipeName);
        Assert.Contains("Autodesk.Navisworks.Api", HostScriptContracts.NavisImports);
        Assert.DoesNotContain("Autodesk.Navisworks.Api.ApplicationParts", HostScriptContracts.NavisImports);
        Assert.Equal(["doc", "app", "units", "ct", "log", "progress", "args"], HostScriptContracts.NavisGlobals);
    }

    [Theory]
    [InlineData("var t = doc.BeginTransaction(\"x\"); return 1;", ".BeginTransaction")]
    [InlineData("using (var t = new Transaction(doc, \"x\")) { } return 1;", "Transaction is not allowed")]
    [InlineData("doc.Undo(); return 1;", ".Undo")]
    [InlineData("doc.Rollback(); return 1;", ".Rollback")]
    [InlineData("var c = doc.Database.ToNavisworksConnection(); return 1;", ".Database")]
    [InlineData("var cmd = new NavisworksCommand(\"select 1\"); return 1;", "NavisworksCommand")]
    [InlineData("using System.Data; return 1;", "System.Data")]
    [InlineData("var e = Expression.Call(Expression.Constant(doc), \"SaveFile\", null); return 1;", "Expression")]
    [InlineData("var d = Delegate.CreateDelegate(typeof(Action), doc, \"SaveFile\"); return 1;", "Delegate")]
    [InlineData("MessageBox.Show(\"hi\"); return 1;", "MessageBox")]
    [InlineData("var a = new Autodesk.Navisworks.Api.Automation.NavisworksApplication(); return 1;", "Autodesk.Navisworks.Api.Automation")]
    public void Navis_guard_profile_denies_undo_transactions_sql_expressions_and_automation(string code, string expected)
    {
        var violations = ScriptGuard.Check(code, GuardProfile.Navis);

        Assert.NotEmpty(violations);
        Assert.Contains(violations, v => v.Message.Contains(expected, StringComparison.Ordinal));
        Assert.All(violations, v => Assert.Contains("Navisworks", v.Message));
    }

    [Fact]
    public void Navis_guard_profile_lets_review_metadata_and_heavy_file_calls_through()
    {
        // Heavy file/clash operations are the Navisworks bridge's business (its own opt-in and pre-pass), not the guard's.
        const string fine =
            "var search = new Search(); search.Selection.SelectAll();\n" +
            "var items = search.FindAll(doc, false);\n" +
            "doc.SelectionSets.AddCopy(new SelectionSet(items) { DisplayName = args.Str(\"name\", \"MCP\") });\n" +
            "doc.AppendFile(args.Require(\"path\"));\n" +
            "return items.Count;";

        Assert.Empty(ScriptGuard.Check(fine, GuardProfile.Navis));
        Assert.Empty(ScriptGuard.Check("return doc.Title;", GuardProfile.Navis));
    }

    [Fact]
    public void Navis_analyzer_profile_flags_both_ways_of_opening_a_transaction()
    {
        Assert.True(ScriptAnalyzer.Analyze("var t = doc.BeginTransaction(\"a\"); return 1;", AnalyzerProfile.Navis).UsesTransaction);
        Assert.True(ScriptAnalyzer.Analyze("using (var t = new Transaction(doc, \"a\")) { } return 1;", AnalyzerProfile.Navis).UsesTransaction);
        Assert.False(ScriptAnalyzer.Analyze("return doc.Title;", AnalyzerProfile.Navis).UsesTransaction);
        // Revit and AutoCAD analyzers are untouched: BeginTransaction means nothing to them.
        Assert.False(ScriptAnalyzer.Analyze("var t = doc.BeginTransaction(\"a\"); return 1;", AnalyzerProfile.Revit).UsesTransaction);
        Assert.False(ScriptAnalyzer.Analyze("var t = doc.BeginTransaction(\"a\"); return 1;", AnalyzerProfile.Autocad).UsesTransaction);
    }

    [Fact]
    public void Timeout_ceiling_defaults_to_120_and_survives_WithHostAssembly()
    {
        Assert.Equal(120, HostProfile.Revit.MaxTimeoutSeconds);
        Assert.Equal(120, new HostProfile
        {
            HostId = "x", DisplayName = "x", ServerName = "x", ProductFolder = "x", EnvPrefix = "X_", DefaultVersion = 1, ValidVersions = new[] { 1 },
            MethodPrefix = "x.", ExecuteToolName = "e", ContextToolName = "c", ResourceScheme = "x", Categories = new[] { "Generic" },
            CoreToolNames = new[] { "e" }, ScriptImports = new[] { "System" }, ScriptContractSummary = "x", HostAssembly = typeof(NavisProfileTests).Assembly,
        }.MaxTimeoutSeconds);
        Assert.Equal(600, Navis().WithHostAssembly(typeof(HostProfile).Assembly).MaxTimeoutSeconds);
    }

    [Fact]
    public void Timeout_ceiling_below_the_minimum_is_refused_at_construction()
    {
        var error = Assert.Throws<ArgumentOutOfRangeException>(() => Navis(maxTimeout: 4));

        Assert.Equal("MaxTimeoutSeconds", error.ParamName);
        Assert.Equal(5, Navis(maxTimeout: 5).MaxTimeoutSeconds);
    }

    [Fact]
    public void Validator_ceiling_follows_the_profile()
    {
        var revitLike = Navis(maxTimeout: 120);

        Assert.Contains(ToolValidator.Validate(Candidate(600), null, [], false, revitLike).Errors, e => e.Contains("between 5 and 120"));
        Assert.DoesNotContain(ToolValidator.Validate(Candidate(600), null, [], false, Navis()).Errors, e => e.Contains("timeoutSeconds"));
        Assert.Contains(ToolValidator.Validate(Candidate(601), null, [], false, Navis()).Errors, e => e.Contains("between 5 and 600"));
        Assert.Contains(ToolValidator.Validate(Candidate(4), null, [], false, Navis()).Errors, e => e.Contains("between 5 and 600"));
    }

    [Theory]
    [InlineData(120, 600, 120)]
    [InlineData(600, 600, 600)]
    [InlineData(601, 600, 600)]
    [InlineData(600, 120, 120)]
    public async Task Execute_service_clamps_to_the_profile_ceiling(int requested, int ceiling, int expected)
    {
        var pipe = "hpnavis-mcp-test-" + Guid.NewGuid().ToString("N");
        var executor = new FakeRevitExecutor();
        using var listener = new PipeListener(pipe, new RequestDispatcher(executor, new BridgeSettings { ExecutionEnabled = true }, "2026", "Navisworks"));
        listener.Start();
        var options = Options.Create(new BridgeOptions { PipeName = pipe, ConnectTimeoutMs = 3000, PingIntervalSeconds = 60 });
        await using var client = new RevitBridgeClient(options, NullLogger<RevitBridgeClient>.Instance, Navis(ceiling));
        var service = new ExecuteCodeService(client, new ResultFormatter(), options);

        var result = await service.ExecuteAsync("return 1;", "none", false, requested, "clamp", null, null, TestContext.Current.CancellationToken);

        Assert.False(result.IsError);
        Assert.Equal(expected, executor.LastExecuteRequest!.TimeoutSeconds);
        await listener.StopAsync();
    }

    [Theory]
    [InlineData(600, 600, 600)]
    [InlineData(600, 120, 120)]
    public async Task Run_tool_clamps_a_stored_timeout_to_the_profile_ceiling(int stored, int ceiling, int expected)
    {
        var root = Path.Combine(Path.GetTempPath(), "hpnavis-registry-" + Guid.NewGuid().ToString("N"));
        var registryOptions = new RegistryOptions { LibraryPath = Path.Combine(root, "lib"), DbPath = Path.Combine(root, "registry.db"), WatchLibrary = false };
        var pipe = "hpnavis-mcp-test-" + Guid.NewGuid().ToString("N");
        var executor = new FakeRevitExecutor();
        using var listener = new PipeListener(pipe, new RequestDispatcher(executor, new BridgeSettings { ExecutionEnabled = true }, "2026", "Navisworks"));
        listener.Start();
        await using var client = new RevitBridgeClient(Options.Create(new BridgeOptions { PipeName = pipe, ConnectTimeoutMs = 3000, PingIntervalSeconds = 60 }), NullLogger<RevitBridgeClient>.Instance, Navis(ceiling));
        using var store = new ToolLibraryStore(registryOptions.LibraryPath, NullLogger<ToolLibraryStore>.Instance);
        store.EnsureRoot();
        var db = new ToolRegistryDb(registryOptions.DbPath, NullLogger<ToolRegistryDb>.Instance);
        db.Initialize();
        var manager = new ToolManager(Options.Create(registryOptions), Options.Create(new BridgeOptions()), store, db, client, NullLogger<ToolManager>.Instance);
        var record = Candidate(stored);
        record.Status = ToolStatus.Published;
        manager.Save(record, "published", "test");

        var result = await manager.RunAsync(record.Name, null, false, false, "run", TestContext.Current.CancellationToken);

        Assert.False(result.IsError);
        Assert.Equal(expected, executor.LastExecuteRequest!.TimeoutSeconds);
        await listener.StopAsync();
        Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
        try { Directory.Delete(root, true); } catch (IOException) { /* best effort */ }
    }

    [Fact]
    public async Task Context_shape_for_navis_drops_revit_fields_and_keeps_the_navis_block()
    {
        var pipe = "hpnavis-mcp-test-" + Guid.NewGuid().ToString("N");
        var executor = new FakeRevitExecutor
        {
            ContextHandler = _ => new ContextResult
            {
                RevitVersion = "2026", Host = "navis", HostVersion = "2026", DocTitle = "gatehouse_pub",
                Navis = new NavisInfo("Feet", 1, [new ModelSummary("gatehouse_pub.nwd", "Millimeters", null)], 2, 3, 0, true, false, false, false, false),
            },
        };
        using var listener = new PipeListener(pipe, new RequestDispatcher(executor, new BridgeSettings(), "2026", "Navisworks"));
        listener.Start();
        var options = Options.Create(new BridgeOptions { PipeName = pipe, ConnectTimeoutMs = 3000, PingIntervalSeconds = 60 });
        await using var client = new RevitBridgeClient(options, NullLogger<RevitBridgeClient>.Instance, Navis());

        var text = await new ContextService(client, new ResultFormatter()).ReadAsync(false, TestContext.Current.CancellationToken);

        using var json = JsonDocument.Parse(text);
        var root = json.RootElement;
        Assert.False(root.TryGetProperty("revitVersion", out _));
        Assert.False(root.TryGetProperty("isFamily", out _));
        Assert.Equal("navis", root.GetProperty("host").GetString());
        Assert.Equal("Feet", root.GetProperty("navis").GetProperty("documentUnits").GetString());
        Assert.True(root.GetProperty("navis").GetProperty("hasClashModule").GetBoolean());
        Assert.False(root.TryGetProperty("autocad", out _));
        await listener.StopAsync();
    }

    [Fact]
    public void A_context_without_a_navis_block_serialises_without_the_key()
    {
        var json = BridgeJson.Serialize(new ContextResult { RevitVersion = "2026", DocTitle = "Project1" });

        Assert.DoesNotContain("\"navis\"", json);
        Assert.DoesNotContain("\"autocad\"", json);
        Assert.Contains("\"revitVersion\":\"2026\"", json);
    }
}
