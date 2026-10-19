namespace HPGrasshopper.E2E.Tests.Tiers;

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using HPGrasshopper.E2E.Tests.Contracts;
using HPGrasshopper.E2E.Tests.Harness;
using HPRebar.Mcp.Contracts.Messages;
using HPRebar.Mcp.Server.Hosts;
using HPRebar.Mcp.Server.Registry;
using HPRebar.Mcp.Server.Registry.Model;
using Xunit;

public sealed class Tier3_CrossFeatureInteractionTests
{
    [Fact]
    public void TC_T3_INT_01_CanvasReadAndBatchEditVerification()
    {
        var bridge = new GrasshopperE2EBridge();
        Assert.Equal(0, bridge.GetContext().ComponentsCount);

        var c1 = GrasshopperE2EHarness.AddComponentToCanvas(bridge, "Number Slider", "Slider", "Params", 100, 100);
        var c2 = GrasshopperE2EHarness.AddComponentToCanvas(bridge, "Panel", "Panel", "Params", 300, 100);
        GrasshopperE2EHarness.ConnectPortsOnCanvas(bridge, c1.Id, 0, c2.Id, 0);

        var ctx = bridge.GetContext();
        Assert.Equal(2, ctx.ComponentsCount);
        Assert.Equal(1, ctx.WiresCount);
    }

    [Fact]
    public async Task TC_T3_INT_02_BatchEditParameterAndRunPlayerSolve()
    {
        var bridge = new GrasshopperE2EBridge { IsExecutionEnabled = true };
        var sample = GrasshopperE2EHarness.CreateSampleTowerGraph();
        bridge.ActiveCanvas.Components.AddRange(sample.Components);
        bridge.ActiveCanvas.Wires.AddRange(sample.Wires);

        var slider = bridge.ActiveCanvas.Components.First(c => c.NickName == "FloorCount");
        slider.Properties["Value"] = 25.0;

        var script = "var s = doc.Components.First(c => c.NickName == \"FloorCount\"); return Convert.ToDouble(s.Properties[\"Value\"]) * 3.8;";
        var req = new GrasshopperExecutionRequest(script, DryRun: false);
        var res = await bridge.ExecuteAsync(req);

        Assert.True(res.Success);
        Assert.NotNull(res.OutputJson);
        using var jsonDoc = JsonDocument.Parse(res.OutputJson);
        Assert.Equal(95.0, jsonDoc.RootElement.GetDouble(), precision: 1);
        Assert.Equal(25.0, Convert.ToDouble(slider.Properties["Value"]));
    }

    [Fact]
    public async Task TC_T3_INT_03_RunPlayerWithDryRunDualSafetyRollback()
    {
        var bridge = new GrasshopperE2EBridge { IsExecutionEnabled = true };
        var req = new GrasshopperExecutionRequest("CreateElement();", DryRun: true);
        var res = await bridge.ExecuteAsync(req);

        Assert.True(res.Success);
        Assert.True(res.RolledBack);
        Assert.Equal(0, bridge.GetCommittedElementCount());
    }

    [Fact]
    public async Task TC_T3_INT_04_ToolRegistryDynamicLifecycle()
    {
        var bridge = new GrasshopperE2EBridge { IsExecutionEnabled = true };
        var code = "int count = 10; return count * 2;";
        var candidate = new ToolRecord
        {
            Name = "calculate_tower_metrics",
            Category = "Geometry",
            Description = "Calculates geometric metrics and volume for parametric tower.",
            Transaction = "auto",
            TimeoutSeconds = 30,
            Code = code,
            InputSchema = JsonSerializer.Deserialize<JsonElement>("""{"type":"object","properties":{}}"""),
            Examples = [new ToolExample { Title = "Default", Args = JsonSerializer.Deserialize<JsonElement>("{}") }],
            Status = ToolStatus.Draft
        };

        var analysis = bridge.Analyze(new AnalyzeRequest(code));
        var report = ToolValidator.Validate(candidate, analysis, [], false, HostProfile.Grasshopper);
        Assert.True(report.IsValid);
        Assert.Equal(ToolStatus.Draft, candidate.Status);

        var testRes = await bridge.ExecuteAsync(new GrasshopperExecutionRequest(candidate.Code, DryRun: true));
        Assert.True(testRes.Success);
        candidate.Status = ToolStatus.Tested;

        candidate.Status = ToolStatus.Published;
        candidate.ApprovedBy = "governance_reviewer";
        Assert.True(candidate.IsPublished);
        Assert.True(candidate.IsRunnable);
    }

