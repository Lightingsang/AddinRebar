using System;
using System.Linq;
using HPRebar.Core.KataRebar.Calculators;
using HPRebar.Core.KataRebar.Models;
using HPRebar.Core.KataRebar.Parsers;
using Xunit;

namespace HPRebar.Core.Tests.KataRebar;

/// <summary>
/// T1-DX10 as Kata draws it (T2-DY7.dwg, title "T1-DX10 (SL=3; L=10350)", sheet Dam of KataB03.xlsm, 2026-10-25):
/// columns C 500 | span D 6900 (300x500) | E 500 | span F 2150 (300x350) | G 300. Stations are mm from the outer face
/// of column C (drawing x − 6732), z from the beam top (drawing y − 27830); ±25 as B01 and B03.
/// </summary>
public sealed class KataT1Dx10DrawingTests
{
    private const double Tolerance = 25.0;

    /// <summary>The cells of the sheet that decide the bars.</summary>
    internal static KataCellTable Sheet()
    {
        var t = new KataCellTable();
        foreach (var (address, value) in new (string, object)[]
                 {
                     ("B3", "T1-DX10"), ("B4", 3.0), ("B5", 500.0), ("B6", 300.0), ("B7", 0.0), ("B9", 0.0), ("B10", "-0.100"),
                     ("B11", "3f18"), ("B12", "3f18"),
                     ("G1", 500.0), ("I1", true), ("G2", 40.0), ("G3", 30.0), ("H3", 0.2), ("I3", "L từ mép cột"), ("G4", 12.0),
                     ("G5", 0.0), ("H5", 0.25), ("I5", "L từ mép cột"), ("G6", 6.0), ("G7", "a150"), ("J7", "a500"), ("G8", "a200"),
                     ("I8", 2.0), ("G9", 150.0), ("J9", "30/25"),
                     ("C14", "3f18"), ("E14", "3f18"), ("G14", "-"), ("D17", "3f18"),
                     ("C19", "450;0"), ("D19", 0.0), ("E19", "450;0"), ("F19", 0.0), ("G19", "250;0"),
                     ("C20", 200.0), ("E20", "200x350"), ("F20", "0f12"), ("G20", "200x350"),
                     ("C21", 125.0), ("D21", 0.0), ("E21", 125.0), ("F21", 150.0), ("G21", -25.0),
                     ("C22", "2.1d"), ("E22", "5.1d"), ("G22", "7.1d"), ("C23", 125.0), ("E23", 125.0), ("G23", -25.0)
                 })
            t.Set(address, value);

        object[] row11 = { 500.0, 6900.0, 500.0, 2150.0, 300.0 };
        for (int i = 0; i < row11.Length; i++)
        {
            string column = ((char)('C' + i)).ToString();
            t.Set(column + "10", i % 2 == 0 ? "Cột" : "Nhịp");
            t.Set(column + "11", row11[i]);
        }

        return t;
    }

    [Fact]
    public void E14_runs_on_through_the_short_span_to_G_whose_row_14_is_a_dash()
    {
        var layout = Layout();
        var e14 = layout.ExtraTopBars.Where(b => b.HostSupportIndex == 1).ToList();

        Assert.Equal(3, e14.Count);
        // Kata: 5650 … leg down at 10255 inside G.
        Assert.All(e14, b => { Near(5650, MinX(b), "E14 cut in span D"); Near(10255, MaxX(b), "E14 anchored in G"); });
        Assert.All(e14, b => Assert.True(b.Polyline.Points.Min(p => p.Z) < -250.0, "E14 bends down in G"));
        Assert.DoesNotContain(layout.Warnings, w => w.StartsWith("G14"));
    }

