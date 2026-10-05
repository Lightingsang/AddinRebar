using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using HPRebar.Core.KataRebar.Calculators;
using HPRebar.Core.KataRebar.Models;
using Serilog;

namespace HPRebar.KataRebar.Service;

/// <summary>What the flat-bar sets of a plan became in the model.</summary>
public readonly record struct KataBarSetOutcome(int Sets, int Bars, IReadOnlyList<string> Warnings);

/// <summary>
/// Draws each <see cref="KataBarSet"/> as one Revit rebar set: the bar at its first station, hooked at both
/// ends by the project's hook type for the angle, laid out along the beam axis at its spacing. Without a
/// hook type for the angle the bars are drawn straight, and a set Revit refuses is left out; the run says so.
/// </summary>
public static class KataBarSetCreator
{
    private const double MmPerFoot = 304.8;

    public static KataBarSetOutcome Create(
        Document doc,
        KataRebarPlan plan,
        KataBeamPlacement placement,
        IReadOnlyDictionary<double, RebarBarType> barTypes)
    {
        int sets = 0, bars = 0;
        var warnings = new List<string>();
        var hooks = new Dictionary<(int, double), KataHookChoice?>();
        var mapper = placement.Mapper;

        foreach (var set in plan.Layout.BarSets)
        {
            if (set.Count == 0) continue;

            using var step = new SubTransaction(doc);
            step.Start();
            try
            {
                var barType = barTypes[set.Diameter];
                var host = placement.HostAt((set.Stations[0] + set.Stations[set.Count - 1]) / 2.0);

                if (!hooks.TryGetValue((set.HookAngle, set.HookFactor), out var hook))
                    hooks[(set.HookAngle, set.HookFactor)] = hook = KataRebarHookResolver.Find(doc, set.HookAngle, set.HookFactor);

                Rebar rebar;
                HPRebar.Core.BeamRebar.Models.Polyline3? planned = null;
                if (set.HookAngle > 0 && hook is not null && set.WrapEnds && set.Shape.Points.Count == 2)
                {
                    // Each hook turns round its bar: the tie runs one bend radius beside the bars and reaches past
                    // them by the radius plus half the bar, which is where Revit puts the outer face of the hook.
                    double radius = BendRadiusMm(barType, hook.Style);
                    if (set.WrappedBarDiameter > 0.0 && radius + 0.5 < (set.WrappedBarDiameter + set.Diameter) / 2.0)
                        warnings.Add($"{set.Description}: móc Ø{set.Diameter:0} uốn bán kính {radius:0} mm, nhỏ hơn Ø{set.WrappedBarDiameter:0}/2 + Ø{set.Diameter:0}/2 — móc không ôm được thanh; chọn đường kính uốn lớn hơn cho kiểu thép Ø{set.Diameter:0}.");
                    var (shape, start, end) = KataTieWrap.Lay(set, radius);
                    planned = shape;
                    var curves = KataRebarCurveFactory.Curves(shape, mapper);
                    rebar = KataRebarCurveFactory.CreateHooked(doc, hook.Style, barType, hook.Hook, host, mapper.AxisX, curves, mapper.ToXyz(start), mapper.ToXyz(end));
                }
                else if (set.HookAngle > 0 && hook is not null)
                {
                    var curves = KataRebarCurveFactory.Curves(set.Shape, mapper);
                    var toward = mapper.ToXyz(new HPRebar.Core.BeamRebar.Models.Point3(set.Stations[0], set.HookToward.Y, set.HookToward.Z));
                    rebar = KataRebarCurveFactory.CreateHooked(doc, hook.Style, barType, hook.Hook, host, mapper.AxisX, curves, toward);
                }
                else
                {
                    if (set.HookAngle > 0)
                        warnings.Add($"Dự án chưa có kiểu móc {set.HookAngle}° (RebarHookType): {set.Description} vẽ không móc.");
                    var shape = set.WrapEnds && set.Shape.Points.Count == 2 ? KataTieWrap.Lay(set, 0.0).Shape : set.Shape;
                    if (set.WrapEnds && set.Shape.Points.Count == 2) planned = shape;
                    rebar = KataRebarCurveFactory.Create(doc, RebarStyle.StirrupTie, barType, host, mapper.AxisX, KataRebarCurveFactory.Curves(shape, mapper));
                }

                if (set.Count > 1)
                    rebar.GetShapeDrivenAccessor().SetLayoutAsNumberWithSpacing(set.Count, set.Spacing / MmPerFoot, true, true, true);

                KataRebarStamp.Apply(rebar, host, plan.Spec.BeamName, set.BarNumber);
                if (planned is not null)
                {
                    // Revit may pull the tie towards the cover; it must stay round the bars it wraps.
                    double left = KataRebarSectionFit.Fit(doc, rebar, mapper, planned.Points[0], planned.Points[planned.Points.Count - 1], acrossToo: true);
                    if (left > 1.0)
                        warnings.Add($"{set.Description}: Revit giữ thanh C lệch {left:0} mm khỏi các thanh nó ôm — kiểm tra trong Revit.");
                }

                CheckLayout(rebar, set, mapper, warnings);
                step.Commit();
            }
            catch (Exception ex) when (ex is InvalidOperationException or ArgumentException or Autodesk.Revit.Exceptions.ApplicationException)
            {
                step.RollBack();
                Log.Warning(ex, "Kata Rebar: set {Mark} was not drawn ({Reason})", set.BarMark, ex.Message);
                warnings.Add($"{set.Description} ({set.ZoneName}): Revit không tạo được — {ex.Message}");
                continue;
            }

            sets++;
            bars += set.Count;
        }

        return new KataBarSetOutcome(sets, bars, warnings.Distinct().ToList());
    }

    /// <summary>Centre-line radius of a hook bend: half the bar type's bend diameter for the style, plus half the bar.</summary>
    private static double BendRadiusMm(RebarBarType barType, RebarStyle style)
    {
        double bend = style == RebarStyle.StirrupTie ? barType.StirrupTieBendDiameter : barType.StandardHookBendDiameter;
        return (bend + barType.BarModelDiameter) / 2.0 * MmPerFoot;
    }

    /// <summary>The copies must run along +X from the first station; a set laid the other way is reported.</summary>
    private static void CheckLayout(Rebar rebar, KataBarSet set, BeamRebar.Service.PointMapper mapper, List<string> warnings)
    {
        if (set.Count < 2) return;

        // The centreline of every position is the first bar's; the positions differ by their transforms.
        var accessor = rebar.GetShapeDrivenAccessor();
        var first = accessor.GetBarPositionTransform(0).Origin;
        var last = accessor.GetBarPositionTransform(set.Count - 1).Origin;
        double run = (last - first).DotProduct(mapper.AxisX) * MmPerFoot;
        double expected = set.Stations[set.Count - 1] - set.Stations[0];
        if (Math.Abs(run - expected) > 1.0)
        {
            Log.Warning("Kata Rebar: set {Mark} runs {Run:0} mm along the axis, expected {Expected:0}", set.BarMark, run, expected);
            warnings.Add($"{set.Description}: bộ thanh rải {run:0} mm dọc dầm, cần {expected:0} mm — kiểm tra trong Revit.");
        }
    }
}
