using System.Diagnostics;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using HPAutoCad.Aec.Cad;
using HPAutoCad.Aec.Classification;
using HPAutoCad.Aec.Geometry;
using HPAutoCad.Aec.Model;
using HPAutoCad.Aec.Relationships;
using HPRebar.McpBridge.Core.Scripting;

namespace HPAutoCad.Aec;

/// <summary>The semantic tools: classification into AEC objects and relationships between them.</summary>
public static partial class AecTools
{
    /// <summary>Default search radius of the <c>near</c> relation when the tool gives none.</summary>
    public const double DefaultNearRadiusMm = 100;

    /// <summary>An AEC object with evidence, properties and alternatives is ~0.5–1 KB: this many per page stays under the 64 KB result cap.</summary>
    public const int MaxClassifyLimit = 50;

    /// <summary>A relationship is ~200 B: this many per page stays under the cap.</summary>
    public const int MaxRelationshipLimit = 250;

    public static AnalysisResult<AecObject> ClassifyEntities(Database db, Editor ed, Transaction tr, ScriptUnits units, CancellationToken ct, Action<string> log,
        ScriptArgs filterArgs, IReadOnlyList<string> handles, IReadOnlyList<string> disciplines, double minConfidence, bool includeUnknown, string? ruleSet,
        int limit, int offset, int maxCandidates)
    {
        var watch = Stopwatch.StartNew();
        var result = new AnalysisResult<AecObject>();
        var filter = ResolveFilter(filterArgs, handles, result);
        var rules = ClassificationRuleSet.Load(ruleSet);
        var wanted = ParseDisciplines(disciplines, result);
        minConfidence = Math.Clamp(minConfidence, 0, 1);
        if (limit > MaxClassifyLimit) result.Warn($"limit is capped at {MaxClassifyLimit} objects per page; page with offset.");
        limit = Math.Clamp(limit, 1, MaxClassifyLimit);
        offset = Math.Max(0, offset);
        maxCandidates = Math.Clamp(maxCandidates, 1, MaxCandidatesCeiling);

        var outcome = ClassificationService.Classify(db, ed, tr, units, ct, filter, rules, GeometryTolerance.Default, wanted, minConfidence, includeUnknown, maxCandidates);
        result.Errors.AddRange(outcome.Query.Errors);
        result.Warnings.AddRange(outcome.Query.Warnings);
        if (outcome.TextsTruncated) result.Warn($"more than {ClassificationService.MaxTextIndex} texts in the drawing: nearby-text evidence covers the first {ClassificationService.MaxTextIndex} only.");
        result.Items = outcome.Objects.Skip(offset).Take(limit).ToArray();
        result.Count = outcome.Objects.Count;
        result.Offset = offset;
        result.Truncated = outcome.Query.Truncated || outcome.Objects.Count > offset + result.Items.Count;
        result.Summary = new
        {
            examined = outcome.Query.Records.Count,
            classified = outcome.Objects.Count(o => o.AecType != AecType.Unknown),
            unknown = outcome.Query.Records.Count - outcome.Objects.Count(o => o.AecType != AecType.Unknown),
            byAecType = outcome.Objects.Where(o => o.AecType != AecType.Unknown).GroupBy(o => o.AecType).OrderByDescending(g => g.Count()).ToDictionary(g => g.Key, g => g.Count()),
            byDiscipline = outcome.Objects.Where(o => o.AecType != AecType.Unknown).GroupBy(o => o.Discipline).ToDictionary(g => g.Key, g => g.Count()),
            ruleSet = new { name = rules.Name, source = rules.Source, rules = rules.Rules.Count, textsIndexed = outcome.TextsIndexed },
            minConfidence,
        };
        log($"classify_aec_entities: {outcome.Query.Records.Count} entities, {result.Count} objects ({rules.Rules.Count} rules '{rules.Name}'), {watch.ElapsedMilliseconds} ms");
        return result;
    }

