using System.Collections.Generic;
using System.Linq;
using HPRebar.Core.KataRebar.Calculators;
using HPRebar.Core.KataRebar.Models;
using HPRebar.Core.KataRebar.Parsers;
using Xunit;

namespace HPRebar.Core.Tests.KataRebar;

/// <summary>
/// T2-DY7 as Kata draws it (T2-DY7.dwg, 2026-10-02): columns 450 | 5500 | 500 | 6950 | 450 | 2200 | beam 200×350,
/// spans 300×500 / 300×600 / 300×350 under a level top (row 21: 0, −100, +150). Stations below are mm from the outer
/// face of column C (drawing x − 6732). Supports: C 0–450, E 5950–6450, G 13400–13850, I 16050–16250.
/// </summary>
public sealed class KataDy7DrawingTests
{
    private const double Tolerance = 25.0;

    internal static KataCellTable Sheet()
    {
        var t = new KataCellTable();
        t.Set("B3", "T2-DY7");
        t.Set("B5", 500.0);
        t.Set("B6", 300.0);
        t.Set("B11", "2f18");
        t.Set("B12", "2f18");
        t.Set("G1", 500.0);
        t.Set("G2", 40.0);
        t.Set("G3", 30.0);
        t.Set("G4", 12.0);
        t.Set("G5", 1.0);
        t.Set("G6", 8.0);
        t.Set("G7", "a100");
        t.Set("G8", "a200");
        t.Set("J7", "a500");
        t.Set("I8", 2);
        t.Set("J9", "30/25");
        t.Set("H3", 0.2);
        t.Set("I3", "L từ mép cột");
        t.Set("H5", 0.25);
        t.Set("I5", "L từ mép cột");
        string[] tags = { "Cột", "Nhịp", "Cột", "Nhịp", "Cột", "Nhịp", "Cột" };
        object[] row11 = { 450.0, 5500.0, 500.0, 6950.0, 450.0, 2200.0, "200x350" };
        for (int i = 0; i < tags.Length; i++)
        {
            string col = ((char)('C' + i)).ToString();
            t.Set(col + "10", tags[i]);
            if (row11[i] is string text) t.Set(col + "11", text); else t.Set(col + "11", (double)row11[i]);
        }

        foreach (var c in new[] { "C", "E", "G" }) { t.Set(c + "13", "1f18"); t.Set(c + "14", "3f18"); }
        t.Set("I13", "-");
        foreach (var c in new[] { "D", "F" }) { t.Set(c + "17", "3f18"); t.Set(c + "18", "1f18"); }
        t.Set("D21", 0.0);
        t.Set("F21", -100.0);
        t.Set("H21", 150.0);
        t.Set("H20", "0");
        return t;
    }

    private static KataRebarLayoutResult Layout() => KataRebarCalculator.Calculate(KataDamSheetParser.Parse(Sheet()));

    private static (double Min, double Max) Extent(KataRebarCurve bar) => (bar.Polyline.Points.Min(p => p.X), bar.Polyline.Points.Max(p => p.X));

    private static void Near(double expected, double actual, string what) =>
        Assert.True(System.Math.Abs(expected - actual) <= Tolerance, $"{what}: Kata {expected}, HPRebar {actual:0.#}");

    [Fact]
    public void Span_depths_come_from_B5_minus_row_21()
    {
        var spec = KataDamSheetParser.Parse(Sheet());

        Assert.Equal(new[] { 500.0, 600.0, 350.0 }, Enumerable.Range(0, 3).Select(spec.DepthOf).ToArray());
        Assert.Equal(200.0, spec.Supports[3].ColumnWidth);
    }

    [Fact]
    public void Additional_top_bars_reach_H5_of_each_span_and_outer_rows_G1_further()
    {
        var top = Layout().ExtraTopBars;
        (double, double) Row(int support, int layer) => Extent(top.First(b => b.HostSupportIndex == support && b.Layer == layer));

        Near(1850, Row(0, 2).Item2, "C14 right");
        Near(2350, Row(0, 1).Item2, "C13 right");
        Near(4550, Row(1, 2).Item1, "E14 left");
        Near(8200, Row(1, 2).Item2, "E14 right");
        Near(4050, Row(1, 1).Item1, "E13 left");
        Near(8700, Row(1, 1).Item2, "E13 right");
        Near(11650, Row(2, 2).Item1, "G14 left");
        Near(14400, Row(2, 2).Item2, "G14 right");
        Near(11150, Row(2, 1).Item1, "G13 left");
        // I13 "-": G13 runs on to the end beam I and anchors there (Kata 16215).
        Near(16215, Row(2, 1).Item2, "G13 right");
        Assert.True(top.First(b => b.HostSupportIndex == 2 && b.Layer == 1).EndHookAngle != HPRebar.Core.BeamRebar.Models.HookAngle.None);
    }

    [Fact]
    public void Additional_bottom_bars_stop_at_the_smaller_of_H3_L_and_L_over_6_and_row_18_G1_nearer()
    {
        var bottom = Layout().ExtraBottomBars;
        (double, double) Row(int span, int layer) => Extent(bottom.First(b => b.HostSpanIndex == span && b.Layer == layer));

        Near(1350, Row(0, 2).Item1, "D17 left");
        Near(5050, Row(0, 2).Item2, "D17 right");
        Near(850, Row(0, 1).Item1, "D18 left");
        Near(5550, Row(0, 1).Item2, "D18 right");
        Near(7600, Row(1, 2).Item1, "F17 left");
        Near(12250, Row(1, 2).Item2, "F17 right");
        Near(7100, Row(1, 1).Item1, "F18 left");
        Near(12750, Row(1, 1).Item2, "F18 right");
    }

