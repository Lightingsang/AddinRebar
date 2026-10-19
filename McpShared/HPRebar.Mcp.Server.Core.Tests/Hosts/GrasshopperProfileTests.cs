using System.Text.Json;
using HPRebar.Mcp.Contracts;
using HPRebar.Mcp.Contracts.JsonRpc;
using HPRebar.Mcp.Contracts.Messages;
using HPRebar.Mcp.Server.Bootstrap;
using HPRebar.Mcp.Server.Hosts;
using HPRebar.Mcp.Server.Models;
using HPRebar.Mcp.Server.Registry;
using HPRebar.Mcp.Server.Services;
using HPRebar.Mcp.Server.Tests.Fakes;
using HPRebar.McpBridge.Core.Model;
using HPRebar.McpBridge.Core.Pipe;
using HPRebar.McpBridge.Core.Scripting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;
using static HPRebar.Mcp.Server.Tests.GrasshopperTestProfile;

namespace HPRebar.Mcp.Server.Tests;

/// <summary>
///     Engine tests for McNeel Grasshopper host profile and contracts: pipe naming, prefix, imports, globals, guard, analyzer,
///     hints, options configuration, context shaping, and wire isolation.
/// </summary>
public sealed class GrasshopperProfileTests
{
    [Fact]
    public void Grasshopper_constants_produce_the_pipe_prefix_imports_and_globals()
    {
        Assert.Equal("hpgrasshopper-mcp-2026", PipeNaming.For(PipeNaming.GrasshopperHost, 2026));
        Assert.Equal("hpgrasshopper-mcp-2026", PipeNaming.For("GRASSHOPPER", 2026));
        Assert.Equal("hpgrasshopper-mcp-2026", PipeNaming.For("grasshopper", 2026));
        Assert.Equal("grasshopper.execute", JsonRpcMethods.For(JsonRpcMethods.GrasshopperPrefix, JsonRpcMethods.ExecuteSuffix));
        Assert.Equal("execute", JsonRpcMethods.Suffix("grasshopper.execute"));
        Assert.Equal("hpgrasshopper-mcp-2026", new BridgeOptions { HostId = "grasshopper", HostVersion = 2026 }.PipeName);
        Assert.Contains("Rhino", HostScriptContracts.GrasshopperImports);
        Assert.Contains("Rhino.Geometry", HostScriptContracts.GrasshopperImports);
        Assert.Contains("Grasshopper", HostScriptContracts.GrasshopperImports);
        Assert.Contains("Grasshopper.Kernel", HostScriptContracts.GrasshopperImports);
        Assert.Contains("RhinoInside.Revit", HostScriptContracts.GrasshopperImports);
        Assert.Contains("Autodesk.Revit.DB", HostScriptContracts.GrasshopperImports);
        Assert.Contains("HPRebar.McpBridge.Core.Scripting", HostScriptContracts.GrasshopperImports);
        Assert.DoesNotContain("System.IO", HostScriptContracts.GrasshopperImports);
        Assert.DoesNotContain("System.Reflection", HostScriptContracts.GrasshopperImports);
        Assert.Equal(["doc", "canvas", "rhinoDoc", "revitDoc", "ct", "log", "progress", "args"], HostScriptContracts.GrasshopperGlobals);
        Assert.Equal(600, HostScriptContracts.GrasshopperHeavyMaxTimeoutSeconds);
        Assert.Equal("hpgrasshopper-mcp-2026", Grasshopper().PipeName(2026));
        Assert.Equal("grasshopper.context", Grasshopper().Method(JsonRpcMethods.ContextSuffix));
        Assert.Equal("grasshopper.analyze", Grasshopper().Method(JsonRpcMethods.AnalyzeSuffix));
    }

