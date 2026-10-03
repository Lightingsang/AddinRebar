using System.Linq;
using HPRebar.Core.KataRebar.Calculators;
using Xunit;

namespace HPRebar.Core.Tests.KataRebar;

/// <summary>Bar lines of T2-DY7.dwg (layer "kata_thep chu", LWPOLYLINE vertices) rebuilt from their centre lines.</summary>
public sealed class KataBarDraftingTests
{
    private static void Near((double X, double Z) expected, (double X, double Z) actual) =>
        Assert.True(System.Math.Abs(expected.X - actual.X) < 0.5 && System.Math.Abs(expected.Z - actual.Z) < 0.5,
            $"expected ({expected.X}, {expected.Z}), got ({actual.X:0.##}, {actual.Z:0.##})");

    [Fact]
    public void The_hooked_top_bar_has_its_slashes_at_the_leg_tips_and_rounded_bends()
    {
        var outline = KataBarDrafting.Outline(new[] { (6762.0, -17580.0), (6762.0, -17280.0), (22947.0, -17280.0), (22947.0, -17580.0) }, topBar: true);

        // AA96: (6787,-17505) (6762,-17580) (6762,-17300 bulge) (6782,-17280) … (22927,-17280 bulge) (22947,-17300) (22947,-17580) (22922,-17505)
        Near((6787, -17505), outline[0]);
        Near((6762, -17580), outline[1]);
        Assert.Contains(outline, p => System.Math.Abs(p.X - 6762) < 0.5 && System.Math.Abs(p.Z + 17300) < 0.5);
        Assert.Contains(outline, p => System.Math.Abs(p.X - 6782) < 0.5 && System.Math.Abs(p.Z + 17280) < 0.5);
        Assert.Contains(outline, p => System.Math.Abs(p.X - 22927) < 0.5 && System.Math.Abs(p.Z + 17280) < 0.5);
        Assert.DoesNotContain(outline, p => System.Math.Abs(p.X - 6762) < 0.5 && System.Math.Abs(p.Z + 17280) < 0.5);
        Near((22947, -17580), outline[outline.Count - 2]);
        Near((22922, -17505), outline[outline.Count - 1]);
    }

    [Theory]
    [InlineData(10782.0, 15432.0, -17280.0, true, -17305.0)]     // AA9B: a top bar ticks down
    [InlineData(7582.0, 12282.0, -17722.0, false, -17697.0)]     // AA9F: a bottom bar ticks up
    [InlineData(7062.0, 20252.0, -17501.0, false, -17476.0)]     // AAA3: side bars tick up
    public void A_straight_bar_ticks_75_back_and_25_inward_at_both_ends(double x0, double x1, double z, bool top, double tickZ)
    {
        var outline = KataBarDrafting.Outline(new[] { (x0, z), (x1, z) }, top);

        Assert.Equal(4, outline.Count);
        Near((x0 + 75, tickZ), outline[0]);
        Near((x1 - 75, tickZ), outline[3]);
    }

    [Fact]
    public void A_crank_keeps_its_sharp_corners()
    {
        // AA97: bottom bar of spans 1-2 hooked up at both ends and cranked down 100 into span 2.
        var outline = KataBarDrafting.Outline(new[]
        {
            (6767.0, -17597.0), (6767.0, -17722.0), (12682.0, -17722.0), (13282.0, -17822.0), (20547.0, -17822.0), (20547.0, -17697.0)
        }, topBar: false);

        Near((6792, -17672), outline[0]);
        Assert.Contains((12682.0, -17722.0), outline);
        Assert.Contains((13282.0, -17822.0), outline);
        Near((20522, -17772), outline.Last());
    }

    [Fact]
    public void A_slash_never_runs_past_a_short_end_segment()
    {
        var outline = KataBarDrafting.Outline(new[] { (0.0, -40.0), (50.0, -40.0) }, topBar: true);

        Near((50, -65), outline[0]);
        Near((0, -65), outline[3]);
    }
}
