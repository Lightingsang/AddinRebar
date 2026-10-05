using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using HPRebar.Core.KataRebar.Models;

namespace HPRebar.Core.KataRebar.Calculators;

/// <summary>
/// The leaders and tags of Kata's section (T2-DY7.dwg, rows and insertions in <see cref="KataSectionStyle"/>):
/// <list type="bullet">
/// <item>an outer layer: the bars of the corner number each on a dotted leader up (down) to the first row and left
/// to one tag; each other number to the next row and right;</item>
/// <item>an inner layer: one leader without arrow from its leftmost bar toward the beam centre and right to the tag,
/// a circle round every bar and a stub from each circle to that leader;</item>
/// <item>side bars: a double-dotted line between the bars of each layer and one tag right; their ties tagged left;</item>
/// <item>the inner-layer ties over the top (under the soffit), the hoop on its left side.</item>
/// </list>
/// </summary>
internal sealed class KataSectionTags
{
    private readonly KataSectionBars _bars;
    private readonly double _slab;

    public KataSectionTags(KataSectionBars bars, double slab)
    {
        _bars = bars;
        _slab = slab;
    }

    public List<KataSectionLeader> Leaders { get; } = new();

    public List<KataSectionMark> Marks { get; } = new();

    public List<KataSectionPolyline> Stubs { get; } = new();

    public List<KataSectionTag> Tags { get; } = new();

    /// <summary>A second row of tags hangs under the soffit (the width dimension goes lower).</summary>
    public bool SecondBottomRow { get; private set; }

    /// <summary>Lowest row of the tags under the beam (the width dimension stays under it).</summary>
    public double LowestRow { get; private set; } = double.PositiveInfinity;

    private double HalfWidth => _bars.Width / 2.0;

    private double Depth => _bars.Depth;

    public void Outer(KataSectionFace face)
    {
        var layer = _bars.On(face, 1).ToList();
        if (layer.Count == 0) return;

        bool top = face == KataSectionFace.Top;
        double outermost = layer.Max(b => Math.Abs(b.X));
        int middleRow = 0;
        foreach (var group in layer.GroupBy(b => b.Bar.BarNumber).OrderBy(g => g.Min(b => Math.Abs(Math.Abs(b.X) - outermost))).ThenBy(g => g.Min(b => b.X)))
        {
            bool corner = group.Any(b => Math.Abs(Math.Abs(b.X) - outermost) < 1.0);
            double row;
            if (corner) row = top ? KataSectionStyle.TopCornerRow : -Depth - KataSectionStyle.BottomCornerRow;
            else
            {
                row = top
                    ? KataSectionStyle.TopMiddleRow + middleRow * KataSectionStyle.TopRowPitch
                    : -Depth - KataSectionStyle.BottomMiddleRow - middleRow * KataSectionStyle.BottomRowPitch;
                middleRow++;
                if (!top) SecondBottomRow = true;
            }

            if (!top) LowestRow = Math.Min(LowestRow, row);
            string text = Count(group.Count(), group.First().Bar.Diameter);
            double length = KataTagStyle.LeaderLength(text);
            double insert = corner ? group.Min(b => b.X) - length : group.Max(b => b.X) + length;
            foreach (var bar in group.OrderBy(b => b.X))
                Leaders.Add(Leader(KataLeaderArrow.DotBlank, KataSectionStyle.ArrowSize, (bar.X, bar.Z), (bar.X, row), (insert, row)));
            Tags.Add(new KataSectionTag(insert, row, !corner, text, new[] { group.Key }));
        }
    }

    /// <summary>
    /// Every layer past the first of a face: one leader from its leftmost bar, circles, stubs, a tag right. A further
    /// layer or number keeps a tag's height (<see cref="KataTagStyle.RowPitch"/>) clear of the one before it.
    /// </summary>
    public void Inner(KataSectionFace face)
    {
        bool top = face == KataSectionFace.Top;
        double toward = top ? -1.0 : 1.0;
        double insert = HalfWidth + (top ? KataSectionStyle.TopInnerTagBeyond : KataSectionStyle.BottomInnerTagBeyond);
        double? previous = null;
        foreach (var layer in _bars.Bars.Where(b => b.Face == face && b.Layer > 1).GroupBy(b => b.Layer).OrderBy(g => g.Key))
        {
            foreach (var group in layer.GroupBy(b => b.Bar.BarNumber).OrderBy(g => g.Min(b => b.X)))
            {
                double row = group.First().Z + toward * KataSectionStyle.InnerLayerDrop;
                if (previous is { } last)
                {
                    // Past Kata's drawings (one inner layer): step on, toward the beam centre, past every tag in the way.
                    row = top ? Math.Min(row, last - KataTagStyle.RowPitch) : Math.Max(row, last + KataTagStyle.RowPitch);
                    while (Tags.Any(t => Math.Abs(t.X - insert) < 1.0 && Math.Abs(t.Z - row) < KataTagStyle.RowPitch - 1e-6))
                        row += toward * KataTagStyle.RowPitch;
                }

                previous = row;
                var ordered = group.OrderBy(b => b.X).ToList();
                Leaders.Add(Leader(KataLeaderArrow.None, KataSectionStyle.ArrowSize, (ordered[0].X, ordered[0].Z), (ordered[0].X, row), (insert, row)));
                foreach (var bar in ordered)
                {
                    Marks.Add(new KataSectionMark(bar.X, bar.Z, KataSectionStyle.MarkRadius));
                    if (bar != ordered[0])
                        Stubs.Add(KataSectionLines.Line(KataDrawingPen.Thin, (bar.X, bar.Z + toward * KataSectionStyle.MarkRadius), (bar.X, row)));
                }

                Tags.Add(new KataSectionTag(insert, row, true, Count(ordered.Count, ordered[0].Bar.Diameter), new[] { group.Key }));
            }
        }
    }