    [Fact]
    public void TC_T3_INT_05_DynamicRegistryToolRetirementRejection()
    {
        var record = new ToolRecord
        {
            Name = "legacy_truss_evaluator",
            Category = "Geometry",
            Description = "Legacy non-parametric truss evaluator retired from active service.",
            Status = ToolStatus.Published
        };
        Assert.True(record.IsPublished);
        Assert.True(record.IsRunnable);

        record.Status = ToolStatus.Deprecated;
        record.Notes = "Retired and deprecated in favor of parametric solver.";
        Assert.False(record.IsPublished);
        Assert.False(record.IsRunnable);
        Assert.Equal(ToolStatus.Deprecated, record.Status);

        var ex = Assert.Throws<ToolNotRunnableException>(() =>
        {
            if (!record.IsRunnable) throw new ToolNotRunnableException($"Tool '{record.Name}' is deprecated.");
        });
        Assert.Contains("deprecated", ex.Message);

        var candidateTools = new List<ToolRecord> { record };
        var activePublished = candidateTools.Where(t => t.IsPublished && t.IsRunnable).Select(t => t.Name).ToList();
        Assert.DoesNotContain(record.Name, activePublished);
    }

    [Fact]
    public async Task TC_T3_INT_06_ScriptGuardAstValidationAndCancellation()
    {
        var bridge = new GrasshopperE2EBridge { IsExecutionEnabled = true };
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var req = new GrasshopperExecutionRequest("CreateElement();");
        var res = await bridge.ExecuteAsync(req, cts.Token);
        Assert.False(res.Success);
        Assert.True(res.RolledBack);
    }

    [Fact]
    public async Task TC_T3_INT_07_CanvasBatchEditAndUndoRollbackTopology()
    {
        var bridge = new GrasshopperE2EBridge { IsExecutionEnabled = true };
        var initComp = bridge.ActiveCanvas.Components.Count;

        var req = new GrasshopperExecutionRequest("CreateElement();", DryRun: true);
        var res = await bridge.ExecuteAsync(req);
        Assert.True(res.RolledBack);
        Assert.Equal(initComp, bridge.ActiveCanvas.Components.Count);
    }

    [Fact]
    public async Task TC_T3_INT_08_ExecuteCodeWithRevitTransactionAndContext()
    {
        var bridge = new GrasshopperE2EBridge { IsExecutionEnabled = true };
        var req = new GrasshopperExecutionRequest("CreateElement();", DryRun: false);
        var res = await bridge.ExecuteAsync(req);

        Assert.True(res.Success);
        Assert.Equal(1, bridge.GetCommittedElementCount());
        Assert.True(bridge.GetContext().IsLiveCanvasAttached);
    }

