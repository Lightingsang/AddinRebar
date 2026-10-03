using System.Linq;
using HPRebar.Core.BeamRebar.Models;
using HPRebar.Core.KataRebar.Calculators;
using HPRebar.Core.KataRebar.Models;
using HPRebar.Core.KataRebar.Parsers;
using Xunit;

namespace HPRebar.Core.Tests.KataRebar;

/// <summary>
/// The single-span beam of the Revit live check, end to end: sheet cells → parser → planner, with the
/// geometry Revit measures. The numbers asserted here are the ones measured on the drawn bars.
/// </summary>
public sealed class KataRebarPlannerTests
{
    private static KataRebarPlan PlanFor(string cover = "43/25") =>
        KataRebarPlanner.Plan(
            KataDamSheetParser.Parse(KataRebarTestSheets.SingleSpan(cover)),
            KataRebarTestSheets.MeasuredSingleSpan());

    [Fact]
    public void The_live_check_beam_can_be_drawn_without_warnings()
    {
        var plan = PlanFor();

        Assert.True(plan.CanGenerate);
        Assert.False(plan.Reversed);
        Assert.Empty(plan.Warnings);
        Assert.Empty(plan.Skipped);
    }

    [Fact]
    public void Top_bars_sit_43_below_the_top_and_hook_down_450_at_both_columns()
    {
        // 40d = 800 needs a 443 leg in a 400 column (cover 43); rounded up to 25 mm it is 450 and still fits 514.
        var top = PlanFor().Layout.MainTopBars;

        Assert.Equal(new[] { -107.0, 0.0, 107.0 }, top.Select(b => b.TransverseY).ToArray());
        foreach (var bar in top)
        {
            var pts = bar.Polyline.Points;
            Assert.Equal(new Point3(43.0, bar.TransverseY, -493.0), pts[0]);
            Assert.Equal(new Point3(43.0, bar.TransverseY, -43.0), pts[1]);
            Assert.Equal(new Point3(6757.0, bar.TransverseY, -43.0), pts[2]);
            Assert.Equal(new Point3(6757.0, bar.TransverseY, -493.0), pts[3]);
        }
    }

    [Fact]
    public void Bottom_bars_sit_43_above_the_soffit_with_legs_moved_inboard()
    {
        // 30d = 600: moved inboard by 45 the bar has 312 straight, so 288 is missing — the 15d = 300 leg wins.
        var bottom = PlanFor().Layout.MainBottomBars;

        double[] expectedY = { -107.0, -107.0 / 3.0, 107.0 / 3.0, 107.0 };
        Assert.Equal(4, bottom.Count);
        for (int i = 0; i < 4; i++)
        {
            var pts = bottom[i].Polyline.Points;
            Assert.Equal(expectedY[i], bottom[i].TransverseY, 6);
            Assert.Equal(88.0, pts[0].X, 6);
            Assert.Equal(-557.0 + 300.0, pts[0].Z, 6);
            Assert.Equal(-557.0, pts[1].Z, 6);
            Assert.Equal(6712.0, pts[3].X, 6);
        }
    }

    [Fact]
    public void Stirrups_run_16_at_a100_then_14_at_200_then_16_at_a100()
    {
        var zones = PlanFor().Layout.StirrupZones;

        // Dense zones end 1500 (L0/4) from the faces 400 and 6400; the middle starts and ends 200 past them.
        Assert.Equal(3, zones.Count);
        Assert.Equal((16, 100.0, 450.0, 1900.0), (zones[0].Count, zones[0].LabelSpacing, zones[0].StartStationX, zones[0].EndStationX));
        Assert.Equal(1450.0 / 15.0, zones[0].Spacing, 6);
        Assert.Equal((14, 200.0, 2100.0, 4700.0), (zones[1].Count, zones[1].Spacing, zones[1].StartStationX, zones[1].EndStationX));
        Assert.Equal((16, 100.0, 4900.0, 6350.0), (zones[2].Count, zones[2].LabelSpacing, zones[2].StartStationX, zones[2].EndStationX));
        Assert.All(zones, z =>
        {
            Assert.Equal(250.0, z.OutToOutWidth);
            Assert.Equal(550.0, z.OutToOutHeight);
            Assert.Equal(-125.0, z.BoxMinY);
            Assert.Equal(-575.0, z.BoxMinZ);
            Assert.Equal(KataStirrupShapeType.ClosedHoop, z.StirrupType);
        });
    }

    [Fact]
    public void A_single_cover_number_moves_the_bars_and_the_stirrup_together()
    {
        var plan = PlanFor("40");

        Assert.Equal(22.0, plan.Rules.StirrupCover);
        Assert.Equal(-40.0, plan.Layout.MainTopBars[0].Polyline.Points[1].Z);
        Assert.Equal(-110.0, plan.Layout.MainTopBars[0].TransverseY);
        Assert.Equal(110.0, plan.Layout.MainTopBars[2].TransverseY);
    }

    [Fact]
    public void Revit_geometry_replaces_the_sheet_numbers()
    {
        var spec = KataDamSheetParser.Parse(KataRebarTestSheets.SingleSpan());
        var plan = KataRebarPlanner.Plan(spec, KataRebarTestSheets.MeasuredSingleSpan(410.0, 5990.0, 400.0));

        Assert.True(plan.CanGenerate);
        Assert.Equal(410.0, plan.Spec.Supports[0].ColumnWidth);
        Assert.Equal(5990.0, plan.Spec.Spans[0].Length);
        Assert.Equal(2, plan.Warnings.Count(w => w.Contains("Revit đo")));
    }

    [Fact]
    public void A_sheet_far_off_the_model_blocks()
    {
        var spec = KataDamSheetParser.Parse(KataRebarTestSheets.SingleSpan());
        var plan = KataRebarPlanner.Plan(spec, KataRebarTestSheets.MeasuredSingleSpan(460.0, 5940.0, 400.0));

        Assert.False(plan.CanGenerate);
        Assert.Contains(plan.Blocking, b => b.StartsWith("C11"));
    }

    [Fact]
    public void A_span_drawn_as_two_framing_pieces_is_planned()
    {
        var spec = KataDamSheetParser.Parse(KataRebarTestSheets.SingleSpan());
        var plan = KataRebarPlanner.Plan(spec, KataRebarTestSheets.MeasuredSingleSpan() with { PieceCount = 2 });

        Assert.True(plan.CanGenerate);
    }

    [Fact]
    public void Without_a_picked_beam_the_sheet_alone_is_laid_out()
    {
        var plan = KataRebarPlanner.Plan(KataDamSheetParser.Parse(KataRebarTestSheets.SingleSpan()), null);

        Assert.True(plan.CanGenerate);
        Assert.Equal(3, plan.Layout.MainTopBars.Count);
    }
}
