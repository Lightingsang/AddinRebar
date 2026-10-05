using System;
using System.Collections.Generic;
using System.Linq;
using HPRebar.Core.BeamRebar.Models;
using HPRebar.Core.KataRebar.Models;

namespace HPRebar.Core.KataRebar.Calculators;

/// <summary>
/// Hanger bars ("vai bò") under a load resting on a span, as Kata draws B01 (2Ø16): level at the top for
/// <see cref="KataDetailingRules.HangerTopLength"/>, down at <see cref="KataDetailingRules.HangerAngleDegrees"/> to the
/// load's underside, level under it from <see cref="KataDetailingRules.FirstStirrupOffset"/> outside one face to as far
/// outside the other, and up again (mid-D: 4000 · 4150 · 5050 ‖ 5550 · 6450 · 6600 round a 900-deep beam 5100…5500; on F
/// under the stub column the bottom is the beam's main bottom bars' level). The bars sit at the level of the main top
/// bars, in the gaps next to the outer ones.
/// A side that does not fit inside the span (HPRebar's rule, no Kata drawing of one): the level end is shortened to
/// the support face (a short of a console tip), then the slope steepens to 60°, then the bar stops on the slope at the
/// face and a warning says so.
/// </summary>
public static class KataHangerBarLayout
{
    public const string Mark = "VB";

    /// <summary>The steeper slope Kata offers for hanger bars (tab "Thép mặc định": 45° or 60°).</summary>
    private const double SteepAngleDegrees = 60.0;

    public static List<KataRebarCurve> Build(KataBeamRebarSpec spec, KataDetailingRules rules, KataBeamStations st, List<string> warnings, ref int barId)
    {
        var bars = new List<KataRebarCurve>();
        double d = rules.HangerBarDiameter;
        if (d <= 0.0 || rules.HangerBarCount <= 0) return bars;

        for (int s = 0; s < spec.Spans.Count && s < st.SpanCount; s++)
        {
            foreach (var load in spec.Spans[s].Loads)
            {
                double top = spec.TopAt(s, load.AtMm);
                double zTop = top - rules.TopBarCentreDepth;
                double zFloor = -spec.DepthOf(s) + rules.BottomBarCentreDepth;
                double zBottom = load.IsColumn
                    ? zFloor
                    : Math.Max(zFloor, top - load.SoffitBelowTopMm - rules.StirrupCover - d / 2.0);
                string where = $"Nhịp {s + 1}: {load.Kind} tại {load.AtMm:0}";
                if (zTop - zBottom <= 0.0)
                {
                    warnings.Add($"{where} không còn chỗ cho thép vai bò — không vẽ.");
                    continue;
                }

                double centre = st.SpanStart[s] + load.AtMm;
                double half = load.WidthMm / 2.0 + rules.FirstStirrupOffset;
                double leftLimit = st.SpanStart[s] + (st.SupportWidth[s] <= 0.0 ? rules.TopEndCover : 0.0);
                double rightLimit = st.SpanEnd[s] - (st.SupportWidth[s + 1] <= 0.0 ? rules.TopEndCover : 0.0);
                var left = Side(rules, centre - half, leftLimit, zTop, zBottom, -1, where, warnings);
                var right = Side(rules, centre + half, rightLimit, zTop, zBottom, +1, where, warnings);
                var path = left.AsEnumerable().Reverse().Concat(right).ToList();

                foreach (double y in Positions(spec, rules, s, d).Take(rules.HangerBarCount))
                {
                    bars.Add(new KataRebarCurve
                    {
                        BarId = barId++,
                        Role = KataBarRole.HangerBar,
                        Diameter = d,
                        Polyline = new Polyline3(path.Select(p => new Point3(p.X, y, p.Z)).ToList()).Simplify(1.0),
                        TransverseY = y,
                        HostSpanIndex = s,
                        ShapeCode = "VB",
                        BarMark = Mark,
                        BarDescription = $"Vai bò {load.Kind} nhịp {s + 1}",
                        DimA = 2.0 * half,
                        DimB = zTop - zBottom,
                        DimC = rules.HangerTopLength,
                        DimR = 2.0 * d,
                        SttCad = 9
                    });
                }
            }
        }

        return bars;
    }

    /// <summary>
    /// One side of the bar from the end of its level bottom outwards: the bottom point, the top of the slope and the end
    /// of the level top, kept inside <paramref name="limit"/>.
    /// </summary>
    private static List<(double X, double Z)> Side(KataDetailingRules rules, double bottomEnd, double limit, double zTop, double zBottom, int outward,
        string where, List<string> warnings)
    {
        double rise = zTop - zBottom;
        if ((limit - bottomEnd) * outward < 0.0)
        {
            // The load reaches the face itself: the level bottom stops there.
            warnings.Add($"{where}: thép vai bò không có chỗ xiên lên trước mặt gối / mút console — chỉ còn đoạn nằm dưới, kiểm tra neo.");
            return new List<(double X, double Z)> { (limit, zBottom) };
        }

        double room = (limit - bottomEnd) * outward;
        double run = rise / Math.Tan(rules.HangerAngleDegrees * Math.PI / 180.0);
        if (run > room + 1e-6) run = rise / Math.Tan(SteepAngleDegrees * Math.PI / 180.0);

        var side = new List<(double X, double Z)> { (bottomEnd, zBottom) };
        if (run > room + 1e-6)
        {
            // Not even the steep slope fits: the bar stops on it at the face.
            side.Add((bottomEnd + outward * room, zBottom + rise * room / run));
            warnings.Add($"{where}: thép vai bò không đủ chỗ xiên lên trước mặt gối — dừng tại mặt gối, kiểm tra neo.");
            return side;
        }

        double level = Math.Min(rules.HangerTopLength, room - run);
        side.Add((bottomEnd + outward * run, zTop));
        if (level > 1.0) side.Add((bottomEnd + outward * (run + level), zTop));
        return side;
    }

    /// <summary>
    /// In the gaps beside the outer top main bars, one each side, when the gap holds a hanger bar; otherwise just
    /// inside the outer bars, one bar gap clear of them.
    /// </summary>
    private static IEnumerable<double> Positions(KataBeamRebarSpec spec, KataDetailingRules rules, int s, double d)
    {
        var main = spec.TopMainOf(s);
        double width = spec.WidthOf(s);
        if (main.IsEmpty)
        {
            double edge = width / 2.0 - rules.StirrupCover - rules.StirrupDiameter - d / 2.0;
            return new[] { -edge, edge };
        }

        var ys = KataRebarCalculator.ComputeTransverseYPositions(width, rules.StirrupCover, rules.StirrupDiameter, main.Diameter, main.Count)
            .OrderBy(y => y).ToList();
        double outer = ys[ys.Count - 1];
        double clear = (main.Diameter + d) / 2.0;
        double gap = ys.Count >= 3 ? (ys[1] - ys[0]) / 2.0 : 0.0;
        double inset = gap >= clear ? gap : clear + rules.BarGap(d);
        return new[] { -(outer - inset), outer - inset };
    }
}
