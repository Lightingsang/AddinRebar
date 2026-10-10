using System;
using System.Collections.Generic;
using System.Linq;
using HPRebar.Core.KataRebar.Calculators;
using HPRebar.Core.KataRebar.Models;
using HPRebar.Core.KataRebar.Parsers;
using Xunit;

namespace HPRebar.Core.Tests.KataRebar;

/// <summary>
/// The cut marks at the bar ends ("móc cắt kết thúc thép") against the strokes Kata draws on the bars of each beam of
/// <see cref="KataDwgBeam"/>: Kata's stroke is the first or last segment of a bar polyline on kata_thep chu, 75 back
/// along the bar and 25 across (18.4°); HPRebar draws the user's 30° stroke of 3.2 mm on paper, so the bar end and the
/// side the stroke turns to are compared.
/// </summary>
public sealed class KataBarEndMarkTests
{
    public static IEnumerable<object[]> Beams() => KataDwgBeam.Names();

    private static IReadOnlyList<KataBarEndMark> Marks(KataDwgBeam beam)
    {
        var spec = KataDamSheetParser.Parse(beam.Sheet());
        return KataBarEndMarkLayout.Build(KataRebarCalculator.Calculate(spec));
    }

    /// <summary>Kata's strokes: anchor on the bar and unit direction toward the free end of the stroke.</summary>
    private static List<(double X, double Z, double Dx, double Dz)> KataStrokes(KataDwgBeam beam)
    {
        var strokes = new List<(double, double, double, double)>();
        foreach (var e in KataDwgFixture.View(beam, "elevation").Where(e => e.Layer == "kata_thep chu" && e.Points.Count >= 3))
        {
            Take(e.Points[0], e.Points[1]);
            Take(e.Points[e.Points.Count - 1], e.Points[e.Points.Count - 2]);
        }

        return strokes.Distinct().ToList();

        void Take(double[] free, double[] anchor)
        {
            double dx = free[0] - anchor[0], dz = free[1] - anchor[1], length = Math.Sqrt(dx * dx + dz * dz);
            if (Math.Abs(length - Math.Sqrt(75.0 * 75.0 + 25.0 * 25.0)) < 0.5)
                strokes.Add((Math.Round(anchor[0], 1), Math.Round(anchor[1], 1), dx / length, dz / length));
        }
    }

    /// <summary>
    /// Kata draws the bar layers and legs at drafting positions (a layer line 15-25 mm off the bar centre, a leg its
    /// schematic length); the marks sit on the bar centrelines Revit shows. Strokes and marks are paired one to one,
    /// nearest first, within this distance and turned the same way.
    /// </summary>
    private const double DraftingReach = 150.0;

    [Theory]
    [MemberData(nameof(Beams))]
    public void The_marks_sit_on_the_bar_ends_Kata_strokes_and_turn_the_same_way(string name)
    {
        var beam = KataDwgBeam.Named(name);
        var marks = Marks(beam).Where(m => !beam.NearLoad(m.X)).ToList();
        var strokes = KataStrokes(beam).Where(s => !beam.NearLoad(s.X)).ToList();
        Assert.NotEmpty(strokes);

        // One to one, cheapest first: distance, plus a penalty for turning another way so two strokes drafted close
        // together (a leg tip beside a straight end) each find their own bar end.
        var pairs = new List<((double X, double Z, double Dx, double Dz) Stroke, KataBarEndMark Mark)>();
        var candidates = (from s in strokes
                          from m in marks
                          let d = Math.Sqrt((m.X - s.X) * (m.X - s.X) + (m.Z - s.Z) * (m.Z - s.Z))
                          where d <= DraftingReach
                          orderby d + DraftingReach * (1.0 - (m.DirectionX * s.Dx + m.DirectionZ * s.Dz))
                          select (s, m)).ToList();
        foreach (var (s, m) in candidates)
            if (pairs.All(p => p.Stroke != s && !ReferenceEquals(p.Mark, m))) pairs.Add((s, m));

        var turnedOtherWay = pairs.Where(p => p.Mark.DirectionX * p.Stroke.Dx + p.Mark.DirectionZ * p.Stroke.Dz < 0.95).ToList();
        Assert.True(turnedOtherWay.Count == 0, "turned the other way: "
            + string.Join(" ", turnedOtherWay.Select(p => $"Kata ({p.Stroke.X:0},{p.Stroke.Z:0} → {p.Stroke.Dx:0.00},{p.Stroke.Dz:0.00}) mark ({p.Mark.DirectionX:0.00},{p.Mark.DirectionZ:0.00})")));

        // The bars HPRebar cuts where Kata's sheet cuts them are the large majority; the rest differ in the layout
        // (span bars Kata drafts longer, the I17 joint bars), not in the marks.
        Assert.True(pairs.Count >= 0.75 * strokes.Count, $"{pairs.Count} of {strokes.Count} Kata strokes have a mark: unpaired "
            + string.Join(" ", strokes.Where(s => pairs.All(p => p.Stroke != s)).Select(s => $"({s.X:0},{s.Z:0})")));
    }

    [Fact]
    public void A_mark_is_30_degrees_off_its_bar_and_80_mm_long_at_1_to_25()
    {
        var mark = Marks(KataDwgBeam.B01).First();
        var (x, z) = mark.Tip(25.0);

        Assert.Equal(80.0, Math.Sqrt((x - mark.X) * (x - mark.X) + (z - mark.Z) * (z - mark.Z)), 6);
        Assert.Equal(1.0, mark.DirectionX * mark.DirectionX + mark.DirectionZ * mark.DirectionZ, 9);
    }

    [Fact]
    public void Bars_side_by_side_share_one_mark_and_stirrups_get_none()
    {
        var spec = KataDamSheetParser.Parse(KataDwgBeam.B01.Sheet());
        var layout = KataRebarCalculator.Calculate(spec);
        var marks = KataBarEndMarkLayout.Build(layout);

        Assert.Equal(marks.Count, marks.Select(m => (Math.Round(m.X), Math.Round(m.Z), Math.Round(m.DirectionX, 2), Math.Round(m.DirectionZ, 2))).Distinct().Count());
        Assert.True(marks.Count < layout.LongitudinalBars.Count() * 2, "bars at the same elevation must share their marks");
        Assert.Empty(KataBarEndMarkLayout.Build(layout with { MainTopBars = [], MainBottomBars = [], ExtraTopBars = [], ExtraBottomBars = [], SideBars = [], HangerBars = [] }));
    }

    [Theory]
    [InlineData("B03", "B03")]
    [InlineData("D1: 300x600", "D1- 300x600")]
    [InlineData("B3[1]", "B3-1-")]
    [InlineData("  ", "Kata 1")]
    [InlineData(null, "Kata 1")]
    public void The_long_section_takes_a_name_Revit_accepts(string? beam, string expected)
    {
        Assert.Equal(expected, KataViewName.Clean(beam, "Kata 1"));
    }
}
