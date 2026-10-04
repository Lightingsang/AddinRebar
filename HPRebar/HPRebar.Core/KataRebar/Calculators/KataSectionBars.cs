using System;
using System.Collections.Generic;
using System.Linq;
using HPRebar.Core.KataRebar.Models;

namespace HPRebar.Core.KataRebar.Calculators;

internal enum KataSectionFace
{
    Top,
    Bottom,
    Side
}

/// <summary>A bar the section cuts, at the place Kata draws it; <paramref name="RealZ"/> is where it lies.</summary>
internal sealed record KataDrawnBar(KataRebarCurve Bar, double X, double Z, KataSectionFace Face, int Layer, double RealZ);

/// <summary>
/// Where Kata draws the bars of a section (T2-DY7.dwg): the hoop's centre line at the cover from each face, the
/// outer bars touching it (cover + (d + ds) / 2 in from the faces: 38 for Ø18 in Ø8 at 25, where they really lie at
/// 42), each inner layer a bar and 25 clear further in, the bars across a layer spread to the corners as they lie;
/// side bars touching the hoop, their layers spread evenly between the inner faces of the top and bottom bars of the
/// depth they were laid out for (DY14: −217 / −383 at 600 deep, lying at −214 / −386).
/// </summary>
internal sealed class KataSectionBars
{
    private KataSectionBars(double width, double depth, double cover, double stirrup, IReadOnlyList<KataDrawnBar> bars)
    {
        Width = width;
        Depth = depth;
        Cover = cover;
        Stirrup = stirrup;
        Bars = bars;
    }

    public double Width { get; }

    public double Depth { get; }

    /// <summary>Distance from a face to the hoop's centre line.</summary>
    public double Cover { get; }

    /// <summary>Hoop diameter.</summary>
    public double Stirrup { get; }

    /// <summary>The hoop's vertical sides.</summary>
    public double HoopX => Width / 2.0 - Cover;

    public IReadOnlyList<KataDrawnBar> Bars { get; }

    public IEnumerable<KataDrawnBar> On(KataSectionFace face, int layer) => Bars.Where(b => b.Face == face && b.Layer == layer);

    /// <summary>Radius of the hoop's bend round the corner bar of a face: half the bar and half the hoop (13 for Ø18 in Ø8).</summary>
    public double CornerRadius(KataSectionFace face)
    {
        var outer = On(face, 1).ToList();
        double d = outer.Count > 0 ? outer.Max(b => b.Bar.Diameter) : Stirrup * 2.0;
        return (d + Stirrup) / 2.0;
    }

    /// <summary>The height a tie lying at <paramref name="realZ"/> is drawn at: that of the bars it holds.</summary>
    public double DrawnZ(double realZ, bool side) =>
        Bars.Where(b => (b.Face == KataSectionFace.Side) == side).OrderBy(b => Math.Abs(b.RealZ - realZ)).Select(b => b.Z).DefaultIfEmpty(realZ).First();

    public static KataSectionBars Lay(KataBeamRebarSpec spec, KataRebarLayoutResult layout, KataDetailingRules rules, KataSectionCut cut, bool mirror)
    {
        // The section of the span it cuts: its width (row 20) and concrete depth under its top (rows 19, 21).
        double at = cut.X - KataBeamStations.From(spec).SpanStart[cut.SpanIndex];
        double top = spec.TopAt(cut.SpanIndex, at);
        double b = spec.WidthOf(cut.SpanIndex), h = spec.HeightOf(cut.SpanIndex, at), c = rules.StirrupCover, ds = rules.StirrupDiameter;
        var crossing = KataSectionCuts.Crossing(layout, cut.X).ToList();
        var main = crossing.Where(x => x.Bar.Role != KataBarRole.SideBar).ToList();
        var sides = crossing.Where(x => x.Bar.Role == KataBarRole.SideBar).ToList();
        double sign = mirror ? -1.0 : 1.0;

        var drawn = new List<KataDrawnBar>();
        double reach = main.Count == 0 ? 0.0 : main.Max(x => Math.Abs(x.Bar.TransverseY));
        double dMax = main.Count == 0 ? 0.0 : main.Max(x => x.Bar.Diameter);
        double corner = b / 2.0 - c - (dMax + ds) / 2.0;
        foreach (var face in new[] { KataSectionFace.Top, KataSectionFace.Bottom })
        {
            var onFace = main.Where(x => IsTop(x.Bar) == (face == KataSectionFace.Top)).ToList();
            double inward = c;
            foreach (var layer in onFace.GroupBy(x => x.Bar.Layer).OrderBy(g => g.Key))
            {
                double d = layer.Max(x => x.Bar.Diameter);
                double fromFace = inward + (layer.Key == onFace.Min(x => x.Bar.Layer) ? (d + ds) / 2.0 : d / 2.0);
                double z = face == KataSectionFace.Top ? -fromFace : -h + fromFace;
                foreach (var (bar, realZ) in layer)
                    drawn.Add(new KataDrawnBar(bar, sign * (reach > 1e-6 ? bar.TransverseY / reach * corner : 0.0), z, face, layer.Key, realZ));
                inward = fromFace + d / 2.0 + KataSectionStyle.LayerClear;
            }
        }

        drawn.AddRange(Sides(sides, main, rules, b, c, ds, sign, top));
        return new KataSectionBars(b, h, c, ds, drawn);
    }

    private static IEnumerable<KataDrawnBar> Sides(List<(KataRebarCurve Bar, double Z)> sides, List<(KataRebarCurve Bar, double Z)> main,
        KataDetailingRules rules, double b, double c, double ds, double sign, double top)
    {
        if (sides.Count == 0) yield break;

        // The layers lie evenly between the top and bottom bar centres of the depth they were laid out for; Kata
        // spreads them between those bars' inner faces instead.
        double dTop = main.Where(x => IsTop(x.Bar) && x.Bar.Layer == 1).Select(x => x.Bar.Diameter).DefaultIfEmpty(0.0).Max();
        double dBottom = main.Where(x => !IsTop(x.Bar) && x.Bar.Layer == 1).Select(x => x.Bar.Diameter).DefaultIfEmpty(0.0).Max();
        double topReal = rules.TopBarCentreDepth > 0.0 ? -rules.TopBarCentreDepth : -(c + ds + dTop / 2.0);
        // Heights below this section's own top (row 19), the section being drawn from its top.
        double highest = sides.Max(x => x.Z) - top, lowest = sides.Min(x => x.Z) - top;
        double step = topReal - highest;
        double bottomReal = lowest - step;
        double topDrawn = topReal - dTop / 2.0, bottomDrawn = bottomReal + dBottom / 2.0;

        foreach (var (bar, z) in sides)
        {
            double t = Math.Abs(bottomReal - topReal) < 1e-6 ? 0.5 : (z - top - topReal) / (bottomReal - topReal);
            double x = b / 2.0 - c - (bar.Diameter + ds) / 2.0;
            yield return new KataDrawnBar(bar, sign * Math.Sign(bar.TransverseY) * x, topDrawn + t * (bottomDrawn - topDrawn), KataSectionFace.Side, bar.Layer, z);
        }
    }

    private static bool IsTop(KataRebarCurve bar) => bar.Role is KataBarRole.MainTop or KataBarRole.ExtraTop;
}
