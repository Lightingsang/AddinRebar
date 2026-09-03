using System.Collections.Generic;
using System.Linq;
using HPRebar.Core.ColumnRebar;
using HPRebar.Core.ColumnRebar.Models;
using Xunit;

namespace HPRebar.Core.Tests.ColumnRebar;

public sealed class BarScheduleCalculatorTests
{
    private const int Precision = 6;

    private static IReadOnlyList<BarPolyline> BuildStack(double splitOverlap = 50)
    {
        var section = TestSections.Rectangle();
        var spec = TestSections.Grid(2, 2) with { SplitOverlap = splitOverlap };
        var bars = BarLayoutCalculator.Compute(section, spec);

        return bars
            .Select(bar => BarPolylineBuilder.Build(
                section,
                spec,
                bar,
                SpliceSpec.Default(bar.BarNumber, spec.BarDiameter, splitOverlap, spec.OverlapFactor),
                new PlanPoint(bar.X0, bar.Y0),
                "T20"))
            .ToList();
    }

    [Fact]
    public void IdenticalBarsCollapseIntoOneRow()
    {
        var bars = BuildStack(splitOverlap: 100);

        var rows = BarScheduleCalculator.Group(bars);

        var row = Assert.Single(rows);
        Assert.Equal(4, row.Count);
        Assert.Equal(20d, row.Diameter, Precision);
    }

    [Fact]
    public void StaggeredLapsSplitTheRowInTwo()
    {
        var bars = BuildStack();

        var rows = BarScheduleCalculator.Group(bars);

        Assert.Equal(2, rows.Count);
        Assert.All(rows, row => Assert.Equal(2, row.Count));
    }

    [Fact]
    public void IdenticalColumnsMultiplyEveryRow()
    {
        var bars = BuildStack(splitOverlap: 100);

        var rows = BarScheduleCalculator.Group(bars, identicalColumns: 6);

        Assert.Equal(24, Assert.Single(rows).Count);
    }

    [Fact]
    public void DifferentBarTypesNeverShareARow()
    {
        var bars = BuildStack(splitOverlap: 100).ToList();
        bars[0] = bars[0] with { BarTypeName = "T25" };

        var rows = BarScheduleCalculator.Group(bars);

        Assert.Equal(2, rows.Count);
    }

    [Fact]
    public void DifferentDiametersNeverShareARow()
    {
        var bars = BuildStack(splitOverlap: 100).ToList();
        bars[0] = bars[0] with { Diameter = 25 };

        Assert.Equal(2, BarScheduleCalculator.Group(bars).Count);
    }

    [Fact]
    public void ADifferentBottomArrangementNeverSharesARow()
    {
        var bars = BuildStack(splitOverlap: 100).ToList();
        bars[0] = bars[0] with { Splice = bars[0].Splice with { IsBottomDowels = true, BottomDowelsType = 1 } };

        Assert.Equal(2, BarScheduleCalculator.Group(bars).Count);
    }

    [Fact]
    public void ALongerTopAnchorageSplitsTheRowThroughTheCutLength()
    {
        var section = TestSections.Rectangle();
        var spec = TestSections.Grid(2, 2);
        var bar = BarLayoutCalculator.Compute(section, spec)[0];
        var splice = SpliceSpec.Default(bar.BarNumber, spec.BarDiameter, 100, spec.OverlapFactor);

        var first = BarPolylineBuilder.Build(section, spec, bar, splice, new PlanPoint(bar.X0, bar.Y0), "T20");
        var second = BarPolylineBuilder.Build(section, spec, bar, splice with { LbTop = splice.LbTop + 100 },
            new PlanPoint(bar.X0, bar.Y0), "T20");

        // AreSameBar never reads LbTop directly; the extra 100 mm of bar is what separates the two rows.
        Assert.True(BarScheduleCalculator.AreSameBar(first, first));
        Assert.False(BarScheduleCalculator.AreSameBar(first, second));
    }

    [Fact]
    public void RowsComeBackInTheOrderTheBarsWereFirstMet()
    {
        var bars = BuildStack();

        var rows = BarScheduleCalculator.Group(bars);

        // Bar 1 is odd and gets the double lap, so the longer row is emitted first.
        Assert.True(rows[0].Length > rows[1].Length);
    }

    [Fact]
    public void AnEmptySetProducesNoRows() =>
        Assert.Empty(BarScheduleCalculator.Group(new List<BarPolyline>()));

    [Fact]
    public void AtLeastOneColumnIsRequired() =>
        Assert.Throws<System.ArgumentOutOfRangeException>(
            () => BarScheduleCalculator.Group(BuildStack(), identicalColumns: 0));
}
