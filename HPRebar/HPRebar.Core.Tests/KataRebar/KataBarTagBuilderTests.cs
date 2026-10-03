using System.Collections.Generic;
using System.Linq;
using HPRebar.Core.KataRebar.Calculators;
using HPRebar.Core.KataRebar.Parsers;
using Xunit;

namespace HPRebar.Core.Tests.KataRebar;

/// <summary>
/// The elevation tags of Kata's drawings in T2-DY7.dwg (LEADER + kata_block_KHT): station of the leader, row of its
/// horizontal part from the beam top (soffit rows from that span's soffit), side it runs to, numbers and text.
/// </summary>
public sealed class KataBarTagBuilderTests
{
    private const double Tolerance = 75.0;

    // Rows: over the top 100 / 237.5, stirrups 387.5; under a 500 / 600 / 350 mm span 137.5 and 275 below its soffit.
    private const double A = 100.0, B = 237.5, S = 387.5;

    private static double Under(double depth, int row) => -depth - 137.5 - 137.5 * row;

    private static string Key(double rowZ, bool left, IEnumerable<int> numbers, string text) =>
        $"{rowZ:0.#}|{(left ? "P" : "T")}|{string.Join("+", numbers)}|{text}";

    private static List<KataBarTag> Tags(KataCellTable table)
    {
        var spec = KataDamSheetParser.Parse(table);
        return KataBarTagBuilder.Build(spec, KataRebarCalculator.Calculate(spec), 8.0)
            .OrderBy(t => t.Kind).ThenBy(t => t.X).ThenBy(t => t.Above ? 0 : 1).ThenBy(t => t.Above ? t.RowZ : -t.RowZ).ToList();
    }

    private static void Match((double X, string Key)[] kata, IReadOnlyList<KataBarTag> ours, bool numbers = true)
    {
        var got = ours.Select(t => (t.X, Key: Key(t.RowZ, t.PointsLeft, numbers ? t.Numbers : new int[0], t.Text))).ToList();
        Assert.Equal(kata.Length, got.Count);
        for (int i = 0; i < kata.Length; i++)
        {
            string expected = numbers ? kata[i].Key : string.Join("|", kata[i].Key.Split('|').Select((p, k) => k == 2 ? "" : p));
            Assert.True(System.Math.Abs(kata[i].X - got[i].X) <= Tolerance && expected == got[i].Key,
                $"tag {i + 1}: Kata {kata[i].X} {expected}, HPRebar {got[i].X:0} {got[i].Key}");
        }
    }

    [Fact]
    public void T2_DY7_tags_sit_where_Kata_draws_them()
    {
        var tags = Tags(KataDy7DrawingTests.Sheet());

        Match(new[]
        {
            (975.0, Key(A, false, new[] { 5 }, "3Ø18")), (975, Key(B, false, new[] { 1, 4 }, "2Ø18+1Ø18")), (975, Key(Under(500, 0), false, new[] { 2, 10 }, "2Ø18+1Ø18")),
            (3050, Key(A, false, new[] { 1 }, "2Ø18")), (3050, Key(Under(500, 0), false, new[] { 11 }, "3Ø18")), (3050, Key(Under(500, 1), false, new[] { 2, 10 }, "2Ø18+1Ø18")),
            (5425, Key(A, true, new[] { 7 }, "3Ø18")), (5425, Key(B, true, new[] { 1, 6 }, "2Ø18+1Ø18")), (5425, Key(Under(500, 0), true, new[] { 2, 10 }, "2Ø18+1Ø18")),
            (7150, Key(A, false, new[] { 7 }, "3Ø18")), (7150, Key(B, false, new[] { 1, 6 }, "2Ø18+1Ø18")), (7150, Key(Under(600, 0), false, new[] { 2, 12 }, "2Ø18+1Ø18")),
            (9775, Key(A, false, new[] { 1 }, "2Ø18")), (9775, Key(Under(600, 0), false, new[] { 6 }, "3Ø18")), (9775, Key(Under(600, 1), false, new[] { 2, 12 }, "2Ø18+1Ø18")),
            (12700, Key(A, true, new[] { 9 }, "3Ø18")), (12700, Key(B, true, new[] { 1, 8 }, "2Ø18+1Ø18")), (12700, Key(Under(600, 0), true, new[] { 2, 12 }, "2Ø18+1Ø18")),
            (14000, Key(A, false, new[] { 9 }, "3Ø18")), (14000, Key(B, false, new[] { 1, 8 }, "2Ø18+1Ø18")), (14000, Key(Under(350, 0), false, new[] { 3 }, "2Ø18")),
            (14900, Key(A, false, new[] { 1, 8 }, "2Ø18+1Ø18")),
        }, tags.Where(t => t.Kind == KataTagKind.Bars).ToList());

        Match(new[] { (1667.0, Key(Under(500, 0), false, new[] { 13 }, "2Ø12")), (8025, Key(Under(600, 0), false, new[] { 13 }, "2Ø12")) },
            tags.Where(t => t.Kind == KataTagKind.SideBars).ToList());

        Match(new[]
        {
            (1300.0, Key(S, false, new[] { 15 }, "Ø8a100")), (3325, Key(S, false, new[] { 15 }, "Ø8a200")), (5350, Key(S, false, new[] { 15 }, "Ø8a100")),
            (7475, Key(S, false, new[] { 16 }, "Ø8a100")), (10050, Key(S, false, new[] { 16 }, "Ø8a200")), (12625, Key(S, false, new[] { 16 }, "Ø8a100")),
            (14275, Key(S, false, new[] { 17 }, "Ø8a100")), (15075, Key(S, false, new[] { 17 }, "Ø8a200")), (15875, Key(S, false, new[] { 17 }, "Ø8a100")),
        }, tags.Where(t => t.Kind == KataTagKind.Stirrups).ToList());
    }

