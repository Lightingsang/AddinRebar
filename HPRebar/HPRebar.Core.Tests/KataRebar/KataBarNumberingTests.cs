using System.Collections.Generic;
using System.Linq;
using HPRebar.Core.KataRebar.Calculators;
using HPRebar.Core.KataRebar.Models;
using HPRebar.Core.KataRebar.Parsers;
using Xunit;

namespace HPRebar.Core.Tests.KataRebar;

/// <summary>
/// Kata's bar numbers as T2-DY7.dwg shows them (blocks kata_block_KHT, attribute SH2) for T2-DY7 and T2-DY14.
/// </summary>
public sealed class KataBarNumberingTests
{
    private static KataRebarLayoutResult Layout(KataCellTable table) => KataRebarCalculator.Calculate(KataDamSheetParser.Parse(table));

    private static int Top(KataRebarLayoutResult l, int support, int layer) =>
        l.ExtraTopBars.Where(b => b.HostSupportIndex == support && b.Layer == layer).Select(b => b.BarNumber).Distinct().Single();

    private static int Bottom(KataRebarLayoutResult l, int span, int layer) =>
        l.ExtraBottomBars.Where(b => b.HostSpanIndex == span && b.Layer == layer).Select(b => b.BarNumber).Distinct().Single();

    private static int[] Runs(KataRebarLayoutResult l) =>
        l.MainBottomBars.GroupBy(b => b.BarNumber).OrderBy(g => g.Min(b => b.Polyline.Points.Min(p => p.X))).Select(g => g.Key).ToArray();

    private static int[] Hoops(KataRebarLayoutResult l) =>
        l.StirrupZones.GroupBy(z => z.SpanIndex).OrderBy(g => g.Key).Select(g => g.Select(z => z.BarNumber).Distinct().Single()).ToArray();

    [Fact]
    public void T2_DY7_takes_Kata_s_numbers_and_E13_shares_6_with_the_identical_F17()
    {
        var l = Layout(KataDy7DrawingTests.Sheet());

        Assert.Equal(new[] { 1 }, l.MainTopBars.Select(b => b.BarNumber).Distinct());
        Assert.Equal(new[] { 2, 3 }, Runs(l));
        Assert.Equal((4, 5), (Top(l, 0, 1), Top(l, 0, 2)));
        Assert.Equal((6, 7), (Top(l, 1, 1), Top(l, 1, 2)));
        Assert.Equal((8, 9), (Top(l, 2, 1), Top(l, 2, 2)));
        Assert.Equal((10, 11), (Bottom(l, 0, 1), Bottom(l, 0, 2)));
        Assert.Equal((12, 6), (Bottom(l, 1, 1), Bottom(l, 1, 2)));
        Assert.Equal(new[] { 13 }, l.SideBars.Select(b => b.BarNumber).Distinct());
        Assert.Equal(new[] { 14 }, l.BarSets.Where(KataBarNumbering.IsTie).Select(s => s.BarNumber).Distinct());
        Assert.Equal(new[] { 15, 16, 17 }, Hoops(l));
    }

    [Fact]
    public void T2_DY14_takes_Kata_s_numbers_and_span_4_shares_the_hoop_19_of_span_3()
    {
        var l = Layout(KataDy14DrawingTests.Sheet());

        Assert.Equal(new[] { 1 }, l.MainTopBars.Select(b => b.BarNumber).Distinct());
        Assert.Equal(new[] { 2, 3, 4 }, Runs(l));
        Assert.Equal(new[] { 5, 6, 7, 8, 9, 10 }, new[] { Top(l, 0, 1), Top(l, 0, 2), Top(l, 1, 1), Top(l, 1, 2), Top(l, 2, 1), Top(l, 2, 2) });
        Assert.Equal(new[] { 11, 12, 13, 14 }, new[] { Bottom(l, 0, 1), Bottom(l, 0, 2), Bottom(l, 1, 1), Bottom(l, 1, 2) });
        Assert.Equal(new[] { 15 }, l.SideBars.Select(b => b.BarNumber).Distinct());
        Assert.Equal(new[] { 16 }, l.BarSets.Where(KataBarNumbering.IsTie).Select(s => s.BarNumber).Distinct());
        Assert.Equal(new[] { 17, 18, 19, 19 }, Hoops(l));
    }

