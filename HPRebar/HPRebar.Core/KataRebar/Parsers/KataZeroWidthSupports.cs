using System.Collections.Generic;
using System.Linq;
using HPRebar.Core.KataRebar.Models;

namespace HPRebar.Core.KataRebar.Parsers;

/// <summary>
/// An interior support whose row 11 is 0 is not a support in Kata: the drawing measures cut lengths and stirrup
/// zones over the two spans as one (B01, column I between spans of 2500 and 4000 drawn as one span of 6500), and a
/// Revit model of such a beam has no column there. The spans are merged into the left one; what the sheet still
/// says in the support column or the right span is reported, never dropped silently. A top that changes there
/// (row 19) becomes a <see cref="KataTopStep"/> of the joined span; a soffit, a width or main bars that change there cannot be
/// joined, so the support stays (and the scope check blocks it). A 0 at the first or last column is a console end and
/// stays; a row 11 that reads as no width at all is left for the scope check to block.
/// </summary>
public static class KataZeroWidthSupports
{
    private static readonly int[] SupportRows = { 13, 14, 15, 16, 17, 18 };
    private static readonly int[] RightSpanRows = { 17, 18, 22 };

    /// <summary>The supports and spans with every interior support of no width merged away, and the notes it adds.</summary>
    public static (List<KataSupportRebarSpec> Supports, List<KataSpanRebarSpec> Spans, List<KataCellNote> Notes) Merge(
        IKataDamCellAccessor accessor,
        IReadOnlyList<KataSupportRebarSpec> supports,
        IReadOnlyList<KataSpanRebarSpec> spans,
        KataBarItem topContinuous,
        KataBarItem bottomContinuous)
    {
        var keptSupports = new List<KataSupportRebarSpec>();
        var keptSpans = new List<KataSpanRebarSpec>();
        var notes = new List<KataCellNote>();

        for (int k = 0; k < supports.Count; k++)
        {
            bool joins = k > 0 && k < supports.Count - 1 && k < spans.Count && IsZeroWidth(supports[k]) && keptSpans.Count > 0
                && CanJoin(keptSpans[keptSpans.Count - 1], spans[k], topContinuous, bottomContinuous);
            if (!joins)
            {
                keptSupports.Add(supports[k] with { SupportIndex = keptSupports.Count });
                if (k < spans.Count) keptSpans.Add(spans[k] with { SpanIndex = keptSpans.Count });
                continue;
            }

            var left = keptSpans[keptSpans.Count - 1];
            var right = spans[k];
            string merged = $"gối bề rộng 0, nhịp {Column(left.SheetColumn)} và {Column(right.SheetColumn)} gộp làm một";
            ReportCells(accessor, notes, supports[k].SheetColumn, SupportRows, $"thép tại {merged}");
            ReportCells(accessor, notes, right.SheetColumn, RightSpanRows, $"ô của nhịp {Column(right.SheetColumn)} ({merged})");
            if (right.InnerStirrups.Count > 0 && !right.InnerStirrups.SequenceEqual(left.InnerStirrups))
                notes.Add(new KataCellNote(Address(25, right), $"{right.InnerStirrups.Count} đai trong",
                    $"đai trong của nhịp phải ({merged})", $"dùng đai trong của nhịp {Column(left.SheetColumn)}"));


            keptSpans[keptSpans.Count - 1] = left with
            {
                Length = left.Length + right.Length,
                TopSteps = Changes(left.TopDrop, left.TopSteps.Concat(StepsOf(right, left.Length)))
            };
        }

        return (keptSupports, keptSpans, notes);
    }

    private static bool IsZeroWidth(KataSupportRebarSpec support) => support.ColumnWidth == 0.0 && !support.WidthUnreadable;

    /// <summary>One span can hold a change of top, not of soffit or width.</summary>
    private static bool CanJoin(KataSpanRebarSpec left, KataSpanRebarSpec right, KataBarItem top, KataBarItem bottom) =>
        left.SoffitDrop == right.SoffitDrop && left.Width == right.Width
        && Or(left.TopMain, top).SameBars(Or(right.TopMain, top))
        && Or(left.BottomMain, bottom).SameBars(Or(right.BottomMain, bottom));

    private static KataBarItem Or(KataBarItem own, KataBarItem beam) => own.IsEmpty ? beam : own;

    /// <summary>The steps that really change the top, from <paramref name="start"/> on.</summary>
    private static List<KataTopStep> Changes(double start, IEnumerable<KataTopStep> steps)
    {
        var kept = new List<KataTopStep>();
        double top = start;
        foreach (var step in steps.Where(s => s.TopDrop != top))
        {
            kept.Add(step);
            top = step.TopDrop;
        }

        return kept;
    }

    /// <summary>The right span's top as steps of the joined span, from <paramref name="offset"/> on.</summary>
    private static IEnumerable<KataTopStep> StepsOf(KataSpanRebarSpec right, double offset) =>
        new[] { new KataTopStep(offset, right.TopDrop) }
            .Concat(right.TopSteps.Select(s => s with { AtMm = s.AtMm + offset }));

    private static void ReportCells(IKataDamCellAccessor accessor, List<KataCellNote> notes, int col, IEnumerable<int> rows, string meaning)
    {
        if (col <= 0) return;
        foreach (int row in rows)
        {
            string text = accessor.GetText(row, col)?.Trim() ?? "";
            if (text.Length > 0 && text != "0" && text != "-")
                notes.Add(new KataCellNote(KataDamCellAccessorExtensions.ToAddress(row, col), text, meaning));
        }
    }

    private static string Address(int row, KataSpanRebarSpec span) =>
        span.SheetColumn > 0 ? KataDamCellAccessorExtensions.ToAddress(row, span.SheetColumn) : $"hàng {row}";

    /// <summary>Column letters of a 1-based sheet column ("I" for 9).</summary>
    private static string Column(int col) =>
        col > 0 ? KataDamCellAccessorExtensions.ToAddress(1, col).TrimEnd('1') : "?";
}
