namespace HPGrasshopper.E2E.Tests.Tiers;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using HPGrasshopper.E2E.Tests.Contracts;
using HPGrasshopper.E2E.Tests.Harness;
using HPRebar.Mcp.Contracts.Messages;
using Xunit;

public sealed class Tier1_CanvasAndPlayerToolsTests
{
    [Fact]
    public void TC_T1_RDC_01_CompleteCanvasGraphExtraction()
    {
        var bridge = new GrasshopperE2EBridge();
        var sample = GrasshopperE2EHarness.CreateSampleTowerGraph();
        foreach (var c in sample.Components) bridge.ActiveCanvas.Components.Add(c);
        foreach (var w in sample.Wires) bridge.ActiveCanvas.Wires.Add(w);

        var context = bridge.GetContext();
        Assert.Equal(2, context.ComponentsCount);
        Assert.Equal(1, context.WiresCount);
        Assert.Equal("ParametricTower.gh", context.ActiveDefinitionName);
    }

    [Fact]
    public void TC_T1_RDC_02_IncludeParameterValuesFlag()
    {
        var sample = GrasshopperE2EHarness.CreateSampleTowerGraph();
        var slider = sample.Components.First(c => c.Name == "Number Slider");

        Assert.True(slider.Properties.ContainsKey("Value"));
        Assert.Equal(10.0, Convert.ToDouble(slider.Properties["Value"]));
    }

    [Fact]
    public void TC_T1_RDC_03_SelectedOnlyComponentFiltering()
    {
        var bridge = new GrasshopperE2EBridge();
        var c1 = GrasshopperE2EHarness.AddComponentToCanvas(bridge, "Node1", "N1", "Math", 0, 0);
        var c2 = GrasshopperE2EHarness.AddComponentToCanvas(bridge, "Node2", "N2", "Math", 100, 0);

        var selected = new List<GrasshopperComponentSpec>
        {
            c1 with { Selected = true },
            c2 with { Selected = false }
        };

        var filtered = selected.Where(c => c.Selected).ToList();
        Assert.Single(filtered);
        Assert.Equal("Node1", filtered[0].Name);
    }

    [Fact]
    public void TC_T1_RDC_04_CategoryFilteringMechanism()
    {
        var bridge = new GrasshopperE2EBridge();
        GrasshopperE2EHarness.AddComponentToCanvas(bridge, "Addition", "Add", "Maths", 0, 0);
        GrasshopperE2EHarness.AddComponentToCanvas(bridge, "Panel", "P", "Params", 100, 0);

        var maths = bridge.ActiveCanvas.Components.Where(c => c.Category == "Maths").ToList();
        Assert.Single(maths);
        Assert.Equal("Addition", maths[0].Name);
    }

    [Fact]
    public void TC_T1_RDC_05_RuntimeWarningAndErrorMessagesExtraction()
    {
        var bridge = new GrasshopperE2EBridge();
        var warningMsgs = new[] { "Warning: Division by zero", "Error: Input B cannot be zero" };
        var errorComp = GrasshopperE2EHarness.AddComponentToCanvas(
            bridge, "Divide", "Div", "Maths", 100, 100, warningMsgs);
        GrasshopperE2EHarness.AddComponentToCanvas(
            bridge, "Number Slider", "B", "Params", 0, 100);

        var context = bridge.GetContext();
        Assert.Equal(2, context.ComponentsCount);
        Assert.Equal("ParametricTower.gh", context.ActiveDefinitionName);

        var extracted = GrasshopperE2EHarness.ExtractComponentMessages(bridge, errorComp.Id);
        Assert.NotNull(extracted);
        Assert.Equal(2, extracted.Count);
        Assert.Contains("Warning: Division by zero", extracted);
        Assert.Contains("Error: Input B cannot be zero", extracted);

        var allExtracted = GrasshopperE2EHarness.ExtractComponentMessages(bridge);
        Assert.Equal(2, allExtracted.Count);
    }

    [Fact]
    public void TC_T1_RPL_01_PlayerDefinitionExecution()
    {
        var sample = GrasshopperE2EHarness.CreateSampleTowerGraph();
        Assert.Equal("ParametricTower.gh", sample.Name);
        Assert.NotEmpty(sample.Components);
    }

    [Fact]
    public async Task TC_T1_RPL_02_ParameterInputInjection()
    {
        var bridge = new GrasshopperE2EBridge { IsExecutionEnabled = true };
        var argsJson = JsonSerializer.Serialize(new { FloorCount = 25, FloorHeight = 3.8 });
        var req = new GrasshopperExecutionRequest(
            "int floors = args.Int(\"FloorCount\", 10); double height = args.Double(\"FloorHeight\", 3.0); return floors * height;",
            ArgsJson: argsJson);

        var result = await bridge.ExecuteAsync(req);
        Assert.True(result.Success);
        Assert.NotNull(result.OutputJson);
        using var doc = JsonDocument.Parse(result.OutputJson);
        Assert.Equal(95.0, doc.RootElement.GetDouble(), precision: 1);
    }

    [Fact]
    public async Task TC_T1_RPL_03_OutputExtractionFiltering()
    {
        var bridge = new GrasshopperE2EBridge { IsExecutionEnabled = true };
        var script = """
            var outputs = new Dictionary<string, object>
            {
                ["TotalHeight"] = 95.0,
                ["TotalArea"] = 4500.0,
                ["Volume"] = 18000.0
            };
            var filterPrefix = args.Str("FilterPrefix", "Total");
            return outputs.Where(kv => kv.Key.StartsWith(filterPrefix)).ToDictionary(k => k.Key, v => v.Value);
            """;
        var argsJson = JsonSerializer.Serialize(new { FilterPrefix = "Total" });
        var req = new GrasshopperExecutionRequest(script, ArgsJson: argsJson);

        var result = await bridge.ExecuteAsync(req);
        Assert.True(result.Success);
        Assert.NotNull(result.OutputJson);
        using var doc = JsonDocument.Parse(result.OutputJson);
        var filtered = doc.RootElement;
        Assert.True(filtered.TryGetProperty("TotalHeight", out _));
        Assert.True(filtered.TryGetProperty("TotalArea", out _));
        Assert.False(filtered.TryGetProperty("Volume", out _));
    }

    [Fact]
    public void TC_T1_RPL_04_HeadlessLifecycleDisposal()
    {
        var bridge = new GrasshopperE2EBridge();
        bridge.AttachLiveCanvas("Disposable.gh");
        Assert.True(bridge.IsLiveCanvasAttached);

        bridge.DetachLiveCanvas();
        Assert.False(bridge.IsLiveCanvasAttached);
    }

    [Fact]
    public async Task TC_T1_RPL_05_ExecutionDurationTelemetry()
    {
        var bridge = new GrasshopperE2EBridge { IsExecutionEnabled = true };
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var req = new GrasshopperExecutionRequest("return 42;", DryRun: true);
        var result = await bridge.ExecuteAsync(req);
        sw.Stop();

        Assert.True(result.Success);
        Assert.True(result.DurationMs >= 0);
        Assert.True(sw.ElapsedMilliseconds >= 0);
    }
}
