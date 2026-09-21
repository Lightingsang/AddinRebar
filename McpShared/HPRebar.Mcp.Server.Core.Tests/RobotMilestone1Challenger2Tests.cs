using System.Reflection;
using System.Text.Json;
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
using static HPRebar.Mcp.Server.Tests.RobotTestProfile;

namespace HPRebar.Mcp.Server.Tests;

/// <summary>
///     Challenger 2 Empirical Verification Suite for Robot Structural Analysis MCP (Milestone M1):
///     1. Host Neutrality: zero RobotOM / host API assembly leakage into McpShared across all 9 hosts.
///     2. Pipe Naming: strict adherence to "hprobot-mcp-2026", whitespace/case invariance, version flexibility, mutual distinction.
///     3. JSON-RPC Methods: "robot." prefix enforcement, bijective mapping, and multi-host suffix isolation.
///     4. ContextResult Wire Invariants: camelCase serialization, null suppression, round-trip fidelity, and multi-host isolation.
///     5. Pipe Round-Trip & Dispatcher: real named pipe interaction with fake executor, progress event streaming, error codes, and ContextService shaping.
///     6. Guard & Analyzer Profiles: transaction-free semantics and defense against termination, dialogs, and bridge tampering.
/// </summary>
public sealed class RobotMilestone1Challenger2Tests
{
    // =========================================================================
    // 1. HOST NEUTRALITY & ZERO-HOST-API LEAKAGE ACROSS ALL 9 HOSTS
    // =========================================================================

    private static readonly string[] ForbiddenHostApiPrefixes =
    [
        // Revit
        "RevitAPI", "RevitAPIUI", "Nice3point",
        // AutoCAD
        "AcDbMgd", "AcMgd", "AcCoreMgd", "Autodesk.AutoCAD",
        // Navisworks
        "Autodesk.Navisworks", "Navisworks",
        // ETABS
        "ETABSv1", "CSi",
        // Civil 3D
        "AeccDbMgd", "AeccPressurePipesMgd", "AecBaseMgd",
        // SAP2000
        "SAP2000v1",
        // Power BI
        "Microsoft.AnalysisServices",
        // Excel
        "Microsoft.Office.Interop.Excel", "ClosedXML", "DocumentFormat.OpenXml", "ExcelDataReader",
        // Robot
        "RobotOM", "Interop.RobotOM"
    ];

    public static IEnumerable<object[]> AllSharedAssemblies() =>
    [
        [typeof(PipeNaming).Assembly, "HPRebar.Mcp.Contracts"],
        [typeof(PipeListener).Assembly, "HPRebar.McpBridge.Core"],
        [typeof(ResultFormatter).Assembly, "HPRebar.Mcp.Server.Core"],
    ];

    [Theory]
    [MemberData(nameof(AllSharedAssemblies))]
    public void McpShared_never_references_any_host_api_across_all_nine_supported_hosts(Assembly assembly, string assemblyName)
    {
        Assert.Equal(assemblyName, assembly.GetName().Name);

        var referencedAssemblies = assembly.GetReferencedAssemblies()
            .Select(a => a.Name ?? string.Empty)
            .ToArray();

        foreach (var forbiddenPrefix in ForbiddenHostApiPrefixes)
        {
            Assert.DoesNotContain(referencedAssemblies, name =>
                name.StartsWith(forbiddenPrefix, StringComparison.OrdinalIgnoreCase) ||
                name.Equals(forbiddenPrefix, StringComparison.OrdinalIgnoreCase));
        }
    }

    // =========================================================================
    // 2. PIPE NAMING STRICT VERIFICATION & ADVERSARIAL EDGE CASES
    // =========================================================================

    [Fact]
    public void PipeNaming_For_RobotHost_2026_strictly_returns_canonical_pipe_name()
    {
        Assert.Equal("hprobot-mcp-2026", PipeNaming.For(PipeNaming.RobotHost, 2026));
    }

