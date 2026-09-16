using System.Text.RegularExpressions;
using HPAutoCad.Aec.Geometry;
using HPAutoCad.Aec.Issues;
using HPAutoCad.Aec.Model;

namespace HPAutoCad.Aec.Standards;

/// <summary>
///     Runs a <see cref="CadStandardsRuleSet"/> over entity records and drawing tables and returns <see cref="AuditIssue"/>s
///     (<c>STD-nnnn</c>, ids assigned in the stable order). Pure: no AutoCAD types, so every rule is unit-tested on synthetic
///     records. Entity checks report one issue per entity; table checks one per layer/block; <c>unused_layer</c> runs only
///     when the caller examined the whole drawing (a subset cannot prove a layer unused).
/// </summary>
public static class CadStandardsChecker
{
    public const string IdPrefix = "STD";

    public static IReadOnlyList<AuditIssue> Check(IReadOnlyList<AecEntityRecord> records, DrawingTables tables, CadStandardsRuleSet rules, IReadOnlySet<string> checks, bool wholeDrawing, CancellationToken ct)
    {
        var found = new List<AuditIssue>();
        var overrides = rules.Overrides;
        try
        {
            foreach (var r in records)
            {
                ct.ThrowIfCancellationRequested();
                var at = r.PositionMm ?? r.BoundsMm?.Center;
                var onLayerZero = checks.Contains(StandardsIssueType.LayerZero) && rules.LayerZero is { } zero && r.Layer == "0" && !Exempts(zero.AllowedTypes, r.Type);
                if (onLayerZero)
                    found.Add(Issue(StandardsIssueType.LayerZero, rules.LayerZero!.Severity, [r.Handle], at, $"{r.Type} {r.Handle} is drawn on layer 0.", "Move it to its discipline layer; layer 0 is for block geometry only.", r.Layer));
                // An entity already reported on layer 0 is not also "on the wrong layer" — one finding per cause.
                if (checks.Contains(StandardsIssueType.EntityLayer) && !onLayerZero)
                    foreach (var rule in rules.EntityLayer.Where(rule => EntityFilter.Matches(rule.Types, r.Type) && !EntityFilter.Matches(rule.Layers, r.Layer)))
                        found.Add(Issue(StandardsIssueType.EntityLayer, rule.Severity, [r.Handle], at, rule.Message is null ? $"{r.Type} {r.Handle} is on layer '{r.Layer}', which rule {rule.Id} does not allow." : $"{r.Type} {r.Handle} on '{r.Layer}': {rule.Message}.", $"Move it to a layer rule {rule.Id} allows (update_entities_batch set.layer).", r.Layer, rule.Id));
                if (overrides is not null && !Exempts(overrides.ExemptTypes, r.Type) && !Exempts(overrides.ExemptLayers, r.Layer))
                {
                    if (checks.Contains(StandardsIssueType.ColorOverride) && overrides.Color && IsOverride(r.Color))
                        found.Add(Issue(StandardsIssueType.ColorOverride, overrides.Severity, [r.Handle], at, $"{r.Type} {r.Handle} has colour {r.Color}{ByBlockNote(r.Color)} instead of ByLayer.", "Set the colour to ByLayer and let the layer carry it.", r.Layer));
                    if (checks.Contains(StandardsIssueType.LinetypeOverride) && overrides.Linetype && IsOverride(r.Linetype))
                        found.Add(Issue(StandardsIssueType.LinetypeOverride, overrides.Severity, [r.Handle], at, $"{r.Type} {r.Handle} has linetype {r.Linetype}{ByBlockNote(r.Linetype)} instead of ByLayer.", "Set the linetype to ByLayer and give the layer the linetype.", r.Layer));
                    if (checks.Contains(StandardsIssueType.LineweightOverride) && overrides.Lineweight && IsOverride(r.Lineweight))
                        found.Add(Issue(StandardsIssueType.LineweightOverride, overrides.Severity, [r.Handle], at, $"{r.Type} {r.Handle} has lineweight {Lineweight(r.Lineweight)}{ByBlockNote(r.Lineweight)} instead of ByLayer.", "Set the lineweight to ByLayer and give the layer the lineweight.", r.Layer));
                }

                var isText = r.Type is "TEXT" or "MTEXT";
                var isDimension = r.Type.EndsWith("DIMENSION", StringComparison.OrdinalIgnoreCase);
                if (checks.Contains(StandardsIssueType.TextStyle) && rules.TextStyles is { Allowed.Count: > 0 } ts && isText && r.Style is not null && !EntityFilter.Matches(ts.Allowed, r.Style))
                    found.Add(Issue(StandardsIssueType.TextStyle, ts.Severity, [r.Handle], at, $"{r.Type} {r.Handle} uses text style '{r.Style}', which is not allowed.", "Set the style (update_entities_batch set.style) to an allowed text style (summary.allowedTextStyles).", r.Layer));
                if (checks.Contains(StandardsIssueType.TextHeight) && rules.TextHeights is { AllowedMm.Count: > 0 } th && isText && r.TextHeightMm is { } height && InSpace(th.Space, r.Space) && !th.AllowedMm.Any(a => Math.Abs(a - height) <= th.ToleranceMm))
                    found.Add(Issue(StandardsIssueType.TextHeight, th.Severity, [r.Handle], at, $"{r.Type} {r.Handle} is {height:0.##} mm high, not an allowed height.", "Set heightMm (update_entities_batch) to the nearest allowed height (summary.allowedTextHeightsMm).", r.Layer, valueMm: height));
                if (checks.Contains(StandardsIssueType.DimStyle) && rules.DimStyles is { Allowed.Count: > 0 } ds && isDimension && r.Style is not null && !EntityFilter.Matches(ds.Allowed, r.Style))
                    found.Add(Issue(StandardsIssueType.DimStyle, ds.Severity, [r.Handle], at, $"{r.Type} {r.Handle} uses dimension style '{r.Style}', which is not allowed.", "Set dimStyle (update_entities_batch) to an allowed dimension style (summary.allowedDimStyles).", r.Layer));
            }

            if (checks.Contains(StandardsIssueType.LayerNaming) && rules.LayerNaming is { Regex: { } layerRegex } naming)
                foreach (var layer in tables.Layers.Where(l => !l.IsXrefDependent && !l.IsHidden && !Exempts(naming.Exempt, l.Name) && !layerRegex.IsMatch(l.Name)))
                    found.Add(Issue(StandardsIssueType.LayerNaming, naming.Severity, [], null, $"Layer '{layer.Name}' does not follow the naming rule{(naming.Description is null ? "" : $" ({naming.Description})")}.", "Rename the layer to the project convention (LAYER command / -RENAME).", layer.Name));
            if (checks.Contains(StandardsIssueType.BlockNaming) && rules.BlockNaming is { Regex: { } blockRegex } blockNaming)
                foreach (var block in tables.Blocks.Where(b => !b.IsAnonymous && !b.IsLayout && !b.IsXref && !b.IsDependent && !Exempts(blockNaming.Exempt, b.Name) && !blockRegex.IsMatch(b.Name)))
                    found.Add(Issue(StandardsIssueType.BlockNaming, blockNaming.Severity, [], null, $"Block '{block.Name}' does not follow the naming rule{(blockNaming.Description is null ? "" : $" ({blockNaming.Description})")}.", "Rename the block definition (RENAME command) to the project convention."));
            if (checks.Contains(StandardsIssueType.UnusedLayer) && rules.UnusedLayers is { } unused && wholeDrawing)
            {
                var used = new HashSet<string>(records.Select(r => r.Layer).Concat(tables.LayersUsedInBlocks), StringComparer.OrdinalIgnoreCase);
                foreach (var layer in tables.Layers.Where(l => !l.IsXrefDependent && !l.IsHidden && !used.Contains(l.Name) && !Exempts(unused.Exempt, l.Name)))
                    found.Add(Issue(StandardsIssueType.UnusedLayer, unused.Severity, [], null, $"Layer '{layer.Name}' has no entities in model/paper space or in any block.", "Purge it (PURGE) if it is not a template layer.", layer.Name));
            }
        }
        catch (RegexMatchTimeoutException exception)
        {
            throw new ArgumentException($"rule set '{rules.Name}': a naming pattern took too long on '{exception.Input}' (catastrophic backtracking) — simplify layerNaming/blockNaming.pattern.");
        }

        var ordered = AuditIssue.Ordered(found);
        return ordered.Select((issue, i) => issue with { IssueId = $"{IdPrefix}-{i + 1:0000}" }).ToArray();
    }

