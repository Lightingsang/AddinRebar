using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using HPRebar.Core.BeamRebar.Models;
using HPRebar.Core.KataRebar.Models;

namespace HPRebar.Core.KataRebar.Calculators;

/// <summary>
/// Kata's bar numbers ("số hiệu"), in the order its drawings use (T2-DY7, T2-DY14, B01): the top main bars, the bottom
/// main bar runs from left to right, the additional top bars support by support (row 13 to 16) — a support numbers its
/// row 17 there too, after its top bars: of no width (B01 I17: 12, between G's 11 and K's 13) or with one (B03: E's
/// 6Ø25 run on into span 2 is 11, I's 2Ø20 16 and 17) —, the additional bottom bars span by
/// span (row 18 then 17), the side bars, then span by span its C ties, its hoops and its inner stirrups (B01: 22 tie,
/// 23 hoop, 24 U, 25 C in span D … 33 tie, 34 hoop, 35 C in the console), the hanger bars last.
/// A bar identical to one already numbered — same diameter, same shape and dimensions within ±1 mm, wherever it
/// sits (Kata's E13 1Ø18 and F17 3Ø18 of 4650 are both 6) — takes that number, as Kata does while
/// "Cho phép các thanh thép giống nhau đánh số hiệu khác nhau" is off.
/// </summary>
public static class KataBarNumbering
{
    /// <param name="spec">The beam: where its supports and joints are, how wide each span is (a tie's length).</param>
    public static KataRebarLayoutResult Apply(KataRebarLayoutResult layout, double stirrupDiameter, KataBeamRebarSpec? spec = null)
    {
        if (layout is null) throw new ArgumentNullException(nameof(layout));
        var st = spec is null ? null : KataBeamStations.From(spec);
        double? JointOf(KataRebarCurve bar) =>
            st is not null && bar.HostSpanIndex >= 0 && bar.HostSpanIndex < spec!.Spans.Count && spec.Spans[bar.HostSpanIndex].BottomExtraJointAtMm is { } at
                ? st.SpanStart[bar.HostSpanIndex] + at
                : null;
        double SupportAt(int k) => st is not null && k >= 0 && k < st.SupportStart.Length ? (st.SupportStart[k] + st.SupportEnd[k]) / 2.0 : k;
        // Row 17 over a support with a width (KataSupportBottomBarLayout): laid over it, or run on through it into the span.
        double? OverSupport(KataRebarCurve bar) =>
            JointOf(bar) is { } joint ? joint
            : bar.HostSupportIndex >= 0 ? SupportAt(bar.HostSupportIndex)
            : bar.BarMark.EndsWith(KataSupportBottomBarLayout.ThroughSuffix, StringComparison.Ordinal) ? SupportAt(bar.HostSpanIndex)
            : null;

        var known = new List<(Shape Shape, int Number)>();
        int Number(IReadOnlyList<Shape> forms)
        {
            foreach (var (shape, n) in known)
                if (forms.Any(f => f.Matches(shape))) return n;
            known.Add((forms[0], known.Count + 1));
            return known.Count;
        }

        // Numbers go in Kata's order; each list keeps its own order.
        List<KataRebarCurve> InOrder(IReadOnlyList<KataRebarCurve> bars, Func<KataRebarCurve, double> group, Func<KataRebarCurve, int> layer,
            Func<KataRebarCurve, bool>? take = null)
        {
            var result = bars.ToArray();
            foreach (int i in Enumerable.Range(0, bars.Count).Where(i => take?.Invoke(bars[i]) ?? true)
                         .OrderBy(i => group(bars[i])).ThenBy(i => layer(bars[i])).ThenBy(i => bars[i].Polyline.Points.Min(p => p.X)).ThenBy(i => i))
                result[i] = bars[i] with { BarNumber = Number(Signature(bars[i])) };
            return result.ToList();
        }

        var top = InOrder(layout.MainTopBars, _ => 0, _ => 0);
        var bottom = InOrder(layout.MainBottomBars, _ => 0, _ => 0);

        // The supports' bars in station order: rows 13-16 of each support and rows 17 / 18 of a support of no width.
        var supportBars = layout.ExtraTopBars.Select(b => (Bar: b, At: SupportAt(b.HostSupportIndex)))
            .Concat(layout.ExtraBottomBars.Where(b => OverSupport(b) is not null).Select(b => (Bar: b, At: OverSupport(b)!.Value)))
            .ToList();
        var numbered = new Dictionary<int, int>();
        foreach (var (bar, _) in supportBars.OrderBy(x => x.At).ThenBy(x => x.Bar.Role == KataBarRole.ExtraBottom ? 1 : 0)
                     .ThenBy(x => x.Bar.Layer).ThenBy(x => x.Bar.Polyline.Points.Min(p => p.X)))
            numbered[bar.BarId] = Number(Signature(bar, JointOf(bar) is null ? "" : "joint"));
        var extraTop = layout.ExtraTopBars.Select(b => b with { BarNumber = numbered[b.BarId] }).ToList();
        var extraBottom = InOrder(layout.ExtraBottomBars, b => b.HostSpanIndex, b => b.Layer, b => OverSupport(b) is null)
            .Select(b => numbered.TryGetValue(b.BarId, out int n) && OverSupport(b) is not null ? b with { BarNumber = n } : b).ToList();
        var side = InOrder(layout.SideBars, _ => 0, b => b.Layer);

        // Span by span: its C ties (one number per diameter and span width: DY7 14 holds side-bar and layer ties
        // alike, B01's 500 and 300 spans have 22 and 33), its hoops, its inner stirrups.
        var sets = layout.BarSets.ToArray();
        var zones = layout.StirrupZones.ToArray();
        foreach (int s in sets.Select(x => x.SpanIndex).Concat(zones.Select(z => z.SpanIndex)).Distinct().OrderBy(x => x).ToList())
        {
            for (int i = 0; i < sets.Length; i++)
                if (sets[i].SpanIndex == s && IsTie(sets[i]))
                    sets[i] = sets[i] with { BarNumber = Number(new[] { new Shape($"tie|{Mm(sets[i].Diameter)}|{Mm(spec?.WidthOf(s) ?? 0.0)}", Array.Empty<double>()) }) };
            foreach (int i in Enumerable.Range(0, zones.Length).Where(i => zones[i].SpanIndex == s).OrderBy(i => zones[i].ZoneIndex).ThenBy(i => i))
                zones[i] = zones[i] with { BarNumber = Number(Signature(zones[i], stirrupDiameter)) };
            for (int i = 0; i < sets.Length; i++)
                if (sets[i].SpanIndex == s && !IsTie(sets[i]))
                    sets[i] = sets[i] with { BarNumber = Number(Signature(sets[i])) };
        }

        var hangers = InOrder(layout.HangerBars, b => b.HostSpanIndex, _ => 0);
        var stirrups = layout.IndividualStirrups
            .Select(s => ZoneOf(zones, s) is { } zone ? s with { BarNumber = zone.BarNumber } : s)
            .ToList();

        return layout with
        {
            MainTopBars = top,
            MainBottomBars = bottom,
            ExtraTopBars = extraTop,
            ExtraBottomBars = extraBottom,
            SideBars = side,
            HangerBars = hangers,
            BarSets = sets.ToList(),
            StirrupZones = zones.ToList(),
            IndividualStirrups = stirrups
        };
    }

