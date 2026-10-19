using System.Reflection;
using System.Text.Json;
using HPRebar.Mcp.Contracts;
using HPRebar.Mcp.Contracts.JsonRpc;
using HPRebar.Mcp.Contracts.Messages;
using HPRebar.Mcp.Server.Bootstrap;
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
using static HPRebar.Mcp.Server.Tests.DynamoTestProfile;

namespace HPRebar.Mcp.Server.Tests;

/// <summary>
///     Adversarial challenger test suite for Milestone M1 (Autodesk Dynamo McpShared Integration).
///     Empirically validates:
///     1. PipeNaming casing variations, whitespace normalization, version flexibility, and mutual host distinction.
///     2. ScriptGuard deny-lists against adversarial AST inputs (nested blocks, lambdas, null conditionals, global aliases).
///     3. Legitimate Dynamo scripts pass without false positives.
///     4. AnalyzerProfile transaction detection under nested AST structures.
///     5. JsonRpcMethods prefix bijection, notification detectors, and multi-host prefix isolation.
///     6. ContextResult wire serialization, camelCase format, null suppression, and multi-host isolation.
///     7. Host neutrality: McpShared zero assembly leakage to Dynamo runtime.
///     8. Real Named Pipe round-trip with fake executor, progress event streaming, and ContextService shaping.
/// </summary>
public sealed class DynamoMilestone1ChallengerTests
{
    // =========================================================================
    // 1. PIPE NAMING CASING & EDGE CASE STRESS TESTS
    // =========================================================================

    [Theory]
    [InlineData("dynamo", 2026, "hpdynamo-mcp-2026")]
    [InlineData("DYNAMO", 2026, "hpdynamo-mcp-2026")]
    [InlineData("Dynamo", 2026, "hpdynamo-mcp-2026")]
    [InlineData("DyNaMo", 2026, "hpdynamo-mcp-2026")]
    [InlineData("  dynamo  ", 2026, "hpdynamo-mcp-2026")]
    [InlineData("\tdynamo\r\n", 2026, "hpdynamo-mcp-2026")]
    [InlineData("dynamo", 2025, "hpdynamo-mcp-2025")]
    [InlineData("dynamo", 2024, "hpdynamo-mcp-2024")]
    public void PipeNaming_For_normalizes_casing_whitespace_and_versions(string host, int version, string expected)
    {
        Assert.Equal(expected, PipeNaming.For(host, version));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t\r\n")]
    public void PipeNaming_For_rejects_empty_or_whitespace_host(string? invalidHost)
    {
        Assert.Throws<ArgumentException>(() => PipeNaming.For(invalidHost!, 2026));
    }

    [Fact]
    public void All_eleven_hosts_have_pairwise_distinct_constants_and_pipe_names()
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
            PipeNaming.TeklaHost,
            PipeNaming.DynamoHost,
        };

        Assert.Equal(11, hosts.Length);
        Assert.Equal(11, hosts.Distinct(StringComparer.OrdinalIgnoreCase).Count());

