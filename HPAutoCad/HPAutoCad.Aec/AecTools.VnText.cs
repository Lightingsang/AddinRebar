using System.Diagnostics;
using System.Text.Json;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using HPAutoCad.Aec.Cad;
using HPAutoCad.Aec.Model;
using HPAutoCad.Aec.Text;
using HPRebar.McpBridge.Core.Scripting;

namespace HPAutoCad.Aec;

/// <summary>One entity whose legacy (TCVN3 / VNI-Win) text a conversion writes, with a preview of its first changed value.</summary>
public sealed record VnTextItem(string Handle, string Type, string Owner, bool InBlockDefinition, string Layer, string Style, int Fields, string Field, string Text, string Preview);

/// <summary>
///     Pre-Unicode Vietnamese text: <c>detect_vn_legacy_text</c> shows the plan (what is converted, held, refused, which styles change),
///     <c>convert_vn_text_encoding</c> writes it. Both build the same scan and the same plan from the same arguments.
/// </summary>
public static partial class AecTools
{
    /// <summary>Items per page: at the worst (every string cut at its limit, all in 3-byte UTF-8) a page stays under 60 KB — pinned by VnTextEnvelopeTests.</summary>
    public const int MaxVnTextLimit = 40;

    /// <summary>Characters kept of the original text and of its preview.</summary>
    public const int VnPreviewChars = 80;

    /// <summary>Characters kept of an owner (block / layout), layer or style name.</summary>
    public const int VnNameChars = 40;

    /// <summary>Errors / warnings listed by detect; the summary has the counts.</summary>
    public const int VnMaxWarnings = 20;

    public static AnalysisResult<VnTextItem> DetectVnLegacyText(Database db, Editor ed, Transaction tr, ScriptUnits units, CancellationToken ct, Action<string> log,
        IReadOnlyList<string> handles, IReadOnlyList<string> layers, bool includeBlocks, bool includeTables, string? sourceCharset, ScriptArgs fontMap, bool remapStyles, int limit, int offset)
    {
        var watch = Stopwatch.StartNew();
        var run = VnRun.Start(new EditContext(db, tr, units), handles, layers, includeBlocks, includeTables, sourceCharset, fontMap, remapStyles, ct);
        limit = Math.Clamp(limit, 1, MaxVnTextLimit);
        offset = Math.Max(0, offset);
        var plan = run.Plan;
        var result = new AnalysisResult<VnTextItem>
        {
            Items = plan.Convert.Skip(offset).Take(limit).Select(VnItem).ToArray(),
            Count = plan.Convert.Count,
            Offset = offset,
        };
        result.Truncated = result.Count > offset + result.Items.Count;
        result.Errors.AddRange(plan.MappingErrors.Take(VnMaxWarnings));
        result.Errors.AddRange(plan.Failed.Take(VnMaxWarnings).Select(f => f.Error));
        foreach (var kept in plan.Kept.Take(VnMaxWarnings))
            result.Warn($"style '{kept.Style.Name}' ({kept.Style.FontLabel}) would be kept: {kept.LegacyLeft} legacy text(s) under it are outside this run (selection, excluded block / table, locked layer, field), so its texts are held too.");
        foreach (var style in run.Styles.All.Where(s => s.IsShx && s.Font.Kind == VnFontKind.Unknown && run.Scan.StylesSeen.Contains(s.Id)).Take(VnMaxWarnings))
            result.Warn($"style '{style.Name}' uses the SHX font '{style.FileName}', which the rules do not classify: if its text is Vietnamese, select it and pass sourceCharset (tcvn3 or vni) and a fontMap face.");
        result.Summary = new
        {
            examined = run.Scan.Examined,
            convert = plan.Convert.Count,
            held = plan.Held.Count,
            failed = plan.Failed.Count,
            byType = plan.Convert.GroupBy(t => t.Type).ToDictionary(g => g.Key, g => g.Count()),
            inBlockDefinitions = plan.Convert.Count(t => t.InBlockDefinition),
            stylesRemapped = plan.Remaps.Take(VnMaxWarnings).Select(r => new
            {
                name = r.Style.Name,
                font = r.Style.FontLabel,
                charset = VnConvertPlanner.Describe(r.Style),
                upperCaseOnly = r.Style.Font.UpperCaseOnly,
                targetFace = r.TargetFace,
            }),
            stylesKept = plan.Kept.Count,
            unicodeStyles = run.Styles.All.Count(s => !s.Font.IsLegacy),
            skipped = run.Scan.Skipped,
            sourceCharset = run.Forced == LegacyCharset.None ? "auto" : run.Forced.ToString(),
        };
        result.Success = plan.MappingErrors.Count == 0;
        log($"detect_vn_legacy_text: {run.Scan.Examined} examined, {plan.Convert.Count} to convert, {plan.Held.Count} held, {plan.Remaps.Count} style(s) to remap, {watch.ElapsedMilliseconds} ms");
        return result;
    }

