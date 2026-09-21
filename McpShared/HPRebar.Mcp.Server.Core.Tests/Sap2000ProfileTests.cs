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
using static HPRebar.Mcp.Server.Tests.Sap2000TestProfile;

namespace HPRebar.Mcp.Server.Tests;

/// <summary>
///     Engine tests for SAP2000 host profile and contracts: pipe, prefix, imports, globals, guard, analyzer,
///     hints, options configuration, and wire isolation.
/// </summary>
public sealed class Sap2000ProfileTests
{
    [Fact]
    public void Sap2000_constants_produce_the_pipe_prefix_imports_and_globals()
    {
        Assert.Equal("hpsap2000-mcp-27", PipeNaming.For(PipeNaming.Sap2000Host, 27));
        Assert.Equal("hpsap2000-mcp-27", PipeNaming.For("SAP2000", 27));
        Assert.Equal("sap2000.execute", JsonRpcMethods.For(JsonRpcMethods.Sap2000Prefix, JsonRpcMethods.ExecuteSuffix));
        Assert.Equal("execute", JsonRpcMethods.Suffix("sap2000.execute"));
        Assert.Equal("hpsap2000-mcp-27", new BridgeOptions { HostId = "sap2000", HostVersion = 27 }.PipeName);
        Assert.Contains("SAP2000v1", HostScriptContracts.Sap2000Imports);
        Assert.DoesNotContain("CSiAPIv1", HostScriptContracts.Sap2000Imports);
        Assert.DoesNotContain("System.Runtime.InteropServices", HostScriptContracts.Sap2000Imports);
        Assert.Equal(["sapModel", "sap", "units", "ct", "log", "progress", "args"], HostScriptContracts.Sap2000Globals);
        Assert.Equal(600, HostScriptContracts.Sap2000HeavyMaxTimeoutSeconds);
        Assert.Equal("hpsap2000-mcp-27", Sap2000().PipeName(27));
        Assert.Equal("sap2000.analyze", Sap2000().Method(JsonRpcMethods.AnalyzeSuffix));
    }

    [Theory]
    [InlineData("sap.ApplicationExit(false); return 1;", ".ApplicationExit")]
    [InlineData("sap.ApplicationStart(); return 1;", ".ApplicationStart")]
    [InlineData("sap.Hide(); return 1;", ".Hide")]
    [InlineData("sap.SetAsActiveObject(); return 1;", ".SetAsActiveObject")]
    [InlineData("var h = new Helper(); return 1;", "Helper")]
    [InlineData("var o = new SAP2000v1.Helper().GetObject(\"CSI.SAP2000.API.SapObject\"); return 1;", "Helper")]
    [InlineData("MessageBox.Show(\"hi\"); return 1;", "MessageBox")]
    [InlineData("HPSap2000.McpBridge.BridgeEntry.Stop(); return 1;", "HPSap2000.McpBridge")]
    [InlineData("HPRebar.McpBridge.Core.Host.McpBridgeHost.Current.Stop(); return 1;", "HPRebar.McpBridge.Core.Host")]
    [InlineData("using System.Windows.Forms; return 1;", "System.Windows.Forms")]
    [InlineData("var o = Marshal.GetActiveObject(\"x\"); return 1;", "Marshal")]
    [InlineData("using System.Runtime.InteropServices; return 1;", "System.Runtime.InteropServices")]
    [InlineData("var e = Expression.Call(Expression.Constant(sapModel), \"InitializeNewModel\", null); return 1;", "Expression")]
    public void Sap2000_guard_profile_denies_attach_lifecycle_bridge_internals_and_the_base_list(string code, string expected)
    {
        var violations = ScriptGuard.Check(code, GuardProfile.Sap2000);

        Assert.NotEmpty(violations);
        Assert.Contains(violations, v => v.Message.Contains(expected, StringComparison.Ordinal));
        Assert.All(violations, v => Assert.Contains("SAP2000", v.Message));
    }

