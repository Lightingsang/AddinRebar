using System;
using System.Linq;
using HPRebar.Core.KataRebar.Calculators;
using HPRebar.Core.KataRebar.Models;
using HPRebar.Core.KataRebar.Parsers;
using Xunit;

namespace HPRebar.Core.Tests.KataRebar;

/// <summary>
/// T1-DY8 as Kata draws it (T2-DY7.dwg, title "T1-DY8 (SL=1; L=30291)", sheet Dam of KataB03.xlsm, 2026-10-26): a
/// console of 216 at C/D, support E 350, span F 5250 (200x500), then spans of 4525 / 4450 ×4 with the soffit 150 up
/// (200x350) between supports of 300-500. Stations are mm from the console tip (drawing x − 6516), z from the beam top
/// (drawing y − 44580); ±25 as B01 and B03.
/// </summary>
public sealed class KataT1Dy8DrawingTests
{
    private const double Tolerance = 25.0;

    internal static KataCellTable Sheet()
    {
        var t = new KataCellTable();
        foreach (var (address, value) in new (string, object)[]
                 {
                     ("B3", "T1-DY8"), ("B4", 1.0), ("B5", 500.0), ("B6", 200.0), ("B7", 100.0), ("B9", 0.0), ("B10", "-0.100"),
                     ("B11", "3f18"), ("B12", "3f18"),
                     ("G1", 500.0), ("I1", true), ("G2", 40.0), ("G3", 30.0), ("H3", 0.2), ("I3", "L từ mép cột"), ("G4", 12.0),
                     ("G5", 0.0), ("H5", 0.25), ("I5", "L từ mép cột"), ("G6", 6.0), ("G7", "a150"), ("J7", "a500"), ("G8", "a200"),
                     ("I8", 2.0), ("G9", 150.0), ("J9", "30/25"),
                     ("E19", "300;0"), ("G19", "450;0"), ("I19", "250;0"), ("K19", "300;0"), ("M19", "300;0"), ("O19", "300;0"), ("Q19", "250;0"),
                     ("E20", "200x350"), ("F20", "0f12"), ("G20", "200x350"), ("I20", "200x350"), ("K20", "250x350"), ("M20", 300.0), ("Q20", "250x350"),
                     ("D21", 0.0), ("E21", -4.0), ("F21", 0.0), ("G21", 0.0), ("H21", 150.0), ("I21", -25.0), ("J21", 150.0), ("K21", 0.0),
                     ("L21", 150.0), ("M21", 0.0), ("N21", 150.0), ("O21", 0.0), ("P21", 150.0), ("Q21", 25.0),
                     ("E22", "A.1d"), ("G22", "G.1d"), ("I22", "I.1d"), ("K22", "J.1d"), ("M22", "K.1d"), ("O22", "L.1d"), ("Q22", "M.1d"),
                     ("E23", -4.0), ("G23", 0.0), ("I23", -25.0), ("K23", 0.0), ("M23", 0.0), ("O23", 0.0), ("Q23", 25.0)
                 })
            t.Set(address, value);

        object[] row11 = { 0.0, 216.0, 350.0, 5250.0, 500.0, 4525.0, 300.0, 4450.0, 350.0, 4450.0, 350.0, 4450.0, 350.0, 4450.0, 300.0 };
        for (int i = 0; i < row11.Length; i++)
        {
            string column = ((char)('C' + i)).ToString();
            t.Set(column + "10", i % 2 == 0 ? "Cột" : "Nhịp");
            t.Set(column + "11", row11[i]);
        }

        return t;
    }

    [Fact]
    public void The_bottom_bars_of_span_F_run_on_to_the_tip_of_the_console_as_deep_as_it_with_no_bars_of_its_own()
    {
        // Kata: one 3Ø18 from the tip (30) through E and F, 440 into G and 100 up (6255); then 3Ø18 from 540 inside G
        // (5776) over the shallower spans to the far end, turned up there. No lapped console bars lie on them.
        var layout = Layout();
        var bottom = layout.MainBottomBars;

        Assert.Equal(6, bottom.Count);
        var first = bottom.Where(b => MinX(b) < 100.0).ToList();
        Assert.Equal(3, first.Count);
        Assert.All(first, b => { Near(30, MinX(b), "from the tip"); Near(6255, MaxX(b), "into G"); });
        Assert.All(bottom.Except(first), b => { Near(5776, MinX(b), "30d into G"); Near(30240, MaxX(b), "to the far end"); });
        Assert.DoesNotContain(layout.Warnings, w => w.Contains("nối chồng của console"));
    }

    [Fact]
    public void The_top_bars_run_from_the_tip_to_the_far_end()
    {
        var top = Layout().MainTopBars;

        Assert.Equal(3, top.Count);
        Assert.All(top, b => { Near(30, MinX(b), "tip"); Near(30260, MaxX(b), "far end"); });
    }

    private static KataRebarLayoutResult Layout() => KataRebarCalculator.Calculate(KataDamSheetParser.Parse(Sheet()));

    private static double MinX(KataRebarCurve bar) => bar.Polyline.Points.Min(p => p.X);

    private static double MaxX(KataRebarCurve bar) => bar.Polyline.Points.Max(p => p.X);

    private static void Near(double expected, double actual, string what) =>
        Assert.True(Math.Abs(expected - actual) <= Tolerance, $"{what}: Kata {expected:0}, HPRebar {actual:0}");
}
