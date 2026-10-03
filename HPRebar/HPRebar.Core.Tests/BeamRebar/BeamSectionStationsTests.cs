using System;
using HPRebar.Core.BeamRebar.Calculators;
using HPRebar.Core.BeamRebar.Models;
using Xunit;

namespace HPRebar.Core.Tests.BeamRebar;

public sealed class BeamSectionStationsTests
{
    private static readonly BeamSpan Supported = new(index: 0, lengthCenter: 6400, clearLength: 6000, startX: 200);

    private static readonly BeamSpan Cantilever = Supported with { Cantilever = CantileverPosition.Left };

    [Fact]
    public void ForSpan_ThreeSections_CutsAtASixthMidspanAndFiveSixthsOfTheClearSpan()
    {
        Assert.Equal(new[] { 1200.0, 3200.0, 5200.0 }, BeamSectionStations.ForSpan(Supported, 3));
    }

    [Fact]
    public void ForSpan_TwoSections_CutsAtTheLeftSupportZoneAndMidspan()
    {
        Assert.Equal(new[] { 1200.0, 3200.0 }, BeamSectionStations.ForSpan(Supported, 2));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(0)]
    [InlineData(-2)]
    public void ForSpan_OneOrFewerSections_CutsAtMidspan(int sectionsPerSpan)
    {
        Assert.Equal(new[] { 3200.0 }, BeamSectionStations.ForSpan(Supported, sectionsPerSpan));
    }

    [Fact]
    public void ForSpan_MoreThanThreeSections_CutsTheSameThreeStations()
    {
        Assert.Equal(BeamSectionStations.ForSpan(Supported, 3), BeamSectionStations.ForSpan(Supported, 5));
    }

    [Fact]
    public void ForSpan_ClearSpanNotDivisibleBySix_KeepsTheExactFractions()
    {
        var span = Supported with { LengthClear = 5000 };

        var stations = BeamSectionStations.ForSpan(span, 3);

        Assert.Equal(new[] { 200 + 5000 / 6.0, 200 + 5000 * 0.5, 200 + 5000 * 5.0 / 6.0 }, stations);
    }

    [Fact]
    public void ForSpan_Cantilever_CutsOnceAtMidspan()
    {
        Assert.Equal(new[] { 3200.0 }, BeamSectionStations.ForSpan(Cantilever, 3));
    }

    [Fact]
    public void Count_SupportedSpanAndCantilever_AddsTheirCuts()
    {
        Assert.Equal(4, BeamSectionStations.Count(new[] { Supported, Cantilever }, 3));
    }

    [Fact]
    public void NullArguments_Throw()
    {
        Assert.Throws<ArgumentNullException>(() => BeamSectionStations.ForSpan(null!, 3));
        Assert.Throws<ArgumentNullException>(() => BeamSectionStations.Count(null!, 3));
    }
}
