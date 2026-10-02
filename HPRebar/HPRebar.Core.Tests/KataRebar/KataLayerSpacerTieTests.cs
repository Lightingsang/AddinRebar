using System.Collections.Generic;
using System.Linq;
using HPRebar.Core.KataRebar.Calculators;
using HPRebar.Core.KataRebar.Models;
using HPRebar.Core.KataRebar.Parsers;
using Xunit;

namespace HPRebar.Core.Tests.KataRebar;

/// <summary>
/// C ties under an inner bar layer (Kata mark 14 "Ø8a500" in every section of T2-DY7.dwg with a second layer of
/// 3Ø18: sections 1-1 to 7-7, none in 8-8 and 9-9), and the J7/I8 spacing they share with the side-bar ties.
/// Stations are mm from the outer face of column C; supports C 0–450, E 5950–6450, G 13400–13850, I 16050–16250.
/// </summary>
public sealed class KataLayerSpacerTieTests
{
    private static KataRebarLayoutResult Layout(KataCellTable table, KataSettings? settings = null) =>
        KataRebarCalculator.Calculate(KataDamSheetParser.Parse(table), settings ?? KataSettings.Default);

    private static List<KataBarSet> Ties(KataRebarLayoutResult layout, string mark) =>
        layout.BarSets.Where(s => s.ZoneName == KataLayerSpacerTieLayout.ZoneName && s.BarMark == mark).ToList();

    [Fact]
    public void Every_second_layer_of_three_bars_in_T2_DY7_gets_ties_at_J7_span_by_span()
    {
        var layout = Layout(KataDy7DrawingTests.Sheet());

        // Range = the layer's bars, 66 (50 + 2 × Ø8) inside their ends and the support faces, every 500.
        var expected = new Dictionary<string, double[][]>
        {
            ["3.1.2C"] = new[] { new[] { 516.0, 1016.0, 1516.0 } },                                   // C14 to 1850
            ["3.2.2C"] = new[] { new[] { 4616.0, 5116.0, 5616.0 }, new[] { 6516.0, 7016.0, 7516.0, 8016.0 } }, // E14 4550–8200
            ["3.3.2C"] = new[] { new[] { 11716.0, 12216.0, 12716.0, 13216.0 }, new[] { 13916.0 } },     // G14 11650–14400 (section 7-7)
            ["4.1.2C"] = new[] { Enumerable.Range(0, 8).Select(i => 1416.0 + 500.0 * i).ToArray() },  // D17 1350–5050
            ["4.2.2C"] = new[] { Enumerable.Range(0, 10).Select(i => 7666.0 + 500.0 * i).ToArray() }  // F17 7600–12250
        };

        var all = layout.BarSets.Where(s => s.ZoneName == KataLayerSpacerTieLayout.ZoneName).ToList();
        Assert.Equal(expected.Keys.OrderBy(k => k), all.Select(s => s.BarMark).Distinct().OrderBy(k => k));
        foreach (var (mark, runs) in expected)
        {
            var sets = Ties(layout, mark);
            Assert.Equal(runs.Length, sets.Count);
            for (int i = 0; i < runs.Length; i++)
            {
                Assert.Equal(runs[i], sets[i].Stations.Select(x => System.Math.Round(x, 3)).ToArray());
                Assert.Equal(500.0, sets[i].Spacing);
            }
        }
    }

    [Fact]
    public void The_tie_wraps_the_two_outer_bars_of_its_layer_and_runs_under_them()
    {
        var layout = Layout(KataDy7DrawingTests.Sheet());

        foreach (var (mark, layer) in new[] { ("3.2.2C", layout.ExtraTopBars.Where(b => b.HostSupportIndex == 1 && b.Layer == 2)),
                     ("4.2.2C", layout.ExtraBottomBars.Where(b => b.HostSpanIndex == 1 && b.Layer == 2)) })
        {
            var bars = layer.OrderBy(b => b.TransverseY).ToList();
            var tie = Ties(layout, mark)[0];
            var (a, b) = (tie.Shape.Points[0], tie.Shape.Points[1]);
            Assert.Equal((bars[0].TransverseY, bars[2].TransverseY), (a.Y, b.Y));
            Assert.Equal(bars[0].Polyline.Points.Max(p => p.Z), a.Z, 6);
            Assert.Equal(a.X, tie.Stations[0]);
            Assert.True(tie.WrapEnds);
            Assert.Equal((0.0, -1.0), (tie.WrapOffset.Y, tie.WrapOffset.Z));
            Assert.Equal((KataBarRole.CrossTie, 8.0, 180), (tie.Role, tie.Diameter, tie.HookAngle));
            // Kata draws the hook centres at ±112 (its stirrup centre line on the cover); HPRebar's bars touch the stirrup's inner face.
            Assert.True(System.Math.Abs(System.Math.Abs(a.Y) - 112.0) <= 5.0, $"{mark}: {a.Y}");
        }
    }

