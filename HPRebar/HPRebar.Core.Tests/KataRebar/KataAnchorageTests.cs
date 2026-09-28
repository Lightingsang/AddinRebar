using System;
using HPRebar.Core.KataRebar.Calculators;
using Xunit;

namespace HPRebar.Core.Tests.KataRebar;

public sealed class KataAnchorageTests
{
    [Fact]
    public void A_wide_support_holds_the_bar_straight()
    {
        // Right end: inner face at 6400, 1000 mm wide, 40d = 800 fits in 1000 − 43.
        var end = KataAnchorage.Solve(6400.0, 1000.0, +1, 43.0, 800.0, 200.0, 514.0);

        Assert.False(end.IsBent);
        Assert.Equal(7200.0, end.X);
    }

    [Fact]
    public void A_narrow_support_bends_the_bar_at_the_far_face()
    {
        // Left end: inner face at 400, column 400 wide: 400 − 43 = 357 horizontal, 800 − 357 = 443 leg.
        var end = KataAnchorage.Solve(400.0, 400.0, -1, 43.0, 800.0, 200.0, 514.0);

        Assert.True(end.IsBent);
        Assert.Equal(43.0, end.X);
        Assert.Equal(443.0, end.Leg);
        Assert.Equal(0.0, end.Shortfall);
    }

    [Fact]
    public void The_leg_is_never_shorter_than_the_minimum()
    {
        var end = KataAnchorage.Solve(400.0, 400.0, -1, 43.0, 400.0, 200.0, 514.0);

        Assert.Equal(200.0, end.Leg);
    }

    [Fact]
    public void A_leg_longer_than_the_beam_allows_is_clamped_and_reported()
    {
        var end = KataAnchorage.Solve(400.0, 400.0, -1, 43.0, 800.0, 200.0, 300.0);

        Assert.Equal(300.0, end.Leg);
        Assert.Equal(143.0, end.Shortfall);
    }

    [Fact]
    public void A_bottom_leg_moved_inboard_keeps_its_anchorage()
    {
        double inset = KataAnchorage.BottomLegInset(20.0, 20.0, 25.0);
        var end = KataAnchorage.Solve(400.0, 400.0, -1, 43.0, 600.0, 200.0, 514.0, inset);

        Assert.Equal(45.0, inset);
        Assert.Equal(88.0, end.X);
        Assert.Equal(288.0, end.Leg);
    }

    [Fact]
    public void Legs_overlap_only_when_both_are_bent_and_together_exceed_the_room()
    {
        var top = new KataBarEnd(43.0, 443.0, 0.0);

        Assert.True(KataAnchorage.LegsOverlap(top, new KataBarEnd(43.0, 243.0, 0.0), 514.0));
        Assert.False(KataAnchorage.LegsOverlap(top, new KataBarEnd(43.0, 60.0, 0.0), 514.0));
        Assert.False(KataAnchorage.LegsOverlap(top, new KataBarEnd(643.0, 0.0, 0.0), 514.0));
    }

    [Fact]
    public void A_free_end_cannot_be_anchored()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => KataAnchorage.Solve(0.0, 0.0, -1, 43.0, 800.0, 200.0, 514.0));
    }
}
