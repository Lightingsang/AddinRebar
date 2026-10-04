using System;
using HPRebar.Core.BeamRebar.Calculators;
using HPRebar.Core.BeamRebar.Models;
using Xunit;

namespace HPRebar.Core.Tests.BeamRebar;

public sealed class BeamSpecRulesTests
{
    private static readonly BeamMainBarSpec MainBars =
        new() { TopCount = 2, BottomCount = 2, TopDiameter = 20, BottomDiameter = 20 };

    private static readonly BeamStirrupSpec Stirrups =
        new() { Diameter = 8, Cover = 25, SpacingDense = 100, SpacingSparse = 200 };

    private static readonly BeamSpan Span =
        new(index: 0, name: "D1", lengthCenter: 6000, width: 300, height: 600, clearLength: 5600);

    private static readonly BeamSideBarSpec SideBars = new()
    {
        AutoSkinBars = true, DepthThreshold = 700, MaxVerticalSpacing = 300, IncludeCrossTies = true, CrossTieSpacing = 400
    };

    private static readonly BeamSpan DeepSpan = Span with { Height = 900 };

    [Fact]
    public void FirstProblem_BuildableRun_ReturnsNull()
    {
        Assert.Null(Check());
    }

    [Theory]
    [InlineData(1, 2)]
    [InlineData(2, 1)]
    public void FirstProblem_FewerThanTwoBarsTopOrBottom_AsksForTwo(int top, int bottom)
    {
        var problem = Check(mainBars: MainBars with { TopCount = top, BottomCount = bottom });

        Assert.Equal("Top and bottom main longitudinal reinforcement must each have at least 2 bars.", problem);
    }

    [Theory]
    [InlineData(0.0, 200.0, 25.0)]
    [InlineData(100.0, -1.0, 25.0)]
    [InlineData(100.0, 200.0, 0.0)]
    [InlineData(100.0, 200.0, double.NaN)]
    [InlineData(100.0, 200.0, double.PositiveInfinity)]
    public void FirstProblem_SpacingOrCoverNotPositive_AsksForPositiveValues(double dense, double sparse, double cover)
    {
        var problem = Check(stirrups: Stirrups with { SpacingDense = dense, SpacingSparse = sparse, Cover = cover });

        Assert.Equal("Stirrup spacing and concrete cover must be positive values greater than zero.", problem);
    }

    [Fact]
    public void FirstProblem_BarTypeMissing_AsksToChooseThem()
    {
        var problem = Check(barTypesChosen: false);

        Assert.Equal("Please ensure main top, bottom, and stirrup rebar types are selected.", problem);
    }

    [Fact]
    public void FirstProblem_NodeStirrupsWithoutSpacing_AsksForNodeSpacing()
    {
        var problem = Check(stirrups: Stirrups with { IncludeStirrupsInNodes = true, NodeSpacing = 0 });

        Assert.Equal("Column node stirrup spacing must be greater than zero.", problem);
    }

    [Fact]
    public void FirstProblem_NodeSpacingZeroButNodeStirrupsOff_IsFine()
    {
        Assert.Null(Check(stirrups: Stirrups with { IncludeStirrupsInNodes = false, NodeSpacing = 0 }));
    }

    [Fact]
    public void FirstProblem_ViewNameWithAForbiddenCharacter_NamesTheCharacter()
    {
        var problem = Check(viewNames: "Beam B1|S-");

        Assert.Equal("View names cannot contain '|' (Revit refuses \\:{}[]|;<>?`~).", problem);
    }

    [Fact]
    public void FirstProblem_SpanNoWiderThanCoverStirrupsAndBar_ReportsTheWidth()
    {
        // 2 × 25 cover + 2 × 8 stirrup + 20 bar = 86 mm
        var problem = Check(span: Span with { Width = 86 });

        Assert.Equal("Span D1: Beam width (86 mm) is too narrow for cover (25 mm) and bar sizes.", problem);
    }

    [Fact]
    public void FirstProblem_SpanNoDeeperThanCoverStirrupsAndBar_ReportsTheHeight()
    {
        var problem = Check(span: Span with { Height = 86 });

        Assert.Equal("Span D1: Beam height (86 mm) is too shallow for cover (25 mm) and bar sizes.", problem);
    }

    [Fact]
    public void FirstProblem_SpanJustWiderAndDeeperThanTheMinimum_IsFine()
    {
        Assert.Null(Check(span: Span with { Width = 86.1, Height = 86.1 }));
    }

    [Fact]
    public void FirstProblem_HeightCheck_UsesTheLargerOfTopAndBottomBars()
    {
        var problem = Check(mainBars: MainBars with { BottomDiameter = 32 }, span: Span with { Height = 98 });

        Assert.StartsWith("Span D1: Beam height (98 mm)", problem);
    }

    [Fact]
    public void FirstProblem_NodeSpacingViewNamesAndSpanAllWrong_ReportsTheNodeSpacing()
    {
        var problem = Check(
            stirrups: Stirrups with { IncludeStirrupsInNodes = true, NodeSpacing = 0 },
            viewNames: "a|b",
            span: Span with { Width = 50 });

        Assert.Equal("Column node stirrup spacing must be greater than zero.", problem);
    }

