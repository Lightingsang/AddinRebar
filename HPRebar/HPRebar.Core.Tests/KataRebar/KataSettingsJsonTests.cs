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
        var back = KataSettingsJson.Read("{ \"MaxBarLength\": 9000, \"SomethingNew\": [1, 2] }".Replace("[1, 2]", "\"x\""));

        Assert.Equal(9000.0, back.MaxBarLength);
        Assert.Equal(KataSettings.Default.SideBarAnchorageFactor, back.SideBarAnchorageFactor);
    }

    [Fact]
    public void Values_out_of_range_fall_back_to_the_Kata_defaults()
    {
        var back = KataSettingsJson.Read("{ \"MaxBarLength\": 1e400, \"CrossTieHookAngle\": 45, \"SideBarTieSpacing\": -5, \"RoundCutExtraMm\": 0 }");

        Assert.Equal(11700.0, back.MaxBarLength);
        Assert.Equal(180, back.CrossTieHookAngle);
        Assert.Equal(400.0, back.SideBarTieSpacing);
        Assert.Equal(0.0, back.RoundCutExtraMm);
    }

    [Fact]
    public void Text_that_is_not_a_flat_object_is_refused()
    {
        Assert.Throws<FormatException>(() => KataSettingsJson.Read("not json"));
        Assert.Throws<FormatException>(() => KataSettingsJson.Read("{ \"MaxBarLength\": [1] }"));
    }
}
