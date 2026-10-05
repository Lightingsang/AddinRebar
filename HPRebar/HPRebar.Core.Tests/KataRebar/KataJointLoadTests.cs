using System.Collections.Generic;
using System.Linq;
using HPRebar.Core.KataRebar.Calculators;
using HPRebar.Core.KataRebar.Models;
using HPRebar.Core.KataRebar.Parsers;
using Xunit;

namespace HPRebar.Core.Tests.KataRebar;

/// <summary>
/// The two loads the Revit model of B01 adds between supports, against T2-DY7.dwg's B01 (x from the outer face of C):
/// a 400 wide, 900 deep beam framing into span 1 at 5300, and a 400 wide stub column standing on span 2 at 14800.
/// </summary>
public sealed class KataJointLoadTests
{
    private static KataRebarLayoutResult Layout()
    {
        var spec = KataDamSheetParser.Parse(KataB01DrawingTests.Sheet());
        var spans = spec.Spans.ToList();
        spans[0] = spans[0] with { Loads = new[] { new KataSpanLoad(4900.0, 400.0, 900.0, false) } };
        spans[1] = spans[1] with { Loads = new[] { new KataSpanLoad(3600.0, 400.0, 0.0, true) } };
        return KataRebarCalculator.Calculate(spec with { Spans = spans });
    }

    private static bool Same(double a, double b) => System.Math.Abs(a - b) <= 1.0;

    private static IEnumerable<double> Stations(KataRebarLayoutResult layout, int span, bool joint) => layout.StirrupZones
        .Where(z => z.SpanIndex == span && KataJointStirrups.IsJointZone(z.ZoneName) == joint)
        .SelectMany(z => z.Stations).OrderBy(x => x);

    [Theory]
    [InlineData(0, 4850.0, 5050.0, 5550.0, 5750.0)]
    [InlineData(1, 14350.0, 14550.0, 15050.0, 15250.0)]
    public void Five_joint_stirrups_at_a50_stand_on_each_face_as_drawn(int span, double l0, double l1, double r0, double r1)
    {
        var joint = Stations(Layout(), span, joint: true).ToList();

        Assert.Equal(10, joint.Count);
        Assert.True(Same(joint[0], l0) && Same(joint[4], l1) && Same(joint[5], r0) && Same(joint[9], r1), string.Join(" ", joint));
    }

    [Theory]
    [InlineData(0, 4650.0, 5950.0)]
    [InlineData(1, 14150.0, 15450.0)]
    public void The_span_stirrups_stop_one_spacing_short_of_the_joint_stirrups(int span, double lastBefore, double firstAfter)
    {
        var own = Stations(Layout(), span, joint: false).ToList();

        Assert.Contains(own, x => Same(x, lastBefore));
        Assert.Contains(own, x => Same(x, firstAfter));
        Assert.DoesNotContain(own, x => x > lastBefore + 1.0 && x < firstAfter - 1.0);
    }

    [Fact]
    public void No_tie_or_inner_stirrup_belongs_to_a_joint_stirrup_zone()
    {
        var layout = Layout();

        Assert.DoesNotContain(layout.BarSets, s => KataJointStirrups.IsJointZone(s.ZoneName));
        // The inner U / C stirrups run every J7 on through the load, as Kata's section 2-2 there shows them.
    }

    [Fact]
    public void Two_hanger_bars_go_under_the_crossing_beam_as_drawn()
    {
        var bars = Layout().HangerBars.Where(b => b.HostSpanIndex == 0).ToList();

        Assert.Equal(2, bars.Count);
        foreach (var bar in bars)
        {
            var p = bar.Polyline.Points;
            Assert.Equal(16.0, bar.Diameter);
            Assert.Equal(6, p.Count);
            // Kata: 4000 · 4150 · 5050 ‖ 5550 · 6450 · 6600, top −25, under the beam −925 (900 deep).
            Assert.True(Same(p[2].X, 5050) && Same(p[3].X, 5550), $"{p[2].X} {p[3].X}");
            Assert.True(System.Math.Abs(p[2].Z - -933) <= 10, $"{p[2].Z}");
            Assert.True(Same(p[1].X - p[0].X, 150) && Same(p[2].X - p[1].X, p[1].Z - p[2].Z));
            Assert.True(System.Math.Abs(p[0].X - 4000) <= 60 && System.Math.Abs(p[5].X - 6600) <= 60, $"{p[0].X} {p[5].X}");
        }
    }

    [Fact]
    public void Under_a_stub_column_the_hanger_bars_reach_the_beam_s_own_soffit()
    {
        var bars = Layout().HangerBars.Where(b => b.HostSpanIndex == 1).ToList();

        Assert.Equal(2, bars.Count);
        // Kata: 13750 · 13900 · 14550 ‖ 15050 · 15700 · 15850, bottom −675 in the 700-deep span.
        Assert.All(bars, b => Assert.True(Same(b.Polyline.Points[2].X, 14550) && Same(b.Polyline.Points[3].X, 15050)
            && System.Math.Abs(b.Polyline.Points[2].Z - -650) <= 30));
    }

