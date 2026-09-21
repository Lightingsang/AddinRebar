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
using static HPRebar.Mcp.Server.Tests.RobotTestProfile;

namespace HPRebar.Mcp.Server.Tests;

/// <summary>
///     Engine tests for Robot Structural Analysis host profile and contracts: pipe naming, prefix, imports, globals, guard, analyzer,
///     hints, options configuration, context shaping, and wire isolation.
/// </summary>
public sealed class RobotProfileTests
{
    [Fact]
    public void Robot_constants_produce_the_pipe_prefix_imports_and_globals()
    {
        Assert.Equal("hprobot-mcp-2026", PipeNaming.For(PipeNaming.RobotHost, 2026));
        Assert.Equal("hprobot-mcp-2026", PipeNaming.For("ROBOT", 2026));
        Assert.Equal("hprobot-mcp-2026", PipeNaming.For("robot", 2026));
        Assert.Equal("robot.execute", JsonRpcMethods.For(JsonRpcMethods.RobotPrefix, JsonRpcMethods.ExecuteSuffix));
        Assert.Equal("execute", JsonRpcMethods.Suffix("robot.execute"));
        Assert.Equal("hprobot-mcp-2026", new BridgeOptions { HostId = "robot", HostVersion = 2026 }.PipeName);
        Assert.Contains("RobotOM", HostScriptContracts.RobotImports);
        Assert.Contains("HPRebar.McpBridge.Core.Scripting", HostScriptContracts.RobotImports);
        Assert.DoesNotContain("System.IO", HostScriptContracts.RobotImports);
        Assert.DoesNotContain("System.Reflection", HostScriptContracts.RobotImports);
        Assert.Equal(["robot", "structure", "units", "ct", "log", "progress", "args"], HostScriptContracts.RobotGlobals);
        Assert.Equal(300, HostScriptContracts.RobotHeavyMaxTimeoutSeconds);
        Assert.Equal("hprobot-mcp-2026", Robot().PipeName(2026));
        Assert.Equal("robot.context", Robot().Method(JsonRpcMethods.ContextSuffix));
        Assert.Equal("robot.analyze", Robot().Method(JsonRpcMethods.AnalyzeSuffix));
    }

    [Theory]
    [InlineData("robot.Quit(); return 1;", "Quit")]
    [InlineData("app.Quit(); return 1;", "Quit")]
    [InlineData("robot.ApplicationExit(); return 1;", "ApplicationExit")]
    [InlineData("robot.Interactive = 0; return 1;", "Interactive")]
    [InlineData("app.Interactive = 1; return 1;", "Interactive")]
    [InlineData("MessageBox.Show(\"hi\"); return 1;", "MessageBox")]
    [InlineData("HPRobot.McpBridge.BridgeEntry.Stop(); return 1;", "HPRobot.McpBridge")]
    [InlineData("HPRebar.McpBridge.Core.Host.McpBridgeHost.Current.Stop(); return 1;", "HPRebar.McpBridge.Core.Host")]
    [InlineData("using System.Windows.Forms; return 1;", "System.Windows.Forms")]
    [InlineData("var p = System.Diagnostics.Process.Start(\"cmd\"); return 1;", "System.Diagnostics.Process")]
    [InlineData("var o = Marshal.GetActiveObject(\"x\"); return 1;", "Marshal")]
    [InlineData("using System.Runtime.InteropServices; return 1;", "System.Runtime.InteropServices")]
    [InlineData("#r \"external.dll\"\nreturn 1;", "#r")]
    [InlineData("#load \"script.csx\"\nreturn 1;", "#load")]
    public void Robot_guard_profile_denies_quit_interactive_dialogs_bridge_internals_and_the_base_list(string code, string expected)
    {
        var violations = ScriptGuard.Check(code, GuardProfile.Robot);

        Assert.NotEmpty(violations);
        Assert.Contains(violations, v => v.Message.Contains(expected, StringComparison.Ordinal));
        Assert.All(violations, v => Assert.True(
            v.Message.Contains("Robot", StringComparison.Ordinal) ||
            v.Message.Contains("bridge owns", StringComparison.Ordinal) ||
            v.Message.Contains("directives", StringComparison.Ordinal) ||
            v.Message.Contains("not allowed in", StringComparison.Ordinal)));
    }

