using System;
using HPRebar.Core.ColumnRebar;
using HPRebar.Core.ColumnRebar.Models;
using Xunit;

namespace HPRebar.Core.Tests.ColumnRebar;

public sealed class BarSideClassifierTests
{
    [Theory]
    [InlineData(1, BarSide.South)]
    [InlineData(3, BarSide.South)]
    [InlineData(4, BarSide.East)]
    [InlineData(5, BarSide.East)]
    [InlineData(6, BarSide.North)]
    [InlineData(8, BarSide.North)]
    [InlineData(9, BarSide.West)]
    [InlineData(10, BarSide.West)]
    public void EachBarNumberMapsToItsFaceOnA3By4Grid(int barNumber, BarSide expected) =>
        Assert.Equal(expected, BarSideClassifier.SideOf(barNumber, 3, 4));

    [Fact]
    public void OnTheSmallestGridEveryBarIsACornerOwnedByAHorizontalFace()
    {
        Assert.Equal(BarSide.South, BarSideClassifier.SideOf(1, 2, 2));
        Assert.Equal(BarSide.South, BarSideClassifier.SideOf(2, 2, 2));
        Assert.Equal(BarSide.North, BarSideClassifier.SideOf(3, 2, 2));
        Assert.Equal(BarSide.North, BarSideClassifier.SideOf(4, 2, 2));
    }

    [Fact]
    public void ABarNumberOutsideTheGridIsRejected()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => BarSideClassifier.SideOf(11, 3, 4));
        Assert.Throws<ArgumentOutOfRangeException>(() => BarSideClassifier.SideOf(0, 3, 4));
    }

    [Theory]
    [InlineData(1, 0)]
    [InlineData(2, 0)]
    [InlineData(3, 1)]
    [InlineData(5, 2)]
    [InlineData(8, 3)]
    public void CircularBarsFallIntoQuadrantsCounterClockwise(int barNumber, int expected) =>
        Assert.Equal(expected, BarSideClassifier.QuadrantOf(barNumber, 8));

    [Fact]
    public void AQuadrantNeedsABarCountDivisibleByFour() =>
        Assert.Throws<ArgumentOutOfRangeException>(() => BarSideClassifier.QuadrantOf(1, 6));
}
