using HPRebar.Core.KataRebar.Models;
using Xunit;

namespace HPRebar.Core.Tests.KataRebar;

public sealed class KataStationMapTests
{
    [Fact]
    public void A_drawing_in_the_sheet_order_starts_at_its_first_support()
    {
        var map = KataStationMap.For(firstStart: 250.0, lastEnd: 7050.0, sameOrder: true);

        Assert.Equal(250.0, map.ToStation(0.0));
        Assert.Equal(6650.0, map.ToStation(6400.0));
    }

    [Fact]
    public void A_drawing_listing_the_run_the_other_way_counts_back_from_its_last_support()
    {
        var map = KataStationMap.For(firstStart: 250.0, lastEnd: 7050.0, sameOrder: false);

        Assert.Equal(7050.0, map.ToStation(0.0));
        Assert.Equal((6607.0, 7007.0), map.ToStations(43.0, 443.0));
    }

    [Fact]
    public void Settings_reach_the_rules_through_the_planner()
    {
        var settings = KataSettings.Default with { SideBarAnchorageFactor = 12.0 };

        var plan = HPRebar.Core.KataRebar.Calculators.KataRebarPlanner.Plan(
            HPRebar.Core.KataRebar.Parsers.KataDamSheetParser.Parse(KataRebarTestSheets.SingleSpan()),
            KataRebarTestSheets.MeasuredSingleSpan(),
            settings);

        Assert.Equal(12.0, plan.Rules.SideBarAnchorageFactor);
    }
}
