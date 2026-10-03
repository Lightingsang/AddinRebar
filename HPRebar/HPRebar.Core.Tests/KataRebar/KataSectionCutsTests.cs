using System.Linq;
using HPRebar.Core.KataRebar.Calculators;
using HPRebar.Core.KataRebar.Models;
using HPRebar.Core.KataRebar.Parsers;
using Xunit;

namespace HPRebar.Core.Tests.KataRebar;

/// <summary>
/// Kata's sections (kata_block_SECBAL, attribute DETAIL) of T2-DY7, T2-DY14 and its E = 500 variant in T2-DY7.dwg;
/// stations from the outer face of column C.
/// </summary>
public sealed class KataSectionCutsTests
{
    private const double Tolerance = 75.0;

    private static (int[] Numbers, double[] X) Cuts(KataCellTable table)
    {
        var spec = KataDamSheetParser.Parse(table);
        var cuts = KataSectionCuts.Build(spec, KataRebarCalculator.Calculate(spec));
        return (cuts.Select(c => c.Number).ToArray(), cuts.Select(c => c.X).ToArray());
    }

    private static void Near(double[] kata, double[] ours)
    {
        Assert.Equal(kata.Length, ours.Length);
        for (int i = 0; i < kata.Length; i++)
            Assert.True(System.Math.Abs(kata[i] - ours[i]) <= Tolerance, $"cut {i + 1}: Kata {kata[i]}, HPRebar {ours[i]:0}");
    }

    [Fact]
    public void T2_DY7_has_nine_sections_where_Kata_draws_them()
    {
        var (numbers, x) = Cuts(KataDy7DrawingTests.Sheet());

        Assert.Equal(Enumerable.Range(1, 9), numbers);
        Near(new[] { 975.0, 3050, 5425, 7150, 9775, 12700, 14000, 14900, 15900 }, x);
    }

    [Theory]
    [InlineData(999.0, new[] { 899.5 })]
    [InlineData(1000.0, new[] { 500.0, 850.0, 1300.0 })]
    [InlineData(2999.0, new[] { 700.0, 1849.5, 3099.0 })]
    [InlineData(3000.0, new[] { 700.0, 1750.0, 3100.0 })]
    public void A_span_under_1_m_has_one_cut_and_the_middle_moves_150_from_3_m(double span, double[] expected)
    {
        var table = KataRebarTestSheets.SingleSpan();
        table.Set("D11", span);

        var (_, x) = Cuts(table);

        Assert.Equal(expected, x.Select(v => System.Math.Round(v, 1)));
    }

    [Fact]
    public void A_cantilever_has_no_cut_near_its_free_tip()
    {
        var table = KataRebarTestSheets.SingleSpan();
        table.Set("C11", 0.0);
        table.Set("D11", 1500.0);

        var (_, x) = Cuts(table);

        // 1500 mm cantilever from x = 0 to the column at 1500: the middle cut and the one beside the column only.
        Assert.Equal(new[] { 700.0, 1350.0 }, x);
    }

    [Theory]
    [InlineData(2000.5, true)]
    [InlineData(2001.5, false)]
    public void A_cut_within_a_millimetre_of_a_bar_end_still_meets_the_bar(double at, bool meets)
    {
        var bar = new KataRebarCurve
        {
            BarNumber = 3, Role = KataBarRole.ExtraBottom, Diameter = 18.0,
            Polyline = new HPRebar.Core.BeamRebar.Models.Polyline3(new System.Collections.Generic.List<HPRebar.Core.BeamRebar.Models.Point3> { new(1000.0, 0.0, -450.0), new(2000.0, 0.0, -450.0) })
        };

        var crossing = KataSectionCuts.Crossing(new KataRebarLayoutResult { ExtraBottomBars = new[] { bar } }, at).ToList();

        Assert.Equal(meets, crossing.Count == 1);
    }

    [Theory]
    [InlineData(350.0, new[] { 1000.0, 3125, 5550, 7150, 9775, 12700, 14000, 14900, 15900, 16425 })]
    [InlineData(500.0, new[] { 1000.0, 3125, 5550, 7300, 9925, 12850, 14150, 15050, 16050, 16575 })]
    public void T2_DY14_shares_section_9_between_the_end_of_span_3_and_span_4(double e, double[] kata)
    {
        var (numbers, x) = Cuts(KataDy14DrawingTests.Sheet(e));

        Assert.Equal(new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 9 }, numbers);
        Near(kata, x);
    }
}