    [Fact]
    public void Bottom_main_bars_crank_over_the_100_step_and_split_at_the_250_step()
    {
        var bars = Layout().MainBottomBars;
        var first = bars.First(b => b.TransverseY < 0 && Extent(b).Min < 1000);
        var second = bars.First(b => b.TransverseY < 0 && Extent(b).Min > 10000);
        var p = first.Polyline.Points;

        // Spans 1-2: one bar, cranked from E's left face (5950) to 600 past it (6550), 100 lower.
        Assert.Contains(p, q => System.Math.Abs(q.X - 5950) < 1 && System.Math.Abs(q.Z - (-500 + 42)) < 1);
        Assert.Contains(p, q => System.Math.Abs(q.X - 6550) < 1 && System.Math.Abs(q.Z - (-600 + 42)) < 1);
        // Deep side of the 250 step: to G's far face − a, bent up (Kata 13815).
        Near(13815, Extent(first).Max, "span 1-2 bottom end");
        Assert.NotEqual(HPRebar.Core.BeamRebar.Models.HookAngle.None, first.EndHookAngle);
        // Shallow side: straight 30d = 540 from G's right face (Kata 13310), at the 350 span's level.
        Near(13310, Extent(second).Min, "span 3 bottom start");
        Assert.Equal(HPRebar.Core.BeamRebar.Models.HookAngle.None, second.StartHookAngle);
        Assert.All(second.Polyline.Points.Where(q => q.X < 16000), q => Assert.Equal(-350 + 42, q.Z, 0));
    }

    [Fact]
    public void Side_bars_run_on_through_E_and_stop_at_spans_with_none()
    {
        var side = Layout().SideBars;

        Assert.Equal(2, side.Count);
        Assert.All(side, b => Near(330, Extent(b).Min, "side start"));
        Assert.All(side, b => Near(13520, Extent(b).Max, "side end"));
        Assert.All(side, b => Assert.Equal(-250.0, b.Polyline.Points[0].Z, 0));
    }

    [Fact]
    public void Dense_stirrup_zones_are_a_quarter_of_each_span()
    {
        var zones = Layout().StirrupZones.Where(z => z.ZoneIndex == 0).ToDictionary(z => z.SpanIndex);

        // Kata: the last dense stirrup 0.25 L0 from the face, rounded up to 50 (1375 → 1400, 1737.5 → 1750, 550).
        Assert.Equal(450 + 1400, zones[0].EndStationX, 0);
        Assert.Equal(6450 + 1750, zones[1].EndStationX, 0);
        Assert.Equal(13850 + 550, zones[2].EndStationX, 0);
        Assert.Equal(new[] { 2050.0, 8400.0, 14600.0 }, Layout().StirrupZones.Where(z => z.ZoneIndex == 1).OrderBy(z => z.SpanIndex).Select(z => z.StartStationX).ToArray());
        // Each span's hoop is as deep as the span.
        Assert.Equal(new[] { 450.0, 550.0, 300.0 }, Layout().StirrupZones.Where(z => z.ZoneIndex == 0).OrderBy(z => z.SpanIndex).Select(z => z.OutToOutHeight).ToArray());
    }

    [Fact]
    public void A_beam_support_and_measured_span_depths_plan_without_blocking()
    {
        var measured = new KataMeasuredBeam(300.0, 500.0, new List<KataMeasuredSegment>
        {
            new(KataMeasuredSupportKind.Column, 450.0),
            new(KataMeasuredSupportKind.None, 5500.0, 500.0),
            new(KataMeasuredSupportKind.Column, 500.0),
            new(KataMeasuredSupportKind.None, 6950.0, 600.0),
            new(KataMeasuredSupportKind.Column, 450.0),
            new(KataMeasuredSupportKind.None, 2200.0, 350.0),
            new(KataMeasuredSupportKind.Beam, 200.0)
        }, 3);

        var plan = KataRebarPlanner.Plan(KataDamSheetParser.Parse(Sheet()), measured);

        Assert.True(plan.CanGenerate, string.Join(" | ", plan.Blocking));
        Assert.DoesNotContain(plan.Skipped, s => s.Contains("giật mép dưới"));
        Assert.Equal(new[] { 500.0, 600.0, 350.0 }, Enumerable.Range(0, 3).Select(plan.Spec.DepthOf).ToArray());
    }

    [Fact]
    public void A_measured_span_depth_off_the_sheet_by_more_than_50_blocks()
    {
        var measured = new KataMeasuredBeam(300.0, 500.0, new List<KataMeasuredSegment>
        {
            new(KataMeasuredSupportKind.Column, 450.0),
            new(KataMeasuredSupportKind.None, 5500.0, 500.0),
            new(KataMeasuredSupportKind.Column, 500.0),
            new(KataMeasuredSupportKind.None, 6950.0, 700.0),
            new(KataMeasuredSupportKind.Column, 450.0),
            new(KataMeasuredSupportKind.None, 2200.0, 350.0),
            new(KataMeasuredSupportKind.Beam, 200.0)
        }, 3);

        var plan = KataRebarPlanner.Plan(KataDamSheetParser.Parse(Sheet()), measured);

        Assert.Contains(plan.Blocking, b => b.StartsWith("F21"));
    }
}
