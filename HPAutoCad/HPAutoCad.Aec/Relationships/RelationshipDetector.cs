using HPAutoCad.Aec.Classification;
using HPAutoCad.Aec.Geometry;
using HPAutoCad.Aec.Spatial;

namespace HPAutoCad.Aec.Relationships;

/// <summary>Relations <c>get_entity_relationships</c> reports; the tool argument uses these names.</summary>
public static class RelationType
{
    public const string Intersect = "intersect";
    public const string Connected = "connected";
    public const string Near = "near";
    public const string Inside = "inside";
    public const string Contains = "contains";
    public const string Touching = "touching";
    public const string Aligned = "aligned";
    public const string Parallel = "parallel";
    public const string Perpendicular = "perpendicular";

    public static readonly IReadOnlyList<string> All = [Intersect, Connected, Near, Inside, Contains, Touching, Aligned, Parallel, Perpendicular];

    /// <summary>Relations that hold in both directions; over one set they are reported once per pair.</summary>
    public static bool IsSymmetric(string relation) => relation is not (Inside or Contains);

    /// <summary>Relations tested on main axes, independent of proximity.</summary>
    public static bool IsAxis(string relation) => relation is Aligned or Parallel or Perpendicular;
}

/// <summary>
///     One relationship between two entities with the confidence and the measure behind it: a distance/gap
///     in mm (<see cref="ValueMm"/>) for proximity relations and <c>aligned</c> (the offset between the axes),
///     an angle in degrees (<see cref="AngleDeg"/>) for <c>parallel</c>/<c>perpendicular</c>.
/// </summary>
public sealed record AecRelationship(
    string Source, string? SourceAecType, string Target, string? TargetAecType,
    string Relation, double Confidence, double? ValueMm, double? AngleDeg, Pt? LocationMm);

/// <summary>What a detection produced: the relationships kept and how many held in total (the cap may have stopped the collection).</summary>
public sealed record RelationshipOutcome(IReadOnlyList<AecRelationship> Items, int Total);

/// <summary>
///     Derives relationships between entities from their plan geometry: <c>intersect</c> (boundaries cross or
///     one lies inside the other), <c>connected</c> (an end of a run reaches the other entity within the
///     connection tolerance — beam to column, pipe to fitting), <c>near</c> (within a radius), <c>inside</c> /
///     <c>contains</c>, <c>touching</c>, and the axis relations <c>aligned</c> / <c>parallel</c> /
///     <c>perpendicular</c> on the main axes of linear members. Confidence is 1 for exact predicates and
///     decays with the gap for connected/near. Pure: no AutoCAD types.
/// </summary>
public static class RelationshipDetector
{
    /// <summary>Source × target pairs an axis relation may examine (every pair, no proximity gate) before the request is refused.</summary>
    public const long MaxAxisPairs = 2_000_000;

    /// <summary>Connected/near confidence drops linearly to this fraction at the tolerance/radius limit.</summary>
    private const double ConfidenceAtLimit = 0.5;

    /// <param name="sameSet">The two lists are the same set: symmetric relations are reported once per pair (source handle ordinal-below target).</param>
    public static RelationshipOutcome Detect(IReadOnlyList<AecObject> sources, IReadOnlyList<AecObject> targets, IReadOnlySet<string> relations,
        GeometryTolerance tol, double nearRadiusMm, CancellationToken ct, int maxResults, bool sameSet = false)
    {
        var axisRelations = relations.Any(RelationType.IsAxis);
        if (axisRelations && (long)sources.Count * targets.Count > MaxAxisPairs)
            throw new ArgumentException($"aligned/parallel/perpendicular test every source against every target: {sources.Count} × {targets.Count} pairs is above {MaxAxisPairs:N0}; narrow the filters.");

        var results = new List<AecRelationship>();
        var total = 0;
        var index = new SpatialIndex<AecObject>();
        foreach (var t in targets)
            if (t.Shape is not null) index.Insert(t.Shape.Bounds, t);

        var reach = Math.Max(tol.EndpointConnection, relations.Contains(RelationType.Near) ? nearRadiusMm : 0);

        foreach (var s in sources)
        {
            ct.ThrowIfCancellationRequested();
            if (s.Shape is null) continue;
            var candidates = axisRelations ? index.All() : index.Query(s.Shape.Bounds, reach);
            foreach (var t in candidates)
            {
                if (t.Handle == s.Handle || t.Shape is null) continue;
                var close = s.Shape.Bounds.IntersectsXY(t.Shape.Bounds, reach);
                foreach (var relation in relations)
                {
                    if (sameSet && RelationType.IsSymmetric(relation) && string.CompareOrdinal(s.Handle, t.Handle) >= 0) continue;
                    var r = Evaluate(s, t, relation, tol, nearRadiusMm, close);
                    if (r is null) continue;
                    total++;
                    if (results.Count < maxResults) results.Add(r);
                }
            }
        }

        return new RelationshipOutcome(results, total);
    }

