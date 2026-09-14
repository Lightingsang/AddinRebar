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
    private static readonly string[] HostApiAssemblies = ["RevitAPI", "RevitAPIUI", "AcDbMgd", "AcMgd", "AcCoreMgd", "Nice3point"];

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
}
