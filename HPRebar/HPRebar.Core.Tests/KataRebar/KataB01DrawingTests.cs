using System;
using System.Collections.Generic;
using System.Linq;
using HPRebar.Core.KataRebar.Calculators;
using HPRebar.Core.KataRebar.Models;
using HPRebar.Core.KataRebar.Parsers;
using Xunit;

namespace HPRebar.Core.Tests.KataRebar;

/// <summary>
/// B01 as Kata draws it (T2-DY7.dwg, title "B01 (SL=1; L=33600)", sheet Dam of KataB1.xlsm, 2026-10-04): six spans
/// 10400 | 6500 | 2500 | 4000 | 6200 | console 2000 between columns of 400, with a support of no width at I and the
/// console at O. Stations are mm from the outer face of column C (drawing x − 6732), z from the beam top (drawing
/// y + 170). The support of no width at I joins H and J into one span, as Kata draws it and as the Revit model has it;
/// the parts Kata draws the way HPRebar already does are checked on the calculator's layout, everything else is
/// listed in the B01 test report.
/// </summary>
public sealed class KataB01DrawingTests
{
    private const double Tolerance = 25.0;

    /// <summary>The cells of KataB1.xlsm that decide the bars.</summary>
    internal static KataCellTable Sheet()
    {
        var t = new KataCellTable();
        foreach (var (address, value) in new (string, object)[]
                 {
                     ("B3", "B01"), ("B5", 1100.0), ("B6", 500.0), ("B7", 150.0), ("B11", "6f25"), ("B12", "6f25"),
                     ("G1", 500.0), ("G2", 40.0), ("G3", 30.0), ("G4", 12.0), ("G5", 2.0), ("G6", 10.0),
                     ("G7", "a150"), ("G8", "a200"), ("G9", 150.0), ("J7", "a500"), ("I8", 2.0), ("J9", "50/25"),
                     ("H3", 0.2), ("I3", "L từ mép cột"), ("H5", 0.25), ("I5", "L từ mép cột"),
                     ("C14", "6f25"), ("E14", "6f20"), ("G14", "2f20"), ("K14", "2f20;2f16"), ("M14", "2f16"),
                     ("E15", "6f20;0"), ("D17", "6f25"), ("F17", "2f20"), ("I17", "2f20"), ("J17", "-"), ("L17", "2f20"),
                     ("H19", -50.0), ("J19", 0.0), ("L19", "3f20"), ("N19", -200.0),
                     ("C20", "400x500"), ("E20", 400.0), ("F20", "0f12"), ("G20", 400.0), ("K20", "400x800"),
                     ("L20", 300.0), ("M20", 400.0), ("N20", "1f12"), ("F21", 400.0), ("L21", "3f20"), ("N21", 0.0),
                     ("C25", "Đai U"), ("D25", "3-4"), ("C26", "Đai C"), ("D26", "2"), ("C27", "Đai C"), ("D27", "5"),
                     ("M25", "Đai C"), ("N25", "2"), ("K24", "*")
                 })
            t.Set(address, value);

        object[] row11 = { 400.0, 10400.0, 400.0, 6500.0, 400.0, 2500.0, 0.0, 4000.0, 400.0, 6200.0, 400.0, 2000.0, 0.0 };
        for (int i = 0; i < row11.Length; i++)
        {
            string column = ((char)('C' + i)).ToString();
            t.Set(column + "10", i % 2 == 0 ? "Cột" : "Nhịp");
            t.Set(column + "11", row11[i]);
        }

        return t;
    }

    [Fact]
    public void B01_reads_as_five_spans_once_I_joins_H_and_J_with_the_soffit_step_of_F_carried_on()
    {
        var spec = KataDamSheetParser.Parse(Sheet());

        Assert.Equal(new[] { 10400.0, 6500.0, 6500.0, 6200.0, 2000.0 }, spec.Spans.Select(s => s.Length));
        Assert.Equal(new[] { 1100.0, 700.0, 700.0, 700.0, 1100.0 }, spec.Spans.Select(s => s.Depth));
        Assert.Equal(0.0, spec.Supports.Last().ColumnWidth);
    }

    [Fact]
    public void B01_agrees_with_the_ten_segments_Revit_measures_and_is_no_longer_blocked()
    {
        double[] lengths = { 400, 10400, 400, 6500, 400, 6500, 400, 6200, 400, 2000 };
        var segments = lengths.Select((l, i) => new KataMeasuredSegment(
            i % 2 == 0 ? KataMeasuredSupportKind.Column : KataMeasuredSupportKind.None, l)).ToList();

        var plan = KataRebarPlanner.Plan(KataDamSheetParser.Parse(Sheet()), new KataMeasuredBeam(500.0, 1100.0, segments, 5));

        Assert.Empty(plan.Blocking);
        Assert.True(plan.CanGenerate);
    }

