using System.Reflection;
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
///     Adversarial challenger test suite for Milestone M1 (Tekla McpShared Integration).
///     Empirically validates:
///     1. GuardProfile.Tekla denies modal dialogs (MessageBox), interactive pickers (Picker),
///        commit bypasses (model.CommitChanges), process execution (Process.Start), directives (#r, #load),
///        bridge internals, and reflection/IO evasion.
///     2. PipeNaming.For("tekla", 2025) strictly produces "hptekla-mcp-2025" and handles edge cases.
///     3. JsonRpcMethods prefix, suffix, and progress method bijection for tekla.
///     4. ContextResult wire serialization, null omission, camelCase format, and multi-host wire isolation.
///     5. ScriptAnalyzer transaction detection under AnalyzerProfile.Tekla.
///     6. Host neutrality: zero Tekla Open API assembly leakage into McpShared.
///     7. Real Named Pipe round-trip with fake executor, progress event streaming, and ContextService shaping.
/// </summary>
public sealed class TeklaMilestone1ChallengerTests
{
    // =========================================================================
    // 1. GUARD PROFILE ADVERSARIAL STRESS TESTS
    // =========================================================================

    /// <summary>
    ///     Remediated: CommitChanges is now in DeniedMembers,
    ///     so aliased receivers or parenthesized expressions are blocked by ScriptGuard.
    /// </summary>
    [Theory]
    [InlineData("var m = model; m.CommitChanges();")]
    [InlineData("var myModel = model; myModel.CommitChanges();")]
    [InlineData("((Tekla.Structures.Model.Model)model).CommitChanges();")]
    [InlineData("(model).CommitChanges();")]
    public void Guard_limitation_CommitChanges_evasion_via_aliased_or_parenthesized_receiver(string script)
    {
        var diagnostics = ScriptGuard.Check(script, GuardProfile.Tekla);
        Assert.NotEmpty(diagnostics);
        Assert.Contains(diagnostics, d => d.Message.Contains("CommitChanges", StringComparison.Ordinal));
    }


