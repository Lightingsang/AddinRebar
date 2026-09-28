using System.Linq;
using HPRebar.Core.BeamRebar.Models;
using HPRebar.Core.KataRebar.Calculators;
using HPRebar.Core.KataRebar.Models;
using HPRebar.Core.KataRebar.Parsers;
using Xunit;

namespace HPRebar.Core.Tests.KataRebar;

/// <summary>
/// Additional top bars over the supports on the live-check beam (300×600, columns 400 | 6000 | 400, 3Ø20 top,
/// 4Ø20 bottom, J9 43/25, H5 0.25 from the face, H3 0.2 from the centre): C13 2Ø20, C14 2Ø18, E14 2Ø18.
/// </summary>
public sealed class KataSupportTopBarTests
{
    private static KataRebarPlan Plan()
    {
        var table = KataRebarTestSheets.SingleSpan();
        table.Set("H3", 0.2);
        table.Set("I3", "L từ tâm cột");
        table.Set("H5", 0.25);
        table.Set("I5", "L từ mép cột");
        table.Set("C13", "2f20");
        table.Set("C14", "2f18");
        table.Set("E14", "2f18");
        return KataRebarPlanner.Plan(KataDamSheetParser.Parse(table), KataRebarTestSheets.MeasuredSingleSpan());
    }

    [Fact]
    public void Row_13_bars_fill_the_gaps_of_the_main_level_and_anchor_like_the_main_bars()
    {
        var plan = Plan();
        var row13 = plan.Layout.ExtraTopBars.Where(b => b.Layer == 1).ToList();

        Assert.True(plan.CanGenerate);
        Assert.Empty(plan.Warnings);
        Assert.Equal(new[] { -53.5, 53.5 }, row13.Select(b => b.TransverseY).ToArray());
        foreach (var bar in row13)
        {
            Assert.Equal(new Point3(43.0, bar.TransverseY, -486.0), bar.Polyline.Points[0]);
            Assert.Equal(new Point3(43.0, bar.TransverseY, -43.0), bar.Polyline.Points[1]);
            Assert.Equal(new Point3(1900.0, bar.TransverseY, -43.0), bar.Polyline.Points[2]);
            Assert.Equal("3.1.1", bar.BarMark);
        }
    }

    [Fact]
    public void Row_14_bars_sit_one_layer_down_with_their_bend_inboard()
    {
        var bars = Plan().Layout.ExtraTopBars.Where(b => b.Layer == 2).OrderBy(b => b.HostSupportIndex).ThenBy(b => b.TransverseY).ToList();

        Assert.Equal(4, bars.Count);
        var first = bars[0].Polyline.Points;
        Assert.Equal(-108.0, bars[0].TransverseY, 6);
        Assert.Equal(new Point3(87.0, -108.0, -494.0), first[0]);
        Assert.Equal(new Point3(87.0, -108.0, -87.0), first[1]);
        Assert.Equal(new Point3(1400.0, -108.0, -87.0), first[2]);

        var last = bars[2].Polyline.Points;
        Assert.Equal(new Point3(5400.0, -108.0, -87.0), last[0]);
        Assert.Equal(new Point3(6713.0, -108.0, -87.0), last[1]);
        Assert.Equal(new Point3(6713.0, -108.0, -494.0), last[2]);
    }

    [Fact]
    public void Bottom_legs_move_inboard_of_the_innermost_top_leg()
    {
        var bottom = Plan().Layout.MainBottomBars[0].Polyline.Points;

        // inset 44 (row 14 level) + (18 + 20)/2 + 25 = 88 → centre 43 + 88 = 131, leg 600 − (400 − 131) = 331.
        Assert.Equal(new Point3(131.0, -107.0, -226.0), bottom[0]);
        Assert.Equal(new Point3(6669.0, -107.0, -226.0), bottom[3]);
    }

    [Fact]
    public void A_left_right_cell_over_an_end_support_uses_the_span_side()
    {
        var table = KataRebarTestSheets.SingleSpan();
        table.Set("C14", "3f16;2f18");
        table.Set("E14", "2f18;0");

        var plan = KataRebarPlanner.Plan(KataDamSheetParser.Parse(table), KataRebarTestSheets.MeasuredSingleSpan());

        Assert.Equal(2, plan.Layout.ExtraTopBars.Count(b => b.HostSupportIndex == 0));
        Assert.Equal(2, plan.Layout.ExtraTopBars.Count(b => b.HostSupportIndex == 1));
        Assert.All(plan.Layout.ExtraTopBars, b => Assert.Equal(18.0, b.Diameter));
    }

    [Fact]
    public void An_interior_support_with_different_sides_draws_one_bar_per_side()
    {
        var spec = TwoSpans(new KataSideBars(new[] { new KataBarItem(2, 20.0) }, new[] { new KataBarItem(2, 16.0) }));
        var result = KataRebarCalculator.Calculate(spec);
        var bars = result.ExtraTopBars.Where(b => b.HostSupportIndex == 1).ToList();

        // Support 1 spans 6400..6800; reach 0.25 × 6000 from the faces; each side ends at the far face − a.
        Assert.Equal(new[] { 4900.0, 4900.0 }, bars.Where(b => b.Diameter == 20.0).Select(b => b.Polyline.Points[0].X).ToArray());
        Assert.All(bars.Where(b => b.Diameter == 20.0), b => Assert.Equal(6800.0 - 43.0, b.Polyline.Points[1].X, 6));
        Assert.All(bars.Where(b => b.Diameter == 16.0), b => Assert.Equal(6400.0 + 43.0, b.Polyline.Points[0].X, 6));
        Assert.All(bars.Where(b => b.Diameter == 16.0), b => Assert.Equal(8300.0, b.Polyline.Points[1].X, 6));
    }