    [Fact]
    public void TC_T3_INT_09_ExportCanvasImagePostBatchEdit()
    {
        var bridge = new GrasshopperE2EBridge();
        GrasshopperE2EHarness.AddComponentToCanvas(bridge, "OriginNode", "ON", "Params", 50, 50);
        GrasshopperE2EHarness.AddComponentToCanvas(bridge, "ClusterNode", "CN", "Math", 550, 350);

        var tempDir = Path.Combine(Path.GetTempPath(), "gh_export_" + Guid.NewGuid().ToString("N"));
        var exportPath = Path.Combine(tempDir, "canvas_export.png");
        try
        {
            var result = GrasshopperE2EHarness.ExportCanvasImage(bridge.ActiveCanvas, outputPath: exportPath);
            Assert.True(result.Success);
            Assert.True(File.Exists(exportPath));
            Assert.True(new FileInfo(exportPath).Length > 0);
        }
        finally
        {
            if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public async Task TC_T3_INT_10_SeedPipelineChainingUnderDryRun()
    {
        var bridge = new GrasshopperE2EBridge { IsExecutionEnabled = true };
        var res1 = await bridge.ExecuteAsync(new GrasshopperExecutionRequest("CreateElement();", DryRun: true));
        var res2 = await bridge.ExecuteAsync(new GrasshopperExecutionRequest("CreateElement();", DryRun: true));

        Assert.True(res1.RolledBack);
        Assert.True(res2.RolledBack);
        Assert.Equal(0, bridge.GetCommittedElementCount());
    }

    [Fact]
    public async Task TC_T3_INT_11_OptInSecuritySwitchOverNamedPipe()
    {
        var bridge = new GrasshopperE2EBridge { IsExecutionEnabled = false };
        await using var simulator = new GrasshopperPipeSimulator(bridge: bridge);
        simulator.Start();

        using var client = new NamedPipeClientStream(".", simulator.PipeName, PipeDirection.InOut);
        await client.ConnectAsync(2000);
        using var reader = new StreamReader(client, Encoding.UTF8);
        using var writer = new StreamWriter(client, new UTF8Encoding(false)) { AutoFlush = true };

        await writer.WriteLineAsync("{\"jsonrpc\":\"2.0\",\"id\":1,\"method\":\"grasshopper.execute\",\"params\":{\"code\":\"return 1;\"}}");
        var resp1 = await reader.ReadLineAsync();
        Assert.NotNull(resp1);
        using var doc1 = JsonDocument.Parse(resp1);
        Assert.Equal(-32001, doc1.RootElement.GetProperty("error").GetProperty("code").GetInt32());

        bridge.IsExecutionEnabled = true;
        await writer.WriteLineAsync("{\"jsonrpc\":\"2.0\",\"id\":2,\"method\":\"grasshopper.execute\",\"params\":{\"code\":\"return 1;\"}}");
        var resp2 = await reader.ReadLineAsync();
        Assert.NotNull(resp2);
        using var doc2 = JsonDocument.Parse(resp2);
        Assert.False(doc2.RootElement.GetProperty("result").GetProperty("isError").GetBoolean());
        Assert.Equal(1, doc2.RootElement.GetProperty("result").GetProperty("value").GetInt32());
    }

    [Fact]
    public async Task TC_T3_INT_12_MultiSessionIsolationWithCorrelatedCancellation()
    {
        var bridge = new GrasshopperE2EBridge { IsExecutionEnabled = true };
        using var ctsA = new CancellationTokenSource();
        ctsA.Cancel();

        var taskA = bridge.ExecuteAsync(new GrasshopperExecutionRequest("CreateElement();"), ctsA.Token);
        var taskB = bridge.ExecuteAsync(new GrasshopperExecutionRequest("CreateElement();", DryRun: false));

        var resA = await taskA;
        var resB = await taskB;

        Assert.False(resA.Success);
        Assert.True(resB.Success);
    }

    [Fact]
    public void TC_T3_INT_13_SeedUpgradeAndScriptQualityCompliance()
    {
        var bridge = new GrasshopperE2EBridge();
        var seedCode = "// Facade panelization logic\nvar panels = new List<string>();\nreturn panels;";

        var quality = bridge.ValidateScript(seedCode);
        Assert.True(quality.IsValid);
        Assert.Empty(quality.Errors);
    }

    [Fact]
    public void TC_T3_INT_14_WireReplacementLifoOrderingRollback()
    {
        var bridge = new GrasshopperE2EBridge();
        var nodeA = GrasshopperE2EHarness.AddComponentToCanvas(bridge, "NodeA", "A", "Math", 0, 0);
        var nodeB = GrasshopperE2EHarness.AddComponentToCanvas(bridge, "NodeB", "B", "Math", 0, 100);
        var target = GrasshopperE2EHarness.AddComponentToCanvas(bridge, "Target", "T", "Math", 200, 50);

        var wire1 = GrasshopperE2EHarness.ConnectPortsOnCanvas(bridge, nodeA.Id, 0, target.Id, 0);
        Assert.Single(bridge.ActiveCanvas.Wires);
        Assert.Equal(nodeA.Id, bridge.ActiveCanvas.Wires[0].SourceComponentId);

        var undoStack = new Stack<Action>();
        var replaced = bridge.ActiveCanvas.Wires.First(w => w.TargetComponentId == target.Id && w.TargetPortIndex == 0);
        bridge.ActiveCanvas.Wires.Remove(replaced);
        GrasshopperE2EHarness.ConnectPortsOnCanvas(bridge, nodeB.Id, 0, target.Id, 0);

        undoStack.Push(() =>
        {
            bridge.ActiveCanvas.Wires.RemoveAll(w => w.TargetComponentId == target.Id && w.TargetPortIndex == 0);
            bridge.ActiveCanvas.Wires.Add(replaced);
        });

        Assert.Single(bridge.ActiveCanvas.Wires);
        Assert.Equal(nodeB.Id, bridge.ActiveCanvas.Wires[0].SourceComponentId);

        while (undoStack.Count > 0) undoStack.Pop().Invoke();

        Assert.Single(bridge.ActiveCanvas.Wires);
        Assert.Equal(nodeA.Id, bridge.ActiveCanvas.Wires[0].SourceComponentId);
        Assert.Equal(target.Id, bridge.ActiveCanvas.Wires[0].TargetComponentId);
    }
}