    [Theory]
    [InlineData("model.CommitChanges(); return 1;", "CommitChanges")]
    [InlineData("model?.CommitChanges(); return 1;", "CommitChanges")]
    [InlineData("Action a = model.CommitChanges; return 1;", "CommitChanges")]
    [InlineData("Action a = () => model.CommitChanges(); return 1;", "CommitChanges")]
    [InlineData("model.CommitChanges(\"Modify beam\"); return 1;", "CommitChanges")]
    // Modal dialogs and picking
    [InlineData("MessageBox.Show(\"modal\"); return 1;", "MessageBox")]
    [InlineData("System.Windows.Forms.MessageBox.Show(\"modal\"); return 1;", "MessageBox")]
    [InlineData("var p = new Picker(); return 1;", "Picker")]
    [InlineData("p.PickObject(); return 1;", "PickObject")]
    [InlineData("p?.PickObject(); return 1;", "PickObject")]
    [InlineData("p.PickObjects(); return 1;", "PickObjects")]
    [InlineData("p?.PickObjects(); return 1;", "PickObjects")]
    [InlineData("p.PickPoint(); return 1;", "PickPoint")]
    [InlineData("p?.PickPoint(); return 1;", "PickPoint")]
    [InlineData("p.PickPoints(); return 1;", "PickPoints")]
    [InlineData("p?.PickPoints(); return 1;", "PickPoints")]
    [InlineData("p.PickLine(); return 1;", "PickLine")]
    [InlineData("p?.PickLine(); return 1;", "PickLine")]
    [InlineData("p.PickPolygon(); return 1;", "PickPolygon")]
    [InlineData("p?.PickPolygon(); return 1;", "PickPolygon")]
    [InlineData("p.PickFace(); return 1;", "PickFace")]
    [InlineData("p?.PickFace(); return 1;", "PickFace")]
    [InlineData("Picker.PickObject(); return 1;", "PickObject")]
    [InlineData("myCustomPicker.PickObject(); return 1;", "PickObject")]
    // Application exit
    [InlineData("Application.Exit(); return 1;", "Exit")]
    [InlineData("app.Quit(); return 1;", "Quit")]
    [InlineData("model.Quit(); return 1;", "Quit")]
    // Forbidden namespaces
    [InlineData("using System.Windows.Forms; return 1;", "System.Windows.Forms")]
    [InlineData("using global::System.Windows.Forms; return 1;", "System.Windows.Forms")]
    [InlineData("using Tekla.Structures.Dialog; return 1;", "Tekla.Structures.Dialog")]
    [InlineData("using global::Tekla.Structures.Dialog; return 1;", "Tekla.Structures.Dialog")]
    [InlineData("using Tekla.Structures.Drawing.UI; return 1;", "Tekla.Structures.Drawing.UI")]
    [InlineData("using global::Tekla.Structures.Drawing.UI; return 1;", "Tekla.Structures.Drawing.UI")]
    [InlineData("using HPTekla.McpBridge; return 1;", "HPTekla.McpBridge")]
    [InlineData("using global::HPTekla.McpBridge; return 1;", "HPTekla.McpBridge")]
    [InlineData("using HPRebar.McpBridge.Core.Host; return 1;", "HPRebar.McpBridge.Core.Host")]
    [InlineData("using global::HPRebar.McpBridge.Core.Host; return 1;", "HPRebar.McpBridge.Core.Host")]
    [InlineData("HPTekla.McpBridge.BridgeEntry.Stop(); return 1;", "HPTekla.McpBridge")]
    [InlineData("global::HPTekla.McpBridge.BridgeEntry.Stop(); return 1;", "HPTekla.McpBridge")]
    [InlineData("HPRebar.McpBridge.Core.Host.McpBridgeHost.Current.Stop(); return 1;", "HPRebar.McpBridge.Core.Host")]
    [InlineData("global::HPRebar.McpBridge.Core.Host.McpBridgeHost.Current.Stop(); return 1;", "HPRebar.McpBridge.Core.Host")]
    // Process and system control
    [InlineData("System.Diagnostics.Process.Start(\"cmd\"); return 1;", "System.Diagnostics.Process")]
    [InlineData("Process.Start(\"calc\"); return 1;", "Process")]
    [InlineData("global::System.Diagnostics.Process.Start(\"notepad\"); return 1;", "System.Diagnostics.Process")]
    // File I/O
    [InlineData("System.IO.File.Delete(\"model.db\"); return 1;", "System.IO")]
    [InlineData("File.Delete(\"test.txt\"); return 1;", "File")]
    [InlineData("global::System.IO.File.WriteAllText(\"test.txt\", \"data\"); return 1;", "System.IO")]
    // Directives
    [InlineData("#r \"untrusted.dll\"\nreturn 1;", "#r")]
    [InlineData("#load \"evil.csx\"\nreturn 1;", "#load")]
    [InlineData("   #r   \"spaces.dll\"\nreturn 1;", "#r")]
    [InlineData("\t#load\t\"tab.csx\"\nreturn 1;", "#load")]
    public void GuardProfile_Tekla_denies_forbidden_operations_across_syntax_variations(string script, string expectedViolation)
    {
        var diagnostics = ScriptGuard.Check(script, GuardProfile.Tekla);
        Assert.NotEmpty(diagnostics);
        Assert.Contains(diagnostics, d => d.Message.Contains(expectedViolation, StringComparison.Ordinal));
    }

    [Fact]
    public void Legitimate_Tekla_Open_API_model_script_passes_guard_cleanly()
    {
        const string validScript = """
            var info = model.GetInfo();
            var beam = new Beam();
            beam.StartPoint = new Point(0, 0, 0);
            beam.EndPoint = new Point(6000, 0, 0);
            beam.Profile.ProfileString = "UB406x178x74";
            beam.Material.MaterialString = "S355JR";
            beam.Class = "3";
            beam.Insert();

            log($"Inserted beam {beam.Identifier.ID} in model {info.ModelName}");
            progress(1, 1, "Beam insertion complete");

            if (ct.IsCancellationRequested) return "Cancelled";

            return new
            {
                success = true,
                id = beam.Identifier.ID,
                modelName = info.ModelName
            };
            """;

        var diagnostics = ScriptGuard.Check(validScript, GuardProfile.Tekla);
        Assert.Empty(diagnostics);
    }