    public static AnalysisResult<AecRelationship> EntityRelationships(Database db, Editor ed, Transaction tr, ScriptUnits units, CancellationToken ct, Action<string> log,
        ScriptArgs sourceArgs, IReadOnlyList<string> handles, ScriptArgs targetArgs, IReadOnlyList<string> relations, double? maxDistanceMm, ScriptArgs toleranceArgs, string? ruleSet,
        int limit, int maxCandidates)
    {
        var watch = Stopwatch.StartNew();
        var result = new AnalysisResult<AecRelationship>();
        var source = ResolveFilter(sourceArgs, handles, result);
        var target = EntityFilter.From(targetArgs, out var unknownTarget);
        foreach (var key in unknownTarget) result.Warn($"target.{key} is not a known filter key.");
        if (source.IsEmpty) throw new ArgumentException("Give the source a filter (types, layers, handles…) — relationships over the whole drawing are not allowed.");
        var tol = GeometryTolerance.From(toleranceArgs, out var unknownTolerance);
        foreach (var key in unknownTolerance) result.Warn($"tolerance.{key} is not a known tolerance or not positive.");

        var wanted = new HashSet<string>(StringComparer.Ordinal);
        foreach (var r in relations)
        {
            var known = RelationType.All.FirstOrDefault(k => string.Equals(k, r.Trim().Replace('-', '_'), StringComparison.OrdinalIgnoreCase));
            if (known is null) result.Warn($"relations: '{r}' is not known (known: {string.Join(", ", RelationType.All)}).");
            else wanted.Add(known);
        }

        if (wanted.Count == 0) wanted.UnionWith([RelationType.Intersect, RelationType.Connected, RelationType.Inside, RelationType.Contains, RelationType.Touching]);
        var nearRadius = maxDistanceMm is > 0 ? maxDistanceMm.Value : DefaultNearRadiusMm;
        if (wanted.Contains(RelationType.Near) && maxDistanceMm is null) result.Warn($"near without maxDistance uses {DefaultNearRadiusMm} mm.");
        if (limit > MaxRelationshipLimit) result.Warn($"limit is capped at {MaxRelationshipLimit} relationships per call.");
        limit = Math.Clamp(limit, 1, MaxRelationshipLimit);
        maxCandidates = Math.Clamp(maxCandidates, 1, MaxCandidatesCeiling);

        var rules = ClassificationRuleSet.Load(ruleSet);
        var (found, sources, targets) = ClassificationService.Relationships(db, ed, tr, units, ct, source, target, rules, wanted, tol, nearRadius, maxCandidates, limit);
        result.Errors.AddRange(sources.Query.Errors);
        result.Warnings.AddRange(sources.Query.Warnings.Select(w => "source: " + w));
        if (!ReferenceEquals(targets, sources))
        {
            result.Errors.AddRange(targets.Query.Errors);
            result.Warnings.AddRange(targets.Query.Warnings.Select(w => "target: " + w));
        }

        if (sources.TextsTruncated) result.Warn($"more than {ClassificationService.MaxTextIndex} texts in the drawing: nearby-text evidence covers the first {ClassificationService.MaxTextIndex} only.");
        result.Items = found.Items;
        result.Count = found.Total;
        result.Truncated = found.Total > found.Items.Count || sources.Query.Truncated || targets.Query.Truncated;
        result.Summary = new
        {
            sources = sources.Objects.Count,
            targets = targets.Objects.Count,
            sameSet = ReferenceEquals(targets, sources),
            relations = wanted.OrderBy(r => r).ToArray(),
            byRelation = found.Items.GroupBy(r => r.Relation).ToDictionary(g => g.Key, g => g.Count()),
            nearRadiusMm = wanted.Contains(RelationType.Near) ? nearRadius : (double?)null,
            tolerance = tol,
            ruleSet = rules.Name,
        };
        log($"get_entity_relationships: {sources.Objects.Count} × {targets.Objects.Count}, {found.Total} relationship(s), {watch.ElapsedMilliseconds} ms");
        return result;
    }

    private static EntityFilter ResolveFilter<T>(ScriptArgs filterArgs, IReadOnlyList<string> handles, AnalysisResult<T> result)
    {
        var filter = EntityFilter.From(filterArgs, out var unknown);
        foreach (var key in unknown) result.Warn($"filter.{key} is not a known filter key (known: {string.Join(", ", EntityFilter.KnownKeys)}).");
        if (handles.Count == 0) return filter;
        if (!filter.IsEmpty) result.Warn("handles[] given: filter.* was ignored (handles select exactly those entities).");
        return new EntityFilter { Handles = handles.Select(h => h.Trim().ToUpperInvariant()).ToArray(), Space = "all" };
    }

    private static HashSet<string>? ParseDisciplines<T>(IReadOnlyList<string> disciplines, AnalysisResult<T> result)
    {
        if (disciplines.Count == 0) return null;
        var set = new HashSet<string>(StringComparer.Ordinal);
        foreach (var d in disciplines)
        {
            var known = Discipline.Normalize(d);
            if (known is null) result.Warn($"disciplines: '{d}' is not known (known: {string.Join(", ", Discipline.All)}).");
            else set.Add(known);
        }

        return set.Count == 0 ? null : set;
    }
}
