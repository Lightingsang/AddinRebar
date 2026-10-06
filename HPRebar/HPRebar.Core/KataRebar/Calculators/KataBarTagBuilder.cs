using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using HPRebar.Core.KataRebar.Models;

namespace HPRebar.Core.KataRebar.Calculators;

public enum KataTagKind
{
    Bars,
    SideBars,
    Stirrups
}

/// <summary>
/// One elevation tag as Kata draws it (kata_block_KHT on a LEADER), in model millimetres: the leader starts with an
/// arrow on each bar of <see cref="FootZ"/> at station <see cref="X"/>, rises (or drops) to the row at
/// <see cref="RowZ"/> and runs <see cref="LeaderLength"/> sideways to the insertion point; the text sits on that
/// horizontal part, the numbers in circles beyond it. A stirrup tag has no leader: <see cref="X"/> is its insertion.
/// </summary>
/// <param name="Numbers">Bar numbers, one circle each.</param>
/// <param name="Text">"2Ø18+1Ø18", "3Ø18", "2x2Ø12", "Ø8a100".</param>
/// <param name="X">Local station of the leader (of the insertion for a stirrup tag), mm.</param>
/// <param name="RowZ">Level of the leader's horizontal part (beam top = 0, up positive), mm.</param>
/// <param name="Above">Over the beam (top bars, stirrups) or under it.</param>
/// <param name="PointsLeft">The horizontal part runs to the left (Kata's "P" tags at the end cut of a span).</param>
/// <param name="FootZ">Levels of the bars the leader starts from (two for two layers of side bars).</param>
/// <param name="LeaderLength">Length of the horizontal part, mm (0 for a stirrup tag).</param>
public sealed record KataBarTag(
    IReadOnlyList<int> Numbers,
    string Text,
    double X,
    double RowZ,
    bool Above,
    bool PointsLeft,
    IReadOnlyList<double> FootZ,
    double LeaderLength,
    KataTagKind Kind = KataTagKind.Bars)
{
    /// <summary>Where the leader ends and the circles begin.</summary>
    public double InsertX => PointsLeft ? X - LeaderLength : X + LeaderLength;
}

/// <summary>
/// The elevation tags of Kata's drawings (T2-DY7, T2-DY14 and its E = 500 variant):
/// <list type="bullet">
/// <item>at each section cut (<see cref="KataSectionCuts"/>) one tag per level of bars crossing it, top bars over the
/// beam, bottom bars under it; the level farthest from the face takes the first row, the next one the row beyond;
/// a level's tag lists the main bars first ("1+4 2Ø18+1Ø18"); the tags of a span's end cut point left;</item>
/// <item>in a span shorter than 3 m the start cut is tagged in full, the middle cut on its top bars only and the end
/// cut not at all (both drawings agree; Kata's own rule is unknown);</item>
/// <item>the side bars once per span, a third of the way from the start cut to the middle cut, under the beam, one
/// leader per layer ("13 2Ø12", "15 2x2Ø12");</item>
/// <item>each stirrup zone over the beam, 125 mm past its middle, no leader ("15 Ø8a100").</item>
/// </list>
/// </summary>
public static class KataBarTagBuilder
{
    private const double ShortSpan = 3000.0;

    /// <summary>Row of the stirrup tags among <paramref name="tags"/>; Kata's usual row when there are none.</summary>
    public static double StirrupRowOf(IReadOnlyList<KataBarTag> tags) =>
        tags.Where(t => t.Kind == KataTagKind.Stirrups).Select(t => t.RowZ).DefaultIfEmpty(KataTagStyle.StirrupRow).Max();

    public static IReadOnlyList<KataBarTag> Build(KataBeamRebarSpec spec, KataRebarLayoutResult layout, double stirrupDiameter)
    {
        if (spec is null) throw new ArgumentNullException(nameof(spec));
        if (layout is null) throw new ArgumentNullException(nameof(layout));

        var st = KataBeamStations.From(spec);
        var tags = new List<KataBarTag>();
        int topRows = 0;
        for (int s = 0; s < st.SpanCount; s++)
        {
            double depth = spec.DepthOf(s);
            var cuts = Classify(st, s);
            foreach (var (x, place) in cuts)
            {
                bool shortSpan = st.SpanEnd[s] - st.SpanStart[s] < ShortSpan && cuts.Count == 3;
                if (shortSpan && place == Place.End) continue;
                bool left = place == Place.End;
                var top = AtCut(layout, x, depth, above: true, left, stirrupDiameter).ToList();
                topRows = Math.Max(topRows, top.Count);
                tags.AddRange(top);
                if (!(shortSpan && place == Place.Middle)) tags.AddRange(AtCut(layout, x, depth, above: false, left, stirrupDiameter));
            }

            if (SideTag(layout, cuts, depth) is { } side) tags.Add(side);
        }

        // Two rows of bar tags leave the stirrup row where Kata has it; a third or fourth level pushes it out.
        double stirrupRow = Math.Max(KataTagStyle.StirrupRow,
            KataTagStyle.FirstRowAbove + (topRows - 1) * KataTagStyle.RowPitch + KataTagStyle.StirrupOverLastRow);
        foreach (var run in KataStirrupRuns.Of(layout))
        {
            tags.Add(new KataBarTag(new[] { run.Number }, $"Ø{Dia(run.Diameter > 0.0 ? run.Diameter : stirrupDiameter)}a{run.Spacing:0}",
                (run.First + run.Last) / 2.0 + KataTagStyle.StirrupShift, stirrupRow, Above: true, PointsLeft: false,
                Array.Empty<double>(), 0.0, KataTagKind.Stirrups));
        }

        return tags;
    }

