using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using HPAutoCad.Aec.Classification;
using HPAutoCad.Aec.Geometry;
using HPAutoCad.Aec.Model;
using HPAutoCad.Aec.Relationships;
using HPRebar.McpBridge.Core.Scripting;

namespace HPAutoCad.Aec.Cad;

/// <summary>
///     Reads entities through <see cref="EntityQueryService"/> and classifies them; builds the nearby-text
///     index only when the rule set asks for it (one TEXT/MTEXT selection over model space, capped).
/// </summary>
public static class ClassificationService
{
    public const int MaxTextIndex = 5000;

    public sealed record Outcome(IReadOnlyList<AecObject> Objects, EntityQueryResult Query, ClassificationRuleSet Rules, int TextsIndexed, bool TextsTruncated);

    public static Outcome Classify(Database db, Editor ed, Transaction tr, ScriptUnits units, CancellationToken ct,
        EntityFilter filter, ClassificationRuleSet rules, GeometryTolerance tol, IReadOnlySet<string>? disciplines, double minConfidence, bool includeUnknown, int maxCandidates,
        (AecClassifier.TextIndex Index, bool Truncated)? texts = null)
    {
        var query = EntityQueryService.Query(db, ed, tr, units, ct, filter, tol, maxCandidates, 0, false, maxCandidates);
        var (index, truncated) = texts ?? (rules.UsesNearbyText ? BuildTextIndex(db, ed, tr, units, ct, tol, filter.Space) : (AecClassifier.TextIndex.Empty, false));
        var objects = AecClassifier.Classify(query.Records, rules, index, tol, ct, disciplines, minConfidence, includeUnknown);
        return new Outcome(objects, query, rules, index.Count, truncated);
    }

    /// <summary>
    ///     Every TEXT/MTEXT in the given space (capped) so rules can look for marks and names around an entity. A handle-based
    ///     filter has space "all": the index then spans model and paper space, which only widens what a mark can be found in.
    /// </summary>
    public static (AecClassifier.TextIndex Index, bool Truncated) BuildTextIndex(Database db, Editor ed, Transaction tr, ScriptUnits units, CancellationToken ct, GeometryTolerance tol, string space)
    {
        var textFilter = new EntityFilter { Types = ["TEXT", "MTEXT"], Space = space };
        var texts = EntityQueryService.Query(db, ed, tr, units, ct, textFilter, tol, MaxTextIndex, 0, false, MaxTextIndex);
        return (new AecClassifier.TextIndex(texts.Records), texts.Truncated);
    }

    /// <summary>Relationships between a source set and a target set, both classified first so the answer names beam/column, not handle/handle.</summary>
    public static (RelationshipOutcome Relationships, Outcome Sources, Outcome Targets) Relationships(Database db, Editor ed, Transaction tr, ScriptUnits units, CancellationToken ct,
        EntityFilter source, EntityFilter target, ClassificationRuleSet rules, IReadOnlySet<string> relations, GeometryTolerance tol, double nearRadiusMm, int maxCandidates, int maxResults)
    {
        // One text index for both sets; an all-space index when the two filters disagree on the space.
        var texts = rules.UsesNearbyText ? BuildTextIndex(db, ed, tr, units, ct, tol, target.IsEmpty || target.Space == source.Space ? source.Space : "all") : (AecClassifier.TextIndex.Empty, false);
        var sources = Classify(db, ed, tr, units, ct, source, rules, tol, null, 0, includeUnknown: true, maxCandidates, texts);
        var targets = target.IsEmpty ? sources : Classify(db, ed, tr, units, ct, target, rules, tol, null, 0, includeUnknown: true, maxCandidates, texts);
        var found = RelationshipDetector.Detect(sources.Objects, targets.Objects, relations, tol, nearRadiusMm, ct, maxResults, sameSet: target.IsEmpty);
        return (found, sources, targets);
    }
}
