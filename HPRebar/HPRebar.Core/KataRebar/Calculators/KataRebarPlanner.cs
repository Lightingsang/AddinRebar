using System;
using System.Collections.Generic;
using System.Linq;
using HPRebar.Core.KataRebar.Models;
using HPRebar.Core.KataRebar.Parsers;

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
            {
                effective = WithMeasuredGeometry(spec, measured, check.SheetOrder);
                warnings.AddRange(EndsAtColumns(spec, effective, out effective));
            }
        }

        var scope = KataScopeFilter.Apply(effective);
        blocking.AddRange(scope.Blocking);
        warnings.AddRange(scope.Warnings);

        var rules = KataDetailingRuleBuilder.Build(scope.Filtered, settings);
        blocking.AddRange(rules.Errors);

        // A refused sheet is not laid out: its cells may hold what the calculator cannot take.
        var layout = blocking.Count == 0
            ? KataRebarCalculator.Calculate(scope.Filtered, rules)
            : new KataRebarLayoutResult { BeamName = scope.Filtered.BeamName };
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

    /// <summary>
    /// A span as Revit has it: its length, and from the framing elements over it the soffit depth, the width and the top
    /// (its first element's, and a step wherever a later element's top differs).
    /// </summary>
    private static KataSpanRebarSpec Measured(KataSpanRebarSpec span, KataMeasuredSegment segment)
    {
        var measured = span with { Length = segment.LengthMm };
        var pieces = segment.PieceList;
        if (pieces.Count == 0)
        {
            measured = measured with { Loads = Loads(segment) };
            return segment.HeightMm > 0.0 ? measured with { Depth = segment.HeightMm - span.TopDrop } : measured;
        }

        var first = pieces[0];
        var steps = new List<KataTopStep>();
        double top = Snap(first.TopMm);
        foreach (var piece in pieces.Skip(1).Where(p => Math.Abs(p.TopMm - top) > KataSheetGeometryCheck.SilentToleranceMm))
        {
            top = Snap(piece.TopMm);
            steps.Add(new KataTopStep(piece.StartMm, top));
        }

        return measured with
        {
            Loads = Loads(segment),
            Depth = Snap(KataSheetGeometryCheck.SoffitDepth(segment)),
            TopDrop = Snap(first.TopMm),
            TopSteps = steps,
            Width = first.WidthMm > 0.0 ? Snap(first.WidthMm) : span.Width
        };
    }

    /// <summary>What rests on the span in Revit: beams framing into it, stub columns on it.</summary>
    private static IReadOnlyList<KataSpanLoad> Loads(KataMeasuredSegment segment) => segment.LoadList
        .Select(l => new KataSpanLoad(Snap(l.StartMm + l.WidthMm / 2.0), Snap(l.WidthMm), Snap(l.SoffitBelowTopMm), l.IsColumn))
        .ToList();

    /// <summary>Measured sizes and levels to whole millimetres: modelling noise must not read as a step.</summary>
    private static double Snap(double mm) => Math.Round(mm, MidpointRounding.AwayFromZero) + 0.0;

    /// <summary>
    /// Neighbouring spans whose tops or widths differ by no more than the silent tolerance are the same: the bars
    /// would otherwise be cut or cranked over a modelling difference of a millimetre.
    /// </summary>
    private static List<KataSpanRebarSpec> Smoothed(List<KataSpanRebarSpec> spans)
    {
        for (int i = 1; i < spans.Count; i++)
        {
            var prev = spans[i - 1];
            double prevTop = prev.TopSteps.Count > 0 ? prev.TopSteps[prev.TopSteps.Count - 1].TopDrop : prev.TopDrop;
            var span = spans[i];
            if (Math.Abs(span.TopDrop - prevTop) <= KataSheetGeometryCheck.SilentToleranceMm) span = span with { TopDrop = prevTop };
            if (Math.Abs(span.Width - prev.Width) <= KataSheetGeometryCheck.SilentToleranceMm) span = span with { Width = prev.Width };
            spans[i] = span;
        }

        return spans;
    }

    /// <summary>
    /// The beam Revit models ends at its end columns' faces: a crossing beam the sheet sets off past a column (rows
    /// 20 / 21, B03 C21 −150: Kata draws the beam 150 longer) is not in it, so the bars anchor in the column as
    /// usual, the preview still drawing Kata's beam (user decision 2026-10-06: Revit decides the 3D bars).
    /// </summary>
    private static IEnumerable<string> EndsAtColumns(KataBeamRebarSpec sheet, KataBeamRebarSpec measured, out KataBeamRebarSpec result)
    {
        result = measured;
        var stations = KataBeamStations.From(measured);
        var notes = new List<string>();
        var supports = measured.Supports.ToList();
        foreach (var (k, overhang) in new[] { (0, stations.StartOverhang), (supports.Count - 1, stations.EndOverhang) })
        {
            if (overhang <= 0.0 || k < 0) continue;
            // No crossing beam past the column: neither its offset nor a width wider than the column may lengthen the beam.
            supports[k] = supports[k] with { CrossingBeamOffset = 0.0, CrossingBeamWidth = 0.0 };
            notes.Add($"{Cell(sheet, k)}: Kata vẽ dầm vượt mặt ngoài gối {k + 1} thêm {overhang:0} mm tới mép dầm giao; dầm trong Revit dừng ở mặt cột — thép neo trong cột.");
        }

        if (notes.Count > 0) result = measured with { Supports = supports };
        return notes;
    }

    private static string Cell(KataBeamRebarSpec spec, int k) =>
        k < spec.Supports.Count && spec.Supports[k].SheetColumn > 0 ? KataDamCellAccessorExtensions.ToAddress(21, spec.Supports[k].SheetColumn) : $"Gối {k + 1} hàng 21";

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
                spans[cell.Index] = Measured(spans[cell.Index], sheetOrder[i]);
        }

        spans = Smoothed(spans);
        double width = spans.Count > 0 && spans[0].Width > 0.0 ? spans[0].Width : Snap(measured.WidthMm);
        return spec with
        {
            Width = width,
            Height = Snap(KataSheetGeometryCheck.FirstSpanDepth(sheetOrder) ?? measured.HeightMm),
            Supports = supports,
            Spans = spans.Select(s => Math.Abs(s.Width - width) <= KataSheetGeometryCheck.SilentToleranceMm ? s with { Width = 0.0 } : s).ToList()
        };
    }
}
