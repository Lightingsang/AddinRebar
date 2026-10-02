using System.Collections.Generic;
using System.Linq;
using HPRebar.Core.BeamRebar.Models;
using HPRebar.Core.KataRebar.Calculators;
using HPRebar.Core.KataRebar.Models;
using HPRebar.Core.KataRebar.Parsers;
using Xunit;

namespace HPRebar.Core.Tests.KataRebar;

/// <summary>Two or more identical, evenly spaced bars of one cell are one fixed-number set; one bar is single.</summary>
public sealed class KataLongitudinalSetGroupingTests
{
    private static KataRebarCurve Bar(double y, double dia = 20.0, string mark = "1", double z = -43.0, double xEnd = 6000.0) => new()
    {
        Role = KataBarRole.MainTop,
        Diameter = dia,
        BarMark = mark,
        TransverseY = y,
        Polyline = new Polyline3(new List<Point3> { new(0.0, y, z), new(xEnd, y, z) })
    };

    [Fact]
    public void Evenly_spaced_copies_are_one_set_from_the_smallest_offset()
    {
        var sets = KataLongitudinalSetGrouping.Group(new[] { Bar(107.0), Bar(-107.0), Bar(0.0) });

        var set = Assert.Single(sets);
        Assert.Equal(3, set.Count);
        Assert.Equal(-107.0, set.Base.TransverseY);
        Assert.Equal(107.0, set.Spacing, 6);
        Assert.Equal(214.0, set.ArrayLength, 6);
        Assert.False(set.IsSingle);
    }

    [Fact]
    public void A_lone_bar_is_single()
    {
        var set = Assert.Single(KataLongitudinalSetGrouping.Group(new[] { Bar(0.0) }));

        Assert.True(set.IsSingle);
        Assert.Equal(1, set.Count);
    }

    [Fact]
    public void Different_diameters_marks_or_shapes_never_share_a_set()
    {
        var sets = KataLongitudinalSetGrouping.Group(new[]
        {
            Bar(-100.0, dia: 20.0), Bar(100.0, dia: 20.0),
            Bar(-50.0, dia: 16.0), Bar(50.0, dia: 16.0),
            Bar(-100.0, mark: "3.2.1T"), Bar(100.0, mark: "3.2.1T"),
            Bar(0.0, xEnd: 5000.0)
        });

        Assert.Equal(new[] { 1, 2, 2, 2 }, sets.Select(s => s.Count).OrderBy(c => c).ToArray());
        Assert.All(sets.Where(s => s.Count == 2), s => Assert.Equal(1, s.Bars.Select(b => b.Diameter).Distinct().Count()));
    }

    [Fact]
    public void Uneven_offsets_split_into_evenly_spaced_runs_and_singles()
    {
        // 0, 50, 100 evenly spaced, then 180 alone.
        var sets = KataLongitudinalSetGrouping.Group(new[] { Bar(0.0), Bar(50.0), Bar(100.0), Bar(180.0) });

        Assert.Equal(2, sets.Count);
        Assert.Equal((3, 0.0, 50.0), (sets[0].Count, sets[0].Base.TransverseY, sets[0].Spacing));
        Assert.True(sets[1].IsSingle);
        Assert.Equal(180.0, sets[1].Base.TransverseY);
    }

    [Fact]
    public void Spacing_is_fitted_first_to_last_so_drift_never_exceeds_the_tolerance()
    {
        // Gaps 100, 100.5, 100.5, 100.5: measured from the first gap the fifth bar would drift 1.5 mm.
        var ys = new[] { 0.0, 100.0, 200.5, 301.0, 401.5 };
        var sets = KataLongitudinalSetGrouping.Group(ys.Select(y => Bar(y)));

        foreach (var set in sets)
            for (int i = 0; i < set.Count; i++)
                Assert.True(System.Math.Abs(set.Bars[i].TransverseY - (set.Base.TransverseY + i * set.Spacing)) <= KataLongitudinalSetGrouping.Tolerance);
        Assert.Equal(ys.Length, sets.Sum(s => s.Count));
    }

    [Theory]
    [InlineData(0.49, 1)]
    [InlineData(0.51, 2)]
    public void Uneven_by_more_than_the_tolerance_splits_the_set(double shift, int sets)
    {
        var result = KataLongitudinalSetGrouping.Group(new[] { Bar(0.0), Bar(100.0 + shift), Bar(200.0) });

        Assert.Equal(sets, result.Count);
    }

    [Fact]
    public void Bars_on_one_line_are_singles()
    {
        var sets = KataLongitudinalSetGrouping.Group(new[] { Bar(0.0), Bar(0.0) });

        Assert.Equal(2, sets.Count);
        Assert.All(sets, s => Assert.True(s.IsSingle));
    }

    [Fact]
    public void Every_bar_of_a_plan_lands_in_exactly_one_set_at_its_own_position()
    {
        var table = KataRebarTestSheets.TwoSpans();
        table.Set("C13", "2f20");
        table.Set("C14", "2f18");
        table.Set("E13", "2f20;2f16");
        table.Set("E14", "2f18");
        table.Set("D18", "2f20+1f16");
        table.Set("G4", 12.0);
        table.Set("G5", 2.0);
        var plan = KataRebarPlanner.Plan(KataDamSheetParser.Parse(table), KataRebarTestSheets.MeasuredTwoSpans());
        var bars = plan.Layout.LongitudinalBars.ToList();

        var sets = KataLongitudinalSetGrouping.Group(bars);
        var expanded = sets.SelectMany(s => Enumerable.Range(0, s.Count).Select(i => (s, y: s.Base.TransverseY + i * s.Spacing))).ToList();

        Assert.Equal(bars.Count, expanded.Count);
        Assert.Equal(bars.Count, sets.Sum(s => s.Bars.Count));
        foreach (var set in sets)
            for (int i = 0; i < set.Count; i++)
                Assert.Equal(set.Bars[i].TransverseY, set.Base.TransverseY + i * set.Spacing, KataLongitudinalSetGrouping.Tolerance);

        // Main bars 3Ø20 top / 4Ø20 bottom are one set each; side bars one set of 2 per layer and span.
        Assert.Single(sets, s => s.Base.Role == KataBarRole.MainTop && s.Count == 3);
        // E13 "2f20;2f16": each side its own set, never merged.
        var e13 = sets.Where(s => s.Base.BarMark.StartsWith("3.2.1")).ToList();
        Assert.Equal(new[] { "3.2.1P", "3.2.1T" }, e13.Select(s => s.Base.BarMark).OrderBy(m => m).ToArray());
        Assert.All(e13, s => Assert.Equal(2, s.Count));
        Assert.Single(sets, s => s.Base.Role == KataBarRole.MainBottom && s.Count == 4);
        Assert.All(sets.Where(s => s.Base.Role == KataBarRole.SideBar), s => Assert.Equal(2, s.Count));
        Assert.True(sets.Count < bars.Count);
    }
}
