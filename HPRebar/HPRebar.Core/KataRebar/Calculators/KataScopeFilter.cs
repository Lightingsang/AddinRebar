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
    IReadOnlyList<string> Blocking);

/// <summary>
/// Limits a spec to what Kata Rebar draws today: spans between supports (columns, walls, footings or beams), each
/// span at its own depth (row 21), the first bar group of B11 and B12, the additional top bars of rows 13-16, the
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

        if (spec.Spans.Count < 1)
            blocking.Add("Sheet không có nhịp nào để vẽ.");

        if (spec.Supports.Count != spec.Spans.Count + 1 || spec.Supports.Any(s => s.ColumnWidth <= 0.0))
            blocking.Add("Các gối phải có bề rộng > 0; console và điểm nối chưa hỗ trợ.");

        if (spec.TopContinuous.IsEmpty && spec.BottomContinuous.IsEmpty && spec.GlobalStirrup.Diameter <= 0.0)
            blocking.Add("B11, B12 trống và G6 không có đường kính đai: không có gì để vẽ.");

        ReportExtraItems(skipped, "B11", spec.TopMainItems, "nhóm thép chủ trên thứ 2 trở đi");
        ReportExtraItems(skipped, "B12", spec.BottomMainItems, "nhóm thép chủ dưới thứ 2 trở đi");

        foreach (var span in spec.Spans)
        {
            if (span.TopDrop != 0.0 || span.TopDropBars.Count > 0)
                skipped.Add($"{Cell(19, span.SheetColumn)} '{Step(span.TopDrop, span.TopDropBars)}': giật mép trên / đổi thép chịu lực trên — {NotSupported}.");
            // The soffit step itself is drawn (the span's depth); bars changed with it are not yet.
            if (span.SoffitDropBars.Count > 0)
                skipped.Add($"{Cell(21, span.SheetColumn)} '{Step(span.SoffitDrop, span.SoffitDropBars)}': đổi thép chịu lực dưới theo bậc đáy — {NotSupported}.");
        }

        foreach (var note in spec.DetailingNotes.Where(n => n.Meaning != "đai trong"))
            skipped.Add($"{note.Address} '{note.Text}': {note.Meaning} — {NotSupported}.");

        return new KataScopeResult(Filter(spec), skipped, blocking);
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
            TopDrop = 0.0,
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
