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
using static HPRebar.Mcp.Server.Tests.DynamoTestProfile;

namespace HPRebar.Mcp.Server.Tests;

/// <summary>
///     Engine tests for Autodesk Dynamo host profile and contracts: pipe naming, prefix, imports, globals, guard, analyzer,
///     hints, options configuration, context shaping, and wire isolation.
/// </summary>
public sealed class DynamoProfileTests
{
    [Fact]
    public void Dynamo_constants_produce_the_pipe_prefix_imports_and_globals()
    {
        Assert.Equal("hpdynamo-mcp-2026", PipeNaming.For(PipeNaming.DynamoHost, 2026));
        Assert.Equal("hpdynamo-mcp-2026", PipeNaming.For("DYNAMO", 2026));
        Assert.Equal("hpdynamo-mcp-2026", PipeNaming.For("dynamo", 2026));
        Assert.Equal("dynamo.execute", JsonRpcMethods.For(JsonRpcMethods.DynamoPrefix, JsonRpcMethods.ExecuteSuffix));
        Assert.Equal("execute", JsonRpcMethods.Suffix("dynamo.execute"));
        Assert.Equal("hpdynamo-mcp-2026", new BridgeOptions { HostId = "dynamo", HostVersion = 2026 }.PipeName);
        Assert.Contains("Autodesk.DesignScript.Geometry", HostScriptContracts.DynamoImports);
        Assert.Contains("Dynamo.Graph.Nodes", HostScriptContracts.DynamoImports);
        Assert.Contains("Dynamo.Graph.Workspaces", HostScriptContracts.DynamoImports);
        Assert.Contains("Dynamo.Models", HostScriptContracts.DynamoImports);
        Assert.Contains("Revit.Elements", HostScriptContracts.DynamoImports);
        Assert.Contains("HPRebar.McpBridge.Core.Scripting", HostScriptContracts.DynamoImports);
        Assert.DoesNotContain("System.IO", HostScriptContracts.DynamoImports);
        Assert.DoesNotContain("System.Reflection", HostScriptContracts.DynamoImports);
        Assert.Equal(["dynamo", "workspace", "doc", "ct", "log", "progress", "args"], HostScriptContracts.DynamoGlobals);
        Assert.Equal(600, HostScriptContracts.DynamoHeavyMaxTimeoutSeconds);
        Assert.Equal("hpdynamo-mcp-2026", Dynamo().PipeName(2026));
        Assert.Equal("dynamo.context", Dynamo().Method(JsonRpcMethods.ContextSuffix));
        Assert.Equal("dynamo.analyze", Dynamo().Method(JsonRpcMethods.AnalyzeSuffix));
    }

    [Theory]
    [InlineData("MessageBox.Show(\"modal\"); return 1;", "MessageBox")]
    [InlineData("System.Windows.Forms.MessageBox.Show(\"modal\"); return 1;", "MessageBox")]
    [InlineData("TaskDialog.Show(\"Revit\", \"modal\"); return 1;", "TaskDialog")]
    [InlineData("Application.Exit(); return 1;", "Exit")]
    [InlineData("app.Quit(); return 1;", "Quit")]
    [InlineData("app.CloseApplication(); return 1;", "CloseApplication")]
    [InlineData("using System.Windows.Forms; return 1;", "System.Windows.Forms")]
    [InlineData("HPDynamo.McpBridge.BridgeEntry.Stop(); return 1;", "HPDynamo.McpBridge")]
    [InlineData("HPRebar.McpBridge.Core.Host.McpBridgeHost.Current.Stop(); return 1;", "HPRebar.McpBridge.Core.Host")]
    [InlineData("var p = System.Diagnostics.Process.Start(\"cmd\"); return 1;", "System.Diagnostics.Process")]
    [InlineData("#r \"external.dll\"\nreturn 1;", "#r")]
    [InlineData("#load \"script.csx\"\nreturn 1;", "#load")]
    public void Dynamo_guard_profile_denies_dialogs_quit_and_base_list(string code, string expected)
    {
        var violations = ScriptGuard.Check(code, GuardProfile.Dynamo);

        Assert.NotEmpty(violations);
        Assert.Contains(violations, v => v.Message.Contains(expected, StringComparison.Ordinal));
        Assert.All(violations, v => Assert.True(
            v.Message.Contains("Dynamo", StringComparison.Ordinal) ||
            v.Message.Contains("bridge owns", StringComparison.Ordinal) ||
            v.Message.Contains("directives", StringComparison.Ordinal) ||
            v.Message.Contains("not allowed in", StringComparison.Ordinal)));
    }

