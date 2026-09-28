using System.Reflection;
using HPRebar.Mcp.Contracts;
using HPRebar.Mcp.Contracts.JsonRpc;
using HPRebar.Mcp.Contracts.Messages;
using HPRebar.Mcp.Server.Hosts;
using HPRebar.Mcp.Server.Models;
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
///     The rules that make McpShared shareable: no host API assembly is referenced, names derive from the
///     profile, and a bridge answers whichever host prefix the server uses.
/// </summary>
public sealed class HostNeutralityTests
{
    private static readonly string[] HostApiAssemblies = ["RevitAPI", "RevitAPIUI", "AcDbMgd", "AcMgd", "AcCoreMgd", "Nice3point", "Tekla.Structures"];

    public static IEnumerable<object[]> SharedAssemblies() =>
    [
        [typeof(PipeNaming).Assembly],
        [typeof(PipeListener).Assembly],
        [typeof(ResultFormatter).Assembly],
    ];

    [Theory]
    [MemberData(nameof(SharedAssemblies))]
    public void Shared_assemblies_reference_no_host_api(Assembly assembly)
    {
        var referenced = assembly.GetReferencedAssemblies().Select(a => a.Name ?? string.Empty).ToArray();

        foreach (var forbidden in HostApiAssemblies)
            Assert.DoesNotContain(referenced, name => name.StartsWith(forbidden, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Pipe_names_are_per_host_and_revit_is_unchanged()
    {
        Assert.Equal("hprebar-mcp-r2026", PipeNaming.For(2026));
        Assert.Equal("hprebar-mcp-r2026", PipeNaming.For("revit", 2026));
        Assert.Equal("hpautocad-mcp-2026", PipeNaming.For("AutoCAD", 2026));
        Assert.Equal("hpcivil3d-mcp-2026", PipeNaming.For("civil3d", 2026));
        Assert.Equal("hptekla-mcp-2025", PipeNaming.For("tekla", 2025));
        Assert.Equal("hptekla-mcp-2025", PipeNaming.For(PipeNaming.TeklaHost, 2025));
    }

    [Fact]
    public void Method_names_split_into_prefix_and_suffix()
    {
        Assert.Equal("execute", JsonRpcMethods.Suffix("autocad.execute"));
        Assert.Equal("execute", JsonRpcMethods.Suffix(JsonRpcMethods.Execute));
        Assert.Equal("ping", JsonRpcMethods.Suffix("ping"));
        Assert.True(JsonRpcMethods.IsProgress("autocad.progress"));
        Assert.True(JsonRpcMethods.IsStatus(JsonRpcMethods.StatusNotification));
        Assert.Equal("autocad.analyze", JsonRpcMethods.For(JsonRpcMethods.AutocadPrefix, JsonRpcMethods.AnalyzeSuffix));
    }

    [Fact]
    public void Revit_profile_reproduces_the_original_server_configuration()
    {
        var profile = HostProfile.Revit;

        Assert.Equal("hprebar-mcp-r2026", profile.PipeName(2026));
        Assert.Equal("revit.execute", profile.Method(JsonRpcMethods.ExecuteSuffix));
        Assert.Equal("execute_revit_code", profile.ExecuteToolName);
        Assert.Equal("HPRebar", profile.ProductFolder);
        Assert.Equal("HPREBAR_MCP_", profile.EnvPrefix);
        Assert.Contains(2026, profile.ValidVersions);

        var options = new BridgeOptions { RevitVersion = 2025 };
        Assert.Equal(2025, options.HostVersion);
        Assert.Equal("hprebar-mcp-r2025", options.PipeName);
        Assert.True(options.IsValid(profile.ValidVersions));
        Assert.False(options.IsValid(new[] { 2026 }));
    }

    [Fact]
    public void Bridge_options_pipe_follows_the_host_id()
    {
        var options = new BridgeOptions { HostId = "autocad", HostVersion = 2026 };

        Assert.Equal("hpautocad-mcp-2026", options.PipeName);
    }

    [Fact]
    public async Task Dispatcher_answers_the_autocad_prefix_and_echoes_it_in_progress()
    {
        var pipe = "hprebar-mcp-test-" + Guid.NewGuid().ToString("N");
        var executor = new FakeRevitExecutor();
        var settings = new BridgeSettings { ExecutionEnabled = true };
        using var listener = new PipeListener(pipe, new RequestDispatcher(executor, settings, "2026", "AutoCAD"));
        listener.Start();

        var autocad = new HostProfile
        {
            HostId = "autocad", DisplayName = "AutoCAD", ServerName = "test", ProductFolder = "HPAutoCadTest", EnvPrefix = "X_",
            DefaultVersion = 2026, ValidVersions = new[] { 2026 }, MethodPrefix = JsonRpcMethods.AutocadPrefix,
            ExecuteToolName = "execute_autocad_code", ContextToolName = "get_autocad_context", ResourceScheme = "autocad",
            Categories = new[] { "Generic" }, CoreToolNames = new[] { "execute_autocad_code" }, ScriptImports = HostScriptContracts.AutocadImports,
            ScriptContractSummary = "test", HostAssembly = typeof(HostNeutralityTests).Assembly,
        };
        var options = Options.Create(new BridgeOptions { PipeName = pipe, ConnectTimeoutMs = 3000, PingIntervalSeconds = 60 });
        await using var client = new RevitBridgeClient(options, NullLogger<RevitBridgeClient>.Instance, autocad);

        var pong = await client.SendAsync<BridgePingResult>(autocad.Method(JsonRpcMethods.PingSuffix), null, TimeSpan.FromSeconds(10), null, TestContext.Current.CancellationToken);
        Assert.True(pong.Pong);

        var progress = new List<ProgressParams>();
        executor.ProgressSteps = 2;
        var result = await client.SendAsync<ExecuteResult>(autocad.Method(JsonRpcMethods.ExecuteSuffix), new ExecuteRequest("return 1;"),
            TimeSpan.FromSeconds(10), new SynchronousProgress<ProgressParams>(progress.Add), TestContext.Current.CancellationToken);
        Assert.False(result.IsError);
        Assert.Equal(2, progress.Count);

        var unknown = await Assert.ThrowsAsync<BridgeErrorException>(() =>
            client.SendAsync<object>("autocad.nope", null, TimeSpan.FromSeconds(10), null, TestContext.Current.CancellationToken));
        Assert.Equal(BridgeErrorCode.MethodNotFound, unknown.Code);

        await listener.StopAsync();
    }

    [Fact]
    public async Task Dispatcher_custom_handler_routes_custom_method_and_falls_back_to_not_found()
    {
        var pipe = "hprebar-mcp-test-" + Guid.NewGuid().ToString("N");
        var executor = new FakeRevitExecutor();
        var settings = new BridgeSettings { ExecutionEnabled = true };

        Task<JsonRpcEnvelope?> CustomHandler(long id, JsonRpcEnvelope request, NdjsonPipeWriter writer, CancellationToken ct)
        {
            if (request.Method == "custom.hello")
            {
                return Task.FromResult<JsonRpcEnvelope?>(JsonRpcEnvelope.Success(id, new { Message = "world" }));
            }
            return Task.FromResult<JsonRpcEnvelope?>(null);
        }

        using var listener = new PipeListener(pipe, new RequestDispatcher(executor, settings, "2026", "CustomHost", customHandler: CustomHandler));
        listener.Start();

        var options = Options.Create(new BridgeOptions { PipeName = pipe, ConnectTimeoutMs = 3000, PingIntervalSeconds = 60 });
        await using var client = new RevitBridgeClient(options, NullLogger<RevitBridgeClient>.Instance);

        // Custom method is routed successfully
        var customResult = await client.SendAsync<System.Text.Json.JsonElement>("custom.hello", null, TimeSpan.FromSeconds(10), null, TestContext.Current.CancellationToken);
        Assert.Equal("world", customResult.GetProperty("message").GetString());

        // Standard ping still works
        var pong = await client.SendAsync<BridgePingResult>(JsonRpcMethods.Ping, null, TimeSpan.FromSeconds(10), null, TestContext.Current.CancellationToken);
        Assert.True(pong.Pong);

        // Unhandled custom method falls through to MethodNotFound
        var unknown = await Assert.ThrowsAsync<BridgeErrorException>(() =>
            client.SendAsync<object>("custom.unhandled", null, TimeSpan.FromSeconds(10), null, TestContext.Current.CancellationToken));
        Assert.Equal(BridgeErrorCode.MethodNotFound, unknown.Code);

        await listener.StopAsync();
    }

    [Fact]
    public void Guard_profile_autocad_blocks_prompts_and_the_bridge_transaction_only_for_autocad()
    {
        const string prompt = "var p = ed.GetPoint(\"pick\"); return p;";
        const string commit = "tr.Commit(); return 1;";
        const string fine = "var id = tr.GetObject(db.BlockTableId, OpenMode.ForRead); ed.WriteMessage(\"hi\"); return units.ToDrawing(100);";

        Assert.Empty(ScriptGuard.Check(prompt));                              // Revit profile: GetPoint means nothing
        Assert.Contains(ScriptGuard.Check(prompt, GuardProfile.Autocad), d => d.Message.Contains("GetPoint"));
        Assert.Contains(ScriptGuard.Check(commit, GuardProfile.Autocad), d => d.Message.Contains("tr.Commit"));
        Assert.Empty(ScriptGuard.Check(fine, GuardProfile.Autocad));
        Assert.All(ScriptGuard.Check("await Task.Delay(1); return 1;", GuardProfile.Autocad), d => Assert.Contains("AutoCAD", d.Message));
    }

    [Fact]
    public void Guard_profile_autocad_denies_transactions_of_the_scripts_own()
    {
        const string nested = "using (var t = db.TransactionManager.StartTransaction()) { t.Commit(); } return 1;";
        const string top = "var t = db.TransactionManager.TopTransaction; return 1;";
        const string fine = "var bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead); return bt.Has(\"x\");";

        Assert.Contains(ScriptGuard.Check(nested, GuardProfile.Autocad), d => d.Message.Contains("StartTransaction"));
        Assert.Contains(ScriptGuard.Check(top, GuardProfile.Autocad), d => d.Message.Contains("TopTransaction"));
        Assert.Contains(ScriptGuard.Check("using (doc.LockDocument()) { return 1; }", GuardProfile.Autocad), d => d.Message.Contains("LockDocument"));
        Assert.Empty(ScriptGuard.Check(fine, GuardProfile.Autocad));
        Assert.Empty(ScriptGuard.Check(nested)); // Revit scripts may still open their own
    }

    [Fact]
    public void Analyzer_profile_autocad_detects_transactions_started_by_call()
    {
        const string code = "using (var t = db.TransactionManager.StartTransaction()) { t.Commit(); } return 1;";

        Assert.False(ScriptAnalyzer.Analyze(code).UsesTransaction);                            // Revit looks for `new Transaction(...)`
        Assert.True(ScriptAnalyzer.Analyze(code, AnalyzerProfile.Autocad).UsesTransaction);
        Assert.True(ScriptAnalyzer.Analyze("var t = new Transaction(doc, \"x\"); return 1;").UsesTransaction);
    }

    [Fact]
    public void Script_units_convert_both_ways()
    {
        var inches = new ScriptUnits("Inches", 25.4);

        Assert.Equal(1.0, inches.ToDrawing(25.4), 12);
        Assert.Equal(304.8, inches.ToMm(12), 12);
        Assert.Equal(150, ScriptUnits.Millimeters.ToDrawing(150), 12);
        Assert.Throws<ArgumentOutOfRangeException>(() => new ScriptUnits("bad", 0));
    }

    [Fact]
    public void Settings_store_paths_are_per_product()
    {
        var revit = BridgeSettingsStore.Revit;
        var autocad = new BridgeSettingsStore("HPAutoCad", "McpBridge");

        Assert.EndsWith(Path.Combine("HPRebar", "McpBridge"), revit.Directory);
        Assert.EndsWith(Path.Combine("HPAutoCad", "McpBridge"), autocad.Directory);
        Assert.NotEqual(revit.AuditDirectory, autocad.AuditDirectory);
    }

    [Fact]
    public void Settings_store_defaults_and_persists_execution_enabled_and_autostart()
    {
        var tempFolder = "HPTest-" + Guid.NewGuid().ToString("N");
        var store = new BridgeSettingsStore("HPTestVendor", tempFolder);
        try
        {
            // Default when file does not exist
            var defaults = store.Load();
            Assert.True(defaults.ExecutionEnabled);
            Assert.True(defaults.AutoStartListener);

            // Persist toggled off
            defaults.ExecutionEnabled = false;
            defaults.AutoStartListener = false;
            store.Save(defaults);

            var reloaded = store.Load();
            Assert.False(reloaded.ExecutionEnabled);
            Assert.False(reloaded.AutoStartListener);

            // Persist toggled back on
            reloaded.ExecutionEnabled = true;
            reloaded.AutoStartListener = true;
            store.Save(reloaded);

            var reloadedOn = store.Load();
            Assert.True(reloadedOn.ExecutionEnabled);
            Assert.True(reloadedOn.AutoStartListener);
        }
        finally
        {
            if (Directory.Exists(store.Directory))
            {
                Directory.Delete(store.Directory, recursive: true);
            }
        }
    }

    [Fact]
    public async Task A_second_listener_on_the_same_pipe_faults_and_names_the_host()
    {
        var pipe = "hpautocad-mcp-test-" + Guid.NewGuid().ToString("N");
        var settings = new BridgeSettings();
        using var first = new PipeListener(pipe, new RequestDispatcher(new FakeRevitExecutor(), settings, "2026", "AutoCAD"));
        using var second = new PipeListener(pipe, new RequestDispatcher(new FakeRevitExecutor(), settings, "2026", "AutoCAD"));
        var faulted = new TaskCompletionSource<string>();
        second.Faulted += reason => faulted.TrySetResult(reason);

        first.Start();
        // Both accept loops race to create the pipe; wait until the first one owns it before starting the second.
        var deadline = DateTime.UtcNow.AddSeconds(10);
        while (DateTime.UtcNow < deadline && !Directory.EnumerateFiles(@"\\.\pipe\").Any(f => f.EndsWith(pipe, StringComparison.Ordinal))) await Task.Delay(50, TestContext.Current.CancellationToken);
        second.Start();
        var reason = await faulted.Task.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);

        Assert.Contains("already in use", reason);
        Assert.Contains("another AutoCAD 2026 instance", reason);
        Assert.DoesNotContain("Revit", reason);
        Assert.False(second.IsListening);
        Assert.True(first.IsListening);
        await first.StopAsync();
    }
}
