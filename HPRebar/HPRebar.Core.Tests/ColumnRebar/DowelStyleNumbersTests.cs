using HPRebar.Core.ColumnRebar;
using HPRebar.Core.ColumnRebar.Models;
using Xunit;

namespace HPRebar.Core.Tests.ColumnRebar;

public sealed class DowelStyleNumbersTests
{
    [Theory]
    [InlineData(0, TopDowelStyle.BendIntoColumnAbove)]
    [InlineData(1, TopDowelStyle.StopUnderBeam)]
    [InlineData(5, TopDowelStyle.StopUnderBeam)]
    [InlineData(-2, TopDowelStyle.StopUnderBeam)]
    public void ToTop_ZeroBendsEveryOtherNumberStops(int number, TopDowelStyle expected)
    {
        Assert.Equal(expected, DowelStyleNumbers.ToTop(number));
    }

    [Theory]
    [InlineData(0, BottomDowelStyle.StartAboveBase)]
    [InlineData(1, BottomDowelStyle.RunPastBase)]
    [InlineData(3, BottomDowelStyle.RunPastBase)]
    public void ToBottom_ZeroStartsAboveTheBaseEveryOtherNumberRunsPast(int number, BottomDowelStyle expected)
    {
        Assert.Equal(expected, DowelStyleNumbers.ToBottom(number));
    }
}
