using Autodesk.AutoCAD.DatabaseServices;
using HPAutoCad.Aec.Model;
using HPAutoCad.Aec.Text;

namespace HPAutoCad.Aec.Cad;

/// <summary>A legacy text style and the Unicode TrueType face it becomes (null = no face known: the caller must pass fontMap).</summary>
public sealed record VnStyleRemap(VnStyle Style, string? TargetFace);

/// <summary>A legacy style left as it is because some of its text is not converted in this run, and how much.</summary>
public sealed record VnKeptStyle(VnStyle Style, int LegacyLeft);

public sealed record VnFailedTarget(int Index, VnTextTarget Target, ToolError Error);

/// <summary>What a run writes: the texts, the styles, and everything it leaves alone with the reason.</summary>
public sealed record VnConvertPlan(
    IReadOnlyList<VnTextTarget> Convert,
    IReadOnlyList<VnFailedTarget> Failed,
    IReadOnlyList<VnTextTarget> Held,
    IReadOnlyList<VnStyleRemap> Remaps,
    IReadOnlyList<VnKeptStyle> Kept,
    IReadOnlyList<ToolError> MappingErrors);

/// <summary>
///     Phase 1 of the conversion, shared by detect (which shows it) and convert (which writes it); reads only. The rule that keeps the
///     screen honest: with <c>remapStyles</c>, a legacy style is remapped only when no legacy text under it is left behind (outside the
///     selection, in an excluded block, on a locked layer, carrying a field); otherwise the style is kept and its texts are held too, since
///     converted text under a legacy font, or legacy text under a Unicode font, both show as garbage. Without <c>remapStyles</c> every
///     selected text is converted and no style is touched — the caller remaps later (converted texts carry <see cref="VnTextMarker"/>).
/// </summary>
public static class VnConvertPlanner
{
    public const string NoFontMapping = "NO_FONT_MAPPING";

    public const string CellLocked = "CELL_LOCKED";

    public static VnConvertPlan Build(EditContext cx, VnScanResult scan, VnStyleCatalog styles, VnFontRules rules, VnScanOptions options, bool remapStyles)
    {
        var failed = Failures(cx, scan);
        var failedIds = failed.Select(f => f.Target.Id).ToHashSet();
        var candidates = Candidates(scan, styles, options);
        if (!remapStyles)
            return new VnConvertPlan(scan.Targets.Where(t => !failedIds.Contains(t.Id)).ToArray(), failed, [], [], [], InlineErrors(scan.Targets));

        var left = new Dictionary<ObjectId, int>(scan.LegacyLeft);
        foreach (var style in failed.SelectMany(f => f.Target.StyleIds)) left[style] = left.GetValueOrDefault(style) + 1;
        var held = new HashSet<ObjectId>();
        // Holding a table that also uses another style leaves text under that style too: repeat until nothing more is held.
        for (var changed = true; changed;)
        {
            changed = false;
            foreach (var target in scan.Targets.Where(t => !failedIds.Contains(t.Id) && !held.Contains(t.Id)))
            {
                if (!target.StyleIds.Any(s => left.GetValueOrDefault(s) > 0 && candidates.ContainsKey(s))) continue;
                held.Add(target.Id);
                foreach (var style in target.StyleIds) left[style] = left.GetValueOrDefault(style) + 1;
                changed = true;
            }
        }

        var convert = scan.Targets.Where(t => !failedIds.Contains(t.Id) && !held.Contains(t.Id)).ToArray();
        var kept = candidates.Values.Where(s => left.GetValueOrDefault(s.Id) > 0).Select(s => new VnKeptStyle(s, left[s.Id])).ToArray();
        var remaps = candidates.Values.Where(s => left.GetValueOrDefault(s.Id) == 0)
            .Select(s => Remap(s, rules))
            .ToArray();
        var errors = InlineErrors(convert);
        errors.AddRange(remaps.Where(r => r.TargetFace is null).Select(r => ToolError.ForHandle(NoFontMapping, r.Style.Handle,
            $"Style '{r.Style.Name}' uses the {Describe(r.Style)} font '{r.Style.FontLabel}' and no Unicode face is known for it: pass fontMap {{\"{r.Style.FontLabel}\": \"Arial\"}}.")));
        return new VnConvertPlan(convert, failed, scan.Targets.Where(t => held.Contains(t.Id)).ToArray(), remaps, kept, errors);
    }

    public static string Describe(VnStyle style) => style.Font.Charset switch
    {
        LegacyCharset.Tcvn3 => "TCVN3",
        LegacyCharset.VniWin => "VNI-Win",
        _ => "unclassified",
    };

    /// <summary>
    ///     Legacy styles a run may remap — all of them on a whole-drawing run, else those the selection uses — plus an unclassified SHX style
    ///     whose text a forced <c>sourceCharset</c> converts: Unicode text cannot be drawn by it either. An unclassified TrueType face
    ///     (Arial holding VNI bytes) is a Unicode face already and stays.
    /// </summary>
    private static Dictionary<ObjectId, VnStyle> Candidates(VnScanResult scan, VnStyleCatalog styles, VnScanOptions options)
    {
        var forcedShx = scan.Targets.SelectMany(t => t.StyleIds).ToHashSet();
        return styles.All
            .Where(s => !s.IsDependent && (!options.IsFiltered || scan.StylesSeen.Contains(s.Id)))
            .Where(s => s.Font.IsLegacy || (s.IsShx && s.Font.Kind == VnFontKind.Unknown && forcedShx.Contains(s.Id)))
            .ToDictionary(s => s.Id);
    }

    private static VnStyleRemap Remap(VnStyle style, VnFontRules rules)
    {
        var face = style.Font.TargetFace ?? rules.Override(style.Face, style.FileName);
        return new VnStyleRemap(style, face);
    }

    private static List<ToolError> InlineErrors(IEnumerable<VnTextTarget> targets) =>
        targets.SelectMany(t => t.UnmappedFonts).Distinct(StringComparer.OrdinalIgnoreCase)
            .Select(font => new ToolError(NoFontMapping, $"Text names the legacy font '{font}' inline (\\f) and no Unicode face is known for it: pass fontMap {{\"{font}\": \"Arial\"}}."))
            .ToList();

    private static List<VnFailedTarget> Failures(EditContext cx, VnScanResult scan)
    {
        var failed = new List<VnFailedTarget>();
        for (var i = 0; i < scan.Targets.Count; i++)
        {
            var target = scan.Targets[i];
            var entity = (Entity)cx.Tr.GetObject(target.Id, OpenMode.ForRead);
            if (!cx.LayerAllowsEdit(entity, out var error)) failed.Add(new VnFailedTarget(i, target, error!));
            else if (entity is Table table && LockedCell(table, target) is { } cell)
                failed.Add(new VnFailedTarget(i, target, ToolError.ForHandle(CellLocked, target.Handle, $"Table {target.Handle}: cell [{cell.Row},{cell.Column}] has its content locked.")));
        }

        return failed;
    }

    private static VnTextField? LockedCell(Table table, VnTextTarget target) =>
        target.Fields.FirstOrDefault(f => (table.Cells[f.Row, f.Column].State & CellStates.ContentLocked) != 0);
}
