using System.Globalization;
using HPAutoCad.Aec.Geometry;
using HPAutoCad.Aec.Issues;
using HPAutoCad.Aec.Spatial;

namespace HPAutoCad.Aec.Coordination;

/// <summary>Something a clash is checked between: a classified entity with its plan shape and the space it lives in.</summary>
public sealed record ClashSubject(string Handle, string AecType, string Layer, PlanShape Shape, string Space = "Model");

/// <summary>Issue types the clash check reports (category <c>coordination</c>).</summary>
public static class ClashIssueType
{
    /// <summary>An MEP element interpenetrates a member or another service's run in plan (crosses, overlaps, lies inside, ends inside).</summary>
    public const string Hard = "hard_clash";
    /// <summary>The shapes stay apart but closer than the required clearance.</summary>
    public const string Clearance = "clearance_clash";
    /// <summary>Boundaries meet without interpenetration: a joint, a tee, equipment on a run, members meeting.</summary>
    public const string Contact = "contact";
    /// <summary>One lies inside, or crosses the edge of, an area outline (room, slab) — the normal state of a plan.</summary>
    public const string AreaOverlap = "area_overlap";

    public const string Category = "coordination";
    public const string Prefix = "CL";

    public static readonly IReadOnlyList<string> All = [Hard, Clearance, Contact, AreaOverlap];
}

/// <summary>
///     Clashes between two sets in plan: a broad phase over bounding boxes grown by the clearance, a narrow phase over the shapes
///     (<see cref="ClashClassifier"/> for the pairs that meet, the boundary distance against the required clearance for the rest).
///     Subjects pair only within one space. A plan has no heights: a duct crossing a beam in plan is a clash to check on the sections,
///     and the description says so. Pure.
/// </summary>
public static class ClashDetector
{
    /// <summary>Candidate pairs the broad phase may hand to the narrow phase; beyond it the caller narrows a set.</summary>
    public const int MaxPairs = 200_000;

    /// <summary>Segment pairs the narrow phase may test in one call (a box test each): bounds the work the pair cap alone does not.</summary>
    public const long MaxSegmentPairs = 200_000_000;

    public sealed record Outcome(IReadOnlyList<AuditIssue> Issues, int PairsChecked, int Hard, int Clearance, int Contacts, int AreaOverlaps, bool Capped);

    public static Outcome Detect(IReadOnlyList<ClashSubject> setA, IReadOnlyList<ClashSubject> setB, bool sameSet, double clearanceMm, GeometryTolerance tol, CancellationToken ct)
    {
        var index = new SpatialIndex<int>();
        for (var j = 0; j < setB.Count; j++) index.Insert(setB[j].Shape.Bounds, j);
        var issues = new List<AuditIssue>();
        var seen = new HashSet<(string, string)>();
        var (pairs, work, hard, clearance, contacts, areas, capped) = (0, 0L, 0, 0, 0, 0, false);
        for (var i = 0; i < setA.Count && !capped; i++)
        {
            var a = setA[i];
            foreach (var j in index.Query(a.Shape.Bounds, clearanceMm + tol.PointEquality).OrderBy(j => j))
            {
                ct.ThrowIfCancellationRequested();
                var b = setB[j];
                if (a.Handle == b.Handle || !string.Equals(a.Space, b.Space, StringComparison.OrdinalIgnoreCase)) continue;
                var key = string.CompareOrdinal(a.Handle, b.Handle) < 0 ? (a.Handle, b.Handle) : (b.Handle, a.Handle);
                if (sameSet && !seen.Add(key)) continue;
                work += (long)a.Shape.Segments.Count * b.Shape.Segments.Count;
                if (pairs >= MaxPairs || work > MaxSegmentPairs) { capped = true; break; }
                pairs++;
                var match = SpatialPredicates.Evaluate(a.Shape, b.Shape, SpatialRelation.Intersects, tol);
                if (match.Holds)
                {
                    var v = ClashClassifier.Classify(a, b, match, tol);
                    if (v.Type == ClashIssueType.Hard) hard++; else if (v.Type == ClashIssueType.Contact) contacts++; else areas++;
                    issues.Add(Issue(v.Type, v.Severity, a, b, v.At, 0, $"{Name(a)} {v.How} {Name(b)} in plan at ({Mm(v.At.X)}, {Mm(v.At.Y)}){More(match)}; {Note(v.Type)}", Action(v.Type)));
                    continue;
                }

                if (clearanceMm <= 0) continue;
                var distance = SpatialPredicates.DistanceXY(a.Shape, b.Shape, tol, clearanceMm);
                if (distance >= clearanceMm) continue;
                clearance++;
                issues.Add(Issue(ClashIssueType.Clearance, IssueSeverity.Warning, a, b, ClashClassifier.Between(a.Shape, b.Shape), Math.Round(distance, 1),
                    $"{Name(a)} is {Mm(distance)} mm from {Name(b)} in plan; {Mm(clearanceMm)} mm required.", "Move one apart to the required clearance, or confirm the rule does not apply to this pair."));
            }
        }

        return new Outcome(AuditIssue.Ordered(issues).Select((i, n) => i with { IssueId = $"{ClashIssueType.Prefix}-{n + 1:0000}" }).ToArray(), pairs, hard, clearance, contacts, areas, capped);
    }

    private static string More(SpatialMatch match) => match.Points.Count > 1 ? $" (+{match.Points.Count - 1} more)" : "";

    private static string Note(string type) => type switch
    {
        ClashIssueType.Hard => "check the section — the plan has no heights.",
        ClashIssueType.Contact => "a joint in plan, not a clash.",
        _ => "an area outline holds what is on its floor.",
    };

    private static string Action(string type) => type switch
    {
        ClashIssueType.Hard => "Reroute or resize one, or confirm on the sections that they pass at different levels; MEP through structure: aec_create_opening_requests.",
        ClashIssueType.Contact => "Nothing unless the members should not connect.",
        _ => "Nothing unless the run should not be on this floor.",
    };

    private static string Name(ClashSubject s) => $"{s.AecType} {s.Handle}";

    private static string Mm(double value) => value.ToString("0.#", CultureInfo.InvariantCulture);

    private static AuditIssue Issue(string type, string severity, ClashSubject a, ClashSubject b, Pt at, double actualMm, string description, string action) =>
        new("", ClashIssueType.Category, type, severity, [a.Handle, b.Handle], at.Rounded(), actualMm, description, action, Layer: a.Layer, Rule: $"{a.AecType}×{b.AecType}");
}