    [Fact]
    public void T2_DY7_elevation_tags_read_as_Kata_s()
    {
        var spec = KataDamSheetParser.Parse(KataDy7DrawingTests.Sheet());
        var l = KataRebarCalculator.Calculate(spec);
        var tags = KataBarTagBuilder.Build(l, KataBeamStations.From(spec))
            .Select(t => $"{string.Join("+", t.Numbers)} {t.Text}{(t.Above ? "" : " (below)")}").ToList();

        // Kata's kata_block_KHT tags of T2-DY7 (SH1+SH2 DKKC1).
        var expected = new[]
        {
            "1+4 2Ø18+1Ø18", "5 3Ø18", "1+6 2Ø18+1Ø18", "7 3Ø18", "1+8 2Ø18+1Ø18", "9 3Ø18",
            "2+10 2Ø18+1Ø18 (below)", "11 3Ø18 (below)", "2+12 2Ø18+1Ø18 (below)", "6 3Ø18 (below)",
            "1 2Ø18", "3 2Ø18 (below)", "13 2Ø12 (below)"
        };
        Assert.Equal(expected.OrderBy(t => t), tags.Distinct().OrderBy(t => t));
    }

    [Fact]
    public void T2_DY14_side_bars_read_2x2_and_the_span_tags_follow_the_cut_bottom_bars()
    {
        var spec = KataDamSheetParser.Parse(KataDy14DrawingTests.Sheet());
        var tags = KataBarTagBuilder.Build(KataRebarCalculator.Calculate(spec), KataBeamStations.From(spec))
            .Select(t => $"{string.Join("+", t.Numbers)} {t.Text}").ToList();

        Assert.Contains("15 2x2Ø12", tags);
        Assert.Contains("2+11 2Ø18+1Ø18", tags);
        Assert.Contains("3+13 2Ø18+1Ø18", tags);
        Assert.Contains("4 2Ø18", tags);
    }

    [Fact]
    public void Mirror_image_bent_bars_at_the_two_ends_of_a_symmetric_beam_share_a_number()
    {
        var table = KataRebarTestSheets.TwoSpans();
        table.Set("F11", 6000.0);
        table.Set("C13", "1f18");
        table.Set("G13", "1f18");

        var l = Layout(table);

        Assert.Equal(Top(l, 0, 1), Top(l, 2, 1));
    }

    [Theory]
    [InlineData(4650.4, 4650.6, true)]
    [InlineData(4650.0, 4651.0, true)]
    [InlineData(4650.0, 4652.0, false)]
    public void Bars_within_a_millimetre_share_a_number(double a, double b, bool same)
    {
        KataRebarCurve Bar(int id, double length) => new()
        {
            BarId = id, Role = KataBarRole.ExtraBottom, Diameter = 18.0, HostSpanIndex = id, Layer = 1,
            Polyline = new HPRebar.Core.BeamRebar.Models.Polyline3(new List<HPRebar.Core.BeamRebar.Models.Point3> { new(1000.0 * id, 0.0, -450.0), new(1000.0 * id + length, 0.0, -450.0) })
        };
        var l = KataBarNumbering.Apply(new KataRebarLayoutResult { ExtraBottomBars = new[] { Bar(0, a), Bar(1, b) } }, 8.0);

        Assert.Equal(same, l.ExtraBottomBars[0].BarNumber == l.ExtraBottomBars[1].BarNumber);
    }

    [Fact]
    public void Every_bar_set_zone_and_single_stirrup_is_numbered_and_the_lists_keep_their_order()
    {
        var table = KataDy7DrawingTests.Sheet();
        var spec = KataDamSheetParser.Parse(table);
        var l = KataRebarCalculator.Calculate(spec);

        var all = l.LongitudinalBars.Select(b => b.BarNumber)
            .Concat(l.BarSets.Select(s => s.BarNumber))
            .Concat(l.StirrupZones.Select(z => z.BarNumber))
            .Concat(l.IndividualStirrups.Select(s => s.BarNumber));
        Assert.DoesNotContain(0, all);
        Assert.Equal(l.ExtraTopBars.Select(b => b.BarId), l.ExtraTopBars.Select(b => b.BarId).OrderBy(id => id));
    }
}
