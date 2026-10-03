using HPRebar.Core.ColumnRebar;
using HPRebar.Core.ColumnRebar.Models;
using Xunit;

namespace HPRebar.Core.Tests.ColumnRebar;

public sealed class ColumnSpecRulesTests
{
    private static readonly StirrupSpec Uniform150 = new() { TypeDis = 0, S = 150 };
    private static readonly AdditionalTieSpec NoTies = new();

    [Theory]
    [InlineData(2, 2, true)]
    [InlineData(1, 3, false)]
    [InlineData(3, 1, false)]
    public void IsLayoutValid_RectangleBarCounts_NeedsTwoPerSide(int nx, int ny, bool expected)
    {
        Assert.Equal(expected, ColumnSpecRules.IsLayoutValid(SectionShape.Rectangle, TestSections.Grid(nx, ny)));
    }

    [Theory]
    [InlineData(8, true)]
    [InlineData(6, false)]
    [InlineData(0, false)]
    public void IsLayoutValid_CircleBarCount_NeedsPositiveMultipleOfFour(int nd, bool expected)
    {
        Assert.Equal(expected, ColumnSpecRules.IsLayoutValid(SectionShape.Circular, TestSections.Ring(nd)));
    }

    [Fact]
    public void FirstProblem_BuildableColumn_ReturnsNull()
    {
        Assert.Null(ColumnSpecRules.FirstProblem(TestSections.Rectangle(), TestSections.Grid(), Uniform150, NoTies));
    }

    [Fact]
    public void FirstProblem_RectangleWithOneBarOnASide_AsksForTwoPerSide()
    {
        var problem = ColumnSpecRules.FirstProblem(TestSections.Rectangle(), TestSections.Grid(nx: 1), Uniform150, NoTies);

        Assert.Equal("at least two bars are needed along each side.", problem);
    }

    [Fact]
    public void FirstProblem_CircleWithSixBars_AsksForMultipleOfFour()
    {
        var problem = ColumnSpecRules.FirstProblem(TestSections.Circular(), TestSections.Ring(6), Uniform150, NoTies);

        Assert.Equal("the bar count around a circular column must be a positive multiple of four.", problem);
    }

    [Fact]
    public void FirstProblem_CoverAndBarsWiderThanSection_ReportsNarrowestSide()
    {
        // 2 × 25 cover + 2 × 8 stirrup + 20 bar = 86 mm, not less than an 86 mm wide section
        var problem = ColumnSpecRules.FirstProblem(TestSections.Rectangle(b: 86), TestSections.Grid(), Uniform150, NoTies);

        Assert.Equal("cover and bar sizes leave no room inside a 86 mm section.", problem);
    }

    [Fact]
    public void FirstProblem_BeamAsDeepAsColumn_ReportsNoTieRun()
    {
        var section = TestSections.Rectangle() with { Hb = 3000 };

        var problem = ColumnSpecRules.FirstProblem(section, TestSections.Grid(), Uniform150, NoTies);

        Assert.Equal("the beam is as deep as the column, leaving nowhere to put ties.", problem);
    }

    [Theory]
    [InlineData(0, 0.0, 150.0, 150.0)]
    [InlineData(1, 150.0, 100.0, 0.0)]
    public void FirstProblem_ZeroSpacing_AsksForPositiveSpacing(int typeDis, double s, double s1, double s2)
    {
        var stirrups = new StirrupSpec { TypeDis = typeDis, S = s, S1 = s1, S2 = s2 };

        var problem = ColumnSpecRules.FirstProblem(TestSections.Rectangle(), TestSections.Grid(), stirrups, NoTies);

        Assert.Equal("tie spacing must be greater than zero.", problem);
    }

    [Fact]
    public void FirstProblem_SpacingNeedingTooManyTies_ReportsRevitLimit()
    {
        var stirrups = new StirrupSpec { TypeDis = 0, S = 1 };

        var problem = ColumnSpecRules.FirstProblem(TestSections.Rectangle(), TestSections.Grid(), stirrups, NoTies);

        Assert.Equal("a spacing of 1 mm needs more than 1002 ties, which Revit will not accept.", problem);
    }

    [Theory]
    [InlineData(true, false, "give the horizontal cross-tie a leg length.")]
    [InlineData(false, true, "give the vertical cross-tie a leg length.")]
    public void FirstProblem_ClosedCrossTieWithoutLeg_AsksForLeg(bool horizontal, bool vertical, string expected)
    {
        var ties = new AdditionalTieSpec { AddH = horizontal, TypeH = 0, AH = 0, AddV = vertical, TypeV = 0, AV = 0 };

        var problem = ColumnSpecRules.FirstProblem(TestSections.Rectangle(), TestSections.Grid(), Uniform150, ties);

        Assert.Equal(expected, problem);
    }

    [Theory]
    [InlineData(true, false, "at least one horizontal cross-tie is required.")]
    [InlineData(false, true, "at least one vertical cross-tie is required.")]
    public void FirstProblem_CrossTiesWithZeroCount_AsksForOne(bool horizontal, bool vertical, string expected)
    {
        var ties = new AdditionalTieSpec { AddH = horizontal, TypeH = 1, NH = 0, AddV = vertical, TypeV = 1, NV = 0 };

        var problem = ColumnSpecRules.FirstProblem(TestSections.Rectangle(), TestSections.Grid(), Uniform150, ties);

        Assert.Equal(expected, problem);
    }
}
