using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using HPRebar.Core.KataRebar.Models;

namespace HPRebar.Core.KataRebar.Calculators;

/// <summary>The inner stirrups' tags (rows 25-44) of <see cref="KataSectionTags"/>.</summary>
internal sealed partial class KataSectionTags
{
    /// <summary>
    /// The inner stirrups (rows 25-44) as Kata tags them on B01, after every other right-hand tag: each U on a level
    /// leader from its right leg ("Ø10" / "a500"), the C ties of one number each on a level leader from its long leg
    /// into one tag ("2xØ10" / "a500"), all in one column right of the beam (<see cref="KataSectionStyle.InnerTagBelowTop"/>).
    /// A C row that would crowd a right-hand tag under it goes over the beam.
    /// </summary>
    /// <param name="inner">Each set with the x its leader starts at.</param>
    public void InnerStirrups(IReadOnlyList<(KataBarSet Set, double X)> inner)
    {
        if (inner.Count == 0) return;
        double insert = HalfWidth + KataSectionStyle.InnerTagBeyond + KataSectionStyle.InnerTagBeyondPerWidth * _bars.Width;
        var right = Tags.Where(t => t.X > 0.0 && t.Z < 0.0).Select(t => t.Z).ToList();
        double row = -KataSectionStyle.InnerTagBelowTop;
        if (right.Any(z => z > row)) row -= KataSectionStyle.InnerTagUnderTopLayer;
        double? reached = right.Where(z => z <= row && z >= -Depth / 2.0 - 1.0).Select(z => (double?)z).Min();
        if (reached is { } side) row = side - KataSectionStyle.InnerTagPitch;
        double bottomFace = _bars.Bars.Where(b => b.Face == KataSectionFace.Bottom).Select(b => b.Z + b.Bar.Diameter / 2.0).DefaultIfEmpty(-Depth).Max();
        // Two layers or more of side bars (one layer: its tag is the reached right-hand tag above).
        var sideLevels = _bars.Bars.Where(b => b.Face == KataSectionFace.Side).Select(b => Math.Round(b.Z, 1)).Distinct().ToList();
        if (sideLevels.Count > 1)
        {
            row = sideLevels.Min() - KataSectionStyle.SideBarInnerTagBelow;
            if (row < bottomFace + KataSectionStyle.InnerTagOverBottomBars) row = sideLevels.Min() + KataSectionStyle.SideBarInnerTagAbove;
        }

        var us = inner.Where(i => i.Set.Role == KataBarRole.StirrupCap).ToList();

        foreach (var (set, x) in us)
        {
            Leaders.Add(Leader(KataLeaderArrow.Closed, KataSectionStyle.ArrowSize, (x, row), (insert, row)));
            Tags.Add(StirrupTag(insert, row, StirrupTexts(1, set.Diameter, set.Spacing), set.BarNumber));
            row -= KataSectionStyle.InnerTagPitch;
        }

        foreach (var group in inner.Where(i => i.Set.Role == KataBarRole.CrossTie).GroupBy(i => i.Set.BarNumber))
        {
            var ties = group.OrderBy(i => i.X).ToList();
            var texts = StirrupTexts(ties.Count, ties[0].Set.Diameter, ties[0].Set.Spacing);
            double at = row;
            bool tooLow = at < bottomFace + KataSectionStyle.InnerTagOverBottomBars;
            if (right.Any(z => z < at && at - z < KataSectionStyle.InnerTagClearance) || tooLow)
            {
                if (tooLow) at = -KataSectionStyle.InnerTagHighLeader;
                double over = KataSectionStyle.InnerTagOverTop, jog = HalfWidth + KataSectionStyle.InnerTagJog;
                foreach (var (_, x) in ties)
                    Leaders.Add(Leader(KataLeaderArrow.Closed, KataSectionStyle.ArrowSize, (x, at), (jog, at), (jog, over), (insert, over)));
                Tags.Add(StirrupTag(insert, over, texts, group.Key));
                continue;
            }

            foreach (var (_, x) in ties)
                Leaders.Add(Leader(KataLeaderArrow.Closed, KataSectionStyle.ArrowSize, (x, at), (insert, at)));
            Tags.Add(StirrupTag(insert, at, texts, group.Key));
            row -= KataSectionStyle.InnerTagPitch;
        }
    }
}
