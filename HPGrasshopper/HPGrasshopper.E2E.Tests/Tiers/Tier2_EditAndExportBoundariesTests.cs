namespace HPGrasshopper.E2E.Tests.Tiers;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HPGrasshopper.E2E.Tests.Contracts;
using HPGrasshopper.E2E.Tests.Harness;
using Xunit;

public sealed class Tier2_EditAndExportBoundariesTests
{
    [Fact]
    public void TC_T2_BEC_01_BatchEditOnNullDocumentNegativeTest()
    {
        var bridge = new GrasshopperE2EBridge();
        bridge.DetachLiveCanvas();

        Assert.False(bridge.IsLiveCanvasAttached);
    }

    [Fact]
    public void TC_T2_BEC_02_ExtremeCoordinateValuesClampingBoundary()
    {
        var bridge = new GrasshopperE2EBridge();
        Assert.Throws<ArgumentException>(() =>
            GrasshopperE2EHarness.AddComponentToCanvas(bridge, "NaNNode", "N", "Math", double.NaN, 100));

        Assert.Throws<ArgumentException>(() =>
            GrasshopperE2EHarness.AddComponentToCanvas(bridge, "InfNode", "I", "Math", 100, double.PositiveInfinity));
    }

    [Fact]
    public void TC_T2_BEC_03_UnknownComponentTypeNameNegativeTest()
    {
        var bridge = new GrasshopperE2EBridge();
        var c = GrasshopperE2EHarness.AddComponentToCanvas(bridge, "Invalid.Type.Does.Not.Exist", "Inv", "Unknown", 0, 0);

        Assert.NotNull(c);
        Assert.Equal("Invalid.Type.Does.Not.Exist", c.Name);
    }

    [Fact]
    public void TC_T2_BEC_04_RemoveNonExistentGuidNegativeTest()
    {
        var bridge = new GrasshopperE2EBridge();
        var removed = GrasshopperE2EHarness.RemoveComponentFromCanvas(bridge, Guid.NewGuid());

        Assert.False(removed);
    }

    [Fact]
    public void TC_T2_BEC_05_DuplicateWireConnectionIdempotency()
    {
        var bridge = new GrasshopperE2EBridge();
        var c1 = GrasshopperE2EHarness.AddComponentToCanvas(bridge, "A", "A", "M", 0, 0);
        var c2 = GrasshopperE2EHarness.AddComponentToCanvas(bridge, "B", "B", "M", 100, 0);

        GrasshopperE2EHarness.ConnectPortsOnCanvas(bridge, c1.Id, 0, c2.Id, 0);
        var existing = bridge.ActiveCanvas.Wires.Any(w =>
            w.SourceComponentId == c1.Id && w.SourcePortIndex == 0 &&
            w.TargetComponentId == c2.Id && w.TargetPortIndex == 0);

        Assert.True(existing);
    }

    [Fact]
    public void TC_T2_IMG_01_NullCanvasNegativeTest()
    {
        var bridge = new GrasshopperE2EBridge();
        bridge.DetachLiveCanvas();

        Assert.False(bridge.IsLiveCanvasAttached);
    }

    [Fact]
    public void TC_T2_IMG_02_ExtremeZoomFactorLowerClamping()
    {
        var sample = GrasshopperE2EHarness.CreateSampleTowerGraph();
        var result = GrasshopperE2EHarness.ExportCanvasImage(sample, zoom: -10.0);

        Assert.True(result.Success);
        Assert.True(result.Width >= 200);
        Assert.True(result.Height >= 200);
        Assert.Equal(200, result.Width);
        Assert.Equal(200, result.Height);
    }

    [Fact]
    public void TC_T2_IMG_03_ExtremeZoomFactorUpperClamping()
    {
        var sample = GrasshopperE2EHarness.CreateSampleTowerGraph();
        var result = GrasshopperE2EHarness.ExportCanvasImage(sample, zoom: 100.0);

        Assert.True(result.Success);
        Assert.Equal(5000, result.Width);
        Assert.Equal(4000, result.Height);
    }

    [Fact]
    public void TC_T2_IMG_04_ZeroDimensionCanvasFallback()
    {
        var emptyCanvas = new GrasshopperCanvasSpec(
            Guid.NewGuid(), "Empty.gh", "Empty",
            new List<GrasshopperComponentSpec>(), new List<GrasshopperWireSpec>());

        var result = GrasshopperE2EHarness.ExportCanvasImage(emptyCanvas, zoom: 0.1);

        Assert.True(result.Success);
        Assert.Equal(200, result.Width);
        Assert.Equal(200, result.Height);
    }

    [Fact]
    public void TC_T2_IMG_05_IllegalOutputFilePathCharactersNegativeTest()
    {
        var sample = GrasshopperE2EHarness.CreateSampleTowerGraph();
        var illegalPath = "C:\\Invalid|Path*?\\file.png";

        var result = GrasshopperE2EHarness.ExportCanvasImage(sample, outputPath: illegalPath);

        Assert.False(result.Success);
        Assert.NotNull(result.Error);
        Assert.NotEmpty(result.Error);
        Assert.Contains(illegalPath, result.OutputPath);
        Assert.Empty(result.Base64);
    }
}