    /// <summary>The tie of an inner layer at <paramref name="z"/> (its straight part under the bars).</summary>
    public void InnerTie(KataBarSet tie, double z, bool top)
    {
        double corner = _bars.Bars.Where(b => b.Face != KataSectionFace.Side).Select(b => Math.Abs(b.X)).DefaultIfEmpty(_bars.HoopX).Max();
        double foot = -corner / 2.0, straight = z - KataSectionStyle.TieBendDiameters * _bars.Stirrup;
        double row = top ? KataSectionStyle.TopTieRow : -Depth - KataSectionStyle.BottomTieRow;
        double insert = -HalfWidth - KataSectionStyle.InnerTieTagBeyond;
        if (!top)
        {
            SecondBottomRow = true;
            LowestRow = Math.Min(LowestRow, row);
        }

        Leaders.Add(Leader(KataLeaderArrow.Closed, KataSectionStyle.ArrowSize, (foot, straight), (foot, row), (insert, row)));
        Tags.Add(StirrupTag(insert, row, StirrupTexts(1, tie.Diameter, tie.Spacing), tie.BarNumber));
    }

    /// <summary>
    /// The inner stirrups as B01 section 2-2 tags them: each U from its right leg to the right ("Ø10" / "a500"), the
    /// C ties of one number together from the top of the rightmost over the beam to the right ("2xØ10" / "a500").
    /// </summary>
    public void InnerStirrups(IReadOnlyList<(KataBarSet Set, double X, double Z)> inner)
    {
        double insert = HalfWidth + KataSectionStyle.SideTieTagBeyond;
        foreach (var (set, x, z) in inner.Where(i => i.Set.Role == KataBarRole.StirrupCap))
        {
            Leaders.Add(Leader(KataLeaderArrow.Closed, KataSectionStyle.ArrowSize, (x, z), (insert, z)));
            Tags.Add(StirrupTag(insert, z, StirrupTexts(1, set.Diameter, set.Spacing), set.BarNumber));
        }

        foreach (var group in inner.Where(i => i.Set.Role == KataBarRole.CrossTie).GroupBy(i => i.Set.BarNumber))
        {
            var right = group.OrderBy(i => i.X).Last();
            double row = KataSectionStyle.TopTieRow;
            Leaders.Add(Leader(KataLeaderArrow.Closed, KataSectionStyle.ArrowSize, (right.X, right.Z), (right.X, row), (insert, row)));
            Tags.Add(StirrupTag(insert, row, StirrupTexts(group.Count(), right.Set.Diameter, right.Set.Spacing), group.Key));
        }
    }

    /// <summary>The side bars and their ties: <paramref name="ties"/> one per layer, at the layers' drawn heights.</summary>
    public void Sides(IReadOnlyList<(KataBarSet Tie, double Z)> ties)
    {
        var sides = _bars.Bars.Where(b => b.Face == KataSectionFace.Side).ToList();
        if (sides.Count == 0) return;

        var layers = sides.GroupBy(b => Math.Round(b.Z)).OrderByDescending(g => g.Key).ToList();
        foreach (var layer in layers)
        {
            var ordered = layer.OrderBy(b => b.X).ToList();
            if (ordered.Count < 2) continue;
            var (a, z) = (ordered[0].X, ordered[0].Z);
            double b = ordered[ordered.Count - 1].X;
            Leaders.Add(Leader(KataLeaderArrow.DotBlank, KataSectionStyle.SmallArrowSize, (a, z), (b, z)));
            Leaders.Add(Leader(KataLeaderArrow.DotBlank, KataSectionStyle.SmallArrowSize, (b, z), (a, z)));
        }

        var first = sides[0];
        int perLayer = layers.Max(l => l.Count());
        string text = layers.Count > 1 ? $"{layers.Count}x{Count(perLayer, first.Bar.Diameter)}" : Count(perLayer, first.Bar.Diameter);
        double highest = layers[0].Key, lowest = layers[layers.Count - 1].Key;
        if (layers.Count == 1)
        {
            double right = sides.Max(s => s.X);
            double insert = HalfWidth + Math.Max(KataSectionStyle.SideBarTagMinBeyond,
                KataSectionStyle.SideBarTagBeyond - KataSectionStyle.SideBarTagPerDepth * (Depth - 500.0));
            Leaders.Add(Leader(KataLeaderArrow.DotSmall, KataSectionStyle.ArrowSize, (right, highest), (insert, highest)));
            Tags.Add(new KataSectionTag(insert, highest, true, text, new[] { first.Bar.BarNumber }));
        }
        else
        {
            double joint = (highest + lowest) / 2.0, foot = KataSectionStyle.SideBarsFoot, insert = HalfWidth + KataSectionStyle.SideBarsTagBeyond;
            Leaders.Add(Leader(KataLeaderArrow.DotSmall, KataSectionStyle.ArrowSize, (foot, highest), (foot, joint), (insert, joint)));
            foreach (var layer in layers.Skip(1))
                Leaders.Add(Leader(KataLeaderArrow.DotSmall, KataSectionStyle.ArrowSize, (foot, layer.Key), (foot, joint)));
            Tags.Add(new KataSectionTag(insert, joint, true, text, new[] { first.Bar.BarNumber }));
        }

        SideTies(ties);
    }