    [Fact]
    public void Side_bars_stop_after_span_1_and_the_console_has_one_layer_as_drawn()
    {
        var side = Layout().SideBars;

        Assert.Equal(4, side.Count(b => Extent(b).Max < 11000));
        Assert.DoesNotContain(side, b => Extent(b).Max > 11000 && Extent(b).Min < 31000);
        Assert.Equal(2, side.Count(b => Extent(b).Min > 31000));
    }

    [Theory]
    [InlineData(0, 450, 3000, 3200, 8000, 8200, 10750)]
    [InlineData(1, 11250, 12850, 13050, 15850, 16050, 17650)]
    [InlineData(3, 25050, 26550, 26750, 29450, 29650, 31150)]
    public void Stirrup_zones_of_the_spans_between_two_columns_match_the_drawing(
        int span, double denseLeftFirst, double denseLeftLast, double middleFirst, double middleLast, double denseRightFirst, double denseRightLast)
    {
        var zones = Layout().StirrupZones.Where(z => z.SpanIndex == span).OrderBy(z => z.ZoneIndex).ToList();

        Assert.Equal(3, zones.Count);
        Assert.Equal(new[] { denseLeftFirst, denseLeftLast, middleFirst, middleLast, denseRightFirst, denseRightLast },
            zones.SelectMany(z => new[] { z.Stations[0], z.Stations[z.Stations.Count - 1] }).ToArray());
    }

    [Fact]
    public void Side_bars_of_span_1_run_from_C_to_E_on_two_layers_as_drawn()
    {
        var span1 = Layout().SideBars.Where(b => Extent(b).Max < 11000).ToList();

        Assert.Equal(4, span1.Count);
        Assert.All(span1, b => Near(280, Extent(b).Min, "side start"));
        Assert.All(span1, b => Near(10920, Extent(b).Max, "side end"));
        Assert.Equal(new[] { -713.0, -713.0, -387.0, -387.0 }, span1.Select(b => b.Polyline.Points[0].Z).OrderBy(z => z).ToArray(), new ToleranceComparer());
    }

    [Fact]
    public void Additional_top_bars_of_row_14_over_C_and_E_reach_as_drawn()
    {
        var bars = Layout().ExtraTopBars.Where(b => b.Layer == 2).ToList();

        Assert.Contains(bars, b => Near(Extent(b), 105, 3000));
        Assert.Contains(bars, b => Near(Extent(b), 7700, 12850));
    }

    [Fact]
    public void The_top_bars_crank_down_50_in_G_and_back_up_at_I_and_stop_hooked_in_K_where_the_beam_narrows()
    {
        var bar = Layout().MainTopBars.First(b => Extent(b).Min < 100);
        var points = bar.Polyline.Points.Select(p => (p.X, p.Z)).ToList();

        Near(17750, points[2].X, "crank down starts");
        Near(18050, points[3].X, "crank down ends");
        Near(points[2].Z - 50, points[3].Z, "50 lower over H");
        Near(20600, points[4].X, "crank up starts at I");
        Near(20900, points[5].X, "crank up ends in J");
        Near(24945, Extent(bar).Max, "hooked in K");
        Assert.True(bar.EndHookLength > 0);
    }

    [Fact]
    public void The_narrow_spans_get_their_own_top_bars_and_the_console_bars_lap_back_into_L_at_its_drop()
    {
        var top = Layout().MainTopBars.Where(b => Extent(b).Min > 20000).ToList();
        var narrow = top.Where(b => Extent(b).Min < 25000).ToList();
        var console = top.Where(b => Extent(b).Min > 30000).ToList();

        // Row 19 / 21 of L name 3f20: sections 11-14 of the drawing show 3Ø20 top and bottom from K to the tip.
        Assert.Equal(3, narrow.Count);
        Assert.All(narrow, b => Assert.Equal(20.0, b.Diameter));
        Assert.All(narrow, b => { Near(24200, Extent(b).Min, "from J"); Near(31550, Extent(b).Max, "hooked in M"); });
        Assert.All(narrow, b => Assert.True(System.Math.Abs(b.TransverseY) < 150 - 25 - 8));
        Assert.Equal(3, console.Count);
        Assert.All(console, b => { Near(30800, Extent(b).Min, "lap into L"); Near(33550, Extent(b).Max, "console tip"); });
        Assert.All(console, b => Near(-230, b.Polyline.Points.Max(p => p.Z), "200 lower"));
    }

    [Fact]
    public void The_bottom_bars_stop_hooked_in_K_and_the_narrow_ones_start_from_J()
    {
        var bottom = Layout().MainBottomBars.Where(b => Extent(b).Max > 20000).ToList();

        Assert.Contains(bottom, b => Near(Extent(b), 10450, 24925) && b.EndHookLength > 0);
        Assert.Contains(bottom, b => System.Math.Abs(Extent(b).Min - 24400) <= 200 && System.Math.Abs(b.TransverseY) < 150);
    }

