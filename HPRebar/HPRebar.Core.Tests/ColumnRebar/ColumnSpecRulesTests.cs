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
    public void FirstProblem_CoverAndBarsFillTheSection_ReportsNarrowestSide()
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

    [Fact]
    public void FirstProblem_InvalidLayoutInATooNarrowSection_ReportsTheLayoutFirst()
    {
        var problem = ColumnSpecRules.FirstProblem(TestSections.Rectangle(b: 50), TestSections.Grid(nx: 1), Uniform150, NoTies);

        Assert.Equal("at least two bars are needed along each side.", problem);
    }

    [Fact]
    public void FirstProblem_BadSpacingAndIncompleteCrossTie_ReportsTheSpacingFirst()
    {
        var stirrups = new StirrupSpec { TypeDis = 0, S = 0 };
        var ties = new AdditionalTieSpec { AddH = true, TypeH = 0, AH = 0 };

        var problem = ColumnSpecRules.FirstProblem(TestSections.Rectangle(), TestSections.Grid(), stirrups, ties);

        Assert.Equal("tie spacing must be greater than zero.", problem);
    }

    [Theory]
    [InlineData(1001, true)]   // 1001 intervals → 1002 ties: accepted
    [InlineData(1002, false)]  // 1002 intervals → 1003 ties: refused
    public void FirstProblem_TieCountAtRevitLimit_AcceptsExactlyTheLimit(int intervals, bool accepted)
    {
        var section = TestSections.Rectangle();
        double run = StirrupDistributionCalculator.ComputeRunLength(section, tiesUp: false);
        var stirrups = new StirrupSpec { TypeDis = 0, S = run / (intervals + 0.5) };

        var problem = ColumnSpecRules.FirstProblem(section, TestSections.Grid(), stirrups, NoTies);

        Assert.Equal(accepted, problem is null);
    }

    /// <summary>The layout rule and the calculator must agree: the previews check the rule instead of catching the calculator's exception.</summary>
    [Fact]
    public void IsLayoutValid_EveryBarCountFromMinusOneToNine_AgreesWithTheCalculator()
    {
        for (int a = -1; a <= 9; a++)
        {
            for (int b = -1; b <= 9; b++)
            {
                AssertAgrees(TestSections.Rectangle(), TestSections.Grid(a, b));
            }

            AssertAgrees(TestSections.Circular(), TestSections.Ring(a));
        }
    }

    private static void AssertAgrees(ColumnSection section, BarLayoutSpec layout)
    {
        bool computes;
        try
        {
            BarLayoutCalculator.Compute(section, layout);
            computes = true;
        }
        catch (System.ArgumentOutOfRangeException)
        {
            computes = false;
        }

        Assert.Equal(computes, ColumnSpecRules.IsLayoutValid(section.Shape, layout));
    }

    [Fact]
    public void FirstProblem_TinySpacing_IsRefusedAtRevitsLimit()
    {
        var stirrups = new StirrupSpec { TypeDis = 0, S = 1e-6 };

        var problem = ColumnSpecRules.FirstProblem(TestSections.Rectangle(), TestSections.Grid(), stirrups, NoTies);

        Assert.EndsWith("needs more than 1002 ties, which Revit will not accept.", problem);
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void FirstProblem_TieSpacingNotAFiniteNumber_AsksForPositiveSpacing(double spacing)
    {
        var stirrups = new StirrupSpec { TypeDis = 0, S = spacing };

        var problem = ColumnSpecRules.FirstProblem(TestSections.Rectangle(), TestSections.Grid(), stirrups, NoTies);

        Assert.Equal("tie spacing must be greater than zero.", problem);
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void FirstProblem_CoverNotAFiniteNumber_AsksForANumber(double cover)
    {
        var layout = TestSections.Grid() with { Cover = cover };

        var problem = ColumnSpecRules.FirstProblem(TestSections.Rectangle(), layout, Uniform150, NoTies);

        Assert.Equal("the cover must be a number.", problem);
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void FirstProblem_ClosedCrossTieLegNotAFiniteNumber_AsksForALeg(double leg)
    {
        var ties = new AdditionalTieSpec { AddH = true, TypeH = 0, AH = leg };

        var problem = ColumnSpecRules.FirstProblem(TestSections.Rectangle(), TestSections.Grid(), Uniform150, ties);

        Assert.Equal("give the horizontal cross-tie a leg length.", problem);
    }
}
