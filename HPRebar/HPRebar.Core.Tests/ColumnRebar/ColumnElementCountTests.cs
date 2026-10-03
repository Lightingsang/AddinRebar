using HPRebar.Core.ColumnRebar;
using HPRebar.Core.ColumnRebar.Models;
using Xunit;

namespace HPRebar.Core.Tests.ColumnRebar;

public sealed class ColumnElementCountTests
{
    [Theory]
    [InlineData(false, 0, 0, 0.0, false, 0, 0, 0.0, 0)]   // no cross-ties
    [InlineData(true, 0, 1, 200.0, false, 0, 0, 0.0, 3)]  // closed inner tie: one per group
    [InlineData(true, 0, 1, 0.0, false, 0, 0, 0.0, 0)]    // closed inner tie without a leg: none
    [InlineData(true, 2, 2, 0.0, true, 3, 1, 0.0, 9)]     // 2 + 1 cross-ties per group
    public void CrossTies_Rectangle_CountsPerTieGroup(
        bool addH, int typeH, int nh, double ah, bool addV, int typeV, int nv, double av, int expected)
    {
        var ties = new AdditionalTieSpec { AddH = addH, TypeH = typeH, NH = nh, AH = ah, AddV = addV, TypeV = typeV, NV = nv, AV = av };

        Assert.Equal(expected, ColumnElementCount.CrossTies(SectionShape.Rectangle, ties, runCount: 3));
    }

    [Theory]
    [InlineData(true, false, 3)]
    [InlineData(false, true, 6)]
    [InlineData(true, true, 9)]
    public void CrossTies_Circle_HorizontalOnePerGroupVerticalAPair(bool addH, bool addV, int expected)
    {
        var ties = new AdditionalTieSpec { AddH = addH, AddV = addV, TypeH = 1, NH = 5, TypeV = 1, NV = 5 };

        Assert.Equal(expected, ColumnElementCount.CrossTies(SectionShape.Circular, ties, runCount: 3));
    }

    [Fact]
    public void Planned_ZonedTiesWithCrossTies_AddsGroupsCrossTiesAndBars()
    {
        var section = TestSections.Rectangle();
        var stirrups = new StirrupSpec { TypeDis = 1, S1 = 100, S2 = 200 };
        var ties = new AdditionalTieSpec { AddH = true, TypeH = 1, NH = 2 };
        int groups = StirrupDistributionCalculator.ComputeRuns(section, stirrups).Count;

        var planned = ColumnElementCount.Planned(section, stirrups, ties, barCount: 10);

        Assert.Equal(groups + (2 * groups) + 10, planned);
    }
}