    [Theory]
    [InlineData("MessageBox.Show(\"modal\"); return 1;", "MessageBox")]
    [InlineData("System.Windows.Forms.MessageBox.Show(\"modal\"); return 1;", "MessageBox")]
    [InlineData("TaskDialog.Show(\"Revit\", \"modal\"); return 1;", "TaskDialog")]
    [InlineData("Application.Exit(); return 1;", "Exit")]
    [InlineData("app.Quit(); return 1;", "Quit")]
    [InlineData("app.CloseApplication(); return 1;", "CloseApplication")]
    [InlineData("using System.Windows.Forms; return 1;", "System.Windows.Forms")]
    [InlineData("HPGrasshopper.McpBridge.BridgeEntry.Stop(); return 1;", "HPGrasshopper.McpBridge")]
    [InlineData("HPRebar.McpBridge.Core.Host.McpBridgeHost.Current.Stop(); return 1;", "HPRebar.McpBridge.Core.Host")]
    [InlineData("var p = System.Diagnostics.Process.Start(\"cmd\"); return 1;", "System.Diagnostics.Process")]
    [InlineData("#r \"external.dll\"\nreturn 1;", "#r")]
    [InlineData("#load \"script.csx\"\nreturn 1;", "#load")]
    public void Grasshopper_guard_profile_denies_dialogs_quit_and_base_list(string code, string expected)
    {
        var violations = ScriptGuard.Check(code, GuardProfile.Grasshopper);

        Assert.NotEmpty(violations);
        Assert.Contains(violations, v => v.Message.Contains(expected, StringComparison.Ordinal));
        Assert.All(violations, v => Assert.True(
            v.Message.Contains("Grasshopper", StringComparison.Ordinal) ||
            v.Message.Contains("bridge owns", StringComparison.Ordinal) ||
            v.Message.Contains("directives", StringComparison.Ordinal) ||
            v.Message.Contains("not allowed in", StringComparison.Ordinal)));
    }

    [Fact]
    public void Global_alias_does_not_bypass_the_grasshopper_bridge_namespace_denial()
    {
        var violations1 = ScriptGuard.Check("var h = global::HPRebar.McpBridge.Core.Host.McpBridgeHost.Current; return 1;", GuardProfile.Grasshopper);
        Assert.Contains(violations1, v => v.Message.Contains("HPRebar.McpBridge.Core.Host", StringComparison.Ordinal));

        var violations2 = ScriptGuard.Check("var h = global::HPGrasshopper.McpBridge.BridgeEntry.Stop(); return 1;", GuardProfile.Grasshopper);
        Assert.Contains(violations2, v => v.Message.Contains("HPGrasshopper.McpBridge", StringComparison.Ordinal));
    }

    [Fact]
    public void Grasshopper_guard_profile_lets_rhino_and_grasshopper_manipulation_through()
    {
        const string fine =
            "var pt = new Point3d(0, 0, 0);\n" +
            "var line = new Line(pt, new Point3d(10, 20, 30));\n" +
            "var ghDoc = doc;\n" +
            "log($\"Active definition contains {ghDoc.Objects.Count} objects\");\n" +
            "progress(1, 1, \"Done\");\n" +
            "return new { length = line.Length, objectCount = ghDoc.Objects.Count };";

        Assert.Empty(ScriptGuard.Check(fine, GuardProfile.Grasshopper));
        Assert.Empty(ScriptGuard.Check("return doc.FilePath;", GuardProfile.Grasshopper));
    }

    [Fact]
    public void Grasshopper_analyzer_profile_detects_transactions()
    {
        const string txCode = "using (var t = new Transaction(revitDoc, \"Create\")) { t.Start(); t.Commit(); } return 1;";
        const string tgCode = "using (var tg = new TransactionGroup(revitDoc, \"Group\")) { tg.Start(); tg.Assimilate(); } return 1;";
        const string subTxCode = "using (var st = new SubTransaction(revitDoc)) { st.Start(); st.Commit(); } return 1;";
        const string readOnlyCode = "return doc.FilePath;";

        Assert.True(ScriptAnalyzer.Analyze(txCode, AnalyzerProfile.Grasshopper).UsesTransaction);
        Assert.True(ScriptAnalyzer.Analyze(tgCode, AnalyzerProfile.Grasshopper).UsesTransaction);
        Assert.True(ScriptAnalyzer.Analyze(subTxCode, AnalyzerProfile.Grasshopper).UsesTransaction);
        Assert.False(ScriptAnalyzer.Analyze(readOnlyCode, AnalyzerProfile.Grasshopper).UsesTransaction);

        Assert.Contains("Transaction", AnalyzerProfile.Grasshopper.TransactionTypeNames);
        Assert.Contains("TransactionGroup", AnalyzerProfile.Grasshopper.TransactionTypeNames);
        Assert.Contains("SubTransaction", AnalyzerProfile.Grasshopper.TransactionTypeNames);
        Assert.Empty(AnalyzerProfile.Grasshopper.TransactionMethodNames);
    }