    /// <summary>An exemption list exempts only what it names: an empty list exempts nothing (a filter's empty list means "anything").</summary>
    public static bool Exempts(IReadOnlyList<string> exempt, string value) => exempt.Count > 0 && EntityFilter.Matches(exempt, value);

    private static bool IsOverride(string? value) => value is not null && !string.Equals(value, "ByLayer", StringComparison.OrdinalIgnoreCase);

    private static string ByBlockNote(string? value) => string.Equals(value, "ByBlock", StringComparison.OrdinalIgnoreCase) ? " (ByBlock on a top-level entity behaves as an override)" : "";

    /// <summary>"LineWeight025" → "0.25 mm"; ByBlock stays as it is.</summary>
    private static string Lineweight(string? value) =>
        value is not null && value.StartsWith("LineWeight", StringComparison.OrdinalIgnoreCase) && int.TryParse(value["LineWeight".Length..], out var hundredths) ? $"{hundredths / 100.0:0.##} mm" : value ?? "?";

    private static bool InSpace(string rule, string? space) => rule.Trim().ToLowerInvariant() switch
    {
        "model" => string.Equals(space, "Model", StringComparison.OrdinalIgnoreCase),
        "paper" => space is not null && !string.Equals(space, "Model", StringComparison.OrdinalIgnoreCase),
        _ => true,
    };

    private static AuditIssue Issue(string type, string severity, IReadOnlyList<string> handles, Pt? at, string description, string action, string? layer = null, string? rule = null, double? valueMm = null) =>
        new("", AuditCategory.Standards, type, severity, handles, at?.Rounded(), valueMm, description, action, layer, rule);
}