    [Fact]
    public void FirstProblem_ViewNamesAndSpanBothWrong_ReportsTheViewNames()
    {
        var problem = Check(viewNames: "a|b", span: Span with { Width = 50 });

        Assert.StartsWith("View names cannot contain '|'", problem);
    }

    [Fact]
    public void FirstProblem_WidthCheck_UsesTheLargerOfTopAndBottomBars()
    {
        var problem = Check(mainBars: MainBars with { BottomDiameter = 32 }, span: Span with { Width = 98 });

        Assert.StartsWith("Span D1: Beam width (98 mm)", problem);
    }

    [Theory]
    [InlineData(4004.0, true)]    // 1001 intervals → 1002 stirrups: accepted
    [InlineData(4005.0, false)]   // 1002 intervals → 1003 stirrups: refused
    public void FirstProblem_DenseStirrupsAtRevitsLimit_AcceptsExactlyTheLimit(double clearLength, bool accepted)
    {
        var problem = Check(
            stirrups: Stirrups with { SpacingDense = 4 }, span: Span with { LengthClear = clearLength });

        Assert.Equal(accepted, problem is null);
        if (!accepted)
        {
            Assert.Equal("Span D1: Dense stirrup spacing produces 1003 ties, exceeding Revit's 1002 limit.", problem);
        }
    }

    [Fact]
    public void FirstProblem_SparseStirrupsOverRevitsLimit_ReportsTheSparseCount()
    {
        var problem = Check(
            stirrups: Stirrups with { SpacingDense = 100, SpacingSparse = 4 }, span: Span with { LengthClear = 4005 });

        Assert.Equal("Span D1: Sparse stirrup spacing produces 1003 ties, exceeding Revit's 1002 limit.", problem);
    }

    [Fact]
    public void FirstProblem_SeveralProblems_ReportsTheFirstInCheckOrder()
    {
        var problem = Check(mainBars: MainBars with { TopCount = 1 }, barTypesChosen: false, viewNames: "a|b");

        Assert.Equal("Top and bottom main longitudinal reinforcement must each have at least 2 bars.", problem);
    }

    [Fact]
    public void FirstProblem_SecondSpanTooNarrow_NamesThatSpan()
    {
        var spans = new[] { Span, Span with { Name = "D2", Width = 80 } };

        var problem = BeamSpecRules.FirstProblem(MainBars, Stirrups, SideBars, true, string.Empty, spans);

        Assert.StartsWith("Span D2:", problem);
    }

