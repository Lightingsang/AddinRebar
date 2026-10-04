using System;
using System.Collections.Generic;
using System.Linq;
using HPRebar.Core.KataRebar.Models;
using HPRebar.Core.KataRebar.Parsers;

namespace HPRebar.Core.KataRebar.Calculators;

/// <summary>What this version draws out of a spec, and the sheet input it leaves out.</summary>
public sealed record KataScopeResult(
    KataBeamRebarSpec Filtered,
    IReadOnlyList<string> Skipped,
    IReadOnlyList<string> Blocking,
    IReadOnlyList<string> Warnings);

/// <summary>
/// Limits a spec to what Kata Rebar draws today: spans between supports (columns, walls, footings or beams) and a
/// console at either end (row 11 = 0 there, drawn by HPRebar's provisional console rule), each span at its own top,
/// depth and width (rows 19, 21, 20), the first bar group of B11 and B12, the additional top bars of rows 13-16, the
/// additional bottom bars of rows 17-18 and the stirrups. Every other filled detailing cell is reported by address
/// so the user knows what the model does not contain yet; a run shape this version cannot draw blocks.
/// </summary>
public static class KataScopeFilter
{
    private const string NotSupported = "chưa hỗ trợ, không vẽ";

    public static KataScopeResult Apply(KataBeamRebarSpec spec)
    {
        if (spec is null) throw new ArgumentNullException(nameof(spec));

        var skipped = new List<string>();
        var blocking = new List<string>();
        var warnings = new List<string>();

        if (spec.Spans.Count < 1)
            blocking.Add("Sheet không có nhịp nào để vẽ.");

        for (int k = 1; k < spec.Spans.Count; k++)
        {
            bool widthChanges = Math.Abs(spec.WidthOf(k - 1) - spec.WidthOf(k)) > 0.5;
            bool topChanges = Math.Abs(spec.TopAt(k - 1, spec.Spans[k - 1].Length) - spec.TopAt(k)) > KataTopProfile.LevelTolerance;
            if (widthChanges && topChanges)
                blocking.Add($"{Cell(11, spec.Supports[k].SheetColumn)}: gối đổi cả bề rộng (hàng 20) và cao độ đỉnh (hàng 19) — chưa hỗ trợ.");
            // Both would cut the top bars at that support, each its own way.
            else if (topChanges && !spec.TopMainOf(k - 1).SameBars(spec.TopMainOf(k)))
                blocking.Add($"{Cell(11, spec.Supports[k].SheetColumn)}: gối đổi cả cao độ đỉnh và thép chủ trên (hàng 19) — chưa hỗ trợ.");
        }

        foreach (var support in spec.Supports.Where(s => s.WidthUnreadable))
            blocking.Add($"{Cell(11, support.SheetColumn)}: không đọc được bề rộng gối (gối không bề rộng ghi 0).");
        ReportRunEnds(spec, blocking, warnings);

        if (spec.TopContinuous.IsEmpty && spec.BottomContinuous.IsEmpty && spec.GlobalStirrup.Diameter <= 0.0)
            blocking.Add("B11, B12 trống và G6 không có đường kính đai: không có gì để vẽ.");

        ReportExtraItems(skipped, "B11", spec.TopMainItems, "nhóm thép chủ trên thứ 2 trở đi");
        ReportExtraItems(skipped, "B12", spec.BottomMainItems, "nhóm thép chủ dưới thứ 2 trở đi");

        foreach (var span in spec.Spans)
        {
            // The step and the first group of bars are drawn (rows 19 and 21); a second group is not yet.
            // Span bars replace B11 / B12 piece by piece; with B11 / B12 empty there are no pieces to give them to.
            if (spec.TopContinuous.IsEmpty && span.TopDropBars.Count > 0)
                skipped.Add($"{Cell(19, span.SheetColumn)} '{span.TopDropBars[0]}': thép chủ trên của nhịp khi B11 trống — {NotSupported}.");
            if (spec.BottomContinuous.IsEmpty && span.SoffitDropBars.Count > 0)
                skipped.Add($"{Cell(21, span.SheetColumn)} '{span.SoffitDropBars[0]}': thép chủ dưới của nhịp khi B12 trống — {NotSupported}.");
            if (span.TopDropBars.Count > 1)
                skipped.Add($"{Cell(19, span.SheetColumn)} '{Notation(span.TopDropBars.Skip(1))}': nhóm thép chủ trên thứ 2 trở đi — {NotSupported}.");
            // The soffit step itself is drawn (the span's depth); bars changed with it are not yet.
            if (span.SoffitDropBars.Count > 1)
                skipped.Add($"{Cell(21, span.SheetColumn)} '{Notation(span.SoffitDropBars.Skip(1))}': nhóm thép chủ dưới thứ 2 trở đi — {NotSupported}.");
        }

        foreach (var note in spec.DetailingNotes.Where(n => n.Meaning != "đai trong"))
            skipped.Add($"{note.Address} '{note.Text}': {note.Meaning} — {note.Outcome ?? NotSupported}.");

        return new KataScopeResult(Filter(spec), skipped, blocking, warnings);
    }

