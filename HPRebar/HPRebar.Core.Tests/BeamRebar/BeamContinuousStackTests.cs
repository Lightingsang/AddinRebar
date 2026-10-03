using System.Collections.Generic;
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
    public void FindSpanAt_StationInSecondSpan_ReturnsSecondSpan()
    {
        var stack = TestBeamData.TwoSpan();

        Assert.Same(stack.Spans[1], stack.FindSpanAt(9000));
    }
}