    [Fact]
    public void Global_alias_does_not_bypass_the_dynamo_bridge_namespace_denial()
    {
        var violations1 = ScriptGuard.Check("var h = global::HPRebar.McpBridge.Core.Host.McpBridgeHost.Current; return 1;", GuardProfile.Dynamo);
        Assert.Contains(violations1, v => v.Message.Contains("HPRebar.McpBridge.Core.Host", StringComparison.Ordinal));

        var violations2 = ScriptGuard.Check("var h = global::HPDynamo.McpBridge.BridgeEntry.Stop(); return 1;", GuardProfile.Dynamo);
        Assert.Contains(violations2, v => v.Message.Contains("HPDynamo.McpBridge", StringComparison.Ordinal));
    }

    [Fact]
    public void Dynamo_guard_profile_lets_graph_and_geometry_manipulation_through()
    {
        const string fine =
            "var p = Point.ByCoordinates(0, 0, 0);\n" +
            "var p2 = Point.ByCoordinates(10, 20, 30);\n" +
            "var line = Line.ByStartPointEndPoint(p, p2);\n" +
            "var nodes = workspace.Nodes;\n" +
            "log($\"Workspace contains {nodes.Count()} nodes\");\n" +
            "progress(1, 1, \"Done\");\n" +
            "return new { lineLength = line.Length, nodeCount = nodes.Count() };";

        Assert.Empty(ScriptGuard.Check(fine, GuardProfile.Dynamo));
        Assert.Empty(ScriptGuard.Check("return workspace.Name;", GuardProfile.Dynamo));
    }

    [Fact]
    public void Dynamo_analyzer_profile_detects_transactions()
    {
        const string txCode = "using (var t = new Transaction(doc, \"Create\")) { t.Start(); t.Commit(); } return 1;";
        const string tgCode = "using (var tg = new TransactionGroup(doc, \"Group\")) { tg.Start(); tg.Assimilate(); } return 1;";
        const string subTxCode = "using (var st = new SubTransaction(doc)) { st.Start(); st.Commit(); } return 1;";
        const string readOnlyCode = "return workspace.Name;";

        Assert.True(ScriptAnalyzer.Analyze(txCode, AnalyzerProfile.Dynamo).UsesTransaction);
        Assert.True(ScriptAnalyzer.Analyze(tgCode, AnalyzerProfile.Dynamo).UsesTransaction);
        Assert.True(ScriptAnalyzer.Analyze(subTxCode, AnalyzerProfile.Dynamo).UsesTransaction);
        Assert.False(ScriptAnalyzer.Analyze(readOnlyCode, AnalyzerProfile.Dynamo).UsesTransaction);

        Assert.Contains("Transaction", AnalyzerProfile.Dynamo.TransactionTypeNames);
        Assert.Contains("TransactionGroup", AnalyzerProfile.Dynamo.TransactionTypeNames);
        Assert.Contains("SubTransaction", AnalyzerProfile.Dynamo.TransactionTypeNames);
        Assert.Empty(AnalyzerProfile.Dynamo.TransactionMethodNames);
    }

    [Fact]
    public void Hints_and_timeout_ceiling_survive_WithHostAssembly_and_default_to_null()
    {
        var copy = Dynamo(notConnected: NotConnectedHint, timeoutHint: TimeoutHint).WithHostAssembly(typeof(HostProfile).Assembly);

        Assert.Equal(600, copy.MaxTimeoutSeconds);
        Assert.Equal(NotConnectedHint, copy.BridgeNotConnectedHint);
        Assert.Equal(TimeoutHint, copy.TimeoutSemanticsHint);
        Assert.Null(Dynamo().BridgeNotConnectedHint);
    }

    [Fact]
    public void Validator_ceiling_follows_the_dynamo_profile()
    {
        Assert.DoesNotContain(ToolValidator.Validate(Candidate(600), null, [], false, Dynamo()).Errors, e => e.Contains("timeoutSeconds"));
        Assert.Contains(ToolValidator.Validate(Candidate(601), null, [], false, Dynamo()).Errors, e => e.Contains("between 5 and 600"));
        Assert.Contains(ToolValidator.Validate(Candidate(600), null, [], false, Dynamo(maxTimeout: 120)).Errors, e => e.Contains("between 5 and 120"));
    }

    [Fact]
    public void ConfigureOptions_seeds_HostVersion_from_the_profile_and_configuration_still_wins()
    {
        Assert.Equal(2026, BridgeOptionsFor(Dynamo(), new Dictionary<string, string?>()).HostVersion);
        Assert.Equal("hpdynamo-mcp-2026", BridgeOptionsFor(Dynamo(), new Dictionary<string, string?>()).PipeName);
        Assert.Throws<OptionsValidationException>(() => BridgeOptionsFor(Dynamo(), new Dictionary<string, string?> { ["Bridge:HostVersion"] = "1997" }));
    }