    /// <summary>The relationship of one pair under one relation, or null when it does not hold.</summary>
    public static AecRelationship? Evaluate(AecObject s, AecObject t, string relation, GeometryTolerance tol, double nearRadiusMm, bool boxesClose = true)
    {
        var a = s.Shape!;
        var b = t.Shape!;
        switch (relation)
        {
            case RelationType.Intersect:
            {
                if (!boxesClose) return null;
                var m = SpatialPredicates.Evaluate(a, b, SpatialRelation.Intersects, tol);
                // Crossing point when the boundaries meet; the inner centroid when one lies inside the other.
                return m.Holds ? Make(s, t, relation, 1, 0, null, FirstOr(m.Points, SpatialPredicates.IsWithin(a, b, tol) ? a.Centroid : b.Centroid)) : null;
            }
            case RelationType.Connected:
            {
                if (!boxesClose) return null;
                var (gap, at) = ConnectionGap(a, b, tol);
                if (gap is null || gap > tol.EndpointConnection) return null;
                return Make(s, t, relation, 1 - gap.Value / tol.EndpointConnection * (1 - ConfidenceAtLimit), gap, null, at);
            }
            case RelationType.Near:
            {
                if (!boxesClose) return null;
                var d = SpatialPredicates.DistanceXY(a, b, tol);
                if (d > nearRadiusMm) return null;
                return Make(s, t, relation, 1 - d / nearRadiusMm * (1 - ConfidenceAtLimit), d, null, ClosestVertex(a, b));
            }
            case RelationType.Inside:
                return boxesClose && SpatialPredicates.IsWithin(a, b, tol) ? Make(s, t, relation, 1, null, null, a.Centroid) : null;
            case RelationType.Contains:
                return boxesClose && SpatialPredicates.IsWithin(b, a, tol) ? Make(s, t, relation, 1, null, null, b.Centroid) : null;
            case RelationType.Touching:
            {
                if (!boxesClose) return null;
                var m = SpatialPredicates.Evaluate(a, b, SpatialRelation.Touches, tol);
                return m.Holds ? Make(s, t, relation, 1, m.DistanceMm, null, FirstOr(m.Points, ClosestVertex(a, b))) : null;
            }
            case RelationType.Aligned:
            case RelationType.Parallel:
            case RelationType.Perpendicular:
            {
                var axisA = ShapeMetrics.MainAxis(a);
                var axisB = ShapeMetrics.MainAxis(b);
                if (axisA is null || axisB is null) return null;
                var holds = relation switch
                {
                    RelationType.Aligned => GeometryMath.AreCollinearXY(axisA.Value, axisB.Value, tol.Collinearity, tol.ParallelAngle),
                    RelationType.Parallel => GeometryMath.AreParallelXY(axisA.Value, axisB.Value, tol.ParallelAngle),
                    _ => GeometryMath.ArePerpendicularXY(axisA.Value, axisB.Value, tol.ParallelAngle),
                };
                if (!holds) return null;
                var angle = GeometryMath.AngleBetweenXY(axisA.Value.DirectionXY, axisB.Value.DirectionXY);
                var acute = Math.Min(angle, 180 - angle);
                return relation == RelationType.Aligned
                    ? Make(s, t, relation, 1, GeometryMath.DistanceToLineXY(axisA.Value, axisB.Value.Mid), acute, axisB.Value.Mid)
                    : Make(s, t, relation, 1, null, acute, null);
            }
            default:
                throw new ArgumentException($"relation must be one of {string.Join(", ", RelationType.All)}.");
        }
    }

    /// <summary>
    ///     How far the nearest end of a run is from the other entity (its boundary, or its ends when both are runs): 0 when it lands on
    ///     it, null when neither shape has an end to test (two footprints connect by touching, not by ends).
    /// </summary>
    public static (double? Gap, Pt? At) ConnectionGap(PlanShape a, PlanShape b, GeometryTolerance tol)
    {
        double? best = null;
        Pt? at = null;
        void Consider(Pt end, PlanShape other)
        {
            var d = other.ContainsPointXY(end, tol.PointEquality) ? 0 : other.DistanceToBoundaryXY(end);
            if (best is null || d < best) (best, at) = (d, end);
        }

        foreach (var end in a.Endpoints) Consider(end, b);
        foreach (var end in b.Endpoints) Consider(end, a);
        return (best is null ? null : Math.Round(best.Value, 2), at);
    }

    private static Pt FirstOr(IReadOnlyList<Pt> points, Pt fallback) => points.Count > 0 ? points[0] : fallback;

    /// <summary>The vertex of <paramref name="a"/> closest to <paramref name="b"/> — a point the AI can zoom to for a proximity relation.</summary>
    private static Pt ClosestVertex(PlanShape a, PlanShape b)
    {
        var best = a.Start;
        var bestDistance = double.PositiveInfinity;
        foreach (var v in a.Vertices)
        {
            var d = b.DistanceToBoundaryXY(v);
            if (d < bestDistance) (best, bestDistance) = (v, d);
        }

        return best;
    }

    private static AecRelationship Make(AecObject s, AecObject t, string relation, double confidence, double? valueMm, double? angleDeg, Pt? at) =>
        new(s.Handle, s.AecType == AecType.Unknown ? null : s.AecType, t.Handle, t.AecType == AecType.Unknown ? null : t.AecType, relation, Math.Round(confidence, 3),
            valueMm is null ? null : Math.Round(valueMm.Value, 2), angleDeg is null ? null : Math.Round(angleDeg.Value, 3), at?.Rounded());
}
