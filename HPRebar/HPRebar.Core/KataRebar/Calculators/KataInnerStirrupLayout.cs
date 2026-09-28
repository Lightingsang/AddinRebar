using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using HPRebar.Core.BeamRebar.Models;
using HPRebar.Core.KataRebar.Models;

namespace HPRebar.Core.KataRebar.Calculators;

/// <summary>
/// Inner stirrups of each span's section, as Kata's stirrup chart draws them from the top bars they name:
/// "Đai □ a-b" a closed hoop round top bars a..b, "Đai U a-b" a U open at the top with its legs at bars a and
/// b, "Đai C a" an upright tie beside bar a from the top bars to the bottom ones. They follow the outer
/// hoop's zones, one stirrup diameter along the beam from each outer hoop so the two never share a plane.
/// </summary>
public static class KataInnerStirrupLayout
{
    public static List<KataBarSet> Build(
        KataBeamRebarSpec spec,
        KataDetailingRules rules,
        IReadOnlyList<KataStirrupZoneResult> outerZones,
        List<string> warnings)
    {
        var sets = new List<KataBarSet>();
        double ds = rules.StirrupDiameter;
        if (ds <= 0.0 || outerZones.Count == 0) return sets;

        var top = spec.TopContinuous;
        var barY = top.IsEmpty
            ? Array.Empty<double>()
            : KataRebarCalculator.ComputeTransverseYPositions(spec.Width, rules.StirrupCover, ds, top.Diameter, top.Count).OrderBy(y => y).ToArray();
        double off = (top.Diameter + ds) / 2.0;
        double zTop = -(rules.StirrupCover + ds / 2.0);
        double zBottom = -spec.Height + rules.StirrupCover + ds / 2.0;

        for (int s = 0; s < spec.Spans.Count; s++)
        {
            var entries = spec.Spans[s].InnerStirrups;
            var zones = outerZones.Where(z => z.SpanIndex == s && z.Count > 0).ToList();
            for (int i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];
                string label = $"{entry.Address} '{Name(entry.ShapeType)} {entry.Position}'";
                if (!TryBars(entry.Position, out int a, out int b))
                {
                    warnings.Add($"{label}: không đọc được thanh được ôm (ví dụ '2-3' hoặc '2') — không vẽ.");
                    continue;
                }

                if (barY.Length == 0 || a < 1 || b > barY.Length)
                {
                    warnings.Add($"{label}: thép chủ trên có {barY.Length} thanh, không có thanh {a}..{b} — không vẽ.");
                    continue;
                }

                if (entry.ShapeType == KataStirrupShapeType.ClosedHoop && a == 1 && b == barY.Length)
                    continue; // the outer hoop itself

                if (entry.ShapeType == KataStirrupShapeType.CrossTie && a != b)
                    warnings.Add($"{label}: đai C ôm một thanh — dùng thanh {a}.");

                var (shape, toward, hookAngle, hookFactor) = Geometry(entry.ShapeType, barY[a - 1], barY[b - 1], off, zTop, zBottom, rules);
                foreach (var zone in zones)
                {
                    var stations = zone.Stations.Select(x => x + ds).ToList();
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
                        ZoneName = zone.ZoneName,
                        Shape = new Polyline3(shape.Select(p => new Point3(stations[0], p.Y, p.Z)).ToList()),
                        Stations = stations,
                        Spacing = zone.Spacing,
                        HookAngle = hookAngle,
                        HookFactor = hookFactor,
                        HookToward = toward
                    });
                }
            }
        }

        return sets;
    }

    /// <summary>Centreline in the section (Y, Z) and the point its hooks turn towards.</summary>
    private static (List<Point3> Shape, Point3 Toward, int HookAngle, double HookFactor) Geometry(
        KataStirrupShapeType type, double ya, double yb, double off, double zTop, double zBottom, KataDetailingRules rules)
    {
        double left = ya - off, right = yb + off;
        var centre = new Point3(0.0, (left + right) / 2.0, (zTop + zBottom) / 2.0);
        switch (type)
        {
            case KataStirrupShapeType.CapStirrup:
                return (new List<Point3> { new(0, left, zTop), new(0, left, zBottom), new(0, right, zBottom), new(0, right, zTop) },
                    centre, rules.ClosedStirrupHookAngle, rules.ClosedStirrupHookFactor);

            case KataStirrupShapeType.CrossTie:
            {
                // Beside the bar on the side of the beam's centre, the hooks turning round the bar.
                double y = ya <= 0.0 ? ya + off : ya - off;
                return (new List<Point3> { new(0, y, zTop), new(0, y, zBottom) },
                    new Point3(0.0, ya, (zTop + zBottom) / 2.0), rules.CrossTieHookAngle, rules.CrossTieHookFactor);
            }

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