    [Fact]
    public void An_interior_support_with_equal_sides_draws_one_bar_across()
    {
        var two = new[] { new KataBarItem(2, 20.0) };
        var result = KataRebarCalculator.Calculate(TwoSpans(new KataSideBars(two, two)));
        var bars = result.ExtraTopBars.Where(b => b.HostSupportIndex == 1).ToList();

        Assert.Equal(2, bars.Count);
        Assert.All(bars, b => Assert.Equal((4900.0, 8300.0), (b.Polyline.Points[0].X, b.Polyline.Points[1].X)));
    }

    [Fact]
    public void Gaps_take_extra_bars_nearest_the_centre_first()
    {
        var rules = new KataDetailingRules { StirrupCover = 25.0, StirrupDiameter = 8.0 };
        var main = new[] { -107.0, 0.0, 107.0 };

        Assert.Equal(new[] { -53.5, 53.5 }, KataLayerPositions.BetweenMainBars(main, 2, 300.0, rules, 20.0).ToArray());
        Assert.Single(KataLayerPositions.BetweenMainBars(main, 1, 300.0, rules, 20.0));
        var pair = KataLayerPositions.BetweenMainBars(new[] { -107.0, 107.0 }, 2, 300.0, rules, 20.0);
        Assert.Equal(-107.0 / 3.0, pair[0], 9);
        Assert.Equal(107.0 / 3.0, pair[1], 9);
    }

    [Fact]
    public void Bars_too_close_in_a_layer_are_reported()
    {
        var table = KataRebarTestSheets.SingleSpan();
        table.Set("C13", "4f20");

        var plan = KataRebarPlanner.Plan(KataDamSheetParser.Parse(table), KataRebarTestSheets.MeasuredSingleSpan());

        Assert.Contains(plan.Warnings, w => w.StartsWith("Thép gia cường 3.1.1"));
    }

    [Fact]
    public void An_end_support_cell_with_bars_only_outside_the_beam_is_reported()
    {
        var table = KataRebarTestSheets.SingleSpan();
        table.Set("C14", "2f18;0");

        var plan = KataRebarPlanner.Plan(KataDamSheetParser.Parse(table), KataRebarTestSheets.MeasuredSingleSpan());

        Assert.Empty(plan.Layout.ExtraTopBars);
        Assert.Contains(plan.Warnings, w => w.StartsWith("C14 '2f18;0'"));
    }

    [Fact]
    public void An_unreadable_cell_is_reported()
    {
        var table = KataRebarTestSheets.SingleSpan();
        table.Set("C13", "2 thanh 20");

        var plan = KataRebarPlanner.Plan(KataDamSheetParser.Parse(table), KataRebarTestSheets.MeasuredSingleSpan());

        Assert.Contains(plan.Warnings, w => w.StartsWith("C13 '2 thanh 20'"));
    }

    [Fact]
    public void More_layers_than_the_depth_holds_block()
    {
        var table = KataRebarTestSheets.SingleSpan();
        table.Set("B5", 260.0);
        table.Set("C14", "2f25");
        table.Set("C15", "2f28");
        table.Set("C16", "2f28");
        var measured = KataRebarTestSheets.MeasuredSingleSpan() with { HeightMm = 260.0 };

        var plan = KataRebarPlanner.Plan(KataDamSheetParser.Parse(table), measured);

        Assert.False(plan.CanGenerate);
        Assert.Contains(plan.Blocking, b => b.StartsWith("C1"));
    }

    [Fact]
    public void A_blank_I3_measures_from_the_support_face()
    {
        var table = KataRebarTestSheets.SingleSpan();
        table.Set("H3", 0.2);
        table.Set("C14", "2f18");

        var plan = KataRebarPlanner.Plan(KataDamSheetParser.Parse(table), KataRebarTestSheets.MeasuredSingleSpan());

        // 400 (face) + 0.2 × 6000
        Assert.All(plan.Layout.ExtraTopBars, b => Assert.Equal(1600.0, b.Polyline.Points[b.Polyline.Points.Count - 1].X, 6));
    }

    [Fact]
    public void Sides_listing_the_same_bars_in_another_order_are_symmetric()
    {
        var sides = KataBarNotationParser.ParseSides("2f20+1f18;1f18+2f20", 1);

        Assert.True(sides.IsSymmetric);
        Assert.False(KataBarNotationParser.ParseSides("2f20;2f16", 1).IsSymmetric);
    }

    private static KataBeamRebarSpec TwoSpans(KataSideBars middle) => new()
    {
        Width = 300.0,
        Height = 600.0,
        CoverStirrup = 25.0,
        TopContinuous = new KataBarItem(3, 20.0),
        BottomContinuous = new KataBarItem(3, 20.0),
        GlobalStirrup = new KataStirrupSpec { Diameter = 8.0 },
        TopCutoffRatioLayer1 = 0.25,
        CutoffOriginLayer1 = KataCutoffOrigin.FromColumnFace,
        Supports = new[]
        {
            new KataSupportRebarSpec { SupportIndex = 0, ColumnWidth = 400.0 },
            new KataSupportRebarSpec { SupportIndex = 1, ColumnWidth = 400.0, TopExtraSides = new[] { middle } },
            new KataSupportRebarSpec { SupportIndex = 2, ColumnWidth = 400.0 }
        },
        Spans = new[]
        {
            new KataSpanRebarSpec { SpanIndex = 0, Length = 6000.0 },
            new KataSpanRebarSpec { SpanIndex = 1, Length = 6000.0 }
        }
    };
}
