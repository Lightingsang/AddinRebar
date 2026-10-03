using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using HPRebar.Core.KataRebar.Models;

namespace HPRebar.Core.KataRebar.Calculators;

/// <summary>
/// Kata's section n-n of a beam (<see cref="KataSectionDrawing"/>) as T2-DY7.dwg draws its 27 sections: outline and
/// slab breaks (<see cref="KataSectionLines"/>), the bars where Kata draws them (<see cref="KataSectionBars"/>), hoop
/// and ties, leaders and tags (<see cref="KataSectionTags"/>), the width under the beam, slab and depth left of it,
/// the title under all.
/// </summary>
public static class KataSectionDrawingBuilder
{
    /// <param name="mirror">Seen from the other side (the drawing lists the run backwards): bars across the beam swap sides.</param>
    public static KataSectionDrawing Build(KataBeamRebarSpec spec, KataRebarLayoutResult layout, KataDetailingRules rules, KataSectionCut cut, bool mirror = false)
    {
        if (spec is null) throw new ArgumentNullException(nameof(spec));
        if (layout is null) throw new ArgumentNullException(nameof(layout));
        if (rules is null) throw new ArgumentNullException(nameof(rules));
        if (cut is null) throw new ArgumentNullException(nameof(cut));

        var bars = KataSectionBars.Lay(spec, layout, rules, cut, mirror);
        double b = bars.Width, h = bars.Depth, slab = spec.SlabThickness > 0.0 && spec.SlabThickness < h ? spec.SlabThickness : 0.0;

        var lines = KataSectionLines.Outline(b, h, slab).ToList();
        var tags = new KataSectionTags(bars, slab);
        var hoops = KataSectionCuts.Hoops(layout, cut.SpanIndex, cut.X);
        if (hoops is not null) lines.Add(KataSectionLines.Hoop(bars));

        var sideTies = new List<(KataBarSet Tie, double Z)>();
        foreach (var set in KataSectionCuts.Sets(layout, cut.X).Where(s => s.Shape.Points.Count > 0))
        {
            double realZ = set.Shape.Points[0].Z;
            if (set.ZoneName == KataSideBarLayout.TieZoneName)
            {
                double z = bars.DrawnZ(realZ, side: true);
                if (sideTies.Any(t => Math.Abs(t.Z - z) < 1.0)) continue;
                sideTies.Add((set, z));
                lines.Add(KataSectionLines.Tie(bars, z, straightAbove: true));
            }
            else if (set.ZoneName == KataLayerSpacerTieLayout.ZoneName)
            {
                double z = bars.DrawnZ(realZ, side: false);
                lines.Add(KataSectionLines.Tie(bars, z, straightAbove: false));
                tags.InnerTie(set, z, top: z > -h / 2.0);
            }
        }

        tags.Sides(sideTies);
        tags.Outer(KataSectionFace.Top);
        tags.Inner(KataSectionFace.Top);
        tags.Outer(KataSectionFace.Bottom);
        tags.Inner(KataSectionFace.Bottom);
        if (hoops is not null) tags.Hoop(hoops);
        lines.AddRange(tags.Stubs);

        double dimZ = Math.Min(-h - KataSectionStyle.WidthDimBelow - (tags.SecondBottomRow ? KataSectionStyle.WidthDimSecondRow : 0.0),
            tags.LowestRow - KataSectionStyle.WidthDimUnderTags);
        var dims = Dims(b, h, slab, dimZ);
        var title = new KataDrawingTitle(0.0, dimZ - KataSectionStyle.TitleBelowDim,
            string.Format(CultureInfo.InvariantCulture, "{0}-{0}", cut.Number), KataDrawingStyle.TitleScale);

        var drawnBars = bars.Bars.Select(x => new KataSectionBar(x.X, x.Z, x.Bar.Diameter, x.Bar.BarNumber)).ToList();
        var (minX, maxX, top) = Extents(b, tags);
        double bottom = title.Z - KataDrawingStyle.TitleScaleDrop - KataDrawingStyle.DimTextHeight;
        return new KataSectionDrawing(cut.Number, lines, drawnBars, tags.Leaders, tags.Marks, tags.Tags, dims, title, minX, maxX, top, bottom);
    }

    private static List<KataDrawingDim> Dims(double b, double h, double slab, double dimZ)
    {
        double face = -b / 2.0, chain = face - KataSectionStyle.DepthChainBeyond, whole = face - KataSectionStyle.DepthDimBeyond;
        var dims = new List<KataDrawingDim> { new(face, -h, b / 2.0, -h, false, dimZ) };
        if (slab > 0.0)
        {
            dims.Add(new KataDrawingDim(face, -h, face, -slab, true, chain));
            dims.Add(new KataDrawingDim(face, -slab, face, 0.0, true, chain));
        }

        dims.Add(new KataDrawingDim(face, -h, face, 0.0, true, whole));
        return dims;
    }

    /// <summary>Left, right and top of what is drawn: the tags with their text and circles, the dimensions left of the beam.</summary>
    private static (double MinX, double MaxX, double Top) Extents(double b, KataSectionTags tags)
    {
        double circle = 2.0 * KataTagStyle.CircleRadius;
        double minX = -b / 2.0 - KataSectionStyle.DepthDimBeyond - KataDrawingStyle.DimTextGap - KataDrawingStyle.DimTextHeight;
        double maxX = b / 2.0 + KataSectionStyle.SlabReach + KataSectionStyle.BreakSize;
        double top = KataSectionStyle.BreakSize;
        foreach (var tag in tags.Tags)
        {
            double text = KataTagStyle.CharWidth * tag.Text.Length + KataTagStyle.TextGapLeft;
            double circles = circle * tag.Numbers.Count;
            minX = Math.Min(minX, tag.PointsRight ? tag.X - text : tag.X - circles);
            maxX = Math.Max(maxX, tag.PointsRight ? tag.X + circles : tag.X + text);
            top = Math.Max(top, tag.Z + KataTagStyle.TextLift + KataTagStyle.TextHeight);
            top = Math.Max(top, tag.Z + KataTagStyle.CircleRadius);
        }

        return (minX, maxX, top);
    }
}
