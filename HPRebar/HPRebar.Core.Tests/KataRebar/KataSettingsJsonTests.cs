using System;
using HPRebar.Core.KataRebar.Models;
using HPRebar.Core.KataRebar.Parsers;
using Xunit;

namespace HPRebar.Core.Tests.KataRebar;

public sealed class KataSettingsJsonTests
{
    [Fact]
    public void Settings_survive_a_round_trip()
    {
        var settings = KataSettings.Default with { SideBarAnchorageFactor = 12.5, CrossTieHookAngle = 135, SideBarTieSpacing = 350.0 };

        var back = KataSettingsJson.Read(KataSettingsJson.Write(settings));

        Assert.Equal(settings, back);
    }

    [Fact]
    public void A_missing_key_keeps_the_Kata_default_and_an_unknown_key_is_ignored()
    {
        var back = KataSettingsJson.Read("{ \"SideBarTieSpacing\": 350, \"SomethingNew\": \"x\" }");

        Assert.Equal(350.0, back.SideBarTieSpacing);
        Assert.Equal(KataSettings.Default.SideBarAnchorageFactor, back.SideBarAnchorageFactor);
    }

    [Fact]
    public void A_file_written_before_the_stock_length_was_dropped_still_reads()
    {
        // Files saved by earlier versions carry MaxBarLength; it is ignored like any unknown key.
        var back = KataSettingsJson.Read("{ \"MaxBarLength\": 9000, \"SideBarAnchorageFactor\": 12.5, \"RoundCutExtraMm\": 25 }");

        Assert.Equal(KataSettings.Default with { SideBarAnchorageFactor = 12.5, RoundCutExtraMm = 25.0 }, back);
    }

    [Fact]
    public void Values_out_of_range_fall_back_to_the_Kata_defaults()
    {
        var back = KataSettingsJson.Read("{ \"ClosedStirrupHookFactor\": 1e400, \"CrossTieHookAngle\": 45, \"SideBarTieSpacing\": -5, \"RoundCutExtraMm\": 0 }");

        Assert.Equal(7.5, back.ClosedStirrupHookFactor);
        Assert.Equal(180, back.CrossTieHookAngle);
        Assert.Equal(400.0, back.SideBarTieSpacing);
        Assert.Equal(0.0, back.RoundCutExtraMm);
    }

    [Fact]
    public void Text_that_is_not_a_flat_object_is_refused()
    {
        Assert.Throws<FormatException>(() => KataSettingsJson.Read("not json"));
        Assert.Throws<FormatException>(() => KataSettingsJson.Read("{ \"RoundCutExtraMm\": [1] }"));
    }
}
