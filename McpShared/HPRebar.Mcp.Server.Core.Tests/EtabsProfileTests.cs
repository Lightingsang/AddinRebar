using System.Text.Json;
using HPRebar.Mcp.Contracts;
using HPRebar.Mcp.Contracts.JsonRpc;
using HPRebar.Mcp.Contracts.Messages;
using HPRebar.Mcp.Server.Bootstrap;
using HPRebar.Mcp.Server.Hosts;
using HPRebar.Mcp.Server.Models;
using HPRebar.Mcp.Server.Registry;
using HPRebar.McpBridge.Core.Scripting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;
using static HPRebar.Mcp.Server.Tests.EtabsTestProfile;

namespace HPRebar.Mcp.Server.Tests;

/// <summary>
///     What the engine gained for a fourth host (ETABS — out-of-process COM, a bridge that is a separate program,
///     no transaction to roll back) and what it must keep for the first three: the `etabs` constants and profiles,
///     host-supplied message hints that default to the historical text, the profile version seeding the bridge
///     options, and wire additions that stay invisible when unused. The pipe-level behaviour is in
///     <see cref="EtabsBridgeMessagesTests"/>.
/// </summary>
public sealed class EtabsProfileTests
{
    [Fact]
    public void Etabs_constants_produce_the_pipe_prefix_imports_and_globals()
    {
        Assert.Equal("hpetabs-mcp-22", PipeNaming.For(PipeNaming.EtabsHost, 22));
        Assert.Equal("hpetabs-mcp-22", PipeNaming.For("ETABS", 22));
        Assert.Equal("etabs.execute", JsonRpcMethods.For(JsonRpcMethods.EtabsPrefix, JsonRpcMethods.ExecuteSuffix));
        Assert.Equal("execute", JsonRpcMethods.Suffix("etabs.execute"));
        Assert.Equal("hpetabs-mcp-22", new BridgeOptions { HostId = "etabs", HostVersion = 22 }.PipeName);
        Assert.Contains("ETABSv1", HostScriptContracts.EtabsImports);
        Assert.DoesNotContain("CSiAPIv1", HostScriptContracts.EtabsImports);
        Assert.DoesNotContain("System.Runtime.InteropServices", HostScriptContracts.EtabsImports);
        Assert.Equal(["sapModel", "etabs", "units", "ct", "log", "progress", "args"], HostScriptContracts.EtabsGlobals);
        Assert.Equal(600, HostScriptContracts.EtabsHeavyMaxTimeoutSeconds);
        Assert.Equal("hpetabs-mcp-22", Etabs().PipeName(22));
        Assert.Equal("etabs.analyze", Etabs().Method(JsonRpcMethods.AnalyzeSuffix));
    }

    [Theory]
    [InlineData("etabs.ApplicationExit(false); return 1;", ".ApplicationExit")]
    [InlineData("etabs.ApplicationStart(); return 1;", ".ApplicationStart")]
    [InlineData("etabs.Hide(); return 1;", ".Hide")]
    [InlineData("etabs.SetAsActiveObject(); return 1;", ".SetAsActiveObject")]
    [InlineData("var h = new Helper(); return 1;", "Helper")]
    [InlineData("var o = new ETABSv1.Helper().GetObject(\"CSI.ETABS.API.ETABSObject\"); return 1;", "Helper")]
    [InlineData("MessageBox.Show(\"hi\"); return 1;", "MessageBox")]
    [InlineData("HPEtabs.McpBridge.BridgeEntry.Stop(); return 1;", "HPEtabs.McpBridge")]
    [InlineData("HPRebar.McpBridge.Core.Host.McpBridgeHost.Current.Stop(); return 1;", "HPRebar.McpBridge.Core.Host")]
    [InlineData("using System.Windows.Forms; return 1;", "System.Windows.Forms")]
    [InlineData("var o = Marshal.GetActiveObject(\"x\"); return 1;", "Marshal")]
    [InlineData("using System.Runtime.InteropServices; return 1;", "System.Runtime.InteropServices")]
    [InlineData("var e = Expression.Call(Expression.Constant(sapModel), \"InitializeNewModel\", null); return 1;", "Expression")]
    public void Etabs_guard_profile_denies_attach_lifecycle_bridge_internals_and_the_base_list(string code, string expected)
    {
        var violations = ScriptGuard.Check(code, GuardProfile.Etabs);

        Assert.NotEmpty(violations);
        Assert.Contains(violations, v => v.Message.Contains(expected, StringComparison.Ordinal));
        Assert.All(violations, v => Assert.Contains("ETABS", v.Message));
    }

