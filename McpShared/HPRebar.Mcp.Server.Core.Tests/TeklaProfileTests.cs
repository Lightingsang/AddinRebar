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
using static HPRebar.Mcp.Server.Tests.TeklaTestProfile;

namespace HPRebar.Mcp.Server.Tests;

/// <summary>
///     Engine tests for Tekla Structures host profile and contracts: pipe naming, prefix, imports, globals, guard, analyzer,
///     hints, options configuration, context shaping, and wire isolation.
/// </summary>
public sealed class TeklaProfileTests
{
    [Fact]
    public void Tekla_constants_produce_the_pipe_prefix_imports_and_globals()
    {
        Assert.Equal("hptekla-mcp-2025", PipeNaming.For(PipeNaming.TeklaHost, 2025));
        Assert.Equal("hptekla-mcp-2025", PipeNaming.For("TEKLA", 2025));
        Assert.Equal("hptekla-mcp-2025", PipeNaming.For("tekla", 2025));
        Assert.Equal("tekla.execute", JsonRpcMethods.For(JsonRpcMethods.TeklaPrefix, JsonRpcMethods.ExecuteSuffix));
        Assert.Equal("execute", JsonRpcMethods.Suffix("tekla.execute"));
        Assert.Equal("hptekla-mcp-2025", new BridgeOptions { HostId = "tekla", HostVersion = 2025 }.PipeName);
        Assert.Contains("Tekla.Structures", HostScriptContracts.TeklaImports);
        Assert.Contains("Tekla.Structures.Model", HostScriptContracts.TeklaImports);
        Assert.Contains("Tekla.Structures.Geometry3d", HostScriptContracts.TeklaImports);
        Assert.Contains("Tekla.Structures.Catalogs", HostScriptContracts.TeklaImports);
        Assert.Contains("HPRebar.McpBridge.Core.Scripting", HostScriptContracts.TeklaImports);
        Assert.DoesNotContain("System.IO", HostScriptContracts.TeklaImports);
        Assert.DoesNotContain("System.Reflection", HostScriptContracts.TeklaImports);
        Assert.Equal(["model", "ct", "log", "progress", "args"], HostScriptContracts.TeklaGlobals);
        Assert.Equal(600, HostScriptContracts.TeklaHeavyMaxTimeoutSeconds);
        Assert.Equal("hptekla-mcp-2025", Tekla().PipeName(2025));
        Assert.Equal("tekla.context", Tekla().Method(JsonRpcMethods.ContextSuffix));
        Assert.Equal("tekla.analyze", Tekla().Method(JsonRpcMethods.AnalyzeSuffix));
    }

    [Theory]
    [InlineData("model.CommitChanges(); return 1;", "CommitChanges")]
    [InlineData("MessageBox.Show(\"hi\"); return 1;", "MessageBox")]
    [InlineData("var p = new Picker(); return 1;", "Picker")]
    [InlineData("p.PickObject(); return 1;", "PickObject")]
    [InlineData("p.PickObjects(); return 1;", "PickObjects")]
    [InlineData("p.PickPoint(); return 1;", "PickPoint")]
    [InlineData("p.PickPoints(); return 1;", "PickPoints")]
    [InlineData("p.PickLine(); return 1;", "PickLine")]
    [InlineData("p.PickPolygon(); return 1;", "PickPolygon")]
    [InlineData("p.PickFace(); return 1;", "PickFace")]
    [InlineData("m.CommitChanges(); return 1;", "CommitChanges")]
    [InlineData("(model).CommitChanges(); return 1;", "CommitChanges")]
    [InlineData("Application.Exit(); return 1;", "Exit")]
    [InlineData("app.Quit(); return 1;", "Quit")]
    [InlineData("using System.Windows.Forms; return 1;", "System.Windows.Forms")]
    [InlineData("using Tekla.Structures.Dialog; return 1;", "Tekla.Structures.Dialog")]
    [InlineData("using Tekla.Structures.Drawing.UI; return 1;", "Tekla.Structures.Drawing.UI")]
    [InlineData("HPTekla.McpBridge.BridgeEntry.Stop(); return 1;", "HPTekla.McpBridge")]
    [InlineData("HPRebar.McpBridge.Core.Host.McpBridgeHost.Current.Stop(); return 1;", "HPRebar.McpBridge.Core.Host")]
    [InlineData("var p = System.Diagnostics.Process.Start(\"cmd\"); return 1;", "System.Diagnostics.Process")]
    [InlineData("#r \"external.dll\"\nreturn 1;", "#r")]
    [InlineData("#load \"script.csx\"\nreturn 1;", "#load")]
    public void Tekla_guard_profile_denies_picker_dialogs_quit_commit_and_the_base_list(string code, string expected)
    {
        var violations = ScriptGuard.Check(code, GuardProfile.Tekla);

        Assert.NotEmpty(violations);
        Assert.Contains(violations, v => v.Message.Contains(expected, StringComparison.Ordinal));
        Assert.All(violations, v => Assert.True(
            v.Message.Contains("Tekla", StringComparison.Ordinal) ||
            v.Message.Contains("bridge owns", StringComparison.Ordinal) ||
            v.Message.Contains("directives", StringComparison.Ordinal) ||
            v.Message.Contains("not allowed in", StringComparison.Ordinal)));
    }