    [Fact]
    public void A_load_near_a_support_face_has_all_its_joint_stirrups_on_the_span_side()
    {
        var spec = KataDamSheetParser.Parse(KataB01DrawingTests.Sheet());
        var spans = spec.Spans.ToList();
        spans[0] = spans[0] with { Loads = new[] { new KataSpanLoad(350.0, 400.0, 900.0, false) } };

        var joint = Stations(KataRebarCalculator.Calculate(spec with { Spans = spans }), 0, joint: true).ToList();

        Assert.Equal(10, joint.Count);
        Assert.All(joint, x => Assert.True(x > 400 + 550));
    }

    private static KataRebarLayoutResult WithLoads(int span, params KataSpanLoad[] loads)
    {
        var spec = KataDamSheetParser.Parse(KataB01DrawingTests.Sheet());
        var spans = spec.Spans.ToList();
        spans[span] = spans[span] with { Loads = loads };
        return KataRebarCalculator.Calculate(spec with { Spans = spans });
    }

    [Theory]
    [InlineData(0, 350.0)]
    [InlineData(4, 1800.0)]
    [InlineData(0, 10150.0)]
    public void Hanger_bars_stay_inside_the_span(int span, double at)
    {
        var layout = WithLoads(span, new KataSpanLoad(at, 400.0, 0.0, true));
        var st = KataBeamStations.From(KataDamSheetParser.Parse(KataB01DrawingTests.Sheet()));

        Assert.NotEmpty(layout.HangerBars);
        Assert.All(layout.HangerBars.SelectMany(b => b.Polyline.Points), p =>
            Assert.True(p.X >= st.SpanStart[span] - 1.0 && p.X <= st.SpanEnd[span] + 1.0, $"{p.X} outside {st.SpanStart[span]}..{st.SpanEnd[span]}"));
    }

    [Fact]
    public void With_two_top_bars_the_hanger_bars_are_two_bars_apart()
    {
        var table = KataB01DrawingTests.Sheet();
        table.Set("B11", "2f25");
        var spec = KataDamSheetParser.Parse(table);
        var spans = spec.Spans.ToList();
        spans[0] = spans[0] with { Loads = new[] { new KataSpanLoad(4900.0, 400.0, 900.0, false) } };

        var ys = KataRebarCalculator.Calculate(spec with { Spans = spans }).HangerBars.Select(b => b.TransverseY).ToList();

        Assert.Equal(2, ys.Count);
        Assert.True(System.Math.Abs(ys[0] - ys[1]) > 100, string.Join(" ", ys));
    }

    [Fact]
    public void Loads_close_together_share_their_joint_stirrups()
    {
        var layout = WithLoads(0, new KataSpanLoad(4900.0, 400.0, 900.0, false), new KataSpanLoad(5500.0, 400.0, 900.0, false));
        var all = layout.StirrupZones.Where(z => z.SpanIndex == 0).SelectMany(z => z.Stations).OrderBy(x => x).ToList();

        for (int i = 1; i < all.Count; i++) Assert.True(all[i] - all[i - 1] >= 24.0, $"{all[i - 1]} {all[i]}");
        Assert.Equal(layout.StirrupZones.Count, layout.StirrupZones.Select(z => (z.SpanIndex, z.ZoneName)).Distinct().Count());
    }

    [Fact]
    public void Wherever_the_load_sits_the_span_stirrups_keep_their_spacing_and_stay_off_it()
    {
        var spec = KataDamSheetParser.Parse(KataB01DrawingTests.Sheet());
        var st = KataBeamStations.From(spec);
        for (double at = 300.0; at <= 10100.0; at += 10.0)
        {
            var layout = WithLoads(0, new KataSpanLoad(at, 400.0, 900.0, false));
            double lo = st.SpanStart[0] + at - 200.0, hi = st.SpanStart[0] + at + 200.0;
            var own = layout.StirrupZones.Where(z => z.SpanIndex == 0 && !KataJointStirrups.IsJointZone(z.ZoneName)).SelectMany(z => z.Stations);
            var all = layout.StirrupZones.Where(z => z.SpanIndex == 0).SelectMany(z => z.Stations).OrderBy(x => x).ToList();

            Assert.DoesNotContain(own, x => x > lo + 1.0 && x < hi - 1.0);
            for (int i = 1; i < all.Count; i++)
            {
                double gap = all[i] - all[i - 1];
                // A remainder shorter than a joint spacing is merged: a gap grows by at most that much (a200 + 50).
                Assert.True(gap >= 24.0 && gap <= 200.0 + 50.0 + 1.0 || (all[i - 1] <= lo + 1.0 && all[i] >= hi - 1.0),
                    $"load at {at}: {all[i - 1]} → {all[i]}");
            }
        }
    }
}
