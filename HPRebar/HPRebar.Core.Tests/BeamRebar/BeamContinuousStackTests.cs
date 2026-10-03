using System.Collections.Generic;
using System.Linq;
using HPRebar.Core.BeamRebar.Calculators;
using HPRebar.Core.BeamRebar.Models;
using Xunit;

namespace HPRebar.Core.Tests.BeamRebar;

public sealed class BeamContinuousStackTests
{
    [Fact]
    public void Validate_StandardSingleSpan_Succeeds()
    {
        Assert.True(TestBeamData.SingleSpan().Validate().IsSuccess);
    }

    [Fact]
    public void Validate_NoSpans_Fails()
    {
        var stack = new BeamContinuousStack(new List<BeamSpan>(), new List<BeamSupportNode>());

        Assert.False(stack.Validate().IsSuccess);
    }

    [Fact]
    public void Validate_SupportCountNotSpansPlusOne_Fails()
    {
        var single = TestBeamData.SingleSpan();
        var stack = new BeamContinuousStack(single.Spans, new List<BeamSupportNode> { single.Supports[0] });

        var result = stack.Validate();

        Assert.False(result.IsSuccess);
        Assert.Contains("Support count", result.ErrorMessage);
    }

    [Theory]
    [InlineData(0, 600, 25)]
    [InlineData(300, 0, 25)]
    [InlineData(300, 600, 0)]
    public void Validate_NonPositiveSpanDimension_Fails(double width, double height, double cover)
    {
        var single = TestBeamData.SingleSpan();
        var span = single.Spans[0] with { Width = width, Height = height, Cover = cover };
        var stack = new BeamContinuousStack(new List<BeamSpan> { span }, single.Supports);

        Assert.False(stack.Validate().IsSuccess);
    }

    [Theory]
    [InlineData(200)]
    [InlineData(3000)]
    [InlineData(5800)]
    public void FindSpanAt_StationInsideClearSpan_ReturnsThatSpan(double station)
    {
        var stack = TestBeamData.SingleSpan();

        Assert.Same(stack.Spans[0], stack.FindSpanAt(station));
    }

    [Theory]
    [InlineData(100)]
    [InlineData(6000)]
    public void FindSpanAt_StationOverSupportOrOutside_ReturnsNull(double station)
    {
        Assert.Null(TestBeamData.SingleSpan().FindSpanAt(station));
    }

    [Fact]
    public void WithCover_PositiveCover_AppliesItToEverySpan()
    {
        var stack = TestBeamData.TwoSpan();

        var covered = stack.WithCover(40);

        Assert.All(covered.Spans, span => Assert.Equal(40.0, span.Cover));
        Assert.Equal(stack.Spans[1].Width, covered.Spans[1].Width);
    }

    [Fact]
    public void WithCover_LeavesTheOriginalStackUnchanged()
    {
        var stack = TestBeamData.SingleSpan();

        stack.WithCover(40);

        Assert.Equal(TestBeamData.DefaultCover, stack.Spans[0].Cover);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    [InlineData(double.NaN)]
    public void WithCover_NonPositiveCover_Throws(double cover)
    {
        Assert.Throws<System.ArgumentOutOfRangeException>(() => TestBeamData.SingleSpan().WithCover(cover));
    }

    [Fact]
    public void WithCover_LargerCover_MovesSupportTopBarsDown()
    {
        var config = new SupportAdditionalTopBarConfig { SupportIndex = 1, Layer1Count = 2, Layer1Diameter = 20.0 };
        var spec = new BeamAdditionalTopBarSpec { SupportTopBars = new[] { config } };
        var stack = TestBeamData.TwoSpan();

        var thin = BeamAdditionalBarCalculator.ComputeSupportTopBars(stack.WithCover(25), spec, 8.0);
        var thick = BeamAdditionalBarCalculator.ComputeSupportTopBars(stack.WithCover(40), spec, 8.0);

        Assert.NotEmpty(thin);
        Assert.Equal(thin[0].Polyline.Points.Max(p => p.Z) - 15.0, thick[0].Polyline.Points.Max(p => p.Z), 6);
    }

    [Fact]
    public void FindSpanAt_StationInSecondSpan_ReturnsSecondSpan()
    {
        var stack = TestBeamData.TwoSpan();

        Assert.Same(stack.Spans[1], stack.FindSpanAt(9000));
    }
}
