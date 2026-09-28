using System;
using System.Collections.Generic;
using System.Linq;
using HPRebar.Core.KataRebar.Models;
using HPRebar.Core.KataRebar.Parsers;

namespace HPRebar.Core.KataRebar.Calculators;

/// <summary>Outcome of comparing the sheet's geometry with the beam Revit models.</summary>
/// <param name="Reversed">The sheet lists the run from Revit's far end.</param>
/// <param name="SheetOrder">The measured segments in sheet order, one per sheet column of row 11.</param>
public sealed record KataGeometryCheckResult(
    bool Reversed,
    IReadOnlyList<KataMeasuredSegment> SheetOrder,
    IReadOnlyList<string> Blocking,
    IReadOnlyList<string> Warnings);

/// <summary>
/// Checks that the sheet describes the beam that was picked: the same support/span sequence in row 11
/// (read either way along the run) and the same section in B5/B6. Revit is the geometry of record; the
/// sheet only has to agree with it: up to <see cref="SilentToleranceMm"/> nothing is said, up to
/// <see cref="RefuseToleranceMm"/> a warning names the cell, beyond that the beam is refused.
/// </summary>
public static class KataSheetGeometryCheck
{
    public const double SilentToleranceMm = 2.0;
    public const double RefuseToleranceMm = 50.0;

    /// <param name="preferReversed">
    /// How the sheet was written, when the caller knows it (Kata Export's direction). It settles a symmetric run,
    /// which reads the same both ways; null reads such a run forward.
    /// </param>
    public static KataGeometryCheckResult Compare(KataBeamRebarSpec spec, KataMeasuredBeam measured, bool? preferReversed = null)
    {
        if (spec is null) throw new ArgumentNullException(nameof(spec));
        if (measured is null) throw new ArgumentNullException(nameof(measured));

        var blocking = new List<string>();
        var warnings = new List<string>();
        var sheet = SheetSequence(spec);
        var forward = measured.Segments.ToList();

        CompareSection(warnings, blocking, "B6", "b", spec.Width, measured.WidthMm);
        CompareSection(warnings, blocking, "B5", "h", spec.Height, measured.HeightMm);

        if (sheet.Count != forward.Count)
        {
            blocking.Add($"Hàng 11 có {sheet.Count} cột gối/nhịp nhưng Revit đo được {forward.Count} ({Describe(forward)}).");
            return new KataGeometryCheckResult(false, forward, blocking, warnings);
        }

        var backward = Enumerable.Reverse(forward).ToList();
        double forwardScore = Score(sheet, forward);
        double backwardScore = Score(sheet, backward);
        if (double.IsInfinity(forwardScore) && double.IsInfinity(backwardScore))
        {
            blocking.Add($"Thứ tự gối/nhịp ở hàng 11 không khớp dải dầm Revit ({Describe(forward)}).");
            return new KataGeometryCheckResult(false, forward, blocking, warnings);
        }

        // A symmetric run reads the same both ways up to measuring noise: a clearly better fit decides, else the
        // direction the sheet was written in when it is known, else forward.
        bool reversed = Math.Abs(backwardScore - forwardScore) <= SilentToleranceMm
            ? preferReversed ?? false
            : backwardScore < forwardScore;
        var order = reversed ? backward : forward;
        for (int i = 0; i < sheet.Count; i++)
            CompareLength(warnings, blocking, sheet[i], order[i]);

        if (reversed)
            warnings.Add("Sheet mô tả dải dầm theo chiều ngược với Revit; thép được đặt theo chiều của sheet.");

        return new KataGeometryCheckResult(reversed, order, blocking, warnings);
    }

    /// <summary>Row 11 in sheet order: supports and spans by sheet column, alternating when no column is known.</summary>
    public static IReadOnlyList<SheetCell> SheetSequence(KataBeamRebarSpec spec)
    {
        var cells = new List<SheetCell>();
        bool hasColumns = spec.Supports.All(s => s.SheetColumn > 0) && spec.Spans.All(s => s.SheetColumn > 0);

        for (int i = 0; i < Math.Max(spec.Supports.Count, spec.Spans.Count); i++)
        {
            if (i < spec.Supports.Count)
                cells.Add(new SheetCell(true, i, spec.Supports[i].ColumnWidth, hasColumns ? spec.Supports[i].SheetColumn : 0));
            if (i < spec.Spans.Count)
                cells.Add(new SheetCell(false, i, spec.Spans[i].Length, hasColumns ? spec.Spans[i].SheetColumn : 0));
        }

        return hasColumns ? cells.OrderBy(c => c.SheetColumn).ToList() : cells;
    }

    private static double Score(IReadOnlyList<SheetCell> sheet, IReadOnlyList<KataMeasuredSegment> measured)
    {
        double worst = 0.0;
        for (int i = 0; i < sheet.Count; i++)
        {
            if (sheet[i].IsSupport != measured[i].IsSupport) return double.PositiveInfinity;
            worst = Math.Max(worst, Math.Abs(sheet[i].LengthMm - measured[i].LengthMm));
        }

        return worst;
    }

    private static void CompareLength(List<string> warnings, List<string> blocking, SheetCell cell, KataMeasuredSegment segment)
    {
        double diff = Math.Abs(cell.LengthMm - segment.LengthMm);
        if (diff <= SilentToleranceMm) return;

        string what = cell.IsSupport ? $"bề rộng gối {cell.Index + 1}" : $"chiều dài nhịp {cell.Index + 1}";
        string message = $"{cell.Address}: {what} trong sheet {cell.LengthMm:0} mm, Revit đo {segment.LengthMm:0} mm (lệch {diff:0} mm).";
        (diff > RefuseToleranceMm ? blocking : warnings).Add(message);
    }

    private static void CompareSection(List<string> warnings, List<string> blocking, string address, string name, double sheetValue, double revitValue)
    {
        if (sheetValue <= 0.0)
        {
            warnings.Add($"{address} trống; dùng {name} = {revitValue:0} mm từ Revit.");
            return;
        }

        double diff = Math.Abs(sheetValue - revitValue);
        if (diff <= SilentToleranceMm) return;

        string message = $"{address}: {name} trong sheet {sheetValue:0} mm, Revit {revitValue:0} mm (lệch {diff:0} mm); thép theo kích thước Revit.";
        (diff > RefuseToleranceMm ? blocking : warnings).Add(message);
    }

    private static string Describe(IEnumerable<KataMeasuredSegment> segments) =>
        string.Join(" · ", segments.Select(s => s.IsSupport ? $"gối {s.LengthMm:0}" : $"nhịp {s.LengthMm:0}"));

    /// <summary>One sheet column of row 11.</summary>
    /// <param name="Index">Index among the supports or among the spans.</param>
    /// <param name="SheetColumn">1-based column, 0 when the spec was not read from a sheet.</param>
    public sealed record SheetCell(bool IsSupport, int Index, double LengthMm, int SheetColumn)
    {
        public string Address => SheetColumn > 0
            ? KataDamCellAccessorExtensions.ToAddress(11, SheetColumn)
            : (IsSupport ? $"Gối {Index + 1}" : $"Nhịp {Index + 1}");
    }
}