    [Fact]
    public void The_end_legs_lie_in_Kata_s_order_main_top_then_bottom_then_the_additional_row()
    {
        // Kata (outer face → inboard): C 30 | 60 | 80 (C14), G 10315 | 10290 | 10255 (E14); one step apart, not
        // moved clear of each other (user decision 2026-10-25: Kata's drawing over clash-free legs).
        var layout = Layout();
        double topC = layout.MainTopBars.Select(MinX).Min(), topG = layout.MainTopBars.Select(MaxX).Max();
        double bottomC = layout.MainBottomBars.Select(MinX).Min(), bottomG = layout.MainBottomBars.Select(MaxX).Max();
        double c14 = layout.ExtraTopBars.Where(b => b.HostSupportIndex == 0).Select(MinX).Min();
        double e14 = layout.ExtraTopBars.Where(b => b.HostSupportIndex == 1).Select(MaxX).Max();

        Assert.True(topC < bottomC && bottomC < c14, $"C: {topC:0} | {bottomC:0} | {c14:0}");
        Assert.True(topG > bottomG && bottomG > e14, $"G: {topG:0} | {bottomG:0} | {e14:0}");
        Near(60, bottomC, "bottom leg in C");
        Near(10290, bottomG, "bottom leg in G");
    }

    [Fact]
    public void In_C_the_bottom_bars_reach_the_far_face_and_turn_up_the_rest_of_G3()
    {
        // Kata: 440 along (to 60 from the outer face) + 100 up = 30 × 18; with G3 = 40 the leg takes the rest, 280.
        foreach (var (g3, leg) in new[] { (30.0, 100.0), (40.0, 280.0) })
        {
            var table = Sheet();
            table.Set("G3", g3);
            var bar = KataRebarCalculator.Calculate(KataDamSheetParser.Parse(table)).MainBottomBars.OrderBy(MinX).First();
            var points = bar.Polyline.Points;
            Near(leg, points.Max(p => p.Z) - points.Min(p => p.Z) <= 0 ? 0 : points.Where(p => Math.Abs(p.X - MinX(bar)) < 1).Max(p => p.Z) - points.Where(p => Math.Abs(p.X - MinX(bar)) < 1).Min(p => p.Z), $"leg in C at G3 {g3}");
        }
    }

    [Fact]
    public void D17_stops_one_beam_depth_short_of_the_support_bars_cut()
    {
        // I1 ticked: C and E cut their top bars H5 × 6900 = 1750 into D; D17 stops 500 (h) short of that, 1250 off each face.
        var d17 = Layout().ExtraBottomBars.Where(b => b.HostSpanIndex == 0).ToList();

        Assert.Equal(3, d17.Count);
        Assert.All(d17, b => { Near(1750, MinX(b), "D17 left"); Near(6150, MaxX(b), "D17 right"); });
    }

    [Fact]
    public void Without_I1_the_span_bars_keep_the_H3_rule()
    {
        var table = Sheet();
        table.Set("I1", false);
        var d17 = KataRebarCalculator.Calculate(KataDamSheetParser.Parse(table)).ExtraBottomBars.Where(b => b.HostSpanIndex == 0).ToList();

        Assert.All(d17, b => Assert.True(MinX(b) < 1700.0, $"H3 rule cuts nearer the support, got {MinX(b):0}"));
    }

    [Fact]
    public void Stirrup_zones_match_the_drawing()
    {
        var zones = Layout().StirrupZones.Where(z => z.Count > 0).Select(z => (z.SpanIndex, From: z.Stations.First(), To: z.Stations.Last())).ToList();

        Assert.Equal(
            new[] { (0, 550.0, 2250.0), (0, 2450.0, 5450.0), (0, 5650.0, 7350.0), (1, 7950.0, 8450.0), (1, 8650.0, 9300.0), (1, 9500.0, 10000.0) },
            zones.Select(z => (z.SpanIndex, Math.Round(z.From), Math.Round(z.To))));
    }

    private static KataRebarLayoutResult Layout() => KataRebarCalculator.Calculate(KataDamSheetParser.Parse(Sheet()));

    private static double MinX(KataRebarCurve bar) => bar.Polyline.Points.Min(p => p.X);

    private static double MaxX(KataRebarCurve bar) => bar.Polyline.Points.Max(p => p.X);

    private static void Near(double expected, double actual, string what) =>
        Assert.True(Math.Abs(expected - actual) <= Tolerance, $"{what}: Kata {expected:0}, HPRebar {actual:0}");
}
