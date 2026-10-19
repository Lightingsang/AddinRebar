namespace HPBim.Client.Tests.TypeResolver;

using HPBim.Client.TypeResolver;
using Xunit;

public sealed class TypeResolutionRulesTests
{
    [Fact]
    public void TypeResolutionRules_WallAndFloorNames_FormatCorrectly()
    {
        Assert.Equal("HP_Tuong_110mm", TypeResolutionRules.GetWallTypeName(110.0));
        Assert.Equal("HP_Tuong_220mm", TypeResolutionRules.GetWallTypeName(220.4));
        Assert.Equal("HP_San_100mm", TypeResolutionRules.GetFloorTypeName(100.0));
    }

    [Fact]
    public void TypeResolutionRules_ColumnAndBeamNames_FormatCorrectly()
    {
        Assert.Equal("HP_Cot_C220x220", TypeResolutionRules.GetColumnSymbolName(220, 220));
        Assert.Equal("HP_Dam_D200x400", TypeResolutionRules.GetBeamSymbolName(200, 400));
    }

    [Fact]
    public void TypeResolutionRules_DoorAndWindowNames_FormatCorrectly()
    {
        Assert.Equal("HP_CuaDi_900x2200", TypeResolutionRules.GetDoorSymbolName(900, 2200));
        Assert.Equal("HP_CuaSo_1200x1400", TypeResolutionRules.GetWindowSymbolName(1200, 1400));
    }

    [Fact]
    public void TypeResolutionRules_Constants_HaveExpectedValues()
    {
        Assert.Equal(110.0, TypeResolutionRules.DefaultWallThickness110Mm);
        Assert.Equal(220.0, TypeResolutionRules.DefaultWallThickness220Mm);
        Assert.Equal(220.0, TypeResolutionRules.DefaultColumnSizeMm);
        Assert.Equal(200.0, TypeResolutionRules.DefaultBeamWidthMm);
        Assert.Equal(400.0, TypeResolutionRules.DefaultBeamHeightMm);
        Assert.Equal(100.0, TypeResolutionRules.DefaultSlabThicknessMm);
    }
}