    [Fact]
    public void A_layer_of_two_bars_gets_no_tie_unless_the_setting_asks_for_two()
    {
        var table = KataDy7DrawingTests.Sheet();
        table.Set("C14", "2f18");

        Assert.Empty(Ties(Layout(table), "3.1.2C"));
        Assert.NotEmpty(Ties(Layout(table, KataSettings.Default with { LayerTieMinBarCount = 2 }), "3.1.2C"));
    }

    [Fact]
    public void Like_hoops_puts_one_tie_beside_every_outer_hoop_within_the_layer()
    {
        var table = KataDy7DrawingTests.Sheet();
        table.Set("I8", 1);
        var layout = Layout(table);

        var hoops = layout.StirrupZones.Where(z => z.SpanIndex == 1).SelectMany(z => z.Stations).Select(x => x + 16.0)
            .Where(x => x >= 7666.0 - 1e-6 && x <= 12184.0 + 1e-6).ToList();
        var sets = Ties(layout, "4.2.2C");

        Assert.Equal(hoops, sets.SelectMany(s => s.Stations));
        Assert.All(sets, s => Assert.Equal(s.Spacing, s.Stations.Count > 1 ? s.Stations[1] - s.Stations[0] : s.Spacing, 6));
        Assert.DoesNotContain(layout.Warnings, w => w.StartsWith("J7"));
    }

    [Fact]
    public void Side_bar_ties_follow_the_same_option_group()
    {
        var table = KataRebarTestSheets.SingleSpan();
        table.Set("G4", 12.0);
        table.Set("G5", 1);
        table.Set("I8", 1);
        var layout = Layout(table);

        var ties = layout.BarSets.Where(s => s.ZoneName == "Cốt giá").ToList();
        var hoops = layout.StirrupZones.SelectMany(z => z.Stations).Select(x => x + 16.0);
        Assert.Equal(hoops, ties.SelectMany(s => s.Stations));
        Assert.Equal(layout.StirrupZones.Count, ties.Count);
    }

    [Theory]
    [InlineData("", "J7 trống")]
    [InlineData("a100/200", "J7 'a100/200'")]
    public void An_unreadable_J7_spaces_the_ties_at_500_and_says_so(string j7, string warning)
    {
        var table = KataDy7DrawingTests.Sheet();
        table.Set("J7", j7);
        var layout = Layout(table);

        Assert.Contains(layout.Warnings, w => w.StartsWith(warning));
        Assert.Equal(500.0, Ties(layout, "4.1.2C")[0].Spacing);
    }

    [Fact]
    public void Each_half_of_a_left_right_cell_is_counted_on_its_own_side()
    {
        // E14 "2f18;3f18": two bars into span 1, three into span 2 — only span 2's sections hold three.
        var table = KataDy7DrawingTests.Sheet();
        table.Set("E14", "2f18;3f18");
        var sets = Ties(Layout(table), "3.2.2C");

        Assert.NotEmpty(sets);
        Assert.All(sets, s => Assert.Equal(1, s.SpanIndex));
        Assert.All(sets, s => Assert.All(s.Stations, x => Assert.True(x > 6450.0, $"{x}")));
    }

    [Fact]
    public void A_J7_far_outside_the_tie_spacings_is_a_typo_and_falls_back_to_500()
    {
        var table = KataDy7DrawingTests.Sheet();
        table.Set("J7", "a5");
        var layout = Layout(table);

        Assert.Contains(layout.Warnings, w => w.StartsWith("J7 'a5' ngoài 100–1000 mm"));
        Assert.All(layout.BarSets.Where(s => s.Role == KataBarRole.CrossTie), s => Assert.Equal(500.0, s.Spacing));
    }

    [Fact]
    public void A_tie_knows_the_diameter_of_the_bars_it_wraps()
    {
        var layout = Layout(KataDy7DrawingTests.Sheet());

        Assert.All(layout.BarSets.Where(s => s.ZoneName == KataLayerSpacerTieLayout.ZoneName), s => Assert.Equal(18.0, s.WrappedBarDiameter));
        Assert.All(layout.BarSets.Where(s => s.ZoneName == "Cốt giá"), s => Assert.Equal(12.0, s.WrappedBarDiameter));
    }

    [Fact]
    public void A_tie_that_would_touch_the_layer_below_is_reported()
    {
        // A Ø16 tie hangs about 40 under the row 17 centres; with no clear gap rule the main level is 34 below.
        var table = KataDy7DrawingTests.Sheet();
        table.Set("G6", 16.0);
        var layout = Layout(table, KataSettings.Default with { LayerClearGap = 0.0 });

        Assert.Contains(layout.Warnings, w => w.StartsWith("Thanh C kê 4.1.2C"));
        Assert.DoesNotContain(Layout(KataDy7DrawingTests.Sheet()).Warnings, w => w.StartsWith("Thanh C kê"));
    }
}
