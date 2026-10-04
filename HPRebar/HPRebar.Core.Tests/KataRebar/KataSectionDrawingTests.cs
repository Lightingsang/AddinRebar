using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using HPRebar.Core.KataRebar.Calculators;
using HPRebar.Core.KataRebar.Models;
using HPRebar.Core.KataRebar.Parsers;
using Xunit;

namespace HPRebar.Core.Tests.KataRebar;

/// <summary>Kata's sections of T2-DY7 and T2-DY14 (E = 350) entity by entity against T2-DY7.dwg (<see cref="KataSectionDrawingGolden"/>).</summary>
public sealed class KataSectionDrawingTests
{
    public static IEnumerable<object[]> Dy7Sections() => new[] { 1, 2, 4, 5, 7, 8 }.Select(n => new object[] { n });

    public static IEnumerable<object[]> Dy14Sections() => new[] { 1, 4, 5 }.Select(n => new object[] { n });

    [Theory]
    [MemberData(nameof(Dy7Sections))]
    public void T2_DY7_section_is_drawn_as_Kata_draws_it(int number) =>
        Same(Golden(KataSectionDrawingGolden.Dy7, number), Keys(Draw(KataDy7DrawingTests.Sheet(), number)), $"DY7 {number}-{number}");

    [Theory]
    [MemberData(nameof(Dy14Sections))]
    public void T2_DY14_section_is_drawn_as_Kata_draws_it(int number) =>
        Same(Golden(KataSectionDrawingGolden.Dy14, number), Keys(Draw(KataDy14DrawingTests.Sheet(), number)), $"DY14 {number}-{number}");

    [Fact]
    public void A_mirrored_section_swaps_the_bars_across_the_beam_but_keeps_Kata_s_hoop_and_tags()
    {
        var plain = Draw(KataDy7DrawingTests.Sheet(), 1);
        var mirrored = Draw(KataDy7DrawingTests.Sheet(), 1, mirror: true);

        Assert.Equal(plain.Bars.Select(b => (-b.X, b.Z)).OrderBy(p => p), mirrored.Bars.Select(b => (b.X, b.Z)).OrderBy(p => p));
        Assert.Equal(plain.Lines.Count, mirrored.Lines.Count);
        Assert.Equal(plain.Tags.Count, mirrored.Tags.Count);
    }

    [Fact]
    public void A_section_without_a_slab_is_the_beam_rectangle_with_its_whole_depth_only()
    {
        var table = KataDy7DrawingTests.Sheet();
        table.Set("B7", "");
        var d = Draw(table, 1, slab: false);

        Assert.Single(d.Lines, l => l.Pen == KataDrawingPen.Outline);
        Assert.DoesNotContain(d.Lines, l => l.Pen == KataDrawingPen.Thin && l.Vertices.Count == 6);
        Assert.Equal(new[] { 300.0, 500.0 }, d.Dims.Select(x => x.Value).OrderBy(v => v));
        Assert.True(d.MinX < -656 && d.MaxX > 520 && d.Top > 295.5 && d.Bottom < d.Title.Z);
    }

    [Fact]
    public void Tags_of_a_third_top_layer_stand_clear_of_the_second_layer_s()
    {
        // Past Kata's drawings: a third layer over the supports (row 15).
        var table = KataDy7DrawingTests.Sheet();
        foreach (var c in new[] { "C", "E", "G" }) table.Set(c + "15", "2f18");
        var d = Draw(table, 1);

        // The tags right of the beam: Kata itself sets the side bars' 119 under the second layer's; the third keeps clear of both.
        var right = d.Tags.Where(t => t.PointsRight && System.Math.Abs(t.X - 520.0) < 1.0).ToList();
        Assert.Equal(3, right.Count);
        var third = right.Single(t => t.Text == "2Ø18");
        foreach (var b in right.Where(t => !ReferenceEquals(t, third)))
            Assert.True(System.Math.Abs(third.Z - b.Z) >= 2.0 * KataTagStyle.CircleRadius, $"{third.Text} at {third.Z:0} and {b.Text} at {b.Z:0}");
    }

    [Fact]
    public void The_width_dimension_stays_under_every_tag_under_the_beam()
    {
        // Past Kata's drawings: two more numbers in the bottom layer of the spans.
        var table = KataDy7DrawingTests.Sheet();
        table.Set("B6", 600.0);
        foreach (var c in new[] { "D", "F" }) table.Set(c + "18", "2f20+2f22");

        foreach (int number in new[] { 1, 2, 4, 5 })
        {
            var d = Draw(table, number);
            double width = d.Dims.Single(x => !x.Vertical).LineAt;
            Assert.True(d.Tags.All(t => t.Z - width >= KataSectionStyle.WidthDimUnderTags - 1e-6), $"{number}-{number}: width dimension at {width:0}");
        }
    }

