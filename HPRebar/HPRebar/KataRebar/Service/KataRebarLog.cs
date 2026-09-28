using System.Linq;
using Autodesk.Revit.DB.Structure;
using HPRebar.Core.KataRebar.Calculators;
using HPRebar.Core.KataRebar.Models;
using HPRebar.KataRebar.Model;
using Serilog;

namespace HPRebar.KataRebar.Service;

/// <summary>One log block per drawn beam: the geometry used, the rules applied, what was left out, what was made.</summary>
public static class KataRebarLog
{
    public static void Plan(KataBeamMatchResult match, KataRebarPlan plan, RebarShape? stirrupShape)
    {
        var spec = plan.Spec;
        var rules = plan.Rules;
        Log.Information(
            "Kata Rebar: beam {Beam} ids [{Ids}] reversed={Reversed} b×h={Width}×{Height} supports/spans [{Segments}]",
            spec.BeamName,
            string.Join(",", match.BeamIds.Select(id => id.ToString())),
            plan.Reversed,
            spec.Width,
            spec.Height,
            string.Join(" ", KataSheetGeometryCheck.SheetSequence(spec).Select(c => $"{(c.IsSupport ? "c" : "L")}{c.LengthMm:0}")));
        Log.Information(
            "Kata Rebar: rules a_top={Top} a_bot={Bottom} stirrup cover={Cover} Ø{Stirrup}, anchorage {TopFactor}d/{BottomFactor}d, stirrup shape {Shape}",
            rules.TopBarCentreDepth,
            rules.BottomBarCentreDepth,
            rules.StirrupCover,
            rules.StirrupDiameter,
            rules.TopAnchorageFactor,
            rules.BottomAnchorageFactor,
            stirrupShape?.Name ?? "(none: single bars)");

        foreach (var bar in plan.Layout.MainTopBars.Concat(plan.Layout.MainBottomBars).GroupBy(b => b.Role))
        {
            var first = bar.First();
            Log.Information(
                "Kata Rebar: {Role} {Count}Ø{Dia} x {Start:0}→{End:0} legs {StartLeg:0}/{EndLeg:0}",
                bar.Key, bar.Count(), first.Diameter,
                first.Polyline.Points[0].X, first.Polyline.Points[first.Polyline.Points.Count - 1].X,
                first.StartHookLength, first.EndHookLength);
        }

        foreach (var zone in plan.Layout.StirrupZones)
            Log.Information("Kata Rebar: stirrups {Zone} {Count}@{Spacing} from x={Start:0}", zone.ZoneName, zone.Count, zone.Spacing, zone.StartStationX);

        foreach (var line in plan.Skipped) Log.Information("Kata Rebar: skipped {Item}", line);
        foreach (var line in plan.Warnings) Log.Warning("Kata Rebar: {Warning}", line);
    }

    public static void Result(string beam, KataRebarGenerationResult result) =>
        Log.Information(
            "Kata Rebar: beam {Beam} done — deleted {Deleted}, main bars {Main}, stirrup sets {Sets}, single stirrups {Singles}, Revit warnings {Warnings}",
            beam, result.DeletedCount, result.MainBarCount, result.StirrupSetCount, result.StirrupSingleBarCount, result.RevitWarnings.Count);
}