    [Fact]
    public void Global_alias_does_not_bypass_the_robot_bridge_namespace_denial()
    {
        var violations1 = ScriptGuard.Check("var h = global::HPRebar.McpBridge.Core.Host.McpBridgeHost.Current; return 1;", GuardProfile.Robot);
        Assert.Contains(violations1, v => v.Message.Contains("HPRebar.McpBridge.Core.Host", StringComparison.Ordinal));

        var violations2 = ScriptGuard.Check("var h = global::HPRobot.McpBridge.BridgeEntry.Stop(); return 1;", GuardProfile.Robot);
        Assert.Contains(violations2, v => v.Message.Contains("HPRobot.McpBridge", StringComparison.Ordinal));
    }

    [Fact]
    public void Robot_guard_profile_lets_reads_writes_and_analysis_through()
    {
        const string fine =
            "var nodes = structure.Nodes;\n" +
            "int count = nodes.GetAll().Count;\n" +
            "nodes.Create(1, 0.0, 0.0, 0.0);\n" +
            "nodes.Create(2, 0.0, 0.0, 3.0);\n" +
            "var bars = structure.Bars;\n" +
            "bars.Create(1, 1, 2);\n" +
            "var u = units.Label;\n" +
            "return new { count, units = u };";

        Assert.Empty(ScriptGuard.Check(fine, GuardProfile.Robot));
        Assert.Empty(ScriptGuard.Check("return robot.Project.FileName;", GuardProfile.Robot));
    }

    [Fact]
    public void Robot_analyzer_profile_never_reports_a_transaction()
    {
        const string code = "using (var t = new Transaction()) { } return robot.Project.FileName;";

        Assert.False(ScriptAnalyzer.Analyze(code, AnalyzerProfile.Robot).UsesTransaction);
        Assert.False(ScriptAnalyzer.Analyze("var t = robot.BeginTransaction(); return 1;", AnalyzerProfile.Robot).UsesTransaction);
        Assert.True(ScriptAnalyzer.Analyze(code, AnalyzerProfile.Revit).UsesTransaction);
        Assert.Empty(AnalyzerProfile.Robot.TransactionTypeNames);
        Assert.Empty(AnalyzerProfile.Robot.TransactionMethodNames);
    }

    [Fact]
    public void Hints_and_timeout_ceiling_survive_WithHostAssembly_and_default_to_null()
    {
        var copy = Robot(notConnected: NotConnectedHint, timeoutHint: TimeoutHint).WithHostAssembly(typeof(HostProfile).Assembly);

        Assert.Equal(300, copy.MaxTimeoutSeconds);
        Assert.Equal(NotConnectedHint, copy.BridgeNotConnectedHint);
        Assert.Equal(TimeoutHint, copy.TimeoutSemanticsHint);
        Assert.Null(Robot().BridgeNotConnectedHint);
    }

    [Fact]
    public void Validator_ceiling_follows_the_robot_profile()
    {
        Assert.DoesNotContain(ToolValidator.Validate(Candidate(300), null, [], false, Robot()).Errors, e => e.Contains("timeoutSeconds"));
        Assert.Contains(ToolValidator.Validate(Candidate(301), null, [], false, Robot()).Errors, e => e.Contains("between 5 and 300"));
        Assert.Contains(ToolValidator.Validate(Candidate(300), null, [], false, Robot(maxTimeout: 120)).Errors, e => e.Contains("between 5 and 120"));
    }

    [Fact]
    public void ConfigureOptions_seeds_HostVersion_from_the_profile_and_configuration_still_wins()
    {
        Assert.Equal(2026, BridgeOptionsFor(Robot(), new Dictionary<string, string?>()).HostVersion);
        Assert.Equal("hprobot-mcp-2026", BridgeOptionsFor(Robot(), new Dictionary<string, string?>()).PipeName);
        Assert.Throws<OptionsValidationException>(() => BridgeOptionsFor(Robot(), new Dictionary<string, string?> { ["Bridge:HostVersion"] = "1997" }));
    }