    [Fact]
    public void Bulges_turn_into_arcs_through_the_points_AutoCAD_draws()
    {
        // A tie's half circle round the left bar (bulge 1, counter-clockwise from top to bottom: out through x = -125).
        var tie = KataBulge.Points(new[] { new KataBulgeVertex(-109, -65, 1.0), new KataBulgeVertex(-109, -97) });
        Assert.Contains(tie, p => System.Math.Abs(p.X + 125) < 0.1 && System.Math.Abs(p.Z + 81) < 0.1);

        // A hoop corner (bulge -0.414, clockwise quarter): every point 13 from the corner bar's centre (112, -462).
        var corner = KataBulge.Points(new[] { new KataBulgeVertex(125, -462, -0.414214), new KataBulgeVertex(112, -475) });
        Assert.True(corner.Count > 2);
        Assert.All(corner, p => Assert.Equal(13.0, System.Math.Sqrt((p.X - 112) * (p.X - 112) + (p.Z + 462) * (p.Z + 462)), 2));
    }

    private static KataSectionDrawing Draw(KataCellTable table, int number, bool mirror = false, bool slab = true)
    {
        if (slab) table.Set("B7", 120.0);
        var spec = KataDamSheetParser.Parse(table);
        var layout = KataRebarCalculator.Calculate(spec);
        var cut = KataSectionCuts.Build(spec, layout).First(c => c.Number == number);
        return KataSectionDrawingBuilder.Build(spec, layout, KataDetailingRuleBuilder.Build(spec), cut, mirror);
    }

    private static List<string> Golden(string text, int number)
    {
        var lines = text.Replace("\r", "").Split('\n');
        int start = Array.IndexOf(lines, $"## {number}-{number}");
        Assert.True(start >= 0, $"no section {number}-{number} in the golden data");
        return lines.Skip(start + 1).TakeWhile(l => !l.StartsWith("##", StringComparison.Ordinal) && l.Length > 0).ToList();
    }

    private static void Same(List<string> kata, List<string> ours, string what)
    {
        Assert.True(kata.Count > 20, $"{what}: golden data not found");
        var missing = kata.Except(ours).ToList();
        var extra = ours.Except(kata).ToList();
        Assert.True(missing.Count == 0 && extra.Count == 0 && kata.Count == ours.Count,
            $"{what}\nKata only:\n  {string.Join("\n  ", missing)}\nHPRebar only:\n  {string.Join("\n  ", extra)}");
    }

    private static List<string> Keys(KataSectionDrawing d)
    {
        var keys = new List<string>();
        foreach (var line in d.Lines) keys.Add($"L {line.Pen} {Vertices(line.Vertices)}");
        foreach (var bar in d.Bars) keys.Add($"B {P(bar.X, bar.Z)} d{R(bar.Diameter)}");
        foreach (var leader in d.Leaders) keys.Add($"A {leader.Arrow} {R(leader.ArrowSize)} {string.Join(" ", leader.Points.Select(p => P(p.X, p.Z)))}");
        foreach (var mark in d.Marks) keys.Add($"M {P(mark.X, mark.Z)} r{R(mark.Radius)}");
        foreach (var tag in d.Tags) keys.Add($"K {P(tag.X, tag.Z)} {tag.BlockState} {tag.Text}{tag.Spacing} #{string.Join(",", tag.Numbers)}");
        foreach (var dim in d.Dims) keys.Add($"D {(dim.Vertical ? "V" : "H")} {R(dim.Value)} at {R(dim.LineAt)}");
        keys.Add($"T {R(d.Title.Z)}");
        keys.Sort(StringComparer.Ordinal);
        return keys;
    }

    /// <summary>Repeated vertices merged, straight collinear ones dropped (as the golden data).</summary>
    private static string Vertices(IReadOnlyList<KataBulgeVertex> vertices)
    {
        var c = new List<KataBulgeVertex>();
        foreach (var v in vertices)
        {
            if (c.Count > 0 && Math.Abs(c[c.Count - 1].X - v.X) < 0.05 && Math.Abs(c[c.Count - 1].Z - v.Z) < 0.05)
            {
                c[c.Count - 1] = c[c.Count - 1] with { Bulge = v.Bulge };
                continue;
            }

            c.Add(v);
        }

        for (int i = c.Count - 2; i >= 1; i--)
        {
            var (a, m, n) = (c[i - 1], c[i], c[i + 1]);
            if (Math.Abs(a.Bulge) > 1e-6 || Math.Abs(m.Bulge) > 1e-6) continue;
            double x1 = m.X - a.X, z1 = m.Z - a.Z, x2 = n.X - m.X, z2 = n.Z - m.Z;
            if (Math.Abs(x1 * z2 - z1 * x2) < 1e-3 && x1 * x2 + z1 * z2 > 0) c.RemoveAt(i);
        }

        return string.Join(" ", c.Select(v => P(v.X, v.Z) + (Math.Abs(v.Bulge) > 1e-6 ? "b" + v.Bulge.ToString("0.###", CultureInfo.InvariantCulture) : "")));
    }

    private static string P(double x, double z) => R(x) + "," + R(z);

    private static string R(double v) => Math.Round(v, 1, MidpointRounding.AwayFromZero).ToString("0.#", CultureInfo.InvariantCulture);
}