    /// <summary>Interior supports need a width; a 0 at an end is a console, and a beam needs at least one support.</summary>
    private static void ReportRunEnds(KataBeamRebarSpec spec, List<string> blocking, List<string> warnings)
    {
        int last = spec.Supports.Count - 1;
        if (spec.Supports.Count != spec.Spans.Count + 1)
        {
            blocking.Add("Hàng 11 phải xen kẽ gối và nhịp, bắt đầu và kết thúc bằng gối.");
            return;
        }

        if (spec.Supports.Where((s, i) => i > 0 && i < last).Any(s => s.ColumnWidth <= 0.0))
            blocking.Add("Các gối giữa phải có bề rộng > 0; điểm nối chưa hỗ trợ.");

        var consoles = new[] { 0, last }.Distinct().Where(i => spec.Spans.Count > 0 && spec.Supports[i].ColumnWidth == 0.0).ToList();
        if (consoles.Count == 2 && spec.Spans.Count == 1)
        {
            blocking.Add("Dầm không có gối nào (hai đầu đều là console).");
            return;
        }

        foreach (int i in consoles)
            warnings.Add($"{Cell(11, spec.Supports[i].SheetColumn)} = 0: đầu console — thép console vẽ theo quy tắc tạm của HPRebar, chưa đối chiếu bản vẽ Kata.");
    }

    private static KataBeamRebarSpec Filter(KataBeamRebarSpec spec) => spec with
    {
        TopMainItems = Single(spec.TopContinuous),
        BottomMainItems = Single(spec.BottomContinuous),
        GlobalSideBars = spec.GlobalSideBars,
        GlobalStirrup = spec.GlobalStirrup with
        {
            EndSupportSpacing = null,
            Branches = spec.GlobalStirrup.Branches.Count > 0 ? spec.GlobalStirrup.Branches : new[] { KataStirrupBranchSpec.Outer }
        },
        Spans = spec.Spans.Select(s => s with
        {
            SideBars = s.SideBars,
            StirrupOverride = s.StirrupOverride,
            TopDropBars = Array.Empty<KataBarItem>(),
            SoffitDropBars = Array.Empty<KataBarItem>()
        }).ToList()
    };

    private static IReadOnlyList<KataBarItem> Single(KataBarItem item) =>
        item.IsEmpty ? Array.Empty<KataBarItem>() : new[] { item };

    private static void ReportExtraItems(List<string> skipped, string address, IReadOnlyList<KataBarItem> items, string meaning)
    {
        if (items.Count > 1)
            skipped.Add($"{address} '{Notation(items.Skip(1))}': {meaning} — {NotSupported}.");
    }

    private static void Report(List<string> skipped, string address, IReadOnlyList<KataBarItem> items, string meaning)
    {
        if (items.Count > 0)
            skipped.Add($"{address} '{Notation(items)}': {meaning} — {NotSupported}.");
    }

    private static string Notation(IEnumerable<KataBarItem> items) => string.Join(";", items.Select(i => i.ToString()));

    private static string Step(double offset, IReadOnlyList<KataBarItem> bars) =>
        bars.Count == 0 ? $"{offset:0}" : $"{offset:0};{Notation(bars)}";

    private static string Cell(int row, int sheetColumn) =>
        sheetColumn > 0 ? KataDamCellAccessorExtensions.ToAddress(row, sheetColumn) : $"hàng {row}";
}