    [Theory]
    [InlineData("global::System.IO.File.WriteAllText(\"x\", \"y\"); return 1;", "System.IO")]
    [InlineData("var p = global::System.Diagnostics.Process.Start(\"cmd\"); return 1;", "System.Diagnostics.Process")]
    [InlineData("var m = global::System.Runtime.InteropServices.Marshal.GetActiveObject(\"x\"); return 1;", "System.Runtime.InteropServices")]
    public void Global_alias_does_not_bypass_the_base_namespace_denials_in_any_profile(string code, string expected)
    {
        foreach (var profile in new[] { GuardProfile.Revit, GuardProfile.Autocad, GuardProfile.Navis, GuardProfile.Etabs })
        {
            var violations = ScriptGuard.Check(code, profile);

            Assert.NotEmpty(violations);
            Assert.Contains(violations, v => v.Message.Contains(expected, StringComparison.Ordinal));
        }
    }

    [Fact]
    public void Global_alias_does_not_bypass_the_etabs_bridge_namespace_denial()
    {
        var violations = ScriptGuard.Check("var h = global::HPRebar.McpBridge.Core.Host.McpBridgeHost.Current; return 1;", GuardProfile.Etabs);

        Assert.Contains(violations, v => v.Message.Contains("HPRebar.McpBridge.Core.Host", StringComparison.Ordinal));
    }

    [Fact]
    public void Etabs_guard_profile_lets_reads_writes_and_analysis_through()
    {
        // Writing and destructive members are the ETABS bridge's business (tiers, snapshot, second opt-in), not the guard's.
        const string fine =
            "int n = 0; string[] names = null;\n" +
            "int ret = sapModel.PointObj.GetNameList(ref n, ref names);\n" +
            "ret = sapModel.FrameObj.SetSection(args.Str(\"frame\", \"1\"), args.Str(\"section\", \"C40x40\"));\n" +
            "ret = sapModel.SetPresentUnits(eUnits.kN_mm_C);\n" +
            "ret = sapModel.Analyze.RunAnalysis();\n" +
            "var visible = etabs.Visible();\n" +
            "return new { n, ret, visible, units = units.Label };";

        Assert.Empty(ScriptGuard.Check(fine, GuardProfile.Etabs));
        Assert.Empty(ScriptGuard.Check("return sapModel.GetModelFilename();", GuardProfile.Etabs));
    }

    [Fact]
    public void Etabs_analyzer_profile_never_reports_a_transaction()
    {
        const string code = "using (var t = new Transaction()) { } return sapModel.GetModelFilename();";

        Assert.False(ScriptAnalyzer.Analyze(code, AnalyzerProfile.Etabs).UsesTransaction);
        Assert.False(ScriptAnalyzer.Analyze("var t = sapModel.BeginTransaction(); return 1;", AnalyzerProfile.Etabs).UsesTransaction);
        // The Revit analyzer still sees a Transaction in the same text: the ETABS profile does not leak into it.
        Assert.True(ScriptAnalyzer.Analyze(code, AnalyzerProfile.Revit).UsesTransaction);
    }

    [Fact]
    public void Hints_and_timeout_ceiling_survive_WithHostAssembly_and_default_to_null()
    {
        var copy = Etabs(notConnected: NotConnectedHint, timeoutHint: TimeoutHint).WithHostAssembly(typeof(HostProfile).Assembly);

        Assert.Equal(600, copy.MaxTimeoutSeconds);
        Assert.Equal(NotConnectedHint, copy.BridgeNotConnectedHint);
        Assert.Equal(TimeoutHint, copy.TimeoutSemanticsHint);
        Assert.Null(HostProfile.Revit.BridgeNotConnectedHint);
        Assert.Null(HostProfile.Revit.TimeoutSemanticsHint);
        Assert.Null(Etabs().BridgeNotConnectedHint);
    }

    [Fact]
    public void Validator_ceiling_follows_the_etabs_profile()
    {
        Assert.DoesNotContain(ToolValidator.Validate(Candidate(600), null, [], false, Etabs()).Errors, e => e.Contains("timeoutSeconds"));
        Assert.Contains(ToolValidator.Validate(Candidate(601), null, [], false, Etabs()).Errors, e => e.Contains("between 5 and 600"));
        Assert.Contains(ToolValidator.Validate(Candidate(600), null, [], false, Etabs(maxTimeout: 120)).Errors, e => e.Contains("between 5 and 120"));
    }