        var pipes = hosts.Select(h => PipeNaming.For(h, 2026)).ToArray();
        Assert.Equal(11, pipes.Distinct(StringComparer.OrdinalIgnoreCase).Count());
        Assert.Contains("hpdynamo-mcp-2026", pipes);
    }

    [Theory]
    [InlineData("dynamo", "hpdynamo-mcp-2026")]
    [InlineData("DYNAMO", "hpdynamo-mcp-2026")]
    [InlineData("Dynamo", "hpdynamo-mcp-2026")]
    [InlineData("  dynamo  ", "hpdynamo-mcp-2026")]
    public void BridgeOptions_PipeName_derives_correctly_across_casing_variations(string hostId, string expectedPipe)
    {
        var options = new BridgeOptions { HostId = hostId, HostVersion = 2026 };
        Assert.Equal(expectedPipe, options.PipeName);
    }

    // =========================================================================
    // 2. SCRIPT GUARD ADVERSARIAL AST STRESS TESTS
    // =========================================================================

    [Theory]
    // Modal dialogs in nested blocks & lambdas
    [InlineData("MessageBox.Show(\"modal\"); return 1;", "MessageBox")]
    [InlineData("{ { { MessageBox.Show(\"nested\"); } } } return 1;", "MessageBox")]
    [InlineData("Action a = () => MessageBox.Show(\"lambda\"); return 1;", "MessageBox")]
    [InlineData("Action a = () => { TaskDialog.Show(\"Revit\", \"modal\"); }; return 1;", "TaskDialog")]
    [InlineData("TaskDialog.Show(\"Revit\", \"modal\"); return 1;", "TaskDialog")]
    [InlineData("System.Windows.Forms.MessageBox.Show(\"modal\"); return 1;", "MessageBox")]
    // Application exit & quit across receivers and null-conditionals
    [InlineData("Application.Exit(); return 1;", "Exit")]
    [InlineData("app.Quit(); return 1;", "Quit")]
    [InlineData("app?.Quit(); return 1;", "Quit")]
    [InlineData("dynamo.Quit(); return 1;", "Quit")]
    [InlineData("dynamo?.Quit(); return 1;", "Quit")]
    [InlineData("app.CloseApplication(); return 1;", "CloseApplication")]
    [InlineData("app?.CloseApplication(); return 1;", "CloseApplication")]
    [InlineData("Action a = app.Quit; return 1;", "Quit")]
    [InlineData("Action a = () => app.Quit(); return 1;", "Quit")]
    // Forbidden namespaces & global aliases
    [InlineData("using System.Windows.Forms; return 1;", "System.Windows.Forms")]
    [InlineData("using global::System.Windows.Forms; return 1;", "System.Windows.Forms")]
    [InlineData("using HPDynamo.McpBridge; return 1;", "HPDynamo.McpBridge")]
    [InlineData("using global::HPDynamo.McpBridge; return 1;", "HPDynamo.McpBridge")]
    [InlineData("using HPRebar.McpBridge.Core.Host; return 1;", "HPRebar.McpBridge.Core.Host")]
    [InlineData("using global::HPRebar.McpBridge.Core.Host; return 1;", "HPRebar.McpBridge.Core.Host")]
    [InlineData("HPDynamo.McpBridge.BridgeEntry.Stop(); return 1;", "HPDynamo.McpBridge")]
    [InlineData("global::HPDynamo.McpBridge.BridgeEntry.Stop(); return 1;", "HPDynamo.McpBridge")]
    [InlineData("HPRebar.McpBridge.Core.Host.McpBridgeHost.Current.Stop(); return 1;", "HPRebar.McpBridge.Core.Host")]
    [InlineData("global::HPRebar.McpBridge.Core.Host.McpBridgeHost.Current.Stop(); return 1;", "HPRebar.McpBridge.Core.Host")]
    // Process and system control
    [InlineData("System.Diagnostics.Process.Start(\"cmd\"); return 1;", "System.Diagnostics.Process")]
    [InlineData("Process.Start(\"calc\"); return 1;", "Process")]
    [InlineData("global::System.Diagnostics.Process.Start(\"notepad\"); return 1;", "System.Diagnostics.Process")]
    // File I/O
    [InlineData("System.IO.File.Delete(\"dyn.dyn\"); return 1;", "System.IO")]
    [InlineData("File.Delete(\"test.txt\"); return 1;", "File")]
    [InlineData("global::System.IO.File.WriteAllText(\"test.txt\", \"data\"); return 1;", "System.IO")]
    // Directives
    [InlineData("#r \"untrusted.dll\"\nreturn 1;", "#r")]
    [InlineData("#load \"evil.csx\"\nreturn 1;", "#load")]
    [InlineData("   #r   \"spaces.dll\"\nreturn 1;", "#r")]
    [InlineData("\t#load\t\"tab.csx\"\nreturn 1;", "#load")]
    // Async lambdas, dynamic, unsafe, string reflection
    [InlineData("Action a = async () => { }; return 1;", "async lambdas")]
    [InlineData("dynamic d = 1; return d;", "dynamic")]
    [InlineData("unsafe { int* p; } return 1;", "unsafe code")]
    public void GuardProfile_Dynamo_denies_adversarial_ast_expressions(string script, string expectedViolation)
    {
        var diagnostics = ScriptGuard.Check(script, GuardProfile.Dynamo);
        Assert.NotEmpty(diagnostics);
        Assert.Contains(diagnostics, d => d.Message.Contains(expectedViolation, StringComparison.Ordinal));
    }

    [Fact]
    public void GuardProfile_Dynamo_allows_legitimate_geometry_and_graph_queries()
    {
        const string validScript = """
            var p1 = Point.ByCoordinates(0, 0, 0);
            var p2 = Point.ByCoordinates(10, 20, 30);
            var line = Line.ByStartPointEndPoint(p1, p2);
            var nodes = workspace.Nodes;
            var path = System.IO.Path.Combine("models", "test.dyn");
            log($"Graph {workspace.Name} has {nodes.Count()} nodes, path: {path}");
            progress(1, 2, "Processing geometry");

            if (ct.IsCancellationRequested) return null;

            return new
            {
                lineLength = line.Length,
                nodeCount = nodes.Count(),
                docTitle = doc.Title
            };
            """;

        var diagnostics = ScriptGuard.Check(validScript, GuardProfile.Dynamo);
        Assert.Empty(diagnostics);
    }

    // =========================================================================
    // 3. SCRIPT ANALYZER TRANSACTION DETECTION UNDER ADVERSARIAL AST
    // =========================================================================

    [Theory]
    [InlineData("using (var t = new Transaction(doc, \"Create\")) { t.Start(); t.Commit(); } return 1;", true)]
    [InlineData("using (var tg = new TransactionGroup(doc, \"Group\")) { tg.Start(); tg.Assimilate(); } return 1;", true)]
    [InlineData("using (var st = new SubTransaction(doc)) { st.Start(); st.Commit(); } return 1;", true)]
    [InlineData("using (var t = new Autodesk.Revit.DB.Transaction(doc, \"Create\")) { } return 1;", true)]
    [InlineData("{ { { var t = new Transaction(doc, \"Nested\"); } } } return 1;", true)]
    [InlineData("Action a = () => { var t = new Transaction(doc, \"Lambda\"); }; return 1;", true)]
    [InlineData("var p = Point.ByCoordinates(0, 0, 0); return p.X;", false)]
    [InlineData("model.CommitChanges(); return 1;", false)]
    public void AnalyzerProfile_Dynamo_detects_transactions_under_adversarial_ast(string script, bool expectedUsesTransaction)
    {
        var analysis = ScriptAnalyzer.Analyze(script, AnalyzerProfile.Dynamo);
        Assert.Equal(expectedUsesTransaction, analysis.UsesTransaction);
    }

    // =========================================================================
    // 4. JSON-RPC METHODS BIJECTION & MULTI-HOST PREFIX ISOLATION
    // =========================================================================

    [Theory]
    [InlineData("execute", "dynamo.execute")]
    [InlineData("context", "dynamo.context")]
    [InlineData("ping", "dynamo.ping")]
    [InlineData("cancel", "dynamo.cancel")]
    [InlineData("analyze", "dynamo.analyze")]
    [InlineData("progress", "dynamo.progress")]
    [InlineData("log", "dynamo.log")]
    [InlineData("status", "dynamo.status")]
    public void JsonRpcMethods_For_and_Suffix_are_strictly_bijective_for_dynamo(string suffix, string expectedFull)
    {
        var full = JsonRpcMethods.For(JsonRpcMethods.DynamoPrefix, suffix);
        Assert.Equal(expectedFull, full);
        Assert.Equal(suffix, JsonRpcMethods.Suffix(full));
    }

    [Theory]
    [InlineData("dynamo.progress", true, false, false)]
    [InlineData("dynamo.log", false, true, false)]
    [InlineData("dynamo.status", false, false, true)]
    [InlineData("dynamo.execute", false, false, false)]
    [InlineData("dynamo.context", false, false, false)]
    public void JsonRpcMethods_notification_detectors_work_for_dynamo(string method, bool expectedProgress, bool expectedLog, bool expectedStatus)
    {
        Assert.Equal(expectedProgress, JsonRpcMethods.IsProgress(method));
        Assert.Equal(expectedLog, JsonRpcMethods.IsLog(method));
        Assert.Equal(expectedStatus, JsonRpcMethods.IsStatus(method));
    }

    [Fact]
    public void All_eleven_hosts_have_mutually_distinct_rpc_prefixes_ending_with_dot()
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
            JsonRpcMethods.TeklaPrefix,
            JsonRpcMethods.DynamoPrefix,
        };

        Assert.Equal(11, prefixes.Length);
        Assert.Equal(11, prefixes.Distinct(StringComparer.Ordinal).Count());
        Assert.All(prefixes, p => Assert.EndsWith(".", p));
    }

    // =========================================================================
    // 5. CONTEXT RESULT WIRE SERIALIZATION & SIBLING ISOLATION
    // =========================================================================

    [Fact]
    public void ContextResult_when_Dynamo_is_null_never_contains_dynamo_in_json()
    {
        var context = new ContextResult
        {
            Host = "revit",
            HostVersion = "2026",
            DocTitle = "Tower.rvt",
            Dynamo = null,
        };

        var json = BridgeJson.Serialize(context);
        Assert.DoesNotContain("\"dynamo\"", json);
    }

    [Fact]
    public void ContextResult_for_all_ten_sibling_hosts_never_contains_dynamo_payload()
    {
        var siblingContexts = new[]
        {
            new ContextResult { Host = "revit", HostVersion = "2026", DocTitle = "RevitModel.rvt" },
            new ContextResult { Host = "autocad", HostVersion = "2026", DocTitle = "Drawing.dwg" },
            new ContextResult { Host = "navis", HostVersion = "2026", DocTitle = "Federated.nwd" },
            new ContextResult { Host = "etabs", HostVersion = "22", DocTitle = "Structure.edb" },
            new ContextResult { Host = "civil3d", HostVersion = "2026", DocTitle = "Corridor.dwg" },
            new ContextResult { Host = "sap2000", HostVersion = "27", DocTitle = "Bridge.sdb" },
            new ContextResult { Host = "powerbi", HostVersion = "2026", DocTitle = "SemanticModel" },
            new ContextResult { Host = "excel", HostVersion = "2026", DocTitle = "Workbook.xlsx" },
            new ContextResult { Host = "robot", HostVersion = "2026", DocTitle = "Frame.rtd" },
            new ContextResult { Host = "tekla", HostVersion = "2025", DocTitle = "Model.db1" },
        };

        foreach (var ctx in siblingContexts)
        {
            var json = BridgeJson.Serialize(ctx);
            Assert.DoesNotContain("\"dynamo\"", json);
        }
    }

    [Fact]
    public void ContextResult_for_dynamo_contains_zero_sibling_payloads()
    {
        var dynamoContext = new ContextResult
        {
            Host = "dynamo",
            HostVersion = "2026",
            Dynamo = new DynamoInfo(true, "3.7.0", "Graph.dyn", @"C:\Graph.dyn", "Automatic", 10, 15, 2, false),
        };

        var json = BridgeJson.Serialize(dynamoContext);

        Assert.Contains("\"dynamo\"", json);
        Assert.DoesNotContain("\"autocad\"", json);
        Assert.DoesNotContain("\"navis\"", json);
        Assert.DoesNotContain("\"etabs\"", json);
        Assert.DoesNotContain("\"civil3d\"", json);
        Assert.DoesNotContain("\"sap2000\"", json);
        Assert.DoesNotContain("\"powerbi\"", json);
        Assert.DoesNotContain("\"excel\"", json);
        Assert.DoesNotContain("\"robot\"", json);
        Assert.DoesNotContain("\"tekla\"", json);
    }

    [Fact]
    public void ContextResult_omits_nullable_DynamoInfo_fields_when_null()
    {
        var info = new DynamoInfo(
            IsLive: false,
            DynamoVersion: null,
            ActiveWorkspaceName: null,
            ActiveWorkspacePath: null,
            RunType: null,
            NodeCount: 0,
            WireCount: 0,
            SelectedNodeCount: 0,
            HasUnsavedChanges: false);

        var context = new ContextResult
        {
            Host = "dynamo",
            HostVersion = "2026",
            Dynamo = info,
        };

        var json = BridgeJson.Serialize(context);

        Assert.DoesNotContain("\"dynamoVersion\"", json);
        Assert.DoesNotContain("\"activeWorkspaceName\"", json);
        Assert.DoesNotContain("\"activeWorkspacePath\"", json);
        Assert.DoesNotContain("\"runType\"", json);

        Assert.Contains("\"isLive\":false", json);
        Assert.Contains("\"nodeCount\":0", json);
        Assert.Contains("\"wireCount\":0", json);
        Assert.Contains("\"selectedNodeCount\":0", json);
        Assert.Contains("\"hasUnsavedChanges\":false", json);

        var deserialized = BridgeJson.Deserialize<ContextResult>(json);
        Assert.NotNull(deserialized?.Dynamo);
        Assert.Null(deserialized.Dynamo.DynamoVersion);
        Assert.Null(deserialized.Dynamo.ActiveWorkspaceName);
    }

    // =========================================================================
    // 6. HOST NEUTRALITY VERIFICATION
    // =========================================================================

    [Fact]
    public void McpShared_never_references_Dynamo_runtime_assemblies()
    {
        var forbiddenDynamoPrefixes = new[] { "DynamoCore", "DynamoServices", "DynamoRevit", "RevitNodes", "Dynamo." };

        var sharedAssemblies = new[]
        {
            typeof(PipeNaming).Assembly,
            typeof(PipeListener).Assembly,
            typeof(ResultFormatter).Assembly,
        };

        foreach (var assembly in sharedAssemblies)
        {
            var referenced = assembly.GetReferencedAssemblies().Select(a => a.Name ?? string.Empty).ToArray();
            foreach (var prefix in forbiddenDynamoPrefixes)
            {
                Assert.DoesNotContain(referenced, name => name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
            }
        }
    }

    // =========================================================================
    // 7. REAL NAMED PIPE ROUND-TRIP WITH FAKE EXECUTOR
    // =========================================================================

    [Fact]
    public async Task Fake_executor_round_trip_for_dynamo_ping_context_execute_cancel_analyze()
    {
        var pipe = "test-dynamo-pipe-" + Guid.NewGuid().ToString("N");
        var executor = new FakeRevitExecutor
        {
            ContextHandler = _ => new ContextResult
            {
                RevitVersion = "2026",
                Host = "dynamo",
                HostVersion = "2026",
                DocTitle = "TowerRebar.dyn",
                DocPath = @"C:\Dynamo\TowerRebar.dyn",
                Dynamo = new DynamoInfo(true, "3.7.0.4500", "TowerRebar.dyn", @"C:\Dynamo\TowerRebar.dyn", "Automatic", 35, 48, 4, false),
            },
            ExecuteHandler = req => new ExecuteResult
            {
                Value = JsonSerializer.SerializeToElement(new { evaluatedNodes = 35 }),
                ValueType = "AnonymousType",
            },
        };

        var settings = new BridgeSettings { ExecutionEnabled = true };
        using var listener = new PipeListener(pipe, new RequestDispatcher(executor, settings, "2026", "Dynamo in Revit"));
        listener.Start();

        var options = Options.Create(new BridgeOptions { PipeName = pipe, HostId = "dynamo", HostVersion = 2026 });
        await using var client = new RevitBridgeClient(options, NullLogger<RevitBridgeClient>.Instance, Dynamo());

        var timeout = TimeSpan.FromSeconds(10);
        var ct = TestContext.Current.CancellationToken;

        // 1. Ping
        var ping = await client.SendAsync<BridgePingResult>("dynamo.ping", null, timeout, null, ct);
        Assert.True(ping.Pong);
        Assert.True(ping.ExecutionEnabled);
        Assert.False(ping.Busy);

        // 2. Context
        var context = await client.SendAsync<ContextResult>("dynamo.context", new ContextRequest(false), timeout, null, ct);
        Assert.Equal("dynamo", context.Host);
        Assert.NotNull(context.Dynamo);
        Assert.True(context.Dynamo.IsLive);
        Assert.Equal("TowerRebar.dyn", context.Dynamo.ActiveWorkspaceName);
        Assert.Equal(35, context.Dynamo.NodeCount);
        Assert.Equal(48, context.Dynamo.WireCount);

        // 3. Execute with progress
        executor.ProgressSteps = 2;
        executor.ProgressDelayMs = 5;
        var progressReports = new List<ProgressParams>();
        var progressSink = new SynchronousProgress<ProgressParams>(p =>
        {
            lock (progressReports) progressReports.Add(p);
        });

        var execRequest = new ExecuteRequest("var p = Point.ByCoordinates(0, 0, 0); return p;", Label: "CreatePoint");
        var execResult = await client.SendAsync<ExecuteResult>("dynamo.execute", execRequest, timeout, progressSink, ct);

        Assert.False(execResult.IsError);
        Assert.NotNull(execResult.Value);
        Assert.Equal(35, execResult.Value.Value.GetProperty("evaluatedNodes").GetInt32());
        Assert.Equal(2, progressReports.Count);

        // 4. Cancel
        var cancelResult = await client.SendAsync<CancelResult>("dynamo.cancel", null, timeout, null, ct);
        Assert.False(cancelResult.WasRunning);
        Assert.Equal(1, executor.CancelCalls);

        // 5. Analyze
        var analyzeResult = await client.SendAsync<AnalyzeResult>("dynamo.analyze", new AnalyzeRequest("int x = 42;"), timeout, null, ct);
        Assert.True(analyzeResult.Compiles);

        // 6. ContextService shaping
        var contextService = new ContextService(client, new ResultFormatter());
        var shapedText = await contextService.ReadAsync(false, ct);
        using var jsonDoc = JsonDocument.Parse(shapedText);
        var root = jsonDoc.RootElement;

        // Verify Revit legacy fields stripped
        Assert.False(root.TryGetProperty("revitVersion", out _));
        Assert.False(root.TryGetProperty("isFamily", out _));

        // Verify Dynamo payload preserved
        Assert.Equal("dynamo", root.GetProperty("host").GetString());
        Assert.Equal("2026", root.GetProperty("hostVersion").GetString());
        Assert.True(root.GetProperty("dynamo").GetProperty("isLive").GetBoolean());
        Assert.Equal("TowerRebar.dyn", root.GetProperty("dynamo").GetProperty("activeWorkspaceName").GetString());
        Assert.Equal(35, root.GetProperty("dynamo").GetProperty("nodeCount").GetInt32());

        // Zero sibling payloads in shaped text
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
}