    private void SideTies(IReadOnlyList<(KataBarSet Tie, double Z)> ties)
    {
        if (ties.Count == 0) return;
        double lift = KataSectionStyle.TieBendDiameters * _bars.Stirrup;
        var levels = ties.Select(t => t.Z + lift).Distinct().OrderByDescending(z => z).ToList();
        var tie = ties[0].Tie;
        if (levels.Count == 1)
        {
            double foot = -KataSectionStyle.SideTieFoot, row = levels[0] - lift + KataSectionStyle.SideTieRise;
            double insert = -HalfWidth - KataSectionStyle.SideTieTagBeyond;
            Leaders.Add(Leader(KataLeaderArrow.Closed, KataSectionStyle.ArrowSize, (foot, levels[0]), (foot, row), (insert, row)));
            Tags.Add(StirrupTag(insert, row, StirrupTexts(1, tie.Diameter, tie.Spacing), tie.BarNumber));
            return;
        }

        double x = -KataSectionStyle.SideTiesFoot, joint = (levels[0] + levels[levels.Count - 1]) / 2.0;
        double at = -HalfWidth - KataSectionStyle.SideTiesTagBeyond;
        Leaders.Add(Leader(KataLeaderArrow.Closed, KataSectionStyle.ArrowSize, (x, levels[0]), (x, joint), (at, joint)));
        foreach (double z in levels.Skip(1)) Leaders.Add(Leader(KataLeaderArrow.Closed, KataSectionStyle.ArrowSize, (x, z), (x, joint)));
        Tags.Add(StirrupTag(at, joint, StirrupTexts(levels.Count, tie.Diameter, tie.Spacing), tie.BarNumber));
    }

    /// <summary>The hoop's tag on its left side.</summary>
    public void Hoop(KataStirrupZoneResult hoops)
    {
        int layers = _bars.Bars.Where(b => b.Face == KataSectionFace.Side).Select(b => Math.Round(b.Z)).Distinct().Count();
        double z = layers == 0
            ? (-_slab - Depth) / 2.0
            : -KataSectionStyle.HoopTagDepthRatio * Depth + KataSectionStyle.HoopTagLift - KataSectionStyle.HoopTagPerLayer * (layers - 1);
        double x = -_bars.HoopX, insert = x - KataSectionStyle.HoopTagLeader;
        Leaders.Add(Leader(KataLeaderArrow.Closed, KataSectionStyle.ArrowSize, (x, z), (insert, z)));
        Tags.Add(StirrupTag(insert, z, StirrupTexts(1, _bars.Stirrup, hoops.LabelSpacing), hoops.BarNumber));
    }

    /// <summary>Kata's stirrup tag (P12): the bars on the leader, the spacing under it, the circle before the insertion.</summary>
    private static KataSectionTag StirrupTag(double insert, double row, (string Bars, string Spacing) texts, int number) =>
        new(insert, row, false, texts.Bars, new[] { number }, texts.Spacing);

    /// <summary>"Ø8" and "a500" of a stirrup or tie, "2xØ8" when <paramref name="layers"/> layers share the tag.</summary>
    private static (string Bars, string Spacing) StirrupTexts(int layers, double diameter, double spacing)
    {
        string bars = string.Format(CultureInfo.InvariantCulture, "Ø{0:0}", diameter);
        string spaced = string.Format(CultureInfo.InvariantCulture, "a{0:0}", spacing);
        return (layers > 1 ? $"{layers}x{bars}" : bars, spaced);
    }

    private static string Count(int count, double diameter) => string.Format(CultureInfo.InvariantCulture, "{0}Ø{1:0}", count, diameter);

    private static KataSectionLeader Leader(KataLeaderArrow arrow, double size, params (double X, double Z)[] points) => new(points, arrow, size);
}
