using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using HPRebar.Core.BeamRebar.Models;
using HPRebar.Core.KataRebar.Models;

namespace HPRebar.Core.KataRebar.Calculators;

/// <summary>
/// Kata's bar numbers ("số hiệu"), in the order its drawings use (T2-DY7 / T2-DY14): the top main bars, the bottom
/// main bar runs from left to right, the additional top bars support by support (row 13 to 16), the additional bottom
/// bars span by span (row 18 then 17), the side bars, every C tie, the inner stirrups, then the hoops span by span.
/// A bar identical to one already numbered — same diameter, same shape and dimensions within ±1 mm, wherever it
/// sits (Kata's E13 1Ø18 and F17 3Ø18 of 4650 are both 6) — takes that number, as Kata does while
/// "Cho phép các thanh thép giống nhau đánh số hiệu khác nhau" is off.
/// </summary>
public static class KataBarNumbering
{
    public static KataRebarLayoutResult Apply(KataRebarLayoutResult layout, double stirrupDiameter)
    {
        if (layout is null) throw new ArgumentNullException(nameof(layout));

        var known = new List<(Shape Shape, int Number)>();
        int Number(IReadOnlyList<Shape> forms)
        {
            foreach (var (shape, n) in known)
                if (forms.Any(f => f.Matches(shape))) return n;
            known.Add((forms[0], known.Count + 1));
            return known.Count;
        }

        // Numbers go in Kata's order; each list keeps its own order.
        List<KataRebarCurve> InOrder(IReadOnlyList<KataRebarCurve> bars, Func<KataRebarCurve, int> group, Func<KataRebarCurve, int> layer)
        {
            var result = bars.ToArray();
            foreach (int i in Enumerable.Range(0, bars.Count)
                         .OrderBy(i => group(bars[i])).ThenBy(i => layer(bars[i])).ThenBy(i => bars[i].Polyline.Points.Min(p => p.X)).ThenBy(i => i))
                result[i] = bars[i] with { BarNumber = Number(Signature(bars[i])) };
            return result.ToList();
        }

        var top = InOrder(layout.MainTopBars, _ => 0, _ => 0);
        var bottom = InOrder(layout.MainBottomBars, _ => 0, _ => 0);
        var extraTop = InOrder(layout.ExtraTopBars, b => b.HostSupportIndex, b => b.Layer);
        var extraBottom = InOrder(layout.ExtraBottomBars, b => b.HostSpanIndex, b => b.Layer);
        var side = InOrder(layout.SideBars, _ => 0, b => b.Layer);

        // Every C tie of one diameter shares a number (DY7 14: side-bar ties and layer ties alike); the inner
        // stirrups follow, identical ones sharing theirs.
        var sets = layout.BarSets.Select(set => IsTie(set)
            ? set with { BarNumber = Number(new[] { new Shape($"tie|{Mm(set.Diameter)}", Array.Empty<double>()) }) }
            : set).ToList();
        sets = sets.Select(set => set.BarNumber > 0 ? set : set with { BarNumber = Number(Signature(set)) }).ToList();

        var zones = layout.StirrupZones
            .Select((zone, index) => (zone, index))
            .OrderBy(z => z.zone.SpanIndex).ThenBy(z => z.zone.ZoneIndex).ThenBy(z => z.index)
            .Select(z => (z.index, zone: z.zone with { BarNumber = Number(Signature(z.zone, stirrupDiameter)) }))
            .OrderBy(z => z.index).Select(z => z.zone).ToList();
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
            BarSets = sets,
            StirrupZones = zones,
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
    private static IReadOnlyList<Shape> Signature(KataRebarCurve bar)
    {
        var points = bar.Polyline.Points;
        var reversed = points.Reverse().ToList();
        Shape Form(IReadOnlyList<Point3> pts, double sx, double sz, (HookAngle Angle, double Length) first, (HookAngle Angle, double Length) last)
        {
            var o = pts[0];
            var values = pts.SelectMany(p => new[] { sx * (p.X - o.X), sz * (p.Z - o.Z) }).Concat(new[] { first.Length, last.Length }).ToList();
            return new Shape($"bar|{Mm(bar.Diameter)}|{pts.Count}|{(int)first.Angle}|{(int)last.Angle}", values);
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
