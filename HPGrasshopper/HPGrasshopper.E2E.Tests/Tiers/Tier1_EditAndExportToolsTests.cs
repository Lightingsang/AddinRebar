namespace HPGrasshopper.E2E.Tests.Tiers;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HPGrasshopper.E2E.Tests.Contracts;
using HPGrasshopper.E2E.Tests.Harness;
using Xunit;

public sealed class Tier1_EditAndExportToolsTests
{
    [Fact]
    public void TC_T1_BEC_01_BatchComponentAddition()
    {
        var bridge = new GrasshopperE2EBridge();
        var c1 = GrasshopperE2EHarness.AddComponentToCanvas(bridge, "Comp1", "C1", "Math", 100, 200);
        var c2 = GrasshopperE2EHarness.AddComponentToCanvas(bridge, "Comp2", "C2", "Math", 300, 200);

        Assert.Equal(2, bridge.ActiveCanvas.Components.Count);
        Assert.Contains(c1, bridge.ActiveCanvas.Components);
        Assert.Contains(c2, bridge.ActiveCanvas.Components);
    }

    [Fact]
    public void TC_T1_BEC_02_BatchComponentRemoval()
    {
        var bridge = new GrasshopperE2EBridge();
        var c = GrasshopperE2EHarness.AddComponentToCanvas(bridge, "ToRemove", "TR", "Test", 0, 0);
        Assert.Single(bridge.ActiveCanvas.Components);

        var removed = GrasshopperE2EHarness.RemoveComponentFromCanvas(bridge, c.Id);
        Assert.True(removed);
        Assert.Empty(bridge.ActiveCanvas.Components);
    }

    [Fact]
    public void TC_T1_BEC_03_WireConnectionCreation()
    {
        var bridge = new GrasshopperE2EBridge();
        var c1 = GrasshopperE2EHarness.AddComponentToCanvas(bridge, "Src", "S", "Math", 0, 0);
        var c2 = GrasshopperE2EHarness.AddComponentToCanvas(bridge, "Tgt", "T", "Math", 200, 0);

        var wire = GrasshopperE2EHarness.ConnectPortsOnCanvas(bridge, c1.Id, 0, c2.Id, 0);
        Assert.Single(bridge.ActiveCanvas.Wires);
        Assert.Equal(c1.Id, wire.SourceComponentId);
        Assert.Equal(c2.Id, wire.TargetComponentId);
    }

    [Fact]
    public void TC_T1_BEC_04_WireDisconnection()
    {
        var bridge = new GrasshopperE2EBridge();
        var c1 = GrasshopperE2EHarness.AddComponentToCanvas(bridge, "Src", "S", "Math", 0, 0);
        var c2 = GrasshopperE2EHarness.AddComponentToCanvas(bridge, "Tgt", "T", "Math", 200, 0);
        GrasshopperE2EHarness.ConnectPortsOnCanvas(bridge, c1.Id, 0, c2.Id, 0);

        bridge.ActiveCanvas.Wires.Clear();
        Assert.Empty(bridge.ActiveCanvas.Wires);
    }

    [Fact]
    public void TC_T1_BEC_05_TempIdInternalResolution()
    {
        var bridge = new GrasshopperE2EBridge();
        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();
        var comp1 = new GrasshopperComponentSpec(id1, "Add", "A", "Math", "Ops", 0, 0, new[] { "x", "y" }, new[] { "r" }, new Dictionary<string, object?>());
        var comp2 = new GrasshopperComponentSpec(id2, "Number", "N", "Params", "Input", 100, 0, Array.Empty<string>(), new[] { "out" }, new Dictionary<string, object?>());
        bridge.ActiveCanvas.Components.Add(comp1);
        bridge.ActiveCanvas.Components.Add(comp2);

        var wire = GrasshopperE2EHarness.ConnectPortsOnCanvas(bridge, id1, 0, id2, 0);
        Assert.Equal(2, bridge.ActiveCanvas.Components.Count);
        Assert.Single(bridge.ActiveCanvas.Wires);
        Assert.Equal(id1, wire.SourceComponentId);
        Assert.Equal(id2, wire.TargetComponentId);
    }

    [Fact]
    public void TC_T1_IMG_01_Base64PngCanvasExport()
    {
        var sample = GrasshopperE2EHarness.CreateSampleTowerGraph();
        var result = GrasshopperE2EHarness.ExportCanvasImage(sample, returnBase64: true);

        Assert.True(result.Success);
        Assert.NotEmpty(result.Base64);
        Assert.True(result.Width > 0);
        Assert.True(result.Height > 0);
    }

    [Fact]
    public void TC_T1_IMG_02_DirectFileOutputExport()
    {
        var sample = GrasshopperE2EHarness.CreateSampleTowerGraph();
        var tempFile = Path.Combine(Path.GetTempPath(), "gh_test_" + Guid.NewGuid().ToString("N") + ".png");
        try
        {
            var result = GrasshopperE2EHarness.ExportCanvasImage(sample, outputPath: tempFile);
            Assert.True(result.Success);
            Assert.True(File.Exists(tempFile));
            Assert.True(new FileInfo(tempFile).Length > 0);
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }
    }

    [Fact]
    public void TC_T1_IMG_03_ZoomScalingAdjustment()
    {
        var sample = GrasshopperE2EHarness.CreateSampleTowerGraph();

        var baseResult = GrasshopperE2EHarness.ExportCanvasImage(sample, zoom: 1.0);
        var zoomedResult = GrasshopperE2EHarness.ExportCanvasImage(sample, zoom: 2.0);

        Assert.True(baseResult.Success);
        Assert.True(zoomedResult.Success);
        Assert.Equal(1000, baseResult.Width);
        Assert.Equal(800, baseResult.Height);
        Assert.Equal(2000, zoomedResult.Width);
        Assert.Equal(1600, zoomedResult.Height);
        Assert.Equal(baseResult.Width * 2, zoomedResult.Width);
        Assert.Equal(baseResult.Height * 2, zoomedResult.Height);
    }

    [Fact]
    public void TC_T1_IMG_04_Base64SuppressionOptimization()
    {
        var sample = GrasshopperE2EHarness.CreateSampleTowerGraph();
        var tempFile = Path.Combine(Path.GetTempPath(), "gh_suppress_" + Guid.NewGuid().ToString("N") + ".png");

        try
        {
            var result = GrasshopperE2EHarness.ExportCanvasImage(sample, outputPath: tempFile, returnBase64: false);
            Assert.True(result.Success);
            Assert.True(File.Exists(tempFile));
            Assert.True(new FileInfo(tempFile).Length > 0);
            Assert.Empty(result.Base64);
            Assert.Equal(tempFile, result.OutputPath);

            var withBase64 = GrasshopperE2EHarness.ExportCanvasImage(sample, returnBase64: true);
            Assert.True(withBase64.Success);
            Assert.NotEmpty(withBase64.Base64);
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }
    }

    [Fact]
    public void TC_T1_IMG_05_AutoDirectoryCreation()
    {
        var sample = GrasshopperE2EHarness.CreateSampleTowerGraph();
        var nestedDir = Path.Combine(Path.GetTempPath(), "gh_sub_" + Guid.NewGuid().ToString("N"));
        var filePath = Path.Combine(nestedDir, "test.png");

        try
        {
            var result = GrasshopperE2EHarness.ExportCanvasImage(sample, outputPath: filePath);
            Assert.True(result.Success);
            Assert.True(Directory.Exists(nestedDir));
            Assert.True(File.Exists(filePath));
        }
        finally
        {
            if (Directory.Exists(nestedDir)) Directory.Delete(nestedDir, true);
        }
    }
}