    [Theory]
    [InlineData("robot", 2026, "hprobot-mcp-2026")]
    [InlineData("ROBOT", 2026, "hprobot-mcp-2026")]
    [InlineData("Robot", 2026, "hprobot-mcp-2026")]
    [InlineData("rObOt", 2026, "hprobot-mcp-2026")]
    [InlineData("  robot  ", 2026, "hprobot-mcp-2026")]
    [InlineData("\trobot\r\n", 2026, "hprobot-mcp-2026")]
    [InlineData(" \n robot \t ", 2026, "hprobot-mcp-2026")]
    [InlineData("robot", 2024, "hprobot-mcp-2024")]
    [InlineData("robot", 2025, "hprobot-mcp-2025")]
    [InlineData("robot", 2027, "hprobot-mcp-2027")]
    public void PipeNaming_For_normalizes_casing_and_whitespace_for_robot(string host, int version, string expectedPipe)
    {
        Assert.Equal(expectedPipe, PipeNaming.For(host, version));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t\r\n")]
    public void PipeNaming_For_throws_ArgumentException_on_empty_or_whitespace_host(string? invalidHost)
    {
        Assert.Throws<ArgumentException>(() => PipeNaming.For(invalidHost!, 2026));
    }

    [Fact]
    public void All_nine_host_constants_are_mutually_distinct()
    {
        var constants = new[]
        {
            PipeNaming.RevitHost,
            PipeNaming.AutocadHost,
            PipeNaming.NavisHost,
            PipeNaming.EtabsHost,
            PipeNaming.Civil3dHost,
            PipeNaming.Sap2000Host,
            PipeNaming.PowerBiHost,
            PipeNaming.ExcelHost,
            PipeNaming.RobotHost,
        };

        Assert.Equal(9, constants.Length);
        Assert.Equal(9, constants.Distinct(StringComparer.OrdinalIgnoreCase).Count());
    }

    [Fact]
    public void PipeNaming_produces_pairwise_distinct_pipes_across_all_nine_hosts_at_version_2026()
    {
        var hosts = new[]
        {
            PipeNaming.RevitHost,
            PipeNaming.AutocadHost,
            PipeNaming.NavisHost,
            PipeNaming.EtabsHost,
            PipeNaming.Civil3dHost,
            PipeNaming.Sap2000Host,
            PipeNaming.PowerBiHost,
            PipeNaming.ExcelHost,
            PipeNaming.RobotHost,
        };

        var pipes = hosts.Select(h => PipeNaming.For(h, 2026)).ToArray();

        Assert.Equal(9, pipes.Length);
        Assert.Equal(9, pipes.Distinct(StringComparer.OrdinalIgnoreCase).Count());
        Assert.Contains("hprobot-mcp-2026", pipes);
        Assert.All(pipes, p => Assert.StartsWith("hp", p));
        Assert.All(pipes, p => Assert.True(p.EndsWith("-2026") || p.EndsWith("-r2026")));
    }

    // =========================================================================
    // 3. JSON-RPC METHODS & BIJECTIVE ROUTING ACROSS ALL 9 HOSTS
    // =========================================================================

    [Fact]
    public void JsonRpc_prefixes_across_all_nine_hosts_are_distinct_and_end_with_period()
    {
        var prefixes = new[]
        {
            JsonRpcMethods.RevitPrefix,
            JsonRpcMethods.AutocadPrefix,
            JsonRpcMethods.NavisPrefix,
            JsonRpcMethods.EtabsPrefix,
            JsonRpcMethods.Civil3dPrefix,
            JsonRpcMethods.Sap2000Prefix,
            JsonRpcMethods.PowerBiPrefix,
            JsonRpcMethods.ExcelPrefix,
            JsonRpcMethods.RobotPrefix,
        };

        Assert.Equal(9, prefixes.Length);
        Assert.Equal(9, prefixes.Distinct(StringComparer.Ordinal).Count());
        Assert.All(prefixes, p => Assert.EndsWith(".", p));
    }

    [Theory]
    [InlineData(JsonRpcMethods.PingSuffix, "robot.ping")]
    [InlineData(JsonRpcMethods.ContextSuffix, "robot.context")]
    [InlineData(JsonRpcMethods.InspectSuffix, "robot.inspect")]
    [InlineData(JsonRpcMethods.ExecuteSuffix, "robot.execute")]
    [InlineData(JsonRpcMethods.CancelSuffix, "robot.cancel")]
    [InlineData(JsonRpcMethods.AnalyzeSuffix, "robot.analyze")]
    [InlineData(JsonRpcMethods.ProgressSuffix, "robot.progress")]
    [InlineData(JsonRpcMethods.LogSuffix, "robot.log")]
    [InlineData(JsonRpcMethods.StatusSuffix, "robot.status")]
    public void JsonRpcMethods_For_strictly_uses_robot_prefix_and_is_bijective_with_Suffix(string suffix, string expectedFull)
    {
        var fullMethod = JsonRpcMethods.For(JsonRpcMethods.RobotPrefix, suffix);
        Assert.Equal(expectedFull, fullMethod);
        Assert.StartsWith("robot.", fullMethod);
        Assert.Equal(suffix, JsonRpcMethods.Suffix(fullMethod));
    }

    [Fact]
    public void RobotTestProfile_Method_matches_JsonRpcMethods_For()
    {
        var profile = Robot();
        Assert.Equal("robot.execute", profile.Method(JsonRpcMethods.ExecuteSuffix));
        Assert.Equal("robot.context", profile.Method(JsonRpcMethods.ContextSuffix));
        Assert.Equal("robot.ping", profile.Method(JsonRpcMethods.PingSuffix));
        Assert.Equal("robot.cancel", profile.Method(JsonRpcMethods.CancelSuffix));
        Assert.Equal("robot.analyze", profile.Method(JsonRpcMethods.AnalyzeSuffix));
    }

    [Theory]
    [InlineData("robot", "execute")] // prefix missing '.'
    [InlineData("robot_prefix", "execute")] // prefix missing '.'
    [InlineData("", "execute")] // empty prefix
    [InlineData(null, "execute")] // null prefix
    public void JsonRpcMethods_For_throws_ArgumentException_on_invalid_prefix(string? invalidPrefix, string suffix)
    {
        Assert.Throws<ArgumentException>(() => JsonRpcMethods.For(invalidPrefix!, suffix));
    }

    [Theory]
    [InlineData("robot.", "")] // empty suffix
    [InlineData("robot.", null)] // null suffix
    [InlineData("robot.", "execute.sub")] // multi-segment suffix
    [InlineData("robot.", ".execute")] // suffix with leading dot
    public void JsonRpcMethods_For_throws_ArgumentException_on_invalid_suffix(string prefix, string? invalidSuffix)
    {
        Assert.Throws<ArgumentException>(() => JsonRpcMethods.For(prefix, invalidSuffix!));
    }

    [Theory]
    [InlineData("robot.progress", true, false, false)]
    [InlineData("robot.log", false, true, false)]
    [InlineData("robot.status", false, false, true)]
    [InlineData("robot.execute", false, false, false)]
    [InlineData("robot.context", false, false, false)]
    [InlineData("robot.ping", false, false, false)]
    [InlineData("revit.progress", true, false, false)]
    [InlineData("excel.progress", true, false, false)]
    public void JsonRpcMethods_notification_detectors_work_for_robot_and_distinguish_actions(string method, bool expectedProgress, bool expectedLog, bool expectedStatus)
    {
        Assert.Equal(expectedProgress, JsonRpcMethods.IsProgress(method));
        Assert.Equal(expectedLog, JsonRpcMethods.IsLog(method));
        Assert.Equal(expectedStatus, JsonRpcMethods.IsStatus(method));
    }

    // =========================================================================
    // 4. CONTEXT RESULT JSON SERIALIZATION, ROUND-TRIP, NULL OMISSION, & WIRE ISOLATION
    // =========================================================================

    [Fact]
    public void ContextResult_when_Robot_is_null_omits_robot_property_from_serialized_json()
    {
        var context = new ContextResult
        {
            Host = "revit",
            HostVersion = "2026",
            RevitVersion = "2026",
            DocTitle = "Project1.rvt",
            Robot = null,
        };

        var json = BridgeJson.Serialize(context);
        Assert.DoesNotContain("\"robot\"", json);
    }

    [Fact]
    public void ContextResult_round_trip_preserves_all_RobotInfo_fields_in_camelCase()
    {
        var info = new RobotInfo(
            IsAttached: true,
            AttachedPid: 12345,
            RobotVersion: "39.0.1.11984",
            StructureType: "Frame3D",
            IsCalculated: true,
            HeavyOperationsEnabled: true,
            NodeCount: 450,
            BarCount: 680,
            PanelCount: 120,
            LoadCaseCount: 18);

        var context = new ContextResult
        {
            Host = "robot",
            HostVersion = "2026",
            DocTitle = "TowerStructure.rtd",
            DocPath = @"D:\Projects\TowerStructure.rtd",
            Robot = info,
        };

        var json = BridgeJson.Serialize(context);

        // Verify camelCase property formatting
        Assert.Contains("\"isAttached\":true", json);
        Assert.Contains("\"attachedPid\":12345", json);
        Assert.Contains("\"robotVersion\":\"39.0.1.11984\"", json);
        Assert.Contains("\"structureType\":\"Frame3D\"", json);
        Assert.Contains("\"isCalculated\":true", json);
        Assert.Contains("\"heavyOperationsEnabled\":true", json);
        Assert.Contains("\"nodeCount\":450", json);
        Assert.Contains("\"barCount\":680", json);
        Assert.Contains("\"panelCount\":120", json);
        Assert.Contains("\"loadCaseCount\":18", json);

        // Deserialization round-trip
        var deserialized = BridgeJson.Deserialize<ContextResult>(json);
        Assert.NotNull(deserialized);
        Assert.NotNull(deserialized.Robot);
        Assert.Equal(info, deserialized.Robot);
        Assert.Equal(12345, deserialized.Robot.AttachedPid);
        Assert.Equal("TowerStructure.rtd", deserialized.DocTitle);
    }

    [Fact]
    public void ContextResult_omits_nullable_RobotInfo_fields_when_they_are_null()
    {
        var info = new RobotInfo(
            IsAttached: false,
            AttachedPid: null,
            RobotVersion: null,
            StructureType: null,
            IsCalculated: false,
            HeavyOperationsEnabled: false,
            NodeCount: 0,
            BarCount: 0,
            PanelCount: 0,
            LoadCaseCount: 0);

        var context = new ContextResult
        {
            Host = "robot",
            HostVersion = "2026",
            Robot = info,
        };

        var json = BridgeJson.Serialize(context);

        // Nullable fields must be omitted by JsonIgnoreCondition.WhenWritingNull
        Assert.DoesNotContain("\"attachedPid\"", json);
        Assert.DoesNotContain("\"robotVersion\"", json);
        Assert.DoesNotContain("\"structureType\"", json);

        // Required non-null fields remain present
        Assert.Contains("\"isAttached\":false", json);
        Assert.Contains("\"isCalculated\":false", json);
        Assert.Contains("\"heavyOperationsEnabled\":false", json);
        Assert.Contains("\"nodeCount\":0", json);

        // Round-trip back to object
        var deserialized = BridgeJson.Deserialize<ContextResult>(json);
        Assert.NotNull(deserialized);
        Assert.NotNull(deserialized.Robot);
        Assert.Equal(info, deserialized.Robot);
        Assert.Null(deserialized.Robot.AttachedPid);
        Assert.Null(deserialized.Robot.RobotVersion);
        Assert.Null(deserialized.Robot.StructureType);
    }

    [Fact]
    public void ContextResult_serialized_for_robot_contains_zero_sibling_host_payloads()
    {
        var context = new ContextResult
        {
            Host = "robot",
            HostVersion = "2026",
            DocTitle = "Model.rtd",
            Robot = new RobotInfo(true, 5555, "39.0", "Shell", false, false, 10, 5, 2, 1),
        };

        var json = BridgeJson.Serialize(context);

        Assert.Contains("\"robot\"", json);
        Assert.DoesNotContain("\"excel\"", json);
        Assert.DoesNotContain("\"powerbi\"", json);
        Assert.DoesNotContain("\"sap2000\"", json);
        Assert.DoesNotContain("\"civil3d\"", json);
        Assert.DoesNotContain("\"etabs\"", json);
        Assert.DoesNotContain("\"navis\"", json);
        Assert.DoesNotContain("\"autocad\"", json);
    }

    [Theory]
    [InlineData("revit")]
    [InlineData("autocad")]
    [InlineData("navis")]
    [InlineData("etabs")]
    [InlineData("civil3d")]
    [InlineData("sap2000")]
    [InlineData("powerbi")]
    [InlineData("excel")]
    public void Sibling_host_context_payloads_never_contain_robot(string siblingHost)
    {
        var context = new ContextResult
        {
            Host = siblingHost,
            HostVersion = "2026",
            DocTitle = "OtherModel",
        };

        var json = BridgeJson.Serialize(context);
        Assert.DoesNotContain("\"robot\"", json);
    }

    // =========================================================================
    // 5. FAKE EXECUTOR PIPE ROUND-TRIP & CONTEXT SHAPING OVER NAMED PIPE
    // =========================================================================

    [Fact]
    public async Task Fake_executor_round_trip_for_robot_ping_context_execute_cancel_analyze()
    {
        var pipe = NewPipe();
        var executor = new FakeRevitExecutor
        {
            ContextHandler = _ => new ContextResult
            {
                RevitVersion = "2026",
                Host = "robot",
                HostVersion = "2026",
                DocTitle = "Building_A.rtd",
                DocPath = @"C:\Structural\Building_A.rtd",
                Robot = new RobotInfo(true, 8888, "39.0.1", "Frame3D", true, true, 200, 350, 40, 6),
            },
            ExecuteHandler = req => new ExecuteResult
            {
                Value = JsonSerializer.SerializeToElement(new { createdBars = 12 }),
                ValueType = "AnonymousType",
                Snapshot = "Building_A_backup_20260921.rtd",
            },
        };

        var settings = new BridgeSettings { ExecutionEnabled = true };
        using var listener = new PipeListener(pipe, new RequestDispatcher(executor, settings, "2026", "Robot Structural Analysis", DisabledText));
        listener.Start();

        var options = Options.Create(PipeOptions(pipe));
        await using var client = new RevitBridgeClient(options, NullLogger<RevitBridgeClient>.Instance, Robot());

        var timeout = TimeSpan.FromSeconds(10);
        var ct = TestContext.Current.CancellationToken;

        // 1. Ping
        var ping = await client.SendAsync<BridgePingResult>("robot.ping", null, timeout, null, ct);
        Assert.True(ping.Pong);
        Assert.Equal("2026", ping.RevitVersion);
        Assert.True(ping.ExecutionEnabled);
        Assert.False(ping.Busy);

        // 2. Context
        var context = await client.SendAsync<ContextResult>("robot.context", new ContextRequest(false), timeout, null, ct);
        Assert.Equal("robot", context.Host);
        Assert.Equal("Building_A.rtd", context.DocTitle);
        Assert.NotNull(context.Robot);
        Assert.Equal(8888, context.Robot.AttachedPid);
        Assert.Equal("Frame3D", context.Robot.StructureType);
        Assert.True(context.Robot.IsCalculated);
        Assert.Equal(350, context.Robot.BarCount);

        // 3. Execute with progress streaming
        executor.ProgressSteps = 4;
        executor.ProgressDelayMs = 5;
        var progressReports = new List<ProgressParams>();
        var progressSink = new HPRebar.Mcp.Contracts.SynchronousProgress<ProgressParams>(p =>
        {
            lock (progressReports) progressReports.Add(p);
        });

        var execRequest = new ExecuteRequest("bars.Create(1, 1, 2); return true;", Label: "CreateBar");
        var execResult = await client.SendAsync<ExecuteResult>("robot.execute", execRequest, timeout, progressSink, ct);

        Assert.False(execResult.IsError);
        Assert.Equal("Building_A_backup_20260921.rtd", execResult.Snapshot);
        Assert.NotNull(execResult.Value);
        Assert.Equal(12, execResult.Value.Value.GetProperty("createdBars").GetInt32());
        Assert.Equal(4, progressReports.Count);
        Assert.Equal([1, 2, 3, 4], progressReports.Select(p => p.Progress));

        // 4. Cancel
        var cancelResult = await client.SendAsync<CancelResult>("robot.cancel", null, timeout, null, ct);
        Assert.False(cancelResult.WasRunning);
        Assert.Equal(1, executor.CancelCalls);

        // 5. Analyze
        var analyzeResult = await client.SendAsync<AnalyzeResult>("robot.analyze", new AnalyzeRequest("int count = 5;"), timeout, null, ct);
        Assert.True(analyzeResult.Compiles);

        // 6. ContextService shaping test
        var contextService = new ContextService(client, new ResultFormatter());
        var shapedText = await contextService.ReadAsync(false, ct);
        using var jsonDoc = JsonDocument.Parse(shapedText);
        var root = jsonDoc.RootElement;

        // Verify Revit-only fields are stripped
        Assert.False(root.TryGetProperty("revitVersion", out _));
        Assert.False(root.TryGetProperty("isFamily", out _));

        // Verify Robot payload is present
        Assert.Equal("robot", root.GetProperty("host").GetString());
        Assert.Equal("2026", root.GetProperty("hostVersion").GetString());
        Assert.True(root.GetProperty("robot").GetProperty("isAttached").GetBoolean());
        Assert.Equal(8888, root.GetProperty("robot").GetProperty("attachedPid").GetInt32());

        // Verify zero sibling payloads in shaped text
        Assert.False(root.TryGetProperty("autocad", out _));
        Assert.False(root.TryGetProperty("navis", out _));
        Assert.False(root.TryGetProperty("etabs", out _));
        Assert.False(root.TryGetProperty("civil3d", out _));
        Assert.False(root.TryGetProperty("sap2000", out _));
        Assert.False(root.TryGetProperty("powerbi", out _));
        Assert.False(root.TryGetProperty("excel", out _));

        await listener.StopAsync();
    }

    [Fact]
    public async Task Fake_executor_refuses_robot_execute_with_actionable_message_when_execution_is_disabled()
    {
        var pipe = NewPipe();
        var executor = new FakeRevitExecutor();
        var settings = new BridgeSettings { ExecutionEnabled = false };

        using var listener = new PipeListener(pipe, new RequestDispatcher(executor, settings, "2026", "Robot Structural Analysis", DisabledText));
        listener.Start();

        var options = Options.Create(PipeOptions(pipe));
        await using var client = new RevitBridgeClient(options, NullLogger<RevitBridgeClient>.Instance, Robot());

        var ex = await Assert.ThrowsAsync<BridgeErrorException>(() =>
            client.SendAsync<ExecuteResult>("robot.execute", new ExecuteRequest("return 1;"), TimeSpan.FromSeconds(5), null, TestContext.Current.CancellationToken));

        Assert.Equal(BridgeErrorCode.ExecutionDisabled, ex.Code);
        Assert.Contains(DisabledText, ex.Message);
        Assert.Null(executor.LastExecuteRequest);

        await listener.StopAsync();
    }

    [Fact]
    public async Task Fake_executor_reports_busy_when_robot_executor_is_already_busy()
    {
        var pipe = NewPipe();
        var executor = new FakeRevitExecutor { IsBusy = true };
        var settings = new BridgeSettings { ExecutionEnabled = true };

        using var listener = new PipeListener(pipe, new RequestDispatcher(executor, settings, "2026", "Robot Structural Analysis", DisabledText));
        listener.Start();

        var options = Options.Create(PipeOptions(pipe));
        await using var client = new RevitBridgeClient(options, NullLogger<RevitBridgeClient>.Instance, Robot());

        var ex = await Assert.ThrowsAsync<BridgeErrorException>(() =>
            client.SendAsync<ExecuteResult>("robot.execute", new ExecuteRequest("return 1;"), TimeSpan.FromSeconds(5), null, TestContext.Current.CancellationToken));

        Assert.Equal(BridgeErrorCode.Busy, ex.Code);
        Assert.Contains("Robot Structural Analysis", ex.Message);

        await listener.StopAsync();
    }

    [Fact]
    public async Task Unknown_method_under_robot_prefix_returns_MethodNotFound()
    {
        var pipe = NewPipe();
        var executor = new FakeRevitExecutor();
        var settings = new BridgeSettings { ExecutionEnabled = true };

        using var listener = new PipeListener(pipe, new RequestDispatcher(executor, settings, "2026", "Robot Structural Analysis", DisabledText));
        listener.Start();

        var options = Options.Create(PipeOptions(pipe));
        await using var client = new RevitBridgeClient(options, NullLogger<RevitBridgeClient>.Instance, Robot());

        var ex = await Assert.ThrowsAsync<BridgeErrorException>(() =>
            client.SendAsync<BridgePingResult>("robot.nonexistent_method", null, TimeSpan.FromSeconds(5), null, TestContext.Current.CancellationToken));

        Assert.Equal(BridgeErrorCode.MethodNotFound, ex.Code);

        await listener.StopAsync();
    }

    // =========================================================================
    // 6. ROSLYN GUARD & ANALYZER PROFILE FOR ROBOT STRUCTURAL ANALYSIS
    // =========================================================================

    [Fact]
    public void AnalyzerProfile_Robot_has_empty_transaction_collections_and_never_flags_transactions()
    {
        Assert.Empty(AnalyzerProfile.Robot.TransactionTypeNames);
        Assert.Empty(AnalyzerProfile.Robot.TransactionMethodNames);

        const string codeWithTransactions = """
            using (var t = new Transaction()) { }
            var tg = new TransactionGroup();
            StartTransaction();
            BeginTransaction();
            return robot.Project.FileName;
            """;

        var analysis = ScriptAnalyzer.Analyze(codeWithTransactions, AnalyzerProfile.Robot);
        Assert.False(analysis.UsesTransaction);
    }

    [Theory]
    [InlineData("robot.Quit();", "Quit")]
    [InlineData("robot?.Quit();", "Quit")]
    [InlineData("app.Quit();", "Quit")]
    [InlineData("app?.Quit();", "Quit")]
    [InlineData("robot.ApplicationExit();", "ApplicationExit")]
    [InlineData("robot.Interactive = 0;", "Interactive")]
    [InlineData("app.Interactive = 1;", "Interactive")]
    [InlineData("MessageBox.Show(\"Modal\");", "MessageBox")]
    [InlineData("System.Windows.Forms.MessageBox.Show(\"Modal\");", "System.Windows.Forms")]
    [InlineData("using HPRobot.McpBridge.Host; return 1;", "HPRobot.McpBridge")]
    [InlineData("global::HPRobot.McpBridge.BridgeEntry.Stop();", "HPRobot.McpBridge")]
    [InlineData("global::HPRebar.McpBridge.Core.Host.McpBridgeHost.Current.Stop();", "HPRebar.McpBridge.Core.Host")]
    [InlineData("#r \"malicious.dll\"\nreturn 1;", "#r")]
    [InlineData("#load \"evil.csx\"\nreturn 1;", "#load")]
    [InlineData("System.Diagnostics.Process.Start(\"calc.exe\");", "System.Diagnostics.Process")]
    public void GuardProfile_Robot_blocks_termination_dialogs_and_bridge_tampering(string code, string expectedForbiddenTerm)
    {
        var diagnostics = ScriptGuard.Check(code, GuardProfile.Robot);
        Assert.NotEmpty(diagnostics);
        Assert.Contains(diagnostics, d => d.Message.Contains(expectedForbiddenTerm, StringComparison.Ordinal));
    }

    [Fact]
    public void GuardProfile_Robot_allows_valid_RobotOM_script_patterns()
    {
        const string script = """
            var structure = robot.Project.Structure;
            var nodes = structure.Nodes;
            int count = nodes.GetAll().Count;
            nodes.Create(1, 0.0, 0.0, 0.0);
            nodes.Create(2, 0.0, 0.0, 3.5);
            var bars = structure.Bars;
            bars.Create(1, 1, 2);
            log($"Created bar between nodes 1 and 2, current count {count}");
            progress(50, 100, "Halfway done");
            if (ct.IsCancellationRequested) return "Cancelled";
            var fileName = robot.Project.FileName;
            return new { success = true, fileName, count };
            """;

        var diagnostics = ScriptGuard.Check(script, GuardProfile.Robot);
        Assert.Empty(diagnostics);
    }
}