    [Fact]
    public void Wire_additions_are_invisible_when_unused()
    {
        var context = BridgeJson.Serialize(new ContextResult { RevitVersion = "2026", DocTitle = "Project1" });
        Assert.DoesNotContain("\"robot\"", context);
    }

    [Fact]
    public void Robot_info_round_trips_in_camel_case_and_is_omitted_when_null()
    {
        var context = new ContextResult
        {
            Host = "robot", HostVersion = "2026",
            Robot = new RobotInfo(
                IsAttached: true,
                AttachedPid: 7890,
                RobotVersion: "39.0",
                StructureType: "Frame3D",
                IsCalculated: true,
                HeavyOperationsEnabled: false,
                NodeCount: 150,
                BarCount: 200,
                PanelCount: 50,
                LoadCaseCount: 12),
        };

        var json = BridgeJson.Serialize(context);
        using var doc = JsonDocument.Parse(json);
        var robot = doc.RootElement.GetProperty("robot");
        Assert.True(robot.GetProperty("isAttached").GetBoolean());
        Assert.Equal(7890, robot.GetProperty("attachedPid").GetInt32());
        Assert.Equal("39.0", robot.GetProperty("robotVersion").GetString());
        Assert.Equal("Frame3D", robot.GetProperty("structureType").GetString());
        Assert.True(robot.GetProperty("isCalculated").GetBoolean());
        Assert.False(robot.GetProperty("heavyOperationsEnabled").GetBoolean());
        Assert.Equal(150, robot.GetProperty("nodeCount").GetInt32());
        Assert.Equal(200, robot.GetProperty("barCount").GetInt32());
        Assert.Equal(50, robot.GetProperty("panelCount").GetInt32());
        Assert.Equal(12, robot.GetProperty("loadCaseCount").GetInt32());
        Assert.Equal(10, robot.EnumerateObject().Count());

        var back = BridgeJson.Deserialize<ContextResult>(json)!;
        Assert.Equal(context.Robot, back.Robot);

        var withoutRobot = BridgeJson.Serialize(new ContextResult { Host = "excel" });
        Assert.DoesNotContain("robot", withoutRobot);
    }

    [Fact]
    public async Task Context_shape_for_robot_drops_revit_fields_and_keeps_robot_block()
    {
        var pipe = NewPipe();
        var executor = new FakeRevitExecutor
        {
            ContextHandler = _ => new ContextResult
            {
                RevitVersion = "2026", Host = "robot", HostVersion = "2026", DocTitle = "Structure1.rtd", DocPath = @"C:\Data\Structure1.rtd",
                Robot = new RobotInfo(true, 9999, "39.0", "Frame3D", true, true, 10, 20, 5, 2),
            },
        };
        using var listener = new PipeListener(pipe, new RequestDispatcher(executor, new BridgeSettings(), "2026", "Robot Structural Analysis"));
        listener.Start();

        await using var client = new RevitBridgeClient(
            Options.Create(PipeOptions(pipe)),
            NullLogger<RevitBridgeClient>.Instance, Robot());

        var text = await new ContextService(client, new ResultFormatter()).ReadAsync(false, TestContext.Current.CancellationToken);

        using var json = JsonDocument.Parse(text);
        var root = json.RootElement;
        Assert.False(root.TryGetProperty("revitVersion", out _));
        Assert.False(root.TryGetProperty("isFamily", out _));
        Assert.Equal("robot", root.GetProperty("host").GetString());
        Assert.True(root.GetProperty("robot").GetProperty("isAttached").GetBoolean());
        Assert.Equal(9999, root.GetProperty("robot").GetProperty("attachedPid").GetInt32());
        Assert.Equal("Frame3D", root.GetProperty("robot").GetProperty("structureType").GetString());
        Assert.False(root.TryGetProperty("autocad", out _));
        Assert.False(root.TryGetProperty("navis", out _));
        Assert.False(root.TryGetProperty("etabs", out _));
        Assert.False(root.TryGetProperty("civil3d", out _));
        Assert.False(root.TryGetProperty("sap2000", out _));
        Assert.False(root.TryGetProperty("powerbi", out _));
        Assert.False(root.TryGetProperty("excel", out _));
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
