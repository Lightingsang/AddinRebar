using HPAutoCad.Aec.Classification;
using HPAutoCad.Aec.Geometry;
using HPAutoCad.Aec.Issues;
using HPAutoCad.Aec.Spatial;

namespace HPAutoCad.Aec.Coordination;

/// <summary>What one pair of meeting shapes means: the issue type, its severity, the verb for the description and the point that shows it.</summary>
public sealed record ClashVerdict(string Type, string Severity, string How, Pt At);

/// <summary>
///     Turns "the two shapes meet in plan" into a verdict. A plan has no heights and its members are drawn to meet: a beam ends on a
///     column face or at its centre, a pipe tees into a main, a diffuser sits on the end of a duct, a door lies in its wall — those are
///     joints, reported as <c>contact</c> (info). A clash is an MEP element interpenetrating something it cannot share the plan with:
///     a duct crossing a beam, a pipe ending inside a column, two runs of different services crossing (<c>hard_clash</c>, critical).
///     Area outlines (rooms, slabs) hold or are crossed by everything on the floor: <c>area_overlap</c> (info). Pure.
/// </summary>
public static class ClashClassifier
{
    /// <summary>Types whose outline is a floor area, not a member: containment by them is the normal state of a plan.</summary>
    public static readonly IReadOnlyList<string> AreaTypes = [AecType.Room, AecType.StructuralSlab];

    public static bool IsArea(string aecType) => AreaTypes.Contains(aecType);
    public static bool IsMepRoute(string aecType) => aecType is AecType.Pipe or AecType.Duct or AecType.CableTray;
    public static bool IsMepNode(string aecType) => aecType is AecType.Equipment or AecType.Fixture or AecType.Terminal or AecType.Fitting;
    public static bool IsMep(string aecType) => IsMepRoute(aecType) || IsMepNode(aecType);

    /// <summary>The verdict for two shapes the intersects test already holds for.</summary>
    public static ClashVerdict Classify(ClashSubject a, ClashSubject b, SpatialMatch intersects, GeometryTolerance tol)
    {
        var proper = SpatialPredicates.Evaluate(a.Shape, b.Shape, SpatialRelation.Crosses, tol).Points;
        var overlaps = SpatialPredicates.CollinearOverlaps(a.Shape, b.Shape, tol);
        var aInB = SpatialPredicates.IsWithin(a.Shape, b.Shape, tol);
        var bInA = !aInB && SpatialPredicates.IsWithin(b.Shape, a.Shape, tol);
        var touch = intersects.Points.Count > 0 ? intersects.Points[0] : Inner(a, b, tol);
        var crossing = proper.Count > 0 ? proper[0] : overlaps.Count > 0 ? overlaps[0] : touch;

        if (IsArea(a.AecType) || IsArea(b.AecType))
            return new ClashVerdict(ClashIssueType.AreaOverlap, IssueSeverity.Info, aInB ? "lies inside" : bInA ? "encloses" : proper.Count > 0 ? "crosses the edge of" : "touches", aInB || bInA ? touch : crossing);
        if (!IsMep(a.AecType) && !IsMep(b.AecType))
            return new ClashVerdict(ClashIssueType.Contact, IssueSeverity.Info, overlaps.Count > 0 ? "runs along" : "meets", crossing);

        // At least one MEP element. A route meeting a node is the connection the network is made of; two nodes overlapping is not.
        if (IsMepRoute(a.AecType) && IsMepNode(b.AecType) || IsMepNode(a.AecType) && IsMepRoute(b.AecType))
            return new ClashVerdict(ClashIssueType.Contact, IssueSeverity.Info, "connects to", touch);
        if (IsMepRoute(a.AecType) && IsMepRoute(b.AecType))
        {
            if (overlaps.Count > 0) return new ClashVerdict(ClashIssueType.Hard, IssueSeverity.Critical, "overlaps", overlaps[0]);
            if (proper.Count == 0) return new ClashVerdict(ClashIssueType.Contact, IssueSeverity.Info, "meets", touch);
            // A branch drawn a little past the main it tees into crosses it "properly" by a few millimetres: still the joint.
            var joint = a.Shape.Endpoints.Concat(b.Shape.Endpoints).Any(e => e.DistanceXY(proper[0]) <= tol.EndpointConnection);
            return joint ? new ClashVerdict(ClashIssueType.Contact, IssueSeverity.Info, "tees into", proper[0]) : new ClashVerdict(ClashIssueType.Hard, IssueSeverity.Critical, "crosses", proper[0]);
        }

        if (aInB) return new ClashVerdict(ClashIssueType.Hard, IssueSeverity.Critical, "lies inside", touch);
        if (bInA) return new ClashVerdict(ClashIssueType.Hard, IssueSeverity.Critical, "encloses", touch);
        if (proper.Count > 0) return new ClashVerdict(ClashIssueType.Hard, IssueSeverity.Critical, "crosses", proper[0]);
        if (overlaps.Count > 0) return new ClashVerdict(ClashIssueType.Hard, IssueSeverity.Critical, "overlaps", overlaps[0]);
        // No boundary crossing, no containment: an end (or a corner) strictly inside the other's outline is a partial penetration.
        if (StrictlyInside(a.Shape, b.Shape, tol) is { } enters) return new ClashVerdict(ClashIssueType.Hard, IssueSeverity.Critical, "ends inside", enters);
        if (StrictlyInside(b.Shape, a.Shape, tol) is { } entered) return new ClashVerdict(ClashIssueType.Hard, IssueSeverity.Critical, "is entered by", entered);
        return new ClashVerdict(ClashIssueType.Contact, IssueSeverity.Info, "touches", touch);
    }