    [Fact]
    public void Hints_and_timeout_ceiling_survive_WithHostAssembly_and_default_to_null()
    {
        var copy = Grasshopper(notConnected: NotConnectedHint, timeoutHint: TimeoutHint).WithHostAssembly(typeof(HostProfile).Assembly);

        Assert.Equal(600, copy.MaxTimeoutSeconds);
        Assert.Equal(NotConnectedHint, copy.BridgeNotConnectedHint);
        Assert.Equal(TimeoutHint, copy.TimeoutSemanticsHint);
        Assert.Null(Grasshopper().BridgeNotConnectedHint);
    }

    [Fact]
    public void Validator_ceiling_follows_the_grasshopper_profile()
    {
        Assert.DoesNotContain(ToolValidator.Validate(Candidate(600), null, [], false, Grasshopper()).Errors, e => e.Contains("timeoutSeconds"));
        Assert.Contains(ToolValidator.Validate(Candidate(601), null, [], false, Grasshopper()).Errors, e => e.Contains("between 5 and 600"));
        Assert.Contains(ToolValidator.Validate(Candidate(600), null, [], false, Grasshopper(maxTimeout: 120)).Errors, e => e.Contains("between 5 and 120"));
    }

    [Fact]
    public void ConfigureOptions_seeds_HostVersion_from_the_profile_and_configuration_still_wins()
    {
        Assert.Equal(2026, BridgeOptionsFor(Grasshopper(), new Dictionary<string, string?>()).HostVersion);
        Assert.Equal("hpgrasshopper-mcp-2026", BridgeOptionsFor(Grasshopper(), new Dictionary<string, string?>()).PipeName);
        Assert.Throws<OptionsValidationException>(() => BridgeOptionsFor(Grasshopper(), new Dictionary<string, string?> { ["Bridge:HostVersion"] = "1997" }));
    }

    [Fact]
    public void Wire_additions_are_invisible_when_unused()
    {
        var context = BridgeJson.Serialize(new ContextResult { RevitVersion = "2026", DocTitle = "Project1" });
        Assert.DoesNotContain("\"grasshopper\"", context);
    }

    [Fact]
    public void Grasshopper_info_round_trips_in_camel_case_and_is_omitted_when_null()
    {
        var context = new ContextResult
        {
            Host = "grasshopper",
            HostVersion = "2026",
            Grasshopper = new GrasshopperInfo(
                IsEditorLoaded: true,
                GrasshopperVersion: "1.0.0007",
                RhinoVersion: "8.12.24282.7001",
                ActiveDefinitionName: "ParametricTruss.gh",
                ActiveDefinitionPath: @"C:\GrasshopperScripts\ParametricTruss.gh",
                ComponentCount: 78,
                WireCount: 112,
                SelectedComponentCount: 5,
                HasUnsavedChanges: false,
                IsRhinoInsideRevitActive: true),
        };

        var json = BridgeJson.Serialize(context);
        using var doc = JsonDocument.Parse(json);
        var gh = doc.RootElement.GetProperty("grasshopper");
        Assert.True(gh.GetProperty("isEditorLoaded").GetBoolean());
        Assert.Equal("1.0.0007", gh.GetProperty("grasshopperVersion").GetString());
        Assert.Equal("8.12.24282.7001", gh.GetProperty("rhinoVersion").GetString());
        Assert.Equal("ParametricTruss.gh", gh.GetProperty("activeDefinitionName").GetString());
        Assert.Equal(@"C:\GrasshopperScripts\ParametricTruss.gh", gh.GetProperty("activeDefinitionPath").GetString());
        Assert.Equal(78, gh.GetProperty("componentCount").GetInt32());
        Assert.Equal(112, gh.GetProperty("wireCount").GetInt32());
        Assert.Equal(5, gh.GetProperty("selectedComponentCount").GetInt32());
        Assert.False(gh.GetProperty("hasUnsavedChanges").GetBoolean());
        Assert.True(gh.GetProperty("isRhinoInsideRevitActive").GetBoolean());
        Assert.Equal(10, gh.EnumerateObject().Count());

        var back = BridgeJson.Deserialize<ContextResult>(json)!;
        Assert.Equal(context.Grasshopper, back.Grasshopper);

        var withoutGh = BridgeJson.Serialize(new ContextResult { Host = "revit" });
        Assert.DoesNotContain("grasshopper", withoutGh);
    }

