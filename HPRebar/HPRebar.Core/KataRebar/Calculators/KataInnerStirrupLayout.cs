using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using HPRebar.Core.BeamRebar.Models;
using HPRebar.Core.KataRebar.Models;

namespace HPRebar.Core.KataRebar.Calculators;

/// <summary>
/// Inner stirrups of each span's section, as Kata's stirrup chart draws them from the top bars they name:
/// "Đai □ a-b" a closed hoop round top bars a..b, "Đai U a-b" a U open at the top with its legs outside bars a and
/// b, each leg turned in over its bar and down (B01 section 2-2: legs ±59 round bars 3-4, in 40, down 55 for Ø10),
/// "Đai C a" an upright tie beside bar a from the top bars to the bottom ones. They are spaced as the C ties are
/// (I8, <see cref="KataTieStations"/>): beside every outer hoop, one stirrup diameter along the beam from it, or
/// evenly at J7 (B01: "Ø10a500", "2xØ10a500"); at the span's width (row 20) and up to the top over each run (row 19).
/// A span without rows 25-27 of its own takes those of the span before, unless the support between them has "*" in
/// row 24 (B01: span 1's U 3-4, C 2, C 5 run on through F, H and J, none past K "*"; B03: on past G into span 3,
/// whose 3Ø20 keep only "Đai C 2"); entries naming bars the span lacks are then dropped.
/// </summary>
public static class KataInnerStirrupLayout
{
    public static List<KataBarSet> Build(
        KataBeamRebarSpec spec,
        KataDetailingRules rules,
        KataBeamStations st,
        IReadOnlyList<KataStirrupZoneResult> outerZones,
        List<string> warnings)
    {
        var sets = new List<KataBarSet>();
        double ds = rules.StirrupDiameter;
        if (ds <= 0.0 || outerZones.Count == 0) return sets;

        // A row carried on to the next spans names the same cell there: say what is wrong with it once.
        var said = new HashSet<string>();
        void Warn(string warning)
        {
            if (said.Add(warning)) warnings.Add(warning);
        }

        IReadOnlyList<KataStirrupBranchSpec> carried = Array.Empty<KataStirrupBranchSpec>();
        for (int s = 0; s < spec.Spans.Count; s++)
        {
            // The bars a stirrup names are the span's own top main bars (B01 console "Đai C 2": the middle one of its 3Ø20).
            var top = spec.TopMainOf(s);
            double off = (top.Diameter + ds) / 2.0;
            var barY = top.IsEmpty
                ? Array.Empty<double>()
                // Kata counts the bars from the left of its section, which looks along the beam (+X): bar 1 is on +Y.
                : KataRebarCalculator.ComputeTransverseYPositions(spec.WidthOf(s), rules.StirrupCover, ds, top.Diameter, top.Count).OrderByDescending(y => y).ToArray();
            double zBottom = -spec.DepthOf(s) + rules.StirrupCover + ds / 2.0;
            var own = spec.Spans[s].InnerStirrups;
            if (own.Count > 0 || s == 0 || s >= spec.Supports.Count || spec.Supports[s].Row24Star) carried = own;
            var entries = carried;
            var runs = SplitAtSteps(spec, st, s, Runs(rules, st, outerZones, s));
            for (int i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];
                string label = $"{entry.Address} '{Name(entry.ShapeType)} {entry.Position}'";
                if (!TryBars(entry.Position, out int a, out int b))
                {
                    Warn($"{label}: không đọc được thanh được ôm (ví dụ '2-3' hoặc '2') — không vẽ.");
                    continue;
                }

                if (barY.Length == 0 || a < 1 || b > barY.Length)
                {
                    // Carried on to fewer top bars, Kata keeps those that fit (B03 span 3, 3Ø20: "Đai C 2" of U 3-4, C 2, C 5).
                    if (own.Count == 0) continue;
                    Warn($"{label}: thép chủ trên có {barY.Length} thanh, không có thanh {a}..{b} — không vẽ.");
                    continue;
                }

                if (entry.ShapeType == KataStirrupShapeType.ClosedHoop && a == 1 && b == barY.Length)
                    continue; // the outer hoop itself

                if (entry.ShapeType == KataStirrupShapeType.CrossTie && a != b)
                    Warn($"{label}: đai C ôm một thanh — dùng thanh {a}.");

                bool wraps = entry.ShapeType == KataStirrupShapeType.CrossTie;
                // As B01 section 2-2 draws every C: its long leg on the left of the bar (+Y), the hooks round to the right.
                var wrapOffset = new Point3(0.0, 1.0, 0.0);
                foreach (var run in runs)
                {
                    var stations = run.Stations;
                    double topLevel = spec.TopAt(s, (stations[0] + stations[stations.Count - 1]) / 2.0 - st.SpanStart[s]);
                    double zTop = topLevel - rules.StirrupCover - ds / 2.0;
                    var (shape, toward, hookAngle, hookFactor) = Geometry(entry.ShapeType, barY[a - 1], barY[b - 1], off, zTop, zBottom, topLevel, rules);
                    sets.Add(new KataBarSet
                    {
                        BarMark = $"{KataStirrupCurveFactory.MarkOf(entry.ShapeType)}.{s + 1}.{i + 1}",
                        Description = $"{Name(entry.ShapeType)} {entry.Position} nhịp {s + 1}",
                        Role = entry.ShapeType switch
                        {
                            KataStirrupShapeType.CapStirrup => KataBarRole.StirrupCap,
                            KataStirrupShapeType.CrossTie => KataBarRole.CrossTie,
                            _ => KataBarRole.StirrupClosed
                        },
                        Diameter = ds,
                        SpanIndex = s,
                        ZoneName = run.ZoneName,
                        Shape = new Polyline3(shape.Select(p => new Point3(stations[0], p.Y, p.Z)).ToList()),
                        Stations = stations,
                        Spacing = run.Spacing,
                        HookAngle = hookAngle,
                        HookFactor = hookFactor,
                        HookToward = toward,
                        WrapEnds = wraps,
                        WrapOffset = wrapOffset
                    });
                }
            }
        }