    /// <summary>A vertex of the candidate inside the ring and clear of its boundary, or null.</summary>
    private static Pt? StrictlyInside(PlanShape candidate, PlanShape ring, GeometryTolerance tol)
    {
        if (!ring.Closed) return null;
        foreach (var v in candidate.Vertices)
            if (ring.ContainsPointXY(v, tol.PointEquality) && ring.DistanceToBoundaryXY(v) > tol.PointEquality) return v;
        return null;
    }

    /// <summary>A point inside the overlap when the boundaries never meet: the inner one's centroid, else a vertex of it in or on the outer, else the centroid anyway.</summary>
    private static Pt Inner(ClashSubject a, ClashSubject b, GeometryTolerance tol)
    {
        var (inner, outer) = a.Shape.Bounds.AreaXY <= b.Shape.Bounds.AreaXY ? (a.Shape, b.Shape) : (b.Shape, a.Shape);
        var centre = inner.Closed ? inner.Centroid : inner.Bounds.Center;
        if (!outer.Closed || outer.ContainsPointXY(centre, tol.PointEquality)) return centre;
        foreach (var v in inner.Vertices)
            if (outer.ContainsPointXY(v, tol.PointEquality) || outer.DistanceToBoundaryXY(v) <= tol.EndpointConnection) return v;
        return centre;
    }

    /// <summary>The midpoint of the closest pair of boundary points: a vertex of one shape and its foot on the other (the closest pair of two polylines always has a vertex on one side).</summary>
    public static Pt Between(PlanShape a, PlanShape b)
    {
        var best = (Distance: double.PositiveInfinity, At: Pt.Mid(a.Bounds.Center, b.Bounds.Center));
        Feet(a, b, ref best);
        Feet(b, a, ref best);
        return best.At;
    }

    private static void Feet(PlanShape from, PlanShape to, ref (double Distance, Pt At) best)
    {
        foreach (var v in from.Vertices)
        foreach (var s in to.Segments)
        {
            if (s.Bounds.DistanceXY(Box.Of(v, v)) >= best.Distance) continue;
            var foot = s.ClosestPointXY(v);
            var d = foot.DistanceXY(v);
            if (d < best.Distance) best = (d, Pt.Mid(v, foot));
        }
    }
}