    [Fact]
    public async Task Context_shape_for_grasshopper_drops_revit_fields_and_keeps_grasshopper_block()
    {
        var pipe = NewPipe();
        var executor = new FakeRevitExecutor
        {
            ContextHandler = _ => new ContextResult
            {
                RevitVersion = "2026",
                IsFamily = false,
                Host = "grasshopper",
                HostVersion = "2026",
                DocTitle = "ParametricTruss.gh",
                DocPath = @"C:\GrasshopperScripts\ParametricTruss.gh",
                Grasshopper = new GrasshopperInfo(true, "1.0.0007", "8.12", "ParametricTruss.gh", @"C:\GrasshopperScripts\ParametricTruss.gh", 78, 112, 5, false, true),
            },
        };
        using var listener = new PipeListener(pipe, new RequestDispatcher(executor, new BridgeSettings(), "2026", "Grasshopper"));
        listener.Start();

        await using var client = new RevitBridgeClient(
            Options.Create(PipeOptions(pipe)),
            NullLogger<RevitBridgeClient>.Instance, Grasshopper());

        var text = await new ContextService(client, new ResultFormatter()).ReadAsync(false, TestContext.Current.CancellationToken);

        using var json = JsonDocument.Parse(text);
        var root = json.RootElement;
        Assert.False(root.TryGetProperty("revitVersion", out _));
        Assert.False(root.TryGetProperty("isFamily", out _));
        Assert.Equal("grasshopper", root.GetProperty("host").GetString());
        Assert.True(root.GetProperty("grasshopper").GetProperty("isEditorLoaded").GetBoolean());
        Assert.Equal("ParametricTruss.gh", root.GetProperty("grasshopper").GetProperty("activeDefinitionName").GetString());
        Assert.Equal(78, root.GetProperty("grasshopper").GetProperty("componentCount").GetInt32());
        Assert.True(root.GetProperty("grasshopper").GetProperty("isRhinoInsideRevitActive").GetBoolean());
        Assert.False(root.TryGetProperty("autocad", out _));
        Assert.False(root.TryGetProperty("navis", out _));
        Assert.False(root.TryGetProperty("etabs", out _));
        Assert.False(root.TryGetProperty("civil3d", out _));
        Assert.False(root.TryGetProperty("sap2000", out _));
        Assert.False(root.TryGetProperty("powerbi", out _));
        Assert.False(root.TryGetProperty("excel", out _));
        Assert.False(root.TryGetProperty("robot", out _));
        Assert.False(root.TryGetProperty("tekla", out _));
        Assert.False(root.TryGetProperty("dynamo", out _));
        await listener.StopAsync();
    }

    private static BridgeOptions BridgeOptionsFor(IHostProfile profile, Dictionary<string, string?> settings)
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
        var services = new ServiceCollection();
        McpServerHost.ConfigureOptions(services, configuration, profile);
        using var provider = services.BuildServiceProvider();
        return provider.GetRequiredService<IOptions<BridgeOptions>>().Value;
    }
}
