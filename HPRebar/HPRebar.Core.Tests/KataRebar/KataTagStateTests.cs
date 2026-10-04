using HPRebar.Core.KataRebar.Models;
using Xunit;

namespace HPRebar.Core.Tests.KataRebar;

/// <summary>The visibility states of kata_block_KHT as T2-DY7.dwg names them.</summary>
public sealed class KataTagStateTests
{
    [Theory]
    [InlineData(false, 1, KataTagLayout.OneLine, "P11")]
    [InlineData(true, 1, KataTagLayout.OneLine, "T11")]
    [InlineData(false, 2, KataTagLayout.OneLine, "P21")]
    [InlineData(true, 2, KataTagLayout.OneLine, "T21")]
    [InlineData(false, 1, KataTagLayout.SpacingBelow, "P12")]
    [InlineData(true, 1, KataTagLayout.Centred, "T13")]
    public void A_tag_names_its_state_by_side_circles_and_layout(bool pointsRight, int circles, KataTagLayout layout, string state) =>
        Assert.Equal(state, KataTagState.Of(pointsRight, circles, layout));

    [Fact]
    public void A_section_tag_with_a_spacing_is_a_P12_tag_and_one_without_is_P11()
    {
        var stirrup = new KataSectionTag(-277.9, 295.5, false, "Ø8", new[] { 14 }, "a500");
        var bars = new KataSectionTag(-312.0, 100.0, false, "2Ø18", new[] { 1 });

        Assert.Equal((KataTagLayout.SpacingBelow, "P12"), (stirrup.Layout, stirrup.BlockState));
        Assert.Equal((KataTagLayout.OneLine, "P11"), (bars.Layout, bars.BlockState));
    }
}