    [Fact]
    public void Global_alias_does_not_bypass_the_tekla_bridge_namespace_denial()
    {
        var violations1 = ScriptGuard.Check("var h = global::HPRebar.McpBridge.Core.Host.McpBridgeHost.Current; return 1;", GuardProfile.Tekla);
        Assert.Contains(violations1, v => v.Message.Contains("HPRebar.McpBridge.Core.Host", StringComparison.Ordinal));

        var violations2 = ScriptGuard.Check("var h = global::HPTekla.McpBridge.BridgeEntry.Stop(); return 1;", GuardProfile.Tekla);
        Assert.Contains(violations2, v => v.Message.Contains("HPTekla.McpBridge", StringComparison.Ordinal));
    }

    [Fact]
    public void Tekla_guard_profile_lets_reads_writes_and_geometry_through()
    {
        const string fine =
            "var info = model.GetInfo();\n" +
            "var beam = new Beam();\n" +
            "beam.StartPoint = new Point(0, 0, 0);\n" +
            "beam.EndPoint = new Point(1000, 0, 0);\n" +
            "beam.Profile.ProfileString = \"HEA300\";\n" +
            "beam.Material.MaterialString = \"S235JR\";\n" +
            "beam.Insert();\n" +
            "return new { info.ModelName, id = beam.Identifier.ID };";

        Assert.Empty(ScriptGuard.Check(fine, GuardProfile.Tekla));
        Assert.Empty(ScriptGuard.Check("return model.GetInfo().ModelName;", GuardProfile.Tekla));
    }

    [Fact]
    public void Tekla_analyzer_profile_detects_CommitChanges_as_transaction_method()
    {
        const string commitCode = "model.CommitChanges(); return 1;";
        const string ctorCode = "using (var t = new Transaction()) { } return 1;";

        Assert.True(ScriptAnalyzer.Analyze(commitCode, AnalyzerProfile.Tekla).UsesTransaction);
        Assert.False(ScriptAnalyzer.Analyze(ctorCode, AnalyzerProfile.Tekla).UsesTransaction);
        Assert.True(ScriptAnalyzer.Analyze(ctorCode, AnalyzerProfile.Revit).UsesTransaction);
        Assert.Empty(AnalyzerProfile.Tekla.TransactionTypeNames);
        Assert.Contains("CommitChanges", AnalyzerProfile.Tekla.TransactionMethodNames);
    }

    [Fact]
    public void Hints_and_timeout_ceiling_survive_WithHostAssembly_and_default_to_null()
    {
        var copy = Tekla(notConnected: NotConnectedHint, timeoutHint: TimeoutHint).WithHostAssembly(typeof(HostProfile).Assembly);

        Assert.Equal(600, copy.MaxTimeoutSeconds);
        Assert.Equal(NotConnectedHint, copy.BridgeNotConnectedHint);
        Assert.Equal(TimeoutHint, copy.TimeoutSemanticsHint);
        Assert.Null(Tekla().BridgeNotConnectedHint);
    }

    [Fact]
    public void Validator_ceiling_follows_the_tekla_profile()
    {
        Assert.DoesNotContain(ToolValidator.Validate(Candidate(600), null, [], false, Tekla()).Errors, e => e.Contains("timeoutSeconds"));
        Assert.Contains(ToolValidator.Validate(Candidate(601), null, [], false, Tekla()).Errors, e => e.Contains("between 5 and 600"));
        Assert.Contains(ToolValidator.Validate(Candidate(600), null, [], false, Tekla(maxTimeout: 120)).Errors, e => e.Contains("between 5 and 120"));
    }

    [Fact]
    public void ConfigureOptions_seeds_HostVersion_from_the_profile_and_configuration_still_wins()
    {
        Assert.Equal(2025, BridgeOptionsFor(Tekla(), new Dictionary<string, string?>()).HostVersion);
        Assert.Equal("hptekla-mcp-2025", BridgeOptionsFor(Tekla(), new Dictionary<string, string?>()).PipeName);
        Assert.Throws<OptionsValidationException>(() => BridgeOptionsFor(Tekla(), new Dictionary<string, string?> { ["Bridge:HostVersion"] = "1997" }));
    }