    [Fact]
    public void Global_alias_does_not_bypass_the_sap2000_bridge_namespace_denial()
    {
        var violations = ScriptGuard.Check("var h = global::HPRebar.McpBridge.Core.Host.McpBridgeHost.Current; return 1;", GuardProfile.Sap2000);

        Assert.Contains(violations, v => v.Message.Contains("HPRebar.McpBridge.Core.Host", StringComparison.Ordinal));
    }

    [Fact]
    public void Sap2000_guard_profile_lets_reads_writes_and_analysis_through()
    {
        const string fine =
            "int n = 0; string[] names = null;\n" +
            "int ret = sapModel.PointObj.GetNameList(ref n, ref names);\n" +
            "ret = sapModel.FrameObj.SetSection(args.Str(\"frame\", \"1\"), args.Str(\"section\", \"W14X90\"));\n" +
            "ret = sapModel.SetPresentUnits(eUnits.kN_m_C);\n" +
            "ret = sapModel.Analyze.RunAnalysis();\n" +
            "var visible = sap.Visible();\n" +
            "return new { n, ret, visible, units = units.Label };";

        Assert.Empty(ScriptGuard.Check(fine, GuardProfile.Sap2000));
        Assert.Empty(ScriptGuard.Check("return sapModel.GetModelFilename();", GuardProfile.Sap2000));
    }

    [Fact]
    public void Sap2000_analyzer_profile_never_reports_a_transaction()
    {
        const string code = "using (var t = new Transaction()) { } return sapModel.GetModelFilename();";

        Assert.False(ScriptAnalyzer.Analyze(code, AnalyzerProfile.Sap2000).UsesTransaction);
        Assert.False(ScriptAnalyzer.Analyze("var t = sapModel.BeginTransaction(); return 1;", AnalyzerProfile.Sap2000).UsesTransaction);
        Assert.True(ScriptAnalyzer.Analyze(code, AnalyzerProfile.Revit).UsesTransaction);
    }

    [Fact]
    public void Hints_and_timeout_ceiling_survive_WithHostAssembly_and_default_to_null()
    {
        var copy = Sap2000(notConnected: NotConnectedHint, timeoutHint: TimeoutHint).WithHostAssembly(typeof(HostProfile).Assembly);

        Assert.Equal(600, copy.MaxTimeoutSeconds);
        Assert.Equal(NotConnectedHint, copy.BridgeNotConnectedHint);
        Assert.Equal(TimeoutHint, copy.TimeoutSemanticsHint);
        Assert.Null(Sap2000().BridgeNotConnectedHint);
    }

    [Fact]
    public void Validator_ceiling_follows_the_sap2000_profile()
    {
        Assert.DoesNotContain(ToolValidator.Validate(Candidate(600), null, [], false, Sap2000()).Errors, e => e.Contains("timeoutSeconds"));
        Assert.Contains(ToolValidator.Validate(Candidate(601), null, [], false, Sap2000()).Errors, e => e.Contains("between 5 and 600"));
        Assert.Contains(ToolValidator.Validate(Candidate(600), null, [], false, Sap2000(maxTimeout: 120)).Errors, e => e.Contains("between 5 and 120"));
    }

    [Fact]
    public void ConfigureOptions_seeds_HostVersion_from_the_profile_and_configuration_still_wins()
    {
        Assert.Equal(27, BridgeOptionsFor(Sap2000(), new Dictionary<string, string?>()).HostVersion);
        Assert.Equal("hpsap2000-mcp-27", BridgeOptionsFor(Sap2000(), new Dictionary<string, string?>()).PipeName);
        Assert.Throws<OptionsValidationException>(() => BridgeOptionsFor(Sap2000(), new Dictionary<string, string?> { ["Bridge:HostVersion"] = "2026" }));
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
        Assert.DoesNotContain("\"sap2000\"", context);
    }
}