    [Theory]
    [InlineData(double.NaN, 200.0)]
    [InlineData(double.PositiveInfinity, 200.0)]
    [InlineData(100.0, double.NaN)]
    [InlineData(100.0, double.PositiveInfinity)]
    public void FirstProblem_SpacingNotAFiniteNumber_AsksForPositiveValues(double dense, double sparse)
    {
        var problem = Check(stirrups: Stirrups with { SpacingDense = dense, SpacingSparse = sparse });

        Assert.Equal("Stirrup spacing and concrete cover must be positive values greater than zero.", problem);
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void FirstProblem_StartOffsetNotAFiniteNumber_AsksForANumber(double startOffset)
    {
        var problem = Check(stirrups: Stirrups with { StartOffset = startOffset });

        Assert.Equal("Stirrup start offset must be a number.", problem);
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void FirstProblem_NodeSpacingNotAFiniteNumber_AsksForNodeSpacing(double nodeSpacing)
    {
        var problem = Check(stirrups: Stirrups with { IncludeStirrupsInNodes = true, NodeSpacing = nodeSpacing });

        Assert.Equal("Column node stirrup spacing must be greater than zero.", problem);
    }

    [Fact]
    public void FirstProblem_NullArguments_Throw()
    {
        var spans = new[] { Span };

        Assert.Throws<ArgumentNullException>(
            () => BeamSpecRules.FirstProblem(null!, Stirrups, SideBars, true, "", spans));
        Assert.Throws<ArgumentNullException>(
            () => BeamSpecRules.FirstProblem(MainBars, null!, SideBars, true, "", spans));
        Assert.Throws<ArgumentNullException>(
            () => BeamSpecRules.FirstProblem(MainBars, Stirrups, null!, true, "", spans));
        Assert.Throws<ArgumentNullException>(
            () => BeamSpecRules.FirstProblem(MainBars, Stirrups, SideBars, true, null!, spans));
        Assert.Throws<ArgumentNullException>(
            () => BeamSpecRules.FirstProblem(MainBars, Stirrups, SideBars, true, "", null!));
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void FirstProblem_DeepSpanWithoutAVerticalSideBarSpacing_AsksForOne(double spacing)
    {
        var problem = Check(span: DeepSpan, sideBars: SideBars with { MaxVerticalSpacing = spacing });

        Assert.Equal("Side bar vertical spacing must be greater than zero.", problem);
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(-400.0)]
    [InlineData(double.NaN)]
    public void FirstProblem_DeepSpanWithCrossTiesButNoSpacing_AsksForOne(double spacing)
    {
        var problem = Check(span: DeepSpan, sideBars: SideBars with { CrossTieSpacing = spacing });

        Assert.Equal("Cross-tie spacing must be greater than zero.", problem);
    }

    [Fact]
    public void FirstProblem_CrossTiesSwitchedOff_DoNotNeedASpacing()
    {
        Assert.Null(Check(span: DeepSpan, sideBars: SideBars with { IncludeCrossTies = false, CrossTieSpacing = 0 }));
    }

    [Fact]
    public void FirstProblem_NoSpanDeepEnough_DoesNotCheckTheSideBarSettings()
    {
        Assert.Null(Check(sideBars: SideBars with { MaxVerticalSpacing = 0, CrossTieSpacing = 0 }));
    }

    [Fact]
    public void FirstProblem_SideBarsSwitchedOff_DoNotCheckTheirSettings()
    {
        Assert.Null(Check(span: DeepSpan, sideBars: SideBars with { AutoSkinBars = false, MaxVerticalSpacing = 0 }));
    }

    [Theory]
    [InlineData(0.01)]
    [InlineData(1e-9)]   // the row count passes int.MaxValue
    public void FirstProblem_SideBarSpacingNeedingTooManyRows_ReportsTheRowCount(double spacing)
    {
        var problem = Check(span: DeepSpan, sideBars: SideBars with { MaxVerticalSpacing = spacing });

        Assert.StartsWith("Span D1: Side bar spacing produces ", problem);
        Assert.EndsWith(" rows, exceeding the 1002-bar limit.", problem);
    }

    [Theory]
    [InlineData(0.01)]
    [InlineData(1e-9)]   // the tie count passes int.MaxValue
    public void FirstProblem_CrossTieSpacingNeedingTooManyTies_ReportsTheTieCount(double spacing)
    {
        var problem = Check(span: DeepSpan, sideBars: SideBars with { CrossTieSpacing = spacing });

        Assert.StartsWith("Span D1: Cross-tie spacing produces ", problem);
        Assert.EndsWith(" ties per row, exceeding the 1002-bar limit.", problem);
    }

    [Fact]
    public void FirstProblem_DeepSpanWithSensibleSideBarSettings_IsFine()
    {
        Assert.Null(Check(span: DeepSpan));
    }

    [Theory]
    [InlineData(1002.5, true)]    // 1002 rows: accepted
    [InlineData(1003.5, false)]   // 1003 rows: refused
    public void FirstProblem_SideBarRowsAtTheLimit_RefusesExactlyWhatTheCalculatorRefuses(
        double spacesPerClearDepth, bool accepted)
    {
        // Clear depth between the main bars: 900 − 2 × (25 cover + 8 stirrup + 10 half bar) = 814 mm
        double spacing = 814.0 / spacesPerClearDepth;

        var problem = Check(span: DeepSpan, sideBars: SideBars with { MaxVerticalSpacing = spacing });
        var calculatorAccepts = Record.Exception(
            () => BeamSideBarCalculator.ComputeRowCount(900, 25, 8, 20, spacing)) is null;

        Assert.Equal(accepted, problem is null);
        Assert.Equal(accepted, calculatorAccepts);
    }

    [Fact]
    public void FirstProblem_SpanShallowerThan700ButOverTheUserThreshold_DoesNotCheckTheSideBarSettings()
    {
        var sideBars = SideBars with { DepthThreshold = 500, MaxVerticalSpacing = 0 };

        Assert.Null(Check(span: Span with { Height = 600 }, sideBars: sideBars));
    }

    [Fact]
    public void FirstProblem_NaNDepthThreshold_StillChecksTheDeepSpansLikeTheCalculator()
    {
        var sideBars = SideBars with { DepthThreshold = double.NaN, MaxVerticalSpacing = 0 };

        Assert.Equal("Side bar vertical spacing must be greater than zero.", Check(span: DeepSpan, sideBars: sideBars));
    }

    private static string? Check(
        BeamMainBarSpec? mainBars = null,
        BeamStirrupSpec? stirrups = null,
        bool barTypesChosen = true,
        string viewNames = "Beam B1S-",
        BeamSpan? span = null,
        BeamSideBarSpec? sideBars = null) =>
        BeamSpecRules.FirstProblem(
            mainBars ?? MainBars,
            stirrups ?? Stirrups,
            sideBars ?? SideBars,
            barTypesChosen,
            viewNames,
            new[] { span ?? Span });

    /// <summary>A spacing so small the stirrup count no longer fits an int is still refused.</summary>
    [Fact]
    public void FirstProblem_TinyDenseSpacing_IsRefusedAtRevitsLimit()
    {
        var problem = Check(stirrups: Stirrups with { SpacingDense = 1e-6 });

        Assert.StartsWith("Span D1: Dense stirrup spacing produces 56", problem);
        Assert.EndsWith("ties, exceeding Revit's 1002 limit.", problem);
    }
}