    [Fact]
    public void T2_DY14_tags_sit_where_Kata_draws_them_including_the_two_layer_side_bars()
    {
        var tags = Tags(KataDy14DrawingTests.Sheet());

        Match(new[]
        {
            (1000.0, Key(A, false, new[] { 6 }, "3Ø18")), (1000, Key(B, false, new[] { 1, 5 }, "2Ø18+1Ø18")), (1000, Key(Under(500, 0), false, new[] { 2, 11 }, "2Ø18+1Ø18")),
            (3125, Key(A, false, new[] { 1 }, "2Ø18")), (3125, Key(Under(500, 0), false, new[] { 12 }, "3Ø18")), (3125, Key(Under(500, 1), false, new[] { 2, 11 }, "2Ø18+1Ø18")),
            (5550, Key(A, true, new[] { 8 }, "3Ø18")), (5550, Key(B, true, new[] { 1, 7 }, "2Ø18+1Ø18")), (5550, Key(Under(500, 0), true, new[] { 2, 11 }, "2Ø18+1Ø18")),
            (7150, Key(A, false, new[] { 8 }, "3Ø18")), (7150, Key(B, false, new[] { 1, 7 }, "2Ø18+1Ø18")), (7150, Key(Under(600, 0), false, new[] { 3, 13 }, "2Ø18+1Ø18")),
            (9775, Key(A, false, new[] { 1 }, "2Ø18")), (9775, Key(Under(600, 0), false, new[] { 14 }, "3Ø18")), (9775, Key(Under(600, 1), false, new[] { 3, 13 }, "2Ø18+1Ø18")),
            (12700, Key(A, true, new[] { 10 }, "3Ø18")), (12700, Key(B, true, new[] { 1, 9 }, "2Ø18+1Ø18")), (12700, Key(Under(600, 0), true, new[] { 3, 13 }, "2Ø18+1Ø18")),
            (14000, Key(A, false, new[] { 10 }, "3Ø18")), (14000, Key(B, false, new[] { 1, 9 }, "2Ø18+1Ø18")), (14000, Key(Under(350, 0), false, new[] { 4 }, "2Ø18")),
            (14900, Key(A, false, new[] { 1, 9 }, "2Ø18+1Ø18")),
            (16425, Key(A, false, new[] { 1, 9 }, "2Ø18+1Ø18")), (16425, Key(Under(350, 0), false, new[] { 4 }, "2Ø18")),
        }, tags.Where(t => t.Kind == KataTagKind.Bars).ToList());

        var side = Assert.Single(tags, t => t.Kind == KataTagKind.SideBars);
        Assert.True(System.Math.Abs(side.X - 8025.0) <= Tolerance, $"side tag at {side.X:0}");
        Assert.Equal(Key(Under(600, 0), false, new[] { 15 }, "2x2Ø12"), Key(side.RowZ, side.PointsLeft, side.Numbers, side.Text));
        Assert.Equal(2, side.FootZ.Count);
        Assert.Equal(288.0, side.LeaderLength);

        // Kata's stirrup tags, 125 past the middle of each zone (the 250 span's two dense zones are one).
        Match(new[] { 1325.0, 3400, 5475, 7475, 10050, 12625, 14275, 15075, 15875, 16550 }
                .Select((x, i) => (x, Key(S, false, new[] { i < 3 ? 17 : i < 6 ? 18 : 19 }, i is 1 or 4 or 7 ? "Ø8a200" : "Ø8a100"))).ToArray(),
            tags.Where(t => t.Kind == KataTagKind.Stirrups).ToList());
    }

