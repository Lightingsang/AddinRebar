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
    /// <param name="settings">The office detailing settings; null uses the Kata defaults.</param>
    /// <param name="preferReversed">The direction the sheet was written in, when known (see <see cref="KataSheetGeometryCheck.Compare"/>).</param>
    public static KataRebarPlan Plan(KataBeamRebarSpec spec, KataMeasuredBeam? measured, KataSettings? settings = null, bool? preferReversed = null)
    {
        if (spec is null) throw new ArgumentNullException(nameof(spec));

        var blocking = new List<string>();
        var warnings = new List<string>();
        var effective = spec;
        bool reversed = false;

        if (measured is not null)
        {
            blocking.AddRange(RunShape(measured));
            var check = KataSheetGeometryCheck.Compare(spec, measured, preferReversed);
            reversed = check.Reversed;
            blocking.AddRange(check.Blocking);
            warnings.AddRange(check.Warnings);
            if (check.Blocking.Count == 0)
                effective = WithMeasuredGeometry(spec, measured, check.SheetOrder);
        }

        var scope = KataScopeFilter.Apply(effective);
        blocking.AddRange(scope.Blocking);

        var rules = KataDetailingRuleBuilder.Build(scope.Filtered, settings);
        blocking.AddRange(rules.Errors);

        var layout = KataRebarCalculator.Calculate(scope.Filtered, rules);
        var skipped = scope.Skipped.ToList();
        warnings.AddRange(layout.Warnings);
        blocking.AddRange(layout.Blocking);

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
        // One framing element may run over several supports, or a span may be drawn in pieces: the bars
        // follow the measured run either way, each hosted by the piece under its middle.
        if (measured.PieceCount < 1)
            yield return "Chưa chọn dầm nào trong Revit.";

        foreach (var segment in measured.Segments.Where(s => s.IsSupport))
        {
            // A crossing beam is a support like a column of its width; a joint has nothing to anchor in.
            if (segment.SupportKind is KataMeasuredSupportKind.Joint)
            {
                yield return "Dải dầm có điểm nối không gối lên cột / vách / móng / dầm; bản này chưa vẽ được.";
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
                spans[cell.Index] = spans[cell.Index] with
                {
                    Length = sheetOrder[i].LengthMm,
                    Depth = sheetOrder[i].HeightMm > 0.0 ? sheetOrder[i].HeightMm : spans[cell.Index].Depth
                };
        }

        return spec with
        {
            Width = measured.WidthMm,
            Height = KataSheetGeometryCheck.FirstSpanDepth(sheetOrder) ?? measured.HeightMm,
            Supports = supports,
            Spans = spans
        };
    }
}