    [Fact]
    public void Wire_additions_are_invisible_when_unused()
    {
        var context = BridgeJson.Serialize(new ContextResult { RevitVersion = "2026", DocTitle = "Project1" });
        Assert.DoesNotContain("\"dynamo\"", context);
    }

    [Fact]
    public void Dynamo_info_round_trips_in_camel_case_and_is_omitted_when_null()
    {
        var context = new ContextResult
        {
            Host = "dynamo",
            HostVersion = "2026",
            Dynamo = new DynamoInfo(
                IsLive: true,
                DynamoVersion: "3.7.0.4500",
                ActiveWorkspaceName: "AutomateRebar.dyn",
                ActiveWorkspacePath: @"C:\DynamoScripts\AutomateRebar.dyn",
                RunType: "Automatic",
                NodeCount: 42,
                WireCount: 56,
                SelectedNodeCount: 3,
                HasUnsavedChanges: false),
        };

        var json = BridgeJson.Serialize(context);
        using var doc = JsonDocument.Parse(json);
        var dynamo = doc.RootElement.GetProperty("dynamo");
        Assert.True(dynamo.GetProperty("isLive").GetBoolean());
        Assert.Equal("3.7.0.4500", dynamo.GetProperty("dynamoVersion").GetString());
        Assert.Equal("AutomateRebar.dyn", dynamo.GetProperty("activeWorkspaceName").GetString());
        Assert.Equal(@"C:\DynamoScripts\AutomateRebar.dyn", dynamo.GetProperty("activeWorkspacePath").GetString());
        Assert.Equal("Automatic", dynamo.GetProperty("runType").GetString());
        Assert.Equal(42, dynamo.GetProperty("nodeCount").GetInt32());
        Assert.Equal(56, dynamo.GetProperty("wireCount").GetInt32());
        Assert.Equal(3, dynamo.GetProperty("selectedNodeCount").GetInt32());
        Assert.False(dynamo.GetProperty("hasUnsavedChanges").GetBoolean());
        Assert.Equal(9, dynamo.EnumerateObject().Count());

        var back = BridgeJson.Deserialize<ContextResult>(json)!;
        Assert.Equal(context.Dynamo, back.Dynamo);

        var withoutDynamo = BridgeJson.Serialize(new ContextResult { Host = "revit" });
        Assert.DoesNotContain("dynamo", withoutDynamo);
    }

    [Fact]
    public async Task Context_shape_for_dynamo_drops_revit_fields_and_keeps_dynamo_block()
    {
        var pipe = NewPipe();
        var executor = new FakeRevitExecutor
        {
            ContextHandler = _ => new ContextResult
            {
                RevitVersion = "2026",
                IsFamily = false,
                Host = "dynamo",
                HostVersion = "2026",
                DocTitle = "AutomateRebar.dyn",
                DocPath = @"C:\DynamoScripts\AutomateRebar.dyn",
                Dynamo = new DynamoInfo(true, "3.7.0.4500", "AutomateRebar.dyn", @"C:\DynamoScripts\AutomateRebar.dyn", "Automatic", 42, 56, 3, false),
            },
        };
        using var listener = new PipeListener(pipe, new RequestDispatcher(executor, new BridgeSettings(), "2026", "Dynamo"));
        listener.Start();

        await using var client = new RevitBridgeClient(
            Options.Create(PipeOptions(pipe)),
            NullLogger<RevitBridgeClient>.Instance, Dynamo());

        var text = await new ContextService(client, new ResultFormatter()).ReadAsync(false, TestContext.Current.CancellationToken);

        using var json = JsonDocument.Parse(text);
        var root = json.RootElement;
        Assert.False(root.TryGetProperty("revitVersion", out _));
        Assert.False(root.TryGetProperty("isFamily", out _));
        Assert.Equal("dynamo", root.GetProperty("host").GetString());
        Assert.True(root.GetProperty("dynamo").GetProperty("isLive").GetBoolean());
        Assert.Equal("AutomateRebar.dyn", root.GetProperty("dynamo").GetProperty("activeWorkspaceName").GetString());
        Assert.Equal(42, root.GetProperty("dynamo").GetProperty("nodeCount").GetInt32());
        Assert.False(root.TryGetProperty("autocad", out _));
        Assert.False(root.TryGetProperty("navis", out _));
        Assert.False(root.TryGetProperty("etabs", out _));
        Assert.False(root.TryGetProperty("civil3d", out _));
        Assert.False(root.TryGetProperty("sap2000", out _));
        Assert.False(root.TryGetProperty("powerbi", out _));
        Assert.False(root.TryGetProperty("excel", out _));
        Assert.False(root.TryGetProperty("robot", out _));
        Assert.False(root.TryGetProperty("tekla", out _));
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