    [Fact]
    public void Legitimate_Tekla_rebar_script_passes_guard_cleanly()
    {
        const string rebarScript = """
            var rebarGroup = new RebarGroup();
            rebarGroup.Polygons.Add(new Polygon());
            rebarGroup.Size = "16";
            rebarGroup.Grade = "B500B";
            rebarGroup.SpacingType = RebarGroup.RebarGroupSpacingTypeEnum.SPACING_TYPE_TARGET_SPACE;
            rebarGroup.Spacings.Add(200.0);
            rebarGroup.Insert();
            return new { id = rebarGroup.Identifier.ID };
            """;

        var diagnostics = ScriptGuard.Check(rebarScript, GuardProfile.Tekla);
        Assert.Empty(diagnostics);
    }

    // =========================================================================
    // 2. PIPE NAMING ADVERSARIAL STRESS TESTS
    // =========================================================================

    [Fact]
    public void PipeNaming_For_tekla_2025_strictly_produces_hptekla_mcp_2025()
    {
        Assert.Equal("hptekla-mcp-2025", PipeNaming.For("tekla", 2025));
        Assert.Equal("hptekla-mcp-2025", PipeNaming.For(PipeNaming.TeklaHost, 2025));
    }

    [Theory]
    [InlineData("tekla", 2025, "hptekla-mcp-2025")]
    [InlineData("TEKLA", 2025, "hptekla-mcp-2025")]
    [InlineData("Tekla", 2025, "hptekla-mcp-2025")]
    [InlineData("tEkLa", 2025, "hptekla-mcp-2025")]
    [InlineData("  tekla  ", 2025, "hptekla-mcp-2025")]
    [InlineData("\ttekla\r\n", 2025, "hptekla-mcp-2025")]
    [InlineData("tekla", 2024, "hptekla-mcp-2024")]
    [InlineData("tekla", 2026, "hptekla-mcp-2026")]
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
        Assert.Throws<ArgumentException>(() => PipeNaming.For(invalidHost!, 2025));
    }

    [Fact]
    public void All_ten_hosts_have_pairwise_distinct_constants_and_pipe_names()
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
        };

        Assert.Equal(10, hosts.Length);
        Assert.Equal(10, hosts.Distinct(StringComparer.OrdinalIgnoreCase).Count());

        var pipes = hosts.Select(h => PipeNaming.For(h, 2025)).ToArray();
        Assert.Equal(10, pipes.Distinct(StringComparer.OrdinalIgnoreCase).Count());
        Assert.Contains("hptekla-mcp-2025", pipes);
    }

    // =========================================================================
    // 3. JSON-RPC METHODS BIJECTION & NOTIFICATION DETECTORS
    // =========================================================================

    [Fact]
    public void JsonRpcMethods_TeklaPrefix_is_tekla_dot()
    {
        Assert.Equal("tekla.", JsonRpcMethods.TeklaPrefix);
    }

    [Fact]
    public void ProgressMethodFor_produces_tekla_progress_and_Suffix_produces_execute()
    {
        // 1. JsonRpcMethods.Suffix
        Assert.Equal("execute", JsonRpcMethods.Suffix("tekla.execute"));

        // 2. RequestDispatcher.ProgressMethodFor via reflection
        var method = typeof(RequestDispatcher).GetMethod("ProgressMethodFor", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(method);
        var progressMethod = (string)method.Invoke(null, ["tekla.execute"])!;
        Assert.Equal("tekla.progress", progressMethod);

        // Also test without prefix falls back to historical Revit name
        var fallback = (string)method.Invoke(null, ["execute"])!;
        Assert.Equal(JsonRpcMethods.ProgressNotification, fallback);
    }

    [Theory]
    [InlineData("execute", "tekla.execute")]

    [InlineData("context", "tekla.context")]
    [InlineData("ping", "tekla.ping")]
    [InlineData("cancel", "tekla.cancel")]
    [InlineData("analyze", "tekla.analyze")]
    [InlineData("progress", "tekla.progress")]
    [InlineData("log", "tekla.log")]
    [InlineData("status", "tekla.status")]
    public void JsonRpcMethods_For_and_Suffix_are_strictly_bijective_for_tekla(string suffix, string expectedFull)
    {
        var full = JsonRpcMethods.For(JsonRpcMethods.TeklaPrefix, suffix);
        Assert.Equal(expectedFull, full);
        Assert.Equal(suffix, JsonRpcMethods.Suffix(full));
    }

    [Theory]
    [InlineData("tekla.progress", true, false, false)]
    [InlineData("tekla.log", false, true, false)]
    [InlineData("tekla.status", false, false, true)]
    [InlineData("tekla.execute", false, false, false)]
    [InlineData("tekla.context", false, false, false)]
    [InlineData("tekla.ping", false, false, false)]
    public void JsonRpcMethods_notification_detectors_work_for_tekla(
        string method, bool expectedProgress, bool expectedLog, bool expectedStatus)
    {
        Assert.Equal(expectedProgress, JsonRpcMethods.IsProgress(method));
        Assert.Equal(expectedLog, JsonRpcMethods.IsLog(method));
        Assert.Equal(expectedStatus, JsonRpcMethods.IsStatus(method));
    }

    [Fact]
    public void All_ten_hosts_have_mutually_distinct_rpc_prefixes_ending_with_dot()
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
        };

        Assert.Equal(10, prefixes.Length);
        Assert.Equal(10, prefixes.Distinct(StringComparer.Ordinal).Count());
        Assert.All(prefixes, p => Assert.EndsWith(".", p));
    }

    // =========================================================================
    // 4. CONTEXT RESULT WIRE SERIALIZATION & SIBLING HOST ISOLATION
    // =========================================================================

    [Fact]
    public void ContextResult_when_Tekla_is_null_never_contains_tekla_in_json()
    {
        var context = new ContextResult
        {
            Host = "revit",
            HostVersion = "2026",
            RevitVersion = "2026",
            DocTitle = "Tower.rvt",
            Tekla = null,
        };

        var json = BridgeJson.Serialize(context);
        Assert.DoesNotContain("\"tekla\"", json);
    }

    [Fact]
    public void ContextResult_for_all_sibling_hosts_never_contains_tekla_payload()
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
        };

        foreach (var ctx in siblingContexts)
        {
            var json = BridgeJson.Serialize(ctx);
            Assert.DoesNotContain("\"tekla\"", json);
        }
    }

    [Fact]
    public void ContextResult_for_tekla_contains_zero_sibling_payloads()
    {
        var teklaContext = new ContextResult
        {
            Host = "tekla",
            HostVersion = "2025",
            Tekla = new TeklaInfo(
                IsConnected: true,
                ModelName: "StandardModel",
                ModelPath: @"C:\TeklaStructuresModels\StandardModel",
                ProjectName: "ProjectAlpha",
                TeklaVersion: "2025.0",
                HeavyOperationsEnabled: true,
                PartCount: 150,
                RebarCount: 300,
                DrawingCount: 12),
        };

        var json = BridgeJson.Serialize(teklaContext);

        Assert.Contains("\"tekla\"", json);
        Assert.DoesNotContain("\"autocad\"", json);
        Assert.DoesNotContain("\"navis\"", json);
        Assert.DoesNotContain("\"etabs\"", json);
        Assert.DoesNotContain("\"civil3d\"", json);
        Assert.DoesNotContain("\"sap2000\"", json);
        Assert.DoesNotContain("\"powerbi\"", json);
        Assert.DoesNotContain("\"excel\"", json);
        Assert.DoesNotContain("\"robot\"", json);
    }

    [Fact]
    public void ContextResult_preserves_TeklaInfo_round_trip_and_formats_camelCase()
    {
        var original = new TeklaInfo(
            IsConnected: true,
            ModelName: "SteelStructure",
            ModelPath: @"D:\TeklaModels\SteelStructure",
            ProjectName: "Warehouse B",
            TeklaVersion: "2025.0 SP1",
            HeavyOperationsEnabled: false,
            PartCount: 1250,
            RebarCount: 4500,
            DrawingCount: 45);

        var context = new ContextResult
        {
            Host = "tekla",
            HostVersion = "2025",
            Tekla = original,
        };

        var json = BridgeJson.Serialize(context);

        // Verify camelCase fields
        Assert.Contains("\"isConnected\":true", json);
        Assert.Contains("\"modelName\":\"SteelStructure\"", json);
        Assert.Contains("\"modelPath\":", json);
        Assert.Contains("\"projectName\":\"Warehouse B\"", json);
        Assert.Contains("\"teklaVersion\":\"2025.0 SP1\"", json);
        Assert.Contains("\"heavyOperationsEnabled\":false", json);
        Assert.Contains("\"partCount\":1250", json);
        Assert.Contains("\"rebarCount\":4500", json);
        Assert.Contains("\"drawingCount\":45", json);

        var deserialized = BridgeJson.Deserialize<ContextResult>(json);
        Assert.NotNull(deserialized);
        Assert.NotNull(deserialized.Tekla);
        Assert.Equal(original, deserialized.Tekla);
        Assert.Equal("SteelStructure", deserialized.Tekla.ModelName);
        Assert.Equal(1250, deserialized.Tekla.PartCount);
        Assert.Equal(4500, deserialized.Tekla.RebarCount);
    }

    [Fact]
    public void ContextResult_omits_nullable_TeklaInfo_fields_when_null()
    {
        var info = new TeklaInfo(
            IsConnected: false,
            ModelName: null,
            ModelPath: null,
            ProjectName: null,
            TeklaVersion: null,
            HeavyOperationsEnabled: false,
            PartCount: 0,
            RebarCount: 0,
            DrawingCount: 0);

        var context = new ContextResult
        {
            Host = "tekla",
            HostVersion = "2025",
            Tekla = info,
        };

        var json = BridgeJson.Serialize(context);

        Assert.DoesNotContain("\"modelName\"", json);
        Assert.DoesNotContain("\"modelPath\"", json);
        Assert.DoesNotContain("\"projectName\"", json);
        Assert.DoesNotContain("\"teklaVersion\"", json);

        Assert.Contains("\"isConnected\":false", json);
        Assert.Contains("\"heavyOperationsEnabled\":false", json);
        Assert.Contains("\"partCount\":0", json);

        var deserialized = BridgeJson.Deserialize<ContextResult>(json);
        Assert.NotNull(deserialized?.Tekla);
        Assert.Null(deserialized.Tekla.ModelName);
        Assert.Null(deserialized.Tekla.ModelPath);
    }

    // =========================================================================
    // 5. SCRIPT ANALYZER & TRANSACTION DETECTION FOR TEKLA
    // =========================================================================

    [Fact]
    public void AnalyzerProfile_Tekla_flags_CommitChanges_and_ignores_Revit_Transaction_objects()
    {
        Assert.Empty(AnalyzerProfile.Tekla.TransactionTypeNames);
        Assert.Contains("CommitChanges", AnalyzerProfile.Tekla.TransactionMethodNames);

        // CommitChanges flags transaction usage
        const string commitScript = "model.CommitChanges(); return 1;";
        var analysisCommit = ScriptAnalyzer.Analyze(commitScript, AnalyzerProfile.Tekla);
        Assert.True(analysisCommit.UsesTransaction);

        // Revit Transaction objects are NOT flagged under Tekla profile
        const string revitTxScript = "using (var t = new Transaction()) { } return 1;";
        var analysisRevitTx = ScriptAnalyzer.Analyze(revitTxScript, AnalyzerProfile.Tekla);
        Assert.False(analysisRevitTx.UsesTransaction);

        // Normal script without CommitChanges is NOT flagged
        const string benignScript = "var p = new Point(0, 0, 0); return p.X;";
        var analysisBenign = ScriptAnalyzer.Analyze(benignScript, AnalyzerProfile.Tekla);
        Assert.False(analysisBenign.UsesTransaction);
    }

    // =========================================================================
    // 6. HOST NEUTRALITY VERIFICATION
    // =========================================================================

    [Fact]
    public void McpShared_never_references_Tekla_Open_API_assemblies()
    {
        var forbiddenTeklaPrefixes = new[] { "Tekla.Structures", "Tekla." };

        var sharedAssemblies = new[]
        {
            typeof(PipeNaming).Assembly,
            typeof(PipeListener).Assembly,
            typeof(ResultFormatter).Assembly,
        };

        foreach (var assembly in sharedAssemblies)
        {
            var referenced = assembly.GetReferencedAssemblies().Select(a => a.Name ?? string.Empty).ToArray();
            foreach (var prefix in forbiddenTeklaPrefixes)
            {
                Assert.DoesNotContain(referenced, name => name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
            }
        }
    }

    // =========================================================================
    // 7. NAMED PIPE ROUND-TRIP WITH FAKE EXECUTOR & CONTEXT SERVICE
    // =========================================================================

    [Fact]
    public async Task Fake_executor_round_trip_for_tekla_ping_context_execute_cancel_analyze()
    {
        var pipe = "test-tekla-pipe-" + Guid.NewGuid().ToString("N");
        var executor = new FakeRevitExecutor
        {
            ContextHandler = _ => new ContextResult
            {
                RevitVersion = "2025",
                Host = "tekla",
                HostVersion = "2025",
                DocTitle = "Warehouse.db1",
                DocPath = @"C:\TeklaStructuresModels\Warehouse",
                Tekla = new TeklaInfo(true, "Warehouse", @"C:\TeklaStructuresModels\Warehouse", "ProjectAlpha", "2025.0", true, 500, 1000, 25),
            },
            ExecuteHandler = req => new ExecuteResult
            {
                Value = JsonSerializer.SerializeToElement(new { insertedBeams = 4 }),
                ValueType = "AnonymousType",
                Snapshot = "Warehouse_backup.zip",
            },
        };

        var settings = new BridgeSettings { ExecutionEnabled = true };
        using var listener = new PipeListener(pipe, new RequestDispatcher(executor, settings, "2025", "Tekla Structures"));
        listener.Start();

        var options = Options.Create(new BridgeOptions { PipeName = pipe, HostId = "tekla", HostVersion = 2025 });
        await using var client = new RevitBridgeClient(options, NullLogger<RevitBridgeClient>.Instance, Tekla());

        var timeout = TimeSpan.FromSeconds(10);
        var ct = TestContext.Current.CancellationToken;

        // 1. Ping
        var ping = await client.SendAsync<BridgePingResult>("tekla.ping", null, timeout, null, ct);
        Assert.True(ping.Pong);
        Assert.True(ping.ExecutionEnabled);
        Assert.False(ping.Busy);

        // 2. Context
        var context = await client.SendAsync<ContextResult>("tekla.context", new ContextRequest(false), timeout, null, ct);
        Assert.Equal("tekla", context.Host);
        Assert.NotNull(context.Tekla);
        Assert.True(context.Tekla.IsConnected);
        Assert.Equal("Warehouse", context.Tekla.ModelName);
        Assert.Equal(500, context.Tekla.PartCount);
        Assert.Equal(1000, context.Tekla.RebarCount);

        // 3. Execute with progress
        executor.ProgressSteps = 3;
        executor.ProgressDelayMs = 5;
        var progressReports = new List<ProgressParams>();
        var progressSink = new SynchronousProgress<ProgressParams>(p =>
        {
            lock (progressReports) progressReports.Add(p);
        });

        var execRequest = new ExecuteRequest("beam.Insert(); return true;", Label: "InsertBeam");
        var execResult = await client.SendAsync<ExecuteResult>("tekla.execute", execRequest, timeout, progressSink, ct);

        Assert.False(execResult.IsError);
        Assert.Equal("Warehouse_backup.zip", execResult.Snapshot);
        Assert.NotNull(execResult.Value);
        Assert.Equal(4, execResult.Value.Value.GetProperty("insertedBeams").GetInt32());
        Assert.Equal(3, progressReports.Count);

        // 4. Cancel
        var cancelResult = await client.SendAsync<CancelResult>("tekla.cancel", null, timeout, null, ct);
        Assert.False(cancelResult.WasRunning);
        Assert.Equal(1, executor.CancelCalls);

        // 5. Analyze
        var analyzeResult = await client.SendAsync<AnalyzeResult>("tekla.analyze", new AnalyzeRequest("int x = 1;"), timeout, null, ct);
        Assert.True(analyzeResult.Compiles);

        // 6. ContextService shaping
        var contextService = new ContextService(client, new ResultFormatter());
        var shapedText = await contextService.ReadAsync(false, ct);
        using var jsonDoc = JsonDocument.Parse(shapedText);
        var root = jsonDoc.RootElement;

        // Verify Revit legacy fields stripped
        Assert.False(root.TryGetProperty("revitVersion", out _));
        Assert.False(root.TryGetProperty("isFamily", out _));

        // Verify Tekla payload preserved
        Assert.Equal("tekla", root.GetProperty("host").GetString());
        Assert.Equal("2025", root.GetProperty("hostVersion").GetString());
        Assert.True(root.GetProperty("tekla").GetProperty("isConnected").GetBoolean());
        Assert.Equal("Warehouse", root.GetProperty("tekla").GetProperty("modelName").GetString());
        Assert.Equal(500, root.GetProperty("tekla").GetProperty("partCount").GetInt32());

        // Zero sibling payloads in shaped text
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
}