    [Fact]
    public void Wire_additions_are_invisible_when_unused()
    {
        var context = BridgeJson.Serialize(new ContextResult { RevitVersion = "2026", DocTitle = "Project1" });
        Assert.DoesNotContain("\"tekla\"", context);
    }

    [Fact]
    public void Tekla_info_round_trips_in_camel_case_and_is_omitted_when_null()
    {
        var context = new ContextResult
        {
            Host = "tekla",
            HostVersion = "2025",
            Tekla = new TeklaInfo(
                IsConnected: true,
                ModelName: "StandardModel",
                ModelPath: @"C:\TeklaStructuresModels\StandardModel",
                ProjectName: "ProjectAlpha",
                TeklaVersion: "2025.0",
                HeavyOperationsEnabled: false,
                PartCount: 250,
                RebarCount: 500,
                DrawingCount: 15),
        };

        var json = BridgeJson.Serialize(context);
        using var doc = JsonDocument.Parse(json);
        var tekla = doc.RootElement.GetProperty("tekla");
        Assert.True(tekla.GetProperty("isConnected").GetBoolean());
        Assert.Equal("StandardModel", tekla.GetProperty("modelName").GetString());
        Assert.Equal(@"C:\TeklaStructuresModels\StandardModel", tekla.GetProperty("modelPath").GetString());
        Assert.Equal("ProjectAlpha", tekla.GetProperty("projectName").GetString());
        Assert.Equal("2025.0", tekla.GetProperty("teklaVersion").GetString());
        Assert.False(tekla.GetProperty("heavyOperationsEnabled").GetBoolean());
        Assert.Equal(250, tekla.GetProperty("partCount").GetInt32());
        Assert.Equal(500, tekla.GetProperty("rebarCount").GetInt32());
        Assert.Equal(15, tekla.GetProperty("drawingCount").GetInt32());
        Assert.Equal(9, tekla.EnumerateObject().Count());

        var back = BridgeJson.Deserialize<ContextResult>(json)!;
        Assert.Equal(context.Tekla, back.Tekla);

        var withoutTekla = BridgeJson.Serialize(new ContextResult { Host = "revit" });
        Assert.DoesNotContain("tekla", withoutTekla);
    }

    [Fact]
    public async Task Context_shape_for_tekla_drops_revit_fields_and_keeps_tekla_block()
    {
        var pipe = NewPipe();
        var executor = new FakeRevitExecutor
        {
            ContextHandler = _ => new ContextResult
            {
                RevitVersion = "2025",
                Host = "tekla",
                HostVersion = "2025",
                DocTitle = "StandardModel",
                DocPath = @"C:\TeklaStructuresModels\StandardModel",
                Tekla = new TeklaInfo(true, "StandardModel", @"C:\TeklaStructuresModels\StandardModel", "ProjectAlpha", "2025.0", true, 250, 500, 15),
            },
        };
        using var listener = new PipeListener(pipe, new RequestDispatcher(executor, new BridgeSettings(), "2025", "Tekla Structures"));
        listener.Start();

        await using var client = new RevitBridgeClient(
            Options.Create(PipeOptions(pipe)),
            NullLogger<RevitBridgeClient>.Instance, Tekla());

        var text = await new ContextService(client, new ResultFormatter()).ReadAsync(false, TestContext.Current.CancellationToken);

        using var json = JsonDocument.Parse(text);
        var root = json.RootElement;
        Assert.False(root.TryGetProperty("revitVersion", out _));
        Assert.False(root.TryGetProperty("isFamily", out _));
        Assert.Equal("tekla", root.GetProperty("host").GetString());
        Assert.True(root.GetProperty("tekla").GetProperty("isConnected").GetBoolean());
        Assert.Equal("StandardModel", root.GetProperty("tekla").GetProperty("modelName").GetString());
        Assert.Equal(250, root.GetProperty("tekla").GetProperty("partCount").GetInt32());
        Assert.False(root.TryGetProperty("autocad", out _));
        Assert.False(root.TryGetProperty("navis", out _));
        Assert.False(root.TryGetProperty("etabs", out _));
        Assert.False(root.TryGetProperty("civil3d", out _));
        Assert.False(root.TryGetProperty("sap2000", out _));
        Assert.False(root.TryGetProperty("powerbi", out _));
        Assert.False(root.TryGetProperty("excel", out _));
        Assert.False(root.TryGetProperty("robot", out _));
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