    /// <summary>
    /// Room the tags take over the highest beam top and under the band bottom <paramref name="bandDepth"/> below it
    /// (outermost row and its circles), mm.
    /// </summary>
    public static (double Above, double Below) Band(IReadOnlyList<KataBarTag> tags, double bandDepth)
    {
        if (tags is null) throw new ArgumentNullException(nameof(tags));
        double above = tags.Where(t => t.Above).Select(t => t.RowZ + KataTagStyle.CircleRadius).DefaultIfEmpty(0.0).Max();
        double below = tags.Where(t => !t.Above).Select(t => -t.RowZ - bandDepth + KataTagStyle.CircleRadius).DefaultIfEmpty(0.0).Max();
        return (Math.Max(0.0, above), Math.Max(0.0, below));
    }

    private enum Place
    {
        Start,
        Middle,
        End
    }

    /// <summary>The cuts of span <paramref name="s"/> and which one each is (a cantilever lacks the one at its tip).</summary>
    private static List<(double X, Place Place)> Classify(KataBeamStations st, int s)
    {
        double mid = (st.SpanStart[s] + st.SpanEnd[s]) / 2.0;
        return KataSectionCuts.Stations(st, s)
            .Select(x => (x, x < mid - 200.0 ? Place.Start : x > mid + 200.0 ? Place.End : Place.Middle))
            .ToList();
    }

    /// <summary>One tag per level of top (or bottom) bars crossing the cut at <paramref name="x"/>.</summary>
    private static IEnumerable<KataBarTag> AtCut(KataRebarLayoutResult layout, double x, double depth, bool above, bool left, double stirrupDiameter)
    {
        var levels = KataSectionCuts.Crossing(layout, x)
            .Where(c => above ? c.Bar.Role is KataBarRole.MainTop or KataBarRole.ExtraTop : c.Bar.Role is KataBarRole.MainBottom or KataBarRole.ExtraBottom)
            .GroupBy(c => Math.Round(c.Z))
            // The level farthest from the face (inside the beam) takes the row next to it.
            .OrderBy(g => above ? g.Key : -g.Key)
            .ToList();

        for (int row = 0; row < levels.Count; row++)
        {
            var bars = levels[row].Select(c => c.Bar).ToList();
            var parts = bars
                .GroupBy(b => b.BarNumber)
                .OrderBy(g => g.Any(b => b.Role is KataBarRole.MainTop or KataBarRole.MainBottom) ? 0 : 1)
                .ThenBy(g => g.Key)
                .Select(g => (Number: g.Key, Count: g.Select(b => Math.Round(b.TransverseY)).Distinct().Count(), g.First().Diameter))
                .ToList();
            string text = string.Join("+", parts.Select(p => $"{p.Count}Ø{Dia(p.Diameter)}"));
            double rowZ = above
                ? KataTagStyle.FirstRowAbove + row * KataTagStyle.RowPitch
                : -depth - KataTagStyle.FirstRowBelow - row * KataTagStyle.RowPitch;
            yield return new KataBarTag(parts.Select(p => p.Number).ToList(), text, x, rowZ, above, left,
                new[] { KataDrawingLevels.Drawn(levels[row].First().Bar, levels[row].First().Z, stirrupDiameter) }, KataTagStyle.LeaderLength(text));
        }
    }

    /// <summary>The span's side bars, a third of the way from its start cut to its middle cut.</summary>
    private static KataBarTag? SideTag(KataRebarLayoutResult layout, List<(double X, Place Place)> cuts, double depth)
    {
        var start = cuts.Where(c => c.Place == Place.Start).Select(c => (double?)c.X).FirstOrDefault();
        var middle = cuts.Where(c => c.Place == Place.Middle).Select(c => (double?)c.X).FirstOrDefault();
        if (start is null || middle is null) return null;

        double x = start.Value + (middle.Value - start.Value) / 3.0;
        var side = KataSectionCuts.Crossing(layout, x).Where(c => c.Bar.Role == KataBarRole.SideBar).ToList();
        if (side.Count == 0) return null;

        var feet = side.Select(c => Math.Round(c.Z, 1)).Distinct().OrderByDescending(z => z).ToList();
        double d = side[0].Bar.Diameter;
        string text = feet.Count > 1 ? $"{feet.Count}x2Ø{Dia(d)}" : $"2Ø{Dia(d)}";
        var numbers = side.Select(c => c.Bar.BarNumber).Distinct().OrderBy(n => n).ToList();
        return new KataBarTag(numbers, text, x, -depth - KataTagStyle.FirstRowBelow, Above: false, PointsLeft: false,
            feet, KataTagStyle.LeaderLength(text), KataTagKind.SideBars);
    }

    private static string Dia(double d) => d.ToString("0.#", CultureInfo.InvariantCulture);
}
