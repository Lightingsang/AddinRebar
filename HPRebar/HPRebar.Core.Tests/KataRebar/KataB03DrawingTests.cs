using System;
using System.Linq;
using HPRebar.Core.KataRebar.Calculators;
using HPRebar.Core.KataRebar.Models;
using HPRebar.Core.KataRebar.Parsers;
using Xunit;

namespace HPRebar.Core.Tests.KataRebar;

/// <summary>
/// What B03 adds to B01 (T2-DY7.dwg 2026-10-06, KataB03.xlsm; the fixture's elevation measured here, ±25 like B01):
/// the beam running 150 past column 1 to its crossing beam (C20 "400x500" at C21 −150), row 17 over supports with a
/// width (E17 "-", G17 "-;0", I17 "2f20"), and side bars laid at the levels of the span they start in.
/// </summary>
public sealed class KataB03DrawingTests
{
    private const double Tolerance = 25.0;

    [Fact]
    public void The_beam_runs_150_past_column_1_and_its_bars_anchor_there()
    {
        var spec = KataDamSheetParser.Parse(KataDwgBeam.B03.Sheet());
        var st = KataBeamStations.From(spec);
        var layout = KataRebarCalculator.Calculate(spec);
        var drawing = KataElevationDrawingBuilder.Build(spec, layout, KataSectionCuts.Build(spec, layout), 10.0);

        Assert.Equal(150.0, st.StartOverhang, 3);
        Assert.Equal(0.0, st.EndOverhang, 3);
        Assert.Contains(drawing.Lines, l => l.Pen == KataDrawingPen.Outline && l.Points.Any(p => Math.Abs(p.X + 150.0) < 1.0 && Math.Abs(p.Z) < 1.0));
        Assert.All(layout.MainTopBars.Where(b => b.BarNumber == 1), b => Near(-100, MinX(b), "top main 1 in the overhang"));
        Assert.All(layout.MainBottomBars.Where(b => b.BarNumber == 4), b => Near(-100, MinX(b), "bottom main 4 in the overhang"));
        Assert.All(layout.SideBars.Where(b => b.BarNumber == 20), b => Near(-120, MinX(b), "side bars to the beam's end"));
    }

    [Fact]
    public void Row_17_runs_into_E_on_into_span_2_ends_in_G_and_lies_over_I()
    {
        var layout = KataRebarCalculator.Calculate(KataDamSheetParser.Parse(KataDwgBeam.B03.Sheet()));
        var bars = layout.ExtraBottomBars;

        // D17 6Ø25 (18): anchored in E, bent up; on into span 2 as 11; F17 2Ø20 (19) and 11 end bent up in G.
        Assert.All(bars.Where(b => b.BarNumber == 18), b => { Near(11100, MaxX(b), "D17 into E"); Assert.True(b.EndHookLength > 0.0); });
        Assert.Equal(6, bars.Count(b => b.BarNumber == 11));
        Assert.All(bars.Where(b => b.BarNumber == 11), b => { Near(10450, MinX(b), "run on from E"); Near(24900, MaxX(b), "into G"); });
        // F17's own cut (Kata 13650, 2450 off E; HPRebar 13450) follows a rule not known yet: only its end is checked.
        Assert.All(bars.Where(b => b.BarNumber == 19), b => Near(24900, MaxX(b), "F17 into G"));
        // F17's 2Ø20 in the gaps either side of the middle of the 6Ø25 run on (between their ±41.5 and ±124.5), not on them.
        var f17 = bars.Where(b => b.BarNumber == 19).Select(b => b.TransverseY).OrderBy(y => y).ToList();
        var through = bars.Where(b => b.BarNumber == 11).Select(b => Math.Abs(b.TransverseY)).Distinct().OrderBy(y => y).ToList();
        Assert.Equal(2, f17.Count);
        Assert.Equal(-f17[0], f17[1], 1);
        Assert.InRange(f17[1], through[0] + 10.0, through[1] - 10.0);
        // I17 2Ø20: 16 from 0.25 L of span 3 to past I as the main bar, 17 anchored in I to the console's tip.
        Assert.All(bars.Where(b => b.BarNumber == 16), b => { Near(29650, MinX(b), "I17 left"); Near(31800, MaxX(b), "I17 left end"); });
        Assert.All(bars.Where(b => b.BarNumber == 17), b => { Near(31275, MinX(b), "I17 right"); Near(33570, MaxX(b), "console tip"); });
    }

    [Fact]
    public void Side_bars_keep_span_1s_levels_into_span_2_and_the_console_has_its_own()
    {
        var layout = KataRebarCalculator.Calculate(KataDamSheetParser.Parse(KataDwgBeam.B03.Sheet()));

        Assert.Equal(new[] { -784.0, -416.0 }, layout.SideBars.Where(b => b.BarNumber == 20).Select(b => Math.Round(b.Polyline.Points[0].Z)).Distinct().OrderBy(z => z));
        Near(24720, layout.SideBars.Where(b => b.BarNumber == 20).Max(MaxX), "spans 1-2 in one bar");
        Assert.Contains(layout.SideBars, b => b.BarNumber == 22 && MinX(b) > 31000.0);
    }

    [Fact]
    public void In_Revit_where_the_beam_ends_at_column_1_the_bars_anchor_in_the_column_with_a_warning()
    {
        // As "Dam kata test.rvt" models B03 (read 2026-10-06): the beam from column 1's outer face, 4 framing pieces.
        double[] lengths = { 400, 10400, 400, 13400, 400, 6200, 400, 2000 };
        var segments = lengths.Select((l, i) => new KataMeasuredSegment(
            i % 2 == 0 ? KataMeasuredSupportKind.Column : KataMeasuredSupportKind.None, l, i == 1 ? 1200.0 : i % 2 == 1 ? 900.0 : 0.0)).ToList();

        var plan = KataRebarPlanner.Plan(KataDamSheetParser.Parse(KataDwgBeam.B03.Sheet()), new KataMeasuredBeam(500.0, 1200.0, segments, 4));

        Assert.Empty(plan.Blocking);
        Assert.Contains(plan.Warnings, w => w.StartsWith("C21") && w.Contains("150"));
        Assert.Equal(0.0, KataBeamStations.From(plan.Spec).StartOverhang);
        Assert.All(plan.Layout.MainTopBars.Concat(plan.Layout.MainBottomBars).Concat(plan.Layout.SideBars), b => Assert.True(MinX(b) >= 0.0, b.BarMark));
    }

    private static double MinX(KataRebarCurve bar) => bar.Polyline.Points.Min(p => p.X);

    private static double MaxX(KataRebarCurve bar) => bar.Polyline.Points.Max(p => p.X);

    private static void Near(double expected, double actual, string what) =>
        Assert.True(Math.Abs(expected - actual) <= Tolerance, $"{what}: Kata {expected:0}, HPRebar {actual:0}");
}