    public static EditResult ConvertVnTextEncoding(Database db, Editor ed, Transaction tr, ScriptUnits units, CancellationToken ct, Action<string> log,
        IReadOnlyList<string> handles, IReadOnlyList<string> layers, bool includeBlocks, bool includeTables, string? sourceCharset, ScriptArgs fontMap, bool remapStyles, bool atomic,
        string? changeSetId, ScriptArgs args)
    {
        if (ChangeSetRecorder.TryRecord(db, tr, changeSetId, "convert_vn_text_encoding", args, out var recordedOp)) return recordedOp;
        var watch = Stopwatch.StartNew();
        var cx = new EditContext(db, tr, units);
        var run = VnRun.Start(cx, handles, layers, includeBlocks, includeTables, sourceCharset, fontMap, remapStyles, ct);
        var result = VnTextWriteService.Write(cx, run.Scan, run.Plan, remapStyles, atomic, ct);
        log($"convert_vn_text_encoding: {run.Plan.Convert.Count} text(s), {run.Plan.Remaps.Count} style(s) planned → {result.ModifiedCount} modified, {result.Errors.Count} error(s), atomic={atomic}, {watch.ElapsedMilliseconds} ms");
        return result;
    }

    private static VnTextItem VnItem(VnTextTarget target)
    {
        var first = target.Fields[0];
        return new VnTextItem(target.Handle, target.Type, Cut(target.Owner, VnNameChars), target.InBlockDefinition, Cut(target.Layer, VnNameChars), Cut(target.Style, VnNameChars),
            target.Fields.Count, first.Row < 0 ? first.Name : $"cell[{first.Row},{first.Column}]", Cut(first.Original, VnPreviewChars), Cut(first.Converted, VnPreviewChars));
    }

    private static string Cut(string text, int max) => text.Length <= max ? text : text[..max] + "…";

    /// <summary>The rules, style catalog, scan and plan both tools build the same way from the same arguments.</summary>
    private sealed record VnRun(VnStyleCatalog Styles, VnScanResult Scan, VnConvertPlan Plan, LegacyCharset Forced)
    {
        public static VnRun Start(EditContext cx, IReadOnlyList<string> handles, IReadOnlyList<string> layers, bool includeBlocks, bool includeTables,
            string? sourceCharset, ScriptArgs fontMap, bool remapStyles, CancellationToken ct)
        {
            var forced = ParseSourceCharset(sourceCharset);
            var rules = VnFontRules.Default.WithFontMap(FontMapPairs(fontMap));
            var styles = new VnStyleCatalog(cx.Db, cx.Tr, rules);
            var normalized = handles.Select(h => HandleResolver.Normalize(h) ?? throw new ArgumentException($"handles: '{h}' is not a handle.")).ToHashSet(StringComparer.OrdinalIgnoreCase);
            var options = new VnScanOptions(normalized, layers.Where(l => !string.IsNullOrWhiteSpace(l)).ToArray(), includeBlocks, includeTables);
            // A forced charset rewrites every Latin-1 character (°, ±, Ø, é) of text in fonts the rules do not know: never on a whole drawing.
            if (forced != LegacyCharset.None && !options.IsFiltered)
                throw new ArgumentException("sourceCharset tcvn3 / vni converts text in fonts the rules do not know, including real Unicode symbols (°, ±, Ø): select the garbled text with handles or layers.");
            var scan = VnTextScanner.Scan(cx.Db, cx.Tr, new VnTextConverter(rules, forced), styles, options, ct);
            return new VnRun(styles, scan, VnConvertPlanner.Build(cx, scan, styles, rules, options, remapStyles), forced);
        }

        private static LegacyCharset ParseSourceCharset(string? text)
        {
            if (string.IsNullOrWhiteSpace(text) || text.Trim().Equals("auto", StringComparison.OrdinalIgnoreCase)) return LegacyCharset.None;
            return VnFontRules.ParseCharset(text) is { } charset and not LegacyCharset.None
                ? charset
                : throw new ArgumentException($"sourceCharset must be auto, tcvn3 or vni (got '{text}').");
        }

        private static IEnumerable<KeyValuePair<string, string>> FontMapPairs(ScriptArgs fontMap)
        {
            if (fontMap.Raw is not { ValueKind: JsonValueKind.Object } map) return [];
            return map.EnumerateObject().Select(p => p.Value.ValueKind == JsonValueKind.String
                ? new KeyValuePair<string, string>(p.Name, p.Value.GetString()!)
                : throw new ArgumentException($"fontMap['{p.Name}'] must be a string naming a Unicode TrueType face."));
        }
    }
}
