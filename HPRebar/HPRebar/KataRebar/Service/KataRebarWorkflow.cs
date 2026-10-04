using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using HPRebar.Core.KataRebar.Calculators;
using HPRebar.Core.KataRebar.Models;
using HPRebar.KataRebar.Model;
using Serilog;
using RevitView = Autodesk.Revit.DB.View;

namespace HPRebar.KataRebar.Service;

/// <summary>
/// The steps every window takes from a parsed sheet to bars in the model: measure the picked beams, plan the
/// sheet on that measurement with the office settings, then draw. Both run on Revit's API thread, and
/// generation always measures again so the model is never changed from a stale preview.
/// </summary>
public static class KataRebarWorkflow
{
    public static KataRebarPreparation Prepare(
        Document doc,
        RevitView view,
        IReadOnlyList<ElementId> beamIds,
        KataBeamRebarSpec spec,
        KataSettings settings,
        bool? preferReversed = null)
    {
        var beams = beamIds.Select(doc.GetElement).Where(e => e is not null).ToList();
        var match = KataBeamMatcher.Measure(doc, view, beams);
        if (!match.IsSuccess || match.Measured is null)
            return new KataRebarPreparation(match, null);

        return new KataRebarPreparation(match, KataRebarPlanner.Plan(spec, match.Measured, settings, preferReversed));
    }

    public static KataRebarGenerationResult Generate(
        Document doc,
        RevitView view,
        IReadOnlyList<ElementId> beamIds,
        KataBeamRebarSpec spec,
        KataSettings settings,
        IReadOnlyDictionary<double, ElementId> barTypeIds,
        bool? preferReversed = null,
        IReadOnlyCollection<string>? removedKeys = null,
        string? plannedFingerprint = null)
    {
        var prepared = Prepare(doc, view, beamIds, spec, settings, preferReversed);
        if (prepared.Plan is null)
            return KataRebarGenerationResult.Failed($"Không đo được dầm: {prepared.Match.Message}");

        var plan = prepared.Plan;
        if (removedKeys is { Count: > 0 })
        {
            // The groups struck off the canvas: the beams must still plan exactly the bars they were picked from, or a
            // key could name another bar now.
            if (plannedFingerprint is not null && plannedFingerprint != KataLayoutRemoval.Fingerprint(plan.Layout))
                return KataRebarGenerationResult.Failed("Dầm đã đổi từ lúc xem trước nên thép đã xóa trên canvas không còn khớp — bấm Đọc thép Excel lại rồi xóa lại.");

            plan = KataLayoutRemoval.Remove(plan, removedKeys, out var unknown);
            if (unknown.Count > 0)
                return KataRebarGenerationResult.Failed($"Dầm đã đổi từ lúc xem trước: {unknown.Count} nhóm thép đã xóa không còn khớp — bấm Đọc thép Excel lại.");
            Log.Information("Kata Rebar: {Count} bar group(s) removed on the canvas are not drawn", removedKeys.Count);
        }
        if (!plan.CanGenerate)
            return KataRebarGenerationResult.Failed("Không vẽ: " + string.Join(" ", plan.Blocking));

        var barTypes = new Dictionary<double, RebarBarType>();
        var missing = new List<string>();
        foreach (double diameter in Diameters(plan))
        {
            if (barTypeIds.TryGetValue(diameter, out var id) && doc.GetElement(id) is RebarBarType type)
                barTypes[diameter] = type;
            else
                missing.Add($"Ø{diameter:0.#}");
        }

        if (missing.Count > 0)
            return KataRebarGenerationResult.Failed($"Thiếu RebarBarType cho {string.Join(", ", missing)}: tải kiểu thép vào dự án hoặc chọn kiểu khác trong bảng.");

        var shape = KataRebarShapeResolver.ClosedStirrup(doc);
        if (shape is null)
            Log.Warning("Kata Rebar: no closed stirrup shape ({Names}) in the project; stirrups are drawn one by one", string.Join(", ", KataRebarShapeResolver.ClosedStirrupNames));

        return KataRebarOrchestrator.Execute(doc, prepared.Match, plan, barTypes, shape);
    }

    /// <summary>Every bar diameter the plan draws, the stirrups' included.</summary>
    public static IEnumerable<double> Diameters(KataRebarPlan plan)
    {
        var diameters = plan.Layout.LongitudinalBars.Select(b => b.Diameter).Concat(plan.Layout.BarSets.Select(s => s.Diameter)).ToList();
        if (plan.Layout.StirrupZones.Count > 0) diameters.Add(plan.Rules.StirrupDiameter);
        return diameters.Distinct();
    }
}
