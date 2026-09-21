using HPAutoCad.Core.SmartPlot.Models;
using Xunit;

namespace HPAutoCad.Tests.SmartPlot;

public sealed class PlotBoundsAndModelTests
{
    [Fact]
    public void PlotBounds_Properties_ComputedCorrectly()
    {
        var bounds = new PlotBounds(100.0, 200.0, 941.0, 794.0);

        Assert.Equal(841.0, bounds.Width);
        Assert.Equal(594.0, bounds.Height);
        Assert.Equal(520.5, bounds.CenterX);
        Assert.Equal(497.0, bounds.CenterY);
        Assert.True(bounds.IsLandscape);
        Assert.True(bounds.IsValid);
    }

    [Fact]
    public void PlotBounds_Portrait_IdentifiedProperly()
    {
        var portrait = new PlotBounds(0.0, 0.0, 594.0, 841.0);
        Assert.False(portrait.IsLandscape);
        Assert.Equal(594.0, portrait.Width);
        Assert.Equal(841.0, portrait.Height);
    }

    [Fact]
    public void PlotBounds_InvalidBounds_Detected()
    {
        var invertedX = new PlotBounds(500.0, 100.0, 200.0, 600.0);
        Assert.False(invertedX.IsValid);

        var nanBounds = new PlotBounds(double.NaN, 0.0, 100.0, 100.0);
        Assert.False(nanBounds.IsValid);

        var infBounds = new PlotBounds(0.0, 0.0, double.PositiveInfinity, 100.0);
        Assert.False(infBounds.IsValid);
    }

    [Fact]
    public void PlotBounds_VerticalOverlap_CalculatesAccurately()
    {
        var b1 = new PlotBounds(0.0, 1000.0, 841.0, 1594.0); // Height = 594
        var b2 = new PlotBounds(900.0, 960.0, 1741.0, 1554.0); // Height = 594, overlap = 1554 - 1000 = 554

        var overlap = b1.VerticalOverlap(b2);
        Assert.Equal(554.0, overlap);

        // Overlap ratio = 554 / 594 = 0.932 > 0.5
        Assert.True(b1.OverlapsVertically(b2, 0.5));
        Assert.True(b2.OverlapsVertically(b1, 0.5));
    }

    [Fact]
    public void PlotBounds_VerticalOverlap_Disjoint_ReturnsZero()
    {
        var b1 = new PlotBounds(0.0, 1000.0, 841.0, 1594.0);
        var b2 = new PlotBounds(0.0, 100.0, 841.0, 694.0);

        Assert.Equal(0.0, b1.VerticalOverlap(b2));
        Assert.False(b1.OverlapsVertically(b2, 0.5));
    }

    [Fact]
    public void PlotBounds_VerticalOverlap_DegenerateZeroHeight_HandlesGracefully()
    {
        var line1 = new PlotBounds(0.0, 500.0, 841.0, 500.0);
        var line2 = new PlotBounds(0.0, 500.5, 841.0, 500.5);

        Assert.True(line1.OverlapsVertically(line2, 0.5));

        var farLine = new PlotBounds(0.0, 600.0, 841.0, 600.0);
        Assert.False(line1.OverlapsVertically(farLine, 0.5));
    }

    [Fact]
    public void PlotItem_Properties_And_Delegation()
    {
        var bounds = new PlotBounds(10.0, 20.0, 851.0, 614.0);
        var item = new PlotItem
        {
            Id = "frame-1",
            Bounds = bounds,
            LayoutName = "Model",
            DisplayName = "Sheet 01",
            SheetNumber = "A-101",
            SheetTitle = "Floor Plan",
            SourceHandle = "2B4F"
        };

        Assert.Equal("frame-1", item.Id);
        Assert.Equal(10.0, item.MinX);
        Assert.Equal(20.0, item.MinY);
        Assert.Equal(851.0, item.MaxX);
        Assert.Equal(614.0, item.MaxY);
        Assert.Equal(841.0, item.Width);
        Assert.Equal(594.0, item.Height);
        Assert.True(item.IsLandscape);
        Assert.Equal("A-101", item.SheetNumber);
        Assert.Equal("Floor Plan", item.SheetTitle);
        Assert.Equal("2B4F", item.SourceHandle);
        Assert.True(item.IsSelected);
    }

    [Fact]
    public void PlotConfiguration_Defaults_AreStandard()
    {
        var config = new PlotConfiguration();

        Assert.Equal(FrameSourceType.Block, config.FrameSource);
        Assert.Equal(OutputMode.SingleFiles, config.OutputMode);
        Assert.Equal(OrientationMode.Auto, config.Orientation);
        Assert.True(config.FitToPaper);
        Assert.True(config.CenterPlot);
        Assert.Equal("monochrome.ctb", config.PlotStyle);
    }

    [Fact]
    public void PlotResult_FactoryMethods_SetAppropriateStatus()
    {
        var success = PlotResult.Succeeded(new[] { "sheet1.pdf", "sheet2.pdf" }, "merged.pdf", TimeSpan.FromSeconds(3.5));
        Assert.True(success.Success);
        Assert.Equal(2, success.TotalSheets);
        Assert.Equal(2, success.PlottedSheets);
        Assert.Equal(0, success.FailedSheets);
        Assert.Equal("merged.pdf", success.MergedPdfPath);
        Assert.Equal(TimeSpan.FromSeconds(3.5), success.ElapsedTime);

        var fail = PlotResult.Failed("Plotter offline", 5, 2);
        Assert.False(fail.Success);
        Assert.Equal(5, fail.TotalSheets);
        Assert.Equal(2, fail.PlottedSheets);
        Assert.Equal(3, fail.FailedSheets);
        Assert.Single(fail.Errors);
        Assert.Equal("Plotter offline", fail.Errors[0]);

        // Merged mode metrics test: 1 merged output file but multiple plotted sheets
        var mergedSuccess = PlotResult.Succeeded(
            new[] { "all_sheets.pdf" },
            merged: "all_sheets.pdf",
            elapsed: TimeSpan.FromSeconds(12.5),
            totalSheets: 25,
            plottedSheets: 25);

        Assert.True(mergedSuccess.Success);
        Assert.Equal(25, mergedSuccess.TotalSheets);
        Assert.Equal(25, mergedSuccess.PlottedSheets);
        Assert.Equal(0, mergedSuccess.FailedSheets);
        Assert.Single(mergedSuccess.OutputFilePaths);
        Assert.Equal("all_sheets.pdf", mergedSuccess.MergedPdfPath);

        // Cancellation result test:
        var cancelled = PlotResult.Failed("Plotting was cancelled by user.", 10, 3);
        Assert.False(cancelled.Success);
        Assert.Equal(10, cancelled.TotalSheets);
        Assert.Equal(3, cancelled.PlottedSheets);
        Assert.Equal(7, cancelled.FailedSheets);
        Assert.Single(cancelled.Errors);
        Assert.Equal("Plotting was cancelled by user.", cancelled.Errors[0]);
    }
}
