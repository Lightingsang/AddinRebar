using System;
using HPRebar.Core.BeamRebar.Calculators;
using HPRebar.Core.BeamRebar.Models;
using Xunit;

namespace HPRebar.Core.Tests.BeamRebar;

public sealed class BeamElementCountTests
{
    private static readonly BeamMainBarSpec MainBars = new() { TopCount = 3, BottomCount = 4 };
    private static readonly BeamAdditionalBarSpec NoAdditional = new();
    private static readonly BeamSideBarSpec NoSkin = new() { AutoSkinBars = false };
    private static readonly BeamSpecialBarSpec NoHanging = new() { EnableHangingStirrups = false };

    [Fact]
    public void Bars_ThreeSpansWithMainBarsOnly_CountsThreeStirrupGroupsPerSpanAndEveryMainBar()
    {
        var count = BeamElementCount.Bars(TestBeamData.ThreeSpan(), MainBars, NoAdditional, NoSkin, NoHanging);

        Assert.Equal((3 * 3) + 3 + 4, count);
    }

    [Fact]
    public void Bars_AdditionalBars_CountTwoEach()
    {
        var additional = new BeamAdditionalBarSpec
        {
            SupportTopBars = new[] { new SupportAdditionalTopBarConfig(), new SupportAdditionalTopBarConfig() },
            SpanBottomBars = new[] { new SpanAdditionalBottomBarConfig() }
        };

        var count = BeamElementCount.Bars(TestBeamData.SingleSpan(), MainBars, additional, NoSkin, NoHanging);

        Assert.Equal(3 + 7 + (2 * 2) + (1 * 2), count);
    }

    [Theory]
    [InlineData(700.0, 4)]    // as deep as the threshold: skin bars
    [InlineData(700.1, 0)]
    public void Bars_SkinBars_CountFourOnceTheRunIsDeepEnough(double threshold, int expectedSkin)
    {
        var skin = new BeamSideBarSpec { AutoSkinBars = true, DepthThreshold = threshold };
        var stack = TestBeamData.SingleSpan(height: 700);

        var count = BeamElementCount.Bars(stack, MainBars, NoAdditional, skin, NoHanging);

        Assert.Equal(3 + 7 + expectedSkin, count);
    }

    [Fact]
    public void Bars_SkinBarsSwitchedOffOnADeepRun_CountNothingForThem()
    {
        var stack = TestBeamData.SingleSpan(height: 900);

        var count = BeamElementCount.Bars(stack, MainBars, NoAdditional, NoSkin, NoHanging);

        Assert.Equal(3 + 7, count);
    }

    [Fact]
    public void Bars_OneDeepSpanAmongShallowOnes_CountsTheSkinBarsOnceForTheRun()
    {
        // Arrange: only the middle span reaches the threshold; the run's deepest span decides
        var shallow = TestBeamData.ThreeSpan();
        var deepMiddle = shallow.Spans[1] with { Height = 800 };
        var stack = shallow with { Spans = new[] { shallow.Spans[0], deepMiddle, shallow.Spans[2] } };
        var skin = new BeamSideBarSpec { AutoSkinBars = true, DepthThreshold = 700 };

        // Act
        var count = BeamElementCount.Bars(stack, MainBars, NoAdditional, skin, NoHanging);

        // Assert
        Assert.Equal((3 * 3) + 7 + 4, count);
    }

    [Fact]
    public void Bars_HangingStirrupsSwitchedOffWithSecondaryBeams_CountNothingForThem()
    {
        var secondary = new SecondaryBeamIntersection(0, 0, 2000, 200, 400);
        var stack = TestBeamData.SingleSpan() with { SecondaryIntersections = new[] { secondary } };

        var count = BeamElementCount.Bars(stack, MainBars, NoAdditional, NoSkin, NoHanging);

        Assert.Equal(3 + 7, count);
    }

    [Fact]
    public void Bars_HangingStirrups_CountBothSidesOfEverySecondaryBeam()
    {
        var stack = TestBeamData.SingleSpan() with
        {
            SecondaryIntersections = new[]
            {
                new SecondaryBeamIntersection(0, 0, 2000, 200, 400), new SecondaryBeamIntersection(1, 0, 4000, 200, 400)
            }
        };
        var hanging = new BeamSpecialBarSpec { EnableHangingStirrups = true, HangingStirrupsPerSide = 3 };

        var count = BeamElementCount.Bars(stack, MainBars, NoAdditional, NoSkin, hanging);

        Assert.Equal(3 + 7 + (2 * 3 * 2), count);
    }

    [Theory]
    [InlineData(true, 3, 8)]
    [InlineData(false, 3, 6)]
    [InlineData(true, 0, 2)]
    public void Dimensions_CountsTwoOnTheElevationAndTwoPerSection(bool onElevation, int sections, int expected)
    {
        Assert.Equal(expected, BeamElementCount.Dimensions(onElevation, sections));
    }

    [Fact]
    public void Bars_NullArguments_Throw()
    {
        var stack = TestBeamData.SingleSpan();

        Assert.Throws<ArgumentNullException>(
            () => BeamElementCount.Bars(null!, MainBars, NoAdditional, NoSkin, NoHanging));
        Assert.Throws<ArgumentNullException>(
            () => BeamElementCount.Bars(stack, null!, NoAdditional, NoSkin, NoHanging));
        Assert.Throws<ArgumentNullException>(
            () => BeamElementCount.Bars(stack, MainBars, null!, NoSkin, NoHanging));
        Assert.Throws<ArgumentNullException>(
            () => BeamElementCount.Bars(stack, MainBars, NoAdditional, null!, NoHanging));
        Assert.Throws<ArgumentNullException>(
            () => BeamElementCount.Bars(stack, MainBars, NoAdditional, NoSkin, null!));
    }
}