    [Fact]
    public void T2_DY14_with_a_500_column_shifts_every_tag_with_its_cut()
    {
        var tags = Tags(KataDy14DrawingTests.Sheet(500.0)).Where(t => t.Kind != KataTagKind.Stirrups).ToList();

        // Kata's leaders of the E = 500 variant (numbers left out: that drawing numbers its bars on its own).
        Match(new[]
        {
            (1000.0, Key(A, false, new int[0], "3Ø18")), (1000, Key(B, false, new int[0], "2Ø18+1Ø18")), (1000, Key(Under(500, 0), false, new int[0], "2Ø18+1Ø18")),
            (3125, Key(A, false, new int[0], "2Ø18")), (3125, Key(Under(500, 0), false, new int[0], "3Ø18")), (3125, Key(Under(500, 1), false, new int[0], "2Ø18+1Ø18")),
            (5550, Key(A, true, new int[0], "3Ø18")), (5550, Key(B, true, new int[0], "2Ø18+1Ø18")), (5550, Key(Under(500, 0), true, new int[0], "2Ø18+1Ø18")),
            (7300, Key(A, false, new int[0], "3Ø18")), (7300, Key(B, false, new int[0], "2Ø18+1Ø18")), (7300, Key(Under(600, 0), false, new int[0], "2Ø18+1Ø18")),
            (9925, Key(A, false, new int[0], "2Ø18")), (9925, Key(Under(600, 0), false, new int[0], "3Ø18")), (9925, Key(Under(600, 1), false, new int[0], "2Ø18+1Ø18")),
            (12850, Key(A, true, new int[0], "3Ø18")), (12850, Key(B, true, new int[0], "2Ø18+1Ø18")), (12850, Key(Under(600, 0), true, new int[0], "2Ø18+1Ø18")),
            (14150, Key(A, false, new int[0], "3Ø18")), (14150, Key(B, false, new int[0], "2Ø18+1Ø18")), (14150, Key(Under(350, 0), false, new int[0], "2Ø18")),
            (15050, Key(A, false, new int[0], "2Ø18+1Ø18")),
            (16575, Key(A, false, new int[0], "2Ø18+1Ø18")), (16575, Key(Under(350, 0), false, new int[0], "2Ø18")),
        }, tags.Where(t => t.Kind == KataTagKind.Bars).ToList(), numbers: false);

        var side = Assert.Single(tags, t => t.Kind == KataTagKind.SideBars);
        Assert.True(System.Math.Abs(side.X - 8175.0) <= Tolerance, $"side tag at {side.X:0}");
    }

    [Theory]
    [InlineData("2Ø18", 200.0)]
    [InlineData("2x2Ø12", 288.0)]
    [InlineData("2Ø18+1Ø18", 432.0)]
    public void The_leader_runs_as_far_as_Kata_draws_it_for_the_text(string text, double length) =>
        Assert.Equal(length, KataTagStyle.LeaderLength(text));

    [Fact]
    public void A_tag_pointing_left_ends_its_leader_left_of_the_bar()
    {
        var tag = new KataBarTag(new[] { 1 }, "2Ø18", 5000.0, 100.0, true, true, new[] { -42.0 }, 200.0);

        Assert.Equal(4800.0, tag.InsertX);
        Assert.Equal(5200.0, (tag with { PointsLeft = false }).InsertX);
    }

    [Fact]
    public void The_tag_band_reaches_the_outermost_row_and_its_circles()
    {
        var spec = KataDamSheetParser.Parse(KataDy7DrawingTests.Sheet());
        var tags = KataBarTagBuilder.Build(spec, KataRebarCalculator.Calculate(spec), 8.0);

        // Band 600 mm deep (span 2): over it the stirrup row 387.5, under it row 2 of span 2 at 600 + 275.
        Assert.Equal((387.5 + 62.5, 275.0 + 62.5), KataBarTagBuilder.Band(tags, 600.0));
    }
}