    /// <summary>The zone a single stirrup belongs to: of its span, the one whose stations reach nearest its own.</summary>
    private static KataStirrupZoneResult? ZoneOf(IReadOnlyList<KataStirrupZoneResult> zones, KataRebarCurve stirrup)
    {
        double x = stirrup.Polyline.Points.Count > 0 ? stirrup.Polyline.Points[0].X : 0.0;
        return zones
            .Where(z => z.SpanIndex == stirrup.HostSpanIndex && z.Stations.Count > 0)
            .OrderBy(z => x < z.Stations[0] ? z.Stations[0] - x : x > z.Stations[z.Stations.Count - 1] ? x - z.Stations[z.Stations.Count - 1] : 0.0)
            .FirstOrDefault();
    }

    /// <summary>The C ties under bar layers and round side bars (not the inner stirrups of rows 25-44).</summary>
    public static bool IsTie(KataBarSet set) =>
        set.ZoneName == KataSideBarLayout.TieZoneName || set.ZoneName == KataLayerSpacerTieLayout.ZoneName;

    /// <summary>Largest difference (mm) between two measures of bars Kata would call identical.</summary>
    public const double Tolerance = 1.0;

    /// <summary>A shape: its exact attributes (kind, diameter, hook angles) and its measures (mm), compared within <see cref="Tolerance"/>.</summary>
    private sealed record Shape(string Kind, IReadOnlyList<double> Values)
    {
        public bool Matches(Shape other) =>
            Kind == other.Kind
            && Values.Count == other.Values.Count
            && Values.Zip(other.Values, (a, b) => Math.Abs(a - b)).All(d => d <= Tolerance + 1e-6);
    }

    /// <summary>
    /// A bar's shape seen four ways (as drawn, end for end, upside down, both), each with its hooks in the order of that
    /// view, so a bar and its mirror image match.
    /// </summary>
    /// <param name="kind">Bars of another kind never share a number (a joint's bars with a span's).</param>
    private static IReadOnlyList<Shape> Signature(KataRebarCurve bar, string kind = "")
    {
        var points = bar.Polyline.Points;
        var reversed = points.Reverse().ToList();
        Shape Form(IReadOnlyList<Point3> pts, double sx, double sz, (HookAngle Angle, double Length) first, (HookAngle Angle, double Length) last)
        {
            var o = pts[0];
            var values = pts.SelectMany(p => new[] { sx * (p.X - o.X), sz * (p.Z - o.Z) }).Concat(new[] { first.Length, last.Length }).ToList();
            return new Shape($"bar{kind}|{Mm(bar.Diameter)}|{pts.Count}|{(int)first.Angle}|{(int)last.Angle}", values);
        }

        var start = (bar.StartHookAngle, bar.StartHookLength);
        var end = (bar.EndHookAngle, bar.EndHookLength);
        return new[]
        {
            Form(points, 1, 1, start, end), Form(points, 1, -1, start, end),
            Form(reversed, -1, 1, end, start), Form(reversed, -1, -1, end, start)
        };
    }

    private static IReadOnlyList<Shape> Signature(KataBarSet set)
    {
        var points = set.Shape.Points;
        var o = points.Count > 0 ? points[0] : new Point3(0, 0, 0);
        var values = points.SelectMany(p => new[] { p.Y - o.Y, p.Z - o.Z }).ToList();
        return new[] { new Shape($"set|{Mm(set.Diameter)}|{points.Count}|{set.HookAngle}|{set.HookFactor.ToString(CultureInfo.InvariantCulture)}|{set.WrapEnds}", values) };
    }

    private static IReadOnlyList<Shape> Signature(KataStirrupZoneResult zone, double diameter) =>
        new[] { new Shape($"hoop|{zone.StirrupType}|{Mm(diameter)}", new[] { zone.OutToOutWidth, zone.OutToOutHeight }) };

    /// <summary>A nominal size (diameter) as text, never "-0".</summary>
    private static string Mm(double value) => ((long)Math.Round(value)).ToString(CultureInfo.InvariantCulture);
}
