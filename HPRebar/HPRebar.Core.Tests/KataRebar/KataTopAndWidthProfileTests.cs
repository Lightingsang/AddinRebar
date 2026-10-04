using System.Collections.Generic;
using System.Linq;
using HPRebar.Core.KataRebar.Calculators;
using HPRebar.Core.KataRebar.Models;
using HPRebar.Core.KataRebar.Parsers;
using Xunit;

namespace HPRebar.Core.Tests.KataRebar;

/// <summary>
/// Spans of their own top (row 19) and width (row 20) on the two-span test beam (300×600, spans 6000 and 4500 between
/// 400 columns, 3f20 / 4f20): the cases a beam other than B01 meets.
/// </summary>
public sealed class KataTopAndWidthProfileTests
{
    private static KataCellTable Sheet(params (string Address, object Value)[] cells)
    {
        var table = KataRebarTestSheets.TwoSpans();
        foreach (var (address, value) in cells) table.Set(address, value);
        return table;
    }

    private static KataRebarLayoutResult Layout(params (string Address, object Value)[] cells) =>
        KataRebarCalculator.Calculate(KataDamSheetParser.Parse(Sheet(cells)));

    private static (double Min, double Max) Extent(KataRebarCurve bar) =>
        (bar.Polyline.Points.Min(p => p.X), bar.Polyline.Points.Max(p => p.X));

    private static KataMeasuredSegment Span(double length, double width, double height, double top) =>
        new(KataMeasuredSupportKind.None, length, height, new[] { new KataMeasuredPiece(0.0, length, width, height, top) });

    private static KataMeasuredBeam Measured(double top1, double top2, double width2 = 300.0) =>
        new(300.0, 600.0, new List<KataMeasuredSegment>
        {
            new(KataMeasuredSupportKind.Column, 400.0), Span(6000.0, 300.0, 600.0, top1),
            new(KataMeasuredSupportKind.Column, 400.0), Span(4500.0, width2, 600.0, top2),
            new(KataMeasuredSupportKind.Column, 400.0)
        }, 2);

    [Fact]
    public void A_short_span_beside_a_width_change_keeps_its_own_main_bars()
    {
        var layout = Layout(("D11", 1000.0), ("F20", 200.0));

        Assert.Equal(3, layout.MainTopBars.Count(b => Extent(b).Min < 500));
        Assert.Equal(4, layout.MainBottomBars.Count(b => Extent(b).Min < 500));
        Assert.Equal(3, layout.MainTopBars.Count(b => Extent(b).Max > 6000));
    }

    [Fact]
    public void Narrow_spans_with_bars_too_close_are_reported_against_row_20()
    {
        var layout = Layout(("F20", 120.0));

        Assert.Contains(layout.Warnings, w => w.StartsWith("F20: nhịp rộng 120"));
    }

    [Fact]
    public void A_millimetre_of_modelling_noise_between_span_tops_neither_cuts_nor_cranks_the_bars()
    {
        var plan = KataRebarPlanner.Plan(KataDamSheetParser.Parse(Sheet(("B11", "3f14"))), Measured(0.0, -0.4));

        Assert.Empty(plan.Blocking);
        Assert.Equal(3, plan.Layout.MainTopBars.Count);
        Assert.All(plan.Layout.MainTopBars, b => Assert.Single(b.Polyline.Points.Select(p => System.Math.Round(p.Z)).Where(z => z > -100).Distinct()));
    }

    [Fact]
    public void A_beam_offset_from_its_level_as_a_whole_is_the_same_beam_one_offset_lower()
    {
        var level = KataRebarPlanner.Plan(KataDamSheetParser.Parse(Sheet()), Measured(0.0, 0.0));
        var lower = KataRebarPlanner.Plan(KataDamSheetParser.Parse(Sheet(("D19", -50.0), ("D21", -50.0))), Measured(-50.0, -50.0));

        Assert.Empty(lower.Blocking);
        Assert.DoesNotContain(lower.Warnings, w => w.StartsWith("B5") || w.Contains("cao độ đỉnh"));
        var a = level.Layout.MainTopBars.First().Polyline.Points;
        var b = lower.Layout.MainTopBars.First().Polyline.Points;
        Assert.Equal(a.Count, b.Count);
        Assert.Equal(a[1].Z - 50.0, b[1].Z, 1);
        Assert.Equal(level.Layout.StirrupZones[0].BoxMinZ - 50.0, lower.Layout.StirrupZones[0].BoxMinZ, 1);
    }

    [Fact]
    public void A_support_changing_both_width_and_top_is_refused()
    {
        var plan = KataRebarPlanner.Plan(KataDamSheetParser.Parse(Sheet(("F20", 200.0), ("F19", -300.0))), null);

        Assert.Contains(plan.Blocking, b => b.Contains("đổi cả bề rộng"));
    }

    [Fact]
    public void A_downward_step_at_a_station_of_no_width_cranks_on_the_higher_side()
    {
        // Spans F and H joined over G (no width): F at 0, H 100 lower.
        var table = Sheet(("G11", 0.0), ("H10", "Nhịp"), ("H11", 3000.0), ("I10", "Cột"), ("I11", 400.0), ("H19", -100.0));

        var layout = KataRebarCalculator.Calculate(KataDamSheetParser.Parse(table));

        double station = 400 + 6000 + 400 + 4500;
        var bar = layout.MainTopBars.First();
        var crank = bar.Polyline.Points.Where(p => p.X > station - 1000 && p.X < station + 1000).ToList();
        Assert.Contains(crank, p => System.Math.Abs(p.X - (station - 600)) < 1);
        Assert.Contains(crank, p => System.Math.Abs(p.X - station) < 1 && p.Z < bar.Polyline.Points[1].Z - 99);
    }

    [Fact]
    public void A_zone_ending_just_past_a_station_step_is_cut_there_too()
    {
        var table = Sheet(("G11", 0.0), ("H10", "Nhịp"), ("H11", 3000.0), ("I10", "Cột"), ("I11", 400.0), ("H19", -100.0));

        var zones = KataRebarCalculator.Calculate(KataDamSheetParser.Parse(table)).StirrupZones;

        double station = 400 + 6000 + 400 + 4500;
        Assert.DoesNotContain(zones, z => z.Stations.Any(x => x < station - 1) && z.Stations.Any(x => x > station + 1));
    }
}
