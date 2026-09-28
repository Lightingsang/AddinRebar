using System;
using System.Collections.Generic;
using System.Linq;
using HPRebar.Core.KataRebar.Models;

namespace HPRebar.Core.KataRebar.Calculators;

/// <summary>
/// Builds the plan for one beam: checks the sheet against the beam Revit measured, replaces the sheet's
/// section and lengths with Revit's, keeps only what this version draws, derives the rules and lays out
/// the bars. Nothing here touches Revit, so the same plan backs the preview and the generation.
/// </summary>
public static class KataRebarPlanner
{
    /// <param name="measured">The picked beam as Revit models it; null before a beam is picked (sheet-only preview).</param>
    public static KataRebarPlan Plan(KataBeamRebarSpec spec, KataMeasuredBeam? measured)
    {
        if (spec is null) throw new ArgumentNullException(nameof(spec));

        var blocking = new List<string>();
        var warnings = new List<string>();
        var effective = spec;
        bool reversed = false;

        if (measured is not null)
        {
            blocking.AddRange(RunShape(measured));
            var check = KataSheetGeometryCheck.Compare(spec, measured);
            reversed = check.Reversed;
            blocking.AddRange(check.Blocking);
            warnings.AddRange(check.Warnings);
            if (check.Blocking.Count == 0)
                effective = WithMeasuredGeometry(spec, measured, check.SheetOrder);
        }

        var scope = KataScopeFilter.Apply(effective);
        blocking.AddRange(scope.Blocking);

        var rules = KataDetailingRuleBuilder.Build(scope.Filtered);
        blocking.AddRange(rules.Errors);

        var layout = KataRebarCalculator.Calculate(scope.Filtered, rules);
        var skipped = scope.Skipped.ToList();
        if (layout.SideBars.Count > 0)
        {
            skipped.Add($"Cốt giá tự động cho dầm cao {scope.Filtered.Height:0} mm (≥ 700) — chưa hỗ trợ, không vẽ.");
            layout = layout with
            {
                SideBars = Array.Empty<KataRebarCurve>(),
                TotalSteelWeightKg = Math.Round(
                    layout.TotalSteelWeightKg - KataRebarCalculator.WeightKg(layout.SideBars), 2)
            };
        }

        warnings.AddRange(layout.Warnings);

        return new KataRebarPlan
        {
            Spec = scope.Filtered,
            Rules = rules,
            Layout = layout,
            Reversed = reversed,
            Skipped = skipped,
            Blocking = blocking.Distinct().ToList(),
            Warnings = warnings.Distinct().ToList()
        };
    }

    /// <summary>Run shapes this version cannot draw even when the sheet agrees with them.</summary>
    private static IEnumerable<string> RunShape(KataMeasuredBeam measured)
    {
        if (measured.PieceCount != 1)
            yield return $"Đã chọn {measured.PieceCount} dầm; bản này mới vẽ 1 dầm (1 nhịp) mỗi lần.";

        foreach (var segment in measured.Segments.Where(s => s.IsSupport))
        {
            if (segment.SupportKind is KataMeasuredSupportKind.Beam or KataMeasuredSupportKind.Joint)
            {
                yield return "Dầm gối lên dầm khác hoặc có điểm nối không gối; bản này chỉ vẽ dầm gối lên cột / vách / móng.";
                yield break;
            }
        }
    }

    /// <summary>The spec with Revit's section and row 11 lengths (in sheet order) in place of the sheet's.</summary>
    private static KataBeamRebarSpec WithMeasuredGeometry(
        KataBeamRebarSpec spec,
        KataMeasuredBeam measured,
        IReadOnlyList<KataMeasuredSegment> sheetOrder)
    {
        var cells = KataSheetGeometryCheck.SheetSequence(spec);
        var supports = spec.Supports.ToList();
        var spans = spec.Spans.ToList();

        for (int i = 0; i < cells.Count && i < sheetOrder.Count; i++)
        {
            var cell = cells[i];
            if (cell.IsSupport)
                supports[cell.Index] = supports[cell.Index] with { ColumnWidth = sheetOrder[i].LengthMm };
            else
                spans[cell.Index] = spans[cell.Index] with { Length = sheetOrder[i].LengthMm };
        }

        return spec with
        {
            Width = measured.WidthMm,
            Height = measured.HeightMm,
            Supports = supports,
            Spans = spans
        };
    }
}
