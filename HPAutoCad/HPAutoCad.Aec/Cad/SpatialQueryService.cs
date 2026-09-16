using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using HPAutoCad.Aec.Geometry;
using HPAutoCad.Aec.Model;
using HPAutoCad.Aec.Spatial;
using HPRebar.McpBridge.Core.Scripting;

namespace HPAutoCad.Aec.Cad;

/// <summary>One source/target pair that satisfies the relation.</summary>
public sealed record SpatialMatchItem(
    string SourceHandle, string SourceType, string SourceLayer,
    string TargetHandle, string TargetType, string TargetLayer,
    string Relation, double? DistanceMm, IReadOnlyList<Pt>? PointsMm);

/// <summary>
///     Source set × target set under one relation: both sets come from <see cref="EntityQueryService"/>
///     (so every filter key works on either side), the targets go into a <see cref="SpatialIndex{T}"/>,
///     and each source is tested only against the targets its box can reach. <c>nearest</c> keeps the
///     closest target per source; <c>distance_to</c> keeps every target within <c>maxDistance</c>.
/// </summary>
public static class SpatialQueryService
{
    /// <summary>Crossing points listed per match; a pipe zig-zagging over a beam rarely needs more to be located.</summary>
    public const int MaxPointsPerMatch = 8;

    public static AnalysisResult<SpatialMatchItem> Query(Database db, Editor ed, Transaction tr, ScriptUnits units, CancellationToken ct,
        EntityFilter source, EntityFilter target, SpatialRelation relation, double? maxDistanceMm, GeometryTolerance tol, int limit, int maxCandidates)
    {
        var result = new AnalysisResult<SpatialMatchItem>();
        if (relation == SpatialRelation.DistanceTo && maxDistanceMm is null) throw new ArgumentException("relation distance_to needs maxDistance (mm).");
        if (source.IsEmpty && target.IsEmpty) throw new ArgumentException("Give at least one of source/target a filter (types, layers, handles…): source × target over the whole drawing is not allowed.");

        var sources = EntityQueryService.Query(db, ed, tr, units, ct, source, tol, maxCandidates, 0, false, maxCandidates);
        var targets = EntityQueryService.Query(db, ed, tr, units, ct, target, tol, maxCandidates, 0, false, maxCandidates);
        result.Errors.AddRange(sources.Errors);
        result.Errors.AddRange(targets.Errors);
        result.Warnings.AddRange(sources.Warnings.Select(w => "source: " + w));
        result.Warnings.AddRange(targets.Warnings.Select(w => "target: " + w));

        var index = new SpatialIndex<AecEntityRecord>();
        var targetShapes = 0;
        foreach (var t in targets.Records)
        {
            if (t.Shape is null) continue;
            index.Insert(t.Shape.Bounds, t);
            targetShapes++;
        }

        var reach = relation switch
        {
            SpatialRelation.DistanceTo => maxDistanceMm!.Value,
            SpatialRelation.Nearest => maxDistanceMm ?? double.PositiveInfinity,
            _ => Math.Max(tol.EndpointConnection, tol.Collinearity),
        };

        var matches = new List<SpatialMatchItem>();
        var total = 0;
        foreach (var s in sources.Records)
        {
            ct.ThrowIfCancellationRequested();
            if (s.Shape is null) continue;

            IEnumerable<AecEntityRecord> candidates = double.IsInfinity(reach) ? index.All() : index.Query(s.Shape.Bounds, reach);
            SpatialMatchItem? nearest = null;
            foreach (var t in candidates)
            {
                if (t.Handle == s.Handle) continue;
                var match = SpatialPredicates.Evaluate(s.Shape, t.Shape!, relation, tol, maxDistanceMm);
                if (!match.Holds) continue;

                var item = new SpatialMatchItem(s.Handle, s.Type, s.Layer, t.Handle, t.Type, t.Layer, SpatialRelations.Name(relation),
                    match.DistanceMm is null ? null : Math.Round(match.DistanceMm.Value, 2),
                    match.Points.Count == 0 ? null : match.Points.Take(MaxPointsPerMatch).Select(p => p.Rounded()).ToArray());

                if (relation == SpatialRelation.Nearest)
                {
                    if (nearest is null || item.DistanceMm < nearest.DistanceMm) nearest = item;
                    continue;
                }

                total++;
                if (matches.Count < limit) matches.Add(item);
            }

            if (nearest is not null)
            {
                total++;
                if (matches.Count < limit) matches.Add(nearest);
            }
        }

        result.Items = matches;
        result.Count = total;
        result.Truncated = total > matches.Count || sources.Truncated || targets.Truncated;
        result.Summary = new
        {
            relation = SpatialRelations.Name(relation),
            sources = sources.Records.Count,
            sourcesWithoutGeometry = sources.Records.Count(r => r.Shape is null),
            targets = targetShapes,
            matches = total,
            maxDistanceMm = maxDistanceMm,
            tolerance = tol,
        };
        return result;
    }
}
