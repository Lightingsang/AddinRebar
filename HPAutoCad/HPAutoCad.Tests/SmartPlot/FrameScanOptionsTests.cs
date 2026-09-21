using HPAutoCad.Core.SmartPlot.Models;
using HPAutoCad.SmartPlot.Cad.Providers;
using Xunit;

namespace HPAutoCad.Tests.SmartPlot;

public sealed class FrameScanOptionsTests
{
    [Fact]
    public void FromConfiguration_SplitsSemicolonAndCommaDelimitedBlockNames()
    {
        var config = new PlotConfiguration
        {
            BlockName = "A1_Frame; KhungTen_A3 , TitleBlock_V1",
            LayerName = "DEFPOINTS",
            LayoutRange = "1-3,5",
            SheetNumberAttribute = "SO_BV",
            SheetTitleAttribute = "TEN_BV",
            ToleranceBandYRatio = 0.6
        };

        var options = FrameScanOptions.FromConfiguration(config);

        Assert.Equal(3, options.SelectedBlockNames.Count);
        Assert.Contains("A1_Frame", options.SelectedBlockNames);
        Assert.Contains("KhungTen_A3", options.SelectedBlockNames);
        Assert.Contains("TitleBlock_V1", options.SelectedBlockNames);
        Assert.Equal("DEFPOINTS", options.LayerName);
        Assert.Equal("1-3,5", options.LayoutRange);
        Assert.Equal("SO_BV", options.SheetNumberAttributeTag);
        Assert.Equal("TEN_BV", options.SheetTitleAttributeTag);
        Assert.Equal(0.6, options.ToleranceBandYRatio);
    }

    [Fact]
    public void FromConfiguration_EmptyBlockName_ProducesEmptySelectedBlockNames()
    {
        var config = new PlotConfiguration
        {
            BlockName = ""
        };

        var options = FrameScanOptions.FromConfiguration(config);

        Assert.Empty(options.SelectedBlockNames);
        Assert.Equal("All", options.LayoutRange);
    }

    [Fact]
    public void DefaultOptions_HaveSensibleValues()
    {
        var options = new FrameScanOptions();

        Assert.Empty(options.BlockName);
        Assert.Empty(options.SelectedBlockNames);
        Assert.Empty(options.LayerName);
        Assert.Equal("All", options.LayoutRange);
        Assert.Equal("SOHIEU", options.SheetNumberAttributeTag);
        Assert.Equal("TENTIEUDE", options.SheetTitleAttributeTag);
        Assert.Equal(0.5, options.ToleranceBandYRatio);
    }
}