    [Fact]
    public void Stirrups_follow_the_top_and_width_of_each_part_as_drawn()
    {
        var zones = Layout().StirrupZones;

        Assert.Contains(zones, z => Same(z.Stations[0], 19950) && Same(z.Stations[z.Count - 1], 20550) && Same(z.OutToOutHeight, 600));
        Assert.Contains(zones, z => Same(z.Stations[0], 20650) && Same(z.Stations[z.Count - 1], 22750) && Same(z.OutToOutHeight, 650));
        Assert.Contains(zones, z => z.ZoneName == "Console" && Same(z.OutToOutHeight, 850) && Same(z.OutToOutWidth, 250)
            && Same(z.Stations[0], 31650) && Same(z.Stations[z.Count - 1], 33500));
        Assert.All(zones.Where(z => z.Stations[0] > 25000), z => Same(z.OutToOutWidth, 250));
    }

    [Fact]
    public void Planned_against_the_Revit_model_the_top_bars_still_crank_over_H_and_the_console_is_200_lower()
    {
        static KataMeasuredPiece P(double start, double length, double width, double height, double top) => new(start, length, width, height, top);
        var segments = new List<KataMeasuredSegment>
        {
            new(KataMeasuredSupportKind.Column, 400), new(KataMeasuredSupportKind.None, 10400, 1100, new[] { P(0, 10400, 500, 1100, 0) }),
            new(KataMeasuredSupportKind.Column, 400), new(KataMeasuredSupportKind.None, 6500, 700, new[] { P(0, 6500, 500, 700, 0) }),
            new(KataMeasuredSupportKind.Column, 400), new(KataMeasuredSupportKind.None, 6500, 700, new[] { P(0, 2500, 500, 650, -50), P(2500, 4000, 500, 700, 0) }),
            new(KataMeasuredSupportKind.Column, 400), new(KataMeasuredSupportKind.None, 6200, 700, new[] { P(0, 6200, 300, 700, 0) }),
            new(KataMeasuredSupportKind.Column, 400), new(KataMeasuredSupportKind.None, 2000, 900, new[] { P(0, 2000, 300, 900, -200) })
        };

        var plan = KataRebarPlanner.Plan(KataDamSheetParser.Parse(Sheet()), new KataMeasuredBeam(500.0, 1100.0, segments, 6));

        Assert.Empty(plan.Blocking);
        var first = plan.Layout.MainTopBars.First(b => Extent(b).Min < 100);
        Assert.Contains(first.Polyline.Points, p => System.Math.Abs(p.X - 18050) <= 1 && p.Z < first.Polyline.Points[1].Z - 49);
        Assert.Contains(plan.Layout.MainTopBars, b => Extent(b).Min > 30000 && b.Polyline.Points.Max(p => p.Z) < -200);
    }

    [Fact]
    public void The_console_gets_its_own_bottom_bars_and_its_top_and_side_bars_run_to_a_short_of_the_tip()
    {
        var layout = Layout();

        var top = layout.MainTopBars.Where(b => Extent(b).Min > 30000).ToList();
        Assert.All(top, b => { Near(33550, Extent(b).Max, "top at tip"); Near(-1030, b.Polyline.Points.Min(p => p.Z), "tip leg down to the bottom bars"); });
        var bottom = layout.MainBottomBars.Where(b => Extent(b).Min > 31000).ToList();
        Assert.Equal(3, bottom.Count);
        Assert.All(bottom, b => { Near(31250, Extent(b).Min, "anchored in M"); Near(33550, Extent(b).Max, "bottom at tip"); Near(250, b.StartHookLength, "10d leg up"); });
        Assert.All(layout.SideBars.Where(b => Extent(b).Min > 31000), b => { Near(31480, Extent(b).Min, "side from M"); Near(33550, Extent(b).Max, "side at tip"); });
        Assert.Contains(layout.ExtraTopBars, b => Extent(b).Min > 30500 && Math.Abs(Extent(b).Max - 33550) <= 1);
    }

    private static bool Same(double a, double b) => System.Math.Abs(a - b) <= 1.0;

    private static KataRebarLayoutResult Layout() => KataRebarCalculator.Calculate(KataDamSheetParser.Parse(Sheet()));

    private static (double Min, double Max) Extent(KataRebarCurve bar) =>
        (bar.Polyline.Points.Min(p => p.X), bar.Polyline.Points.Max(p => p.X));

    private static bool Near((double Min, double Max) extent, double min, double max) =>
        System.Math.Abs(extent.Min - min) <= Tolerance && System.Math.Abs(extent.Max - max) <= Tolerance;

    private static void Near(double expected, double actual, string what) =>
        Assert.True(System.Math.Abs(expected - actual) <= Tolerance, $"{what}: Kata {expected:0}, HPRebar {actual:0}");

    /// <summary>Heights compared within <see cref="Tolerance"/> (Kata draws the side bars between the main bars' inner faces).</summary>
    private sealed class ToleranceComparer : IEqualityComparer<double>
    {
        public bool Equals(double x, double y) => System.Math.Abs(x - y) <= Tolerance;

        public int GetHashCode(double obj) => 0;
    }
}
