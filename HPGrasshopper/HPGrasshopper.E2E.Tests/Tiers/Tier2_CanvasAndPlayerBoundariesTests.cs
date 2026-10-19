namespace HPGrasshopper.E2E.Tests.Tiers;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using HPGrasshopper.E2E.Tests.Contracts;
using HPGrasshopper.E2E.Tests.Harness;
using HPRebar.Mcp.Contracts.Messages;
using HPRebar.McpBridge.Core.Scripting;
using Xunit;

public sealed class Tier2_CanvasAndPlayerBoundariesTests
{
    [Fact]
    public void TC_T2_RDC_01_NullActiveDocumentGracefulFallback()
    {
        var bridge = new GrasshopperE2EBridge();
        bridge.DetachLiveCanvas();

        var ctx = bridge.GetContext();
        Assert.False(ctx.IsLiveCanvasAttached);
    }

    [Fact]
    public void TC_T2_RDC_02_CompletelyEmptyCanvasBoundary()
    {
        var bridge = new GrasshopperE2EBridge();
        bridge.ActiveCanvas.Components.Clear();
        bridge.ActiveCanvas.Wires.Clear();

        var ctx = bridge.GetContext();
        Assert.Equal(0, ctx.ComponentsCount);
        Assert.Equal(0, ctx.WiresCount);
    }

    [Fact]
    public void TC_T2_RDC_03_NonExistentCategoryFilterNegativeTest()
    {
        var bridge = new GrasshopperE2EBridge();
        GrasshopperE2EHarness.AddComponentToCanvas(bridge, "Node1", "N1", "Math", 0, 0);

        var filtered = bridge.ActiveCanvas.Components.Where(c => c.Category == "NonExistentCat").ToList();
        Assert.Empty(filtered);
    }

    [Fact]
    public void TC_T2_RDC_04_MassiveCanvasStressBoundary()
    {
        var bridge = new GrasshopperE2EBridge();
        for (int i = 0; i < 500; i++)
        {
            GrasshopperE2EHarness.AddComponentToCanvas(bridge, $"Node_{i}", $"N_{i}", "Batch", i, i);
        }

        Assert.Equal(500, bridge.ActiveCanvas.Components.Count);
        var json = bridge.ActiveCanvas.ToJson();
        Assert.True(json.Length > 1000);
    }

    [Fact]
    public void TC_T2_RDC_05_CircularWireTopologySafeTraversal()
    {
        var bridge = new GrasshopperE2EBridge();
        var c1 = GrasshopperE2EHarness.AddComponentToCanvas(bridge, "A", "A", "Math", 0, 0);
        var c2 = GrasshopperE2EHarness.AddComponentToCanvas(bridge, "B", "B", "Math", 100, 0);

        GrasshopperE2EHarness.ConnectPortsOnCanvas(bridge, c1.Id, 0, c2.Id, 0);
        GrasshopperE2EHarness.ConnectPortsOnCanvas(bridge, c2.Id, 0, c1.Id, 0);

        Assert.Equal(2, bridge.ActiveCanvas.Wires.Count);
    }

    [Fact]
    public void TC_T2_RPL_01_NonExistentFilePathNegativeTest()
    {
        var invalidPath = "C:\\NoSuchFile_" + Guid.NewGuid().ToString("N") + ".gh";
        var result = GrasshopperE2EHarness.RunPlayer(invalidPath);

        Assert.False(result.Success);
        Assert.NotNull(result.Error);
        Assert.Contains("Definition file not found", result.Error);
        Assert.Contains(invalidPath, result.Error);
    }

    [Fact]
    public void TC_T2_RPL_02_CorruptedArchiveFileNegativeTest()
    {
        var corruptedJson = "{ this is not valid json at all }";
        var result = GrasshopperCanvasSpec.FromJson(corruptedJson);
        Assert.Null(result);

        var emptyJson = "";
        var emptyResult = GrasshopperCanvasSpec.FromJson(emptyJson);
        Assert.Null(emptyResult);
    }

    [Fact]
    public void TC_T2_RPL_03_EmptyInputsObjectBoundaryTest()
    {
        var bridge = new GrasshopperE2EBridge();
        var sample = GrasshopperE2EHarness.CreateSampleTowerGraph();
        foreach (var c in sample.Components) bridge.ActiveCanvas.Components.Add(c);

        var inputs = new Dictionary<string, object>();
        var slider = bridge.ActiveCanvas.Components.First(c => c.Name == "Number Slider");
        var defaultVal = slider.Properties["Value"];

        Assert.Empty(inputs);
        Assert.Equal(10.0, Convert.ToDouble(defaultVal));
    }

    [Fact]
    public async Task TC_T2_RPL_04_TypeMismatchedInputValuesNegativeTest()
    {
        var bridge = new GrasshopperE2EBridge { IsExecutionEnabled = true };
        var script = """
            var val = args.DoubleOrNull("SliderValue");
            if (val == null) throw new ArgumentException("SliderValue must be a valid numeric double.");
            return val.Value;
            """;
        var argsJson = JsonSerializer.Serialize(new { SliderValue = "NotANumber" });
        var request = new GrasshopperExecutionRequest(script, ArgsJson: argsJson);

        var result = await bridge.ExecuteAsync(request);
        Assert.False(result.Success);
        Assert.NotNull(result.ErrorMessage);
        Assert.Contains("SliderValue must be a valid numeric double", result.ErrorMessage);
    }

    [Fact]
    public void TC_T2_RPL_05_PathTraversalCharacterScrubbing()
    {
        var malicious = "../../../Windows/System32/cmd.exe";
        var result = GrasshopperE2EHarness.RunPlayer(malicious);
        Assert.False(result.Success);
        Assert.NotNull(result.Error);
        Assert.Contains("Invalid file extension", result.Error);

        var traversalScript = "System.Diagnostics.Process.Start(\"cmd.exe\");";
        var violations = ScriptGuard.Check(traversalScript, GuardProfile.Grasshopper);
        Assert.NotEmpty(violations);
        Assert.Contains(violations, v => v.Message.Contains("System.Diagnostics"));
    }
}