        return sets;
    }

    /// <summary>
    /// Each run cut where the span's top steps (row 19): the stirrups on either side are of different heights, Kata
    /// numbers them apart (B01 span H, top 50 down, its U and C 30 and 31 beside J's 27 and 28).
    /// </summary>
    private static List<KataTieStations.Run> SplitAtSteps(KataBeamRebarSpec spec, KataBeamStations st, int s, List<KataTieStations.Run> runs)
    {
        var steps = spec.Spans[s].TopSteps.Select(t => st.SpanStart[s] + t.AtMm).OrderBy(x => x).ToList();
        if (steps.Count == 0) return runs;

        var parts = new List<KataTieStations.Run>();
        foreach (var run in runs)
        {
            int k = 0;
            foreach (var group in run.Stations.GroupBy(x => steps.Count(step => x >= step)).OrderBy(g => g.Key))
                parts.Add(run with { Stations = group.ToList(), ZoneName = k++ == 0 ? run.ZoneName : $"{run.ZoneName} [{k}]" });
        }

        return parts;
    }

    /// <summary>
    /// Where the inner stirrups of span <paramref name="s"/> go: beside each outer hoop of the span's own zones (joint
    /// stirrups round a load are outer hoops only), one stirrup diameter along so the two never share a plane; or
    /// evenly at J7 as the C ties, one stirrup diameter short of them.
    /// </summary>
    private static List<KataTieStations.Run> Runs(KataDetailingRules rules, KataBeamStations st, IReadOnlyList<KataStirrupZoneResult> zones, int s)
    {
        double ds = rules.StirrupDiameter;
        if (rules.TieSpacingMode == KataTieSpacingMode.LikeHoops)
            return zones.Where(z => z.SpanIndex == s && z.Count > 0 && !KataJointStirrups.IsJointZone(z.ZoneName))
                .Select(z => new KataTieStations.Run(z.Stations.Select(x => x + ds).ToList(), z.Spacing, z.ZoneName))
                .ToList();

        // Kata runs them on through a load (B01 section 2-2, cut at the beam framing into span 1, shows them).
        return KataTieStations.InSpan(rules, st, zones, s, st.SpanStart[s], st.SpanEnd[s])
            .Where(r => r.Stations.Count > 0)
            .Select(r => r with { Stations = r.Stations.Select(x => x - ds).ToList() })
            .ToList();
    }

    /// <summary>Turn of a U's leg over its bar, and its drop inside it, in stirrup diameters (B01 section 2-2: 40 and 55 for Ø10).</summary>
    private const double UReturnDiameters = 4.0;
    private const double UDropDiameters = 5.5;

    /// <summary>Centreline in the section (Y, Z) and the point its hooks turn towards.</summary>
    private static (List<Point3> Shape, Point3 Toward, int HookAngle, double HookFactor) Geometry(
        KataStirrupShapeType type, double ya, double yb, double off, double zTop, double zBottom, double topLevel, KataDetailingRules rules)
    {
        double zBottomBar = zBottom - rules.StirrupCover - rules.StirrupDiameter / 2.0 + rules.BottomBarCentreDepth;
        // Bars a..b run from +Y to −Y: the U's legs stand outside them.
        double left = Math.Max(ya, yb) + off, right = Math.Min(ya, yb) - off;
        var centre = new Point3(0.0, (left + right) / 2.0, (zTop + zBottom) / 2.0);
        switch (type)
        {
            case KataStirrupShapeType.CapStirrup:
            {
                // Open at the top: each leg turns in over its bar and down inside it — bends, not hooks.
                double turn = UReturnDiameters * rules.StirrupDiameter, drop = UDropDiameters * rules.StirrupDiameter;
                return (new List<Point3>
                    {
                        new(0, left - turn, zTop - drop), new(0, left - turn, zTop), new(0, left, zTop), new(0, left, zBottom),
                        new(0, right, zBottom), new(0, right, zTop), new(0, right + turn, zTop), new(0, right + turn, zTop - drop)
                    },
                    centre, 0, 0.0);
            }

            case KataStirrupShapeType.CrossTie:
                // The top bar it wraps and the bottom bar position below it; the tie runs beside them on the side
                // of the beam's centre and each hook turns round its bar.
                return (new List<Point3> { new(0, ya, topLevel - rules.TopBarCentreDepth), new(0, ya, zBottomBar) },
                    new Point3(0.0, ya, (zTop + zBottom) / 2.0), rules.CrossTieHookAngle, rules.CrossTieHookFactor);

            default:
                return (new List<Point3> { new(0, left, zTop), new(0, right, zTop), new(0, right, zBottom), new(0, left, zBottom), new(0, left, zTop) },
                    centre, rules.ClosedStirrupHookAngle, rules.ClosedStirrupHookFactor);
        }
    }

    /// <summary>"3-4" wraps bars 3 to 4, "2" bar 2 alone.</summary>
    internal static bool TryBars(string position, out int a, out int b)
    {
        a = b = 0;
        var parts = (position ?? "").Split('-');
        if (parts.Length is < 1 or > 2) return false;
        if (!int.TryParse(parts[0].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out a)) return false;
        if (parts.Length == 1) { b = a; return true; }
        if (!int.TryParse(parts[1].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out b)) return false;
        if (b < a) (a, b) = (b, a);
        return true;
    }

    private static string Name(KataStirrupShapeType type) => type switch
    {
        KataStirrupShapeType.CapStirrup => "Đai U",
        KataStirrupShapeType.CrossTie => "Đai C",
        _ => "Đai □"
    };
}
