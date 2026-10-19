namespace HPGrasshopper.McpBridge.Tests.Tools;

using System;
using HPGrasshopper.McpBridge.Tools;
using Xunit;

public sealed class ImageExporterTests
{
    [Fact]
    public void Export_NullCanvas_ReturnsError()
    {
        var exporter = new ImageExporter();
        var rawResult = exporter.Export(null);

        Assert.NotNull(rawResult);
        var successProp = rawResult.GetType().GetProperty("success");
        Assert.NotNull(successProp);
        var successVal = (bool)successProp.GetValue(rawResult)!;
        Assert.False(successVal);

        var errorProp = rawResult.GetType().GetProperty("error");
        Assert.NotNull(errorProp);
        var errorVal = (string)errorProp.GetValue(rawResult)!;
        Assert.Contains("not open or accessible", errorVal);
    }

    [Fact]
    public void CalculateDimensions_NormalZoom_ScalesCorrectly()
    {
        var (width, height) = ImageExporter.CalculateDimensions(1000, 800, 1.0);

        Assert.Equal(1000, width);
        Assert.Equal(800, height);
    }

    [Fact]
    public void CalculateDimensions_ExtremeLowZoom_ClampedAtMinZoom()
    {
        var (width, height) = ImageExporter.CalculateDimensions(1000, 800, -10.0);

        Assert.Equal(200, width);
        Assert.Equal(200, height);
    }

    [Fact]
    public void CalculateDimensions_ExtremeHighZoom_ClampedAtMaxZoom()
    {
        var (width, height) = ImageExporter.CalculateDimensions(1000, 800, 100.0);

        Assert.Equal(5000, width);
        Assert.Equal(4000, height);
    }

    [Fact]
    public void CalculateDimensions_MinDimensionsClamp_Enforces200px()
    {
        var (width, height) = ImageExporter.CalculateDimensions(100, 50, 1.0);

        Assert.Equal(200, width);
        Assert.Equal(200, height);
    }

    [Fact]
    public void Constants_MinAndMaxZoom_MatchContract()
    {
        Assert.Equal(0.1, ImageExporter.MinZoom);
        Assert.Equal(5.0, ImageExporter.MaxZoom);
    }
}