    [Fact]
    public void ConfigureOptions_seeds_HostVersion_from_the_profile_and_configuration_still_wins()
    {
        Assert.Equal(22, BridgeOptionsFor(Etabs(), new Dictionary<string, string?>()).HostVersion);
        Assert.Equal("hpetabs-mcp-22", BridgeOptionsFor(Etabs(), new Dictionary<string, string?>()).PipeName);
        Assert.Equal(2026, BridgeOptionsFor(HostProfile.Revit, new Dictionary<string, string?>()).HostVersion);
        Assert.Equal("hprebar-mcp-r2026", BridgeOptionsFor(HostProfile.Revit, new Dictionary<string, string?>()).PipeName);
        Assert.Equal(2025, BridgeOptionsFor(HostProfile.Revit, new Dictionary<string, string?> { ["Bridge:HostVersion"] = "2025" }).HostVersion);
        Assert.Equal(2025, BridgeOptionsFor(HostProfile.Revit, new Dictionary<string, string?> { ["Bridge:RevitVersion"] = "2025" }).HostVersion);
        Assert.Throws<OptionsValidationException>(() => BridgeOptionsFor(Etabs(), new Dictionary<string, string?> { ["Bridge:HostVersion"] = "2026" }));
    }

    private static BridgeOptions BridgeOptionsFor(IHostProfile profile, Dictionary<string, string?> settings)
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
        var services = new ServiceCollection();
        McpServerHost.ConfigureOptions(services, configuration, profile);
        using var provider = services.BuildServiceProvider();
        return provider.GetRequiredService<IOptions<BridgeOptions>>().Value;
    }

    [Fact]
    public void Wire_additions_are_invisible_when_unused()
    {
        var context = BridgeJson.Serialize(new ContextResult { RevitVersion = "2026", DocTitle = "Project1" });
        Assert.DoesNotContain("\"etabs\"", context);
        Assert.DoesNotContain("\"navis\"", context);

        var result = BridgeJson.Serialize(new ExecuteResult { Value = JsonSerializer.SerializeToElement(1) });
        Assert.DoesNotContain("\"snapshot\"", result);
        Assert.Contains("\"snapshot\":\"20260916-230000-assign-section.EDB\"", BridgeJson.Serialize(new ExecuteResult { Snapshot = "20260916-230000-assign-section.EDB" }));

        var analyze = BridgeJson.Serialize(new AnalyzeRequest("return 1;"));
        Assert.DoesNotContain("\"transaction\"", analyze);
        Assert.Contains("\"transaction\":\"none\"", BridgeJson.Serialize(new AnalyzeRequest("return 1;", "none")));
        // A request from a server built before the field existed still deserialises, with the mode unknown.
        var old = BridgeJson.Deserialize<AnalyzeRequest>("""{"code":"return 1;"}""");
        Assert.NotNull(old);
        Assert.Null(old!.Transaction);
        Assert.Equal("return 1;", old.Code);
    }

    [Fact]
    public void Snapshot_reaches_the_model_as_a_file_name_even_if_a_bridge_sent_a_path()
    {
        var formatter = new HPRebar.Mcp.Server.Services.ResultFormatter();

        var leaked = formatter.FromExecute(new ExecuteResult { Snapshot = @"C:\Users\someone\AppData\Local\HPEtabs\McpBridge\snapshots\Tower\20260916-230000-x.EDB" });
        var clean = formatter.FromExecute(new ExecuteResult { Snapshot = "20260916-230000-x.EDB" });
        var none = formatter.FromExecute(new ExecuteResult { Value = JsonSerializer.SerializeToElement(1) });

        Assert.Contains("\"snapshot\":\"20260916-230000-x.EDB\"", Text(leaked));
        Assert.DoesNotContain("someone", Text(leaked));
        Assert.Contains("\"snapshot\":\"20260916-230000-x.EDB\"", Text(clean));
        Assert.DoesNotContain("snapshot", Text(none));
    }

    private static string Text(ModelContextProtocol.Protocol.CallToolResult result) =>
        ((ModelContextProtocol.Protocol.TextContentBlock)result.Content[0]).Text;
}
