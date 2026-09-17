using HPAutoCad.Aec.Geometry;
using HPAutoCad.Aec.Spatial;

namespace HPAutoCad.Aec.Mep;

/// <summary>
///     The MEP network as a graph: a run end is connected when it meets another run's end (joined), lands on a run's body — another
///     run's or its own, a loop — (a branch tee) or touches a node entity, all within <c>tolerance.endpointConnection</c>; a node is
///     also attached to every run that passes through it (an inline valve, a pump, a VAV on an unbroken line). Runs that merely cross
///     each other in plan are not connected (a 2D plan does not know their heights) and are only counted. Runs joined directly are one
///     network; through a node, only runs of the same system join — supply and return meet at every AHU without becoming one network,
///     and a node belongs to every network it touches. Two runs of one system drawn over each other (collinear, or a copy a few mm
///     off) are a duplicate; a node no run touches is an orphan. An open end reports the nearest run or node within
///     <paramref name="nearMissMm"/>. Pure.
/// </summary>
public static class MepNetworkBuilder
{
    /// <summary>An open end this close to another run or node was probably meant to connect.</summary>
    public const double DefaultNearMissMm = 100;

    /// <summary>Beyond this many runs + nodes the caller narrows the filter (every end is checked against its neighbourhood).</summary>
    public const int MaxElements = 20_000;

    /// <summary>Duct runs with a parallel duct run this far away (or closer) over half their length look like the two sides of a double-line duct.</summary>
    public const double DoubleLineDuctMm = 1500;

    public static MepOutcome Build(IReadOnlyList<MepRun> runs, IReadOnlyList<MepNode> nodes, GeometryTolerance tol, double nearMissMm, CancellationToken ct)
    {
        if (runs.Count + nodes.Count > MaxElements) throw new ArgumentException($"{runs.Count} runs + {nodes.Count} nodes; the maximum is {MaxElements} — narrow the filter (layers, space) or split the plan.");
        var runIndex = new SpatialIndex<int>();
        for (var i = 0; i < runs.Count; i++) runIndex.Insert(runs[i].Shape.Bounds, i);
        var nodeIndex = new SpatialIndex<int>();
        for (var j = 0; j < nodes.Count; j++) nodeIndex.Insert(nodes[j].Shape.Bounds, j);
        var union = new UnionFind(runs.Count);
        var reach = Math.Max(nearMissMm, tol.EndpointConnection);

        // Nodes: attached to every run that ends on them or passes through them; same-system runs meeting at a node are one network.
        var runsAtNode = new List<int>[nodes.Count];
        for (var j = 0; j < nodes.Count; j++)
        {
            ct.ThrowIfCancellationRequested();
            var shape = nodes[j].Shape;
            runsAtNode[j] = runIndex.Query(shape.Bounds, tol.EndpointConnection).Where(i => Touches(runs[i].Shape, shape, tol)).OrderBy(i => i).ToList();
            foreach (var group in runsAtNode[j].GroupBy(i => runs[i].System, StringComparer.OrdinalIgnoreCase))
            {
                var first = group.First();
                foreach (var i in group) union.Join(first, i);
            }
        }

        var endpoints = new List<MepEndpoint>();
        for (var i = 0; i < runs.Count; i++)
        {
            ct.ThrowIfCancellationRequested();
            var run = runs[i];
            for (var end = 0; end < 2; end++)
            {
                var at = end == 0 ? run.Start : run.End;
                var connected = new List<string>();
                bool joined = false, tee = false, node = false;
                string? nearestHandle = null;
                var nearestGap = double.PositiveInfinity;
                foreach (var k in runIndex.Query(Box.Of(at, at), reach).OrderBy(k => k))
                {
                    var other = runs[k];
                    // the run's own body counts through its other segments and its far end (a loop or a ring main), never through the segment the end sits on
                    var d = k == i ? OwnBodyDistance(run.Shape, end, at) : other.Shape.DistanceToBoundaryXY(at);
                    if (d <= tol.EndpointConnection)
                    {
                        var atEnd = k != i ? other.Start.AlmostEqualsXY(at, tol.EndpointConnection) || other.End.AlmostEqualsXY(at, tol.EndpointConnection) : (end == 0 ? run.End : run.Start).AlmostEqualsXY(at, tol.EndpointConnection);
                        if (atEnd) joined = true; else tee = true;
                        connected.Add(other.Handle);
                        union.Join(i, k);
                    }
                    else if (d < nearestGap || d == nearestGap && string.CompareOrdinal(other.Handle, nearestHandle) < 0) (nearestHandle, nearestGap) = (other.Handle, d);
                }

                foreach (var j in nodeIndex.Query(Box.Of(at, at), reach).OrderBy(j => j))
                {
                    var shape = nodes[j].Shape;
                    var d = shape.ContainsPointXY(at, tol.PointEquality) ? 0 : shape.DistanceToBoundaryXY(at);
                    if (d <= tol.EndpointConnection) { node = true; connected.Add(nodes[j].Handle); }
                    else if (d < nearestGap || d == nearestGap && string.CompareOrdinal(nodes[j].Handle, nearestHandle) < 0) (nearestHandle, nearestGap) = (nodes[j].Handle, d);
                }

                var state = joined ? MepEndpointState.Joined : tee ? MepEndpointState.Tee : node ? MepEndpointState.Node : MepEndpointState.Open;
                var near = state == MepEndpointState.Open && nearestHandle is not null && nearestGap <= nearMissMm;
                endpoints.Add(new MepEndpoint(run.Handle, run.System, end, at.Rounded(), state, connected.Distinct().OrderBy(h => h.Length).ThenBy(h => h, StringComparer.Ordinal).ToArray(), near ? nearestHandle : null, near ? Math.Round(nearestGap, 1) : null));
            }
        }

        var (duplicates, crossings, doubleLine) = Pairs(runs, runIndex, tol, ct);

        var openByRun = endpoints.Where(e => e.IsOpen).GroupBy(e => e.RunHandle).ToDictionary(g => g.Key, g => g.ToArray(), StringComparer.Ordinal);
        var groups = new Dictionary<int, List<MepRun>>();
        var nodesByRoot = new Dictionary<int, HashSet<int>>();
        for (var i = 0; i < runs.Count; i++)
        {
            var root = union.Find(i);
            if (!groups.TryGetValue(root, out var g)) groups[root] = g = [];
            g.Add(runs[i]);
        }

        var orphans = new List<MepNode>();
        for (var j = 0; j < nodes.Count; j++)
        {
            if (runsAtNode[j].Count == 0) { orphans.Add(nodes[j]); continue; }
            foreach (var root in runsAtNode[j].Select(union.Find).Distinct())
            {
                if (!nodesByRoot.TryGetValue(root, out var set)) nodesByRoot[root] = set = [];
                set.Add(j);
            }
        }

        var networks = new List<MepNetwork>();
        foreach (var (root, g) in groups)
        {
            ct.ThrowIfCancellationRequested();
            var members = g.OrderBy(r => r.Handle.Length).ThenBy(r => r.Handle, StringComparer.Ordinal).ToArray();
            var open = members.SelectMany(r => openByRun.TryGetValue(r.Handle, out var e) ? e : []).ToArray();
            var attachedNodes = nodesByRoot.TryGetValue(root, out var set) ? set.Select(j => nodes[j]).OrderBy(n => n.Handle.Length).ThenBy(n => n.Handle, StringComparer.Ordinal).ToArray() : [];
            var systems = members.Select(r => r.System).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(s => s, StringComparer.Ordinal).ToArray();
            var handles = new HashSet<string>(members.Select(r => r.Handle), StringComparer.Ordinal);
            var overlap = duplicates.Where(d => handles.Contains(d.Handle)).Sum(d => d.OverlapMm);
            networks.Add(new MepNetwork("", members, attachedNodes, open, systems) { DuplicateOverlapMm = Math.Round(overlap, 1) });
        }

        var ordered = networks.OrderByDescending(n => n.LengthMm).ThenBy(n => n.Runs[0].Handle.Length).ThenBy(n => n.Runs[0].Handle, StringComparer.Ordinal)
            .Select((n, k) => n with { Id = $"N-{k + 1:000}" }).ToArray();
        return new MepOutcome(ordered, endpoints, orphans.OrderBy(n => n.Handle.Length).ThenBy(n => n.Handle, StringComparer.Ordinal).ToArray(), duplicates, crossings, runs.Count, nodes.Count, doubleLine);
    }

    /// <summary>A run touches a node when any of its vertices is inside the node or any of its segments comes within the connection tolerance of the node's boundary.</summary>
    private static bool Touches(PlanShape run, PlanShape node, GeometryTolerance tol) =>
        run.Vertices.Any(v => node.ContainsPointXY(v, tol.PointEquality) || node.DistanceToBoundaryXY(v) <= tol.EndpointConnection)
        || node.Vertices.Any(v => run.DistanceToBoundaryXY(v) <= tol.EndpointConnection)
        || Spatial.SpatialPredicates.IntersectionPoints(run, node, tol, proper: true).Count > 0;

    /// <summary>Distance from an end to the run's own body, ignoring the segment the end belongs to.</summary>
    private static double OwnBodyDistance(PlanShape shape, int end, Pt at)
    {
        var segments = shape.Segments;
        if (segments.Count < 2) return double.PositiveInfinity;
        var best = double.PositiveInfinity;
        for (var s = 0; s < segments.Count; s++)
        {
            if (end == 0 && s == 0 || end == 1 && s == segments.Count - 1) continue;
            best = Math.Min(best, segments[s].DistanceXY(at));
        }

        return best;
    }

    /// <summary>Pairwise: duplicates (same system, collinear or a copy within the connection tolerance off-axis, shared run beyond tolerance.duplicate), crossings, and duct runs that look like the two sides of a double-line duct.</summary>
    private static (List<MepDuplicate> Duplicates, int Crossings, int DoubleLineDucts) Pairs(IReadOnlyList<MepRun> runs, SpatialIndex<int> runIndex, GeometryTolerance tol, CancellationToken ct)
    {
        var duplicates = new List<MepDuplicate>();
        var crossings = 0;
        var doubleLine = new HashSet<int>();
        for (var i = 0; i < runs.Count; i++)
        {
            ct.ThrowIfCancellationRequested();
            foreach (var k in runIndex.Query(runs[i].Shape.Bounds, DoubleLineDuctMm))
            {
                if (k <= i) continue;
                var (a, b) = (runs[i], runs[k]);
                var sameSystem = string.Equals(a.System, b.System, StringComparison.OrdinalIgnoreCase);
                var (shared, from, to, offset) = LongestParallelRun(a.Shape, b.Shape, tol);
                if (sameSystem && shared > tol.Duplicate && offset <= tol.EndpointConnection)
                {
                    var (h1, h2) = string.CompareOrdinal(a.Handle, b.Handle) <= 0 ? (a.Handle, b.Handle) : (b.Handle, a.Handle);
                    duplicates.Add(new MepDuplicate(h1, h2, from.Rounded(), to.Rounded(), Math.Round(shared, 1)));
                    continue;
                }

                if (a.Kind == MepRunKind.Duct && b.Kind == MepRunKind.Duct && offset > tol.EndpointConnection && offset <= DoubleLineDuctMm && shared >= 0.5 * Math.Min(a.LengthMm, b.LengthMm)) { doubleLine.Add(i); doubleLine.Add(k); }
                if (a.Shape.Bounds.IntersectsXY(b.Shape.Bounds, tol.PointEquality) && Crosses(a.Shape, b.Shape, tol)) crossings++;
            }
        }

        return (duplicates.OrderBy(d => d.Handle.Length).ThenBy(d => d.Handle, StringComparer.Ordinal).ThenBy(d => d.OtherHandle, StringComparer.Ordinal).ToList(), crossings, doubleLine.Count);
    }

    /// <summary>The longest stretch where a segment of one chain runs parallel to a segment of the other, with the offset between their lines.</summary>
    private static (double Length, Pt From, Pt To, double Offset) LongestParallelRun(PlanShape a, PlanShape b, GeometryTolerance tol)
    {
        var best = (0.0, Pt.Origin, Pt.Origin, double.PositiveInfinity);
        foreach (var s in a.Segments)
            foreach (var t in b.Segments)
            {
                if (!s.Bounds.IntersectsXY(t.Bounds, DoubleLineDuctMm) || !GeometryMath.AreParallelXY(s, t, tol.ParallelAngle)) continue;
                var direction = s.DirectionXY;
                var lo = Math.Max(0, Math.Min((t.A - s.A).DotXY(direction), (t.B - s.A).DotXY(direction)));
                var hi = Math.Min(s.LengthXY, Math.Max((t.A - s.A).DotXY(direction), (t.B - s.A).DotXY(direction)));
                if (hi - lo <= best.Item1) continue;
                var offset = GeometryMath.DistanceToLineXY(s, Pt.Mid(t.A, t.B));
                best = (hi - lo, s.A + direction * lo, s.A + direction * hi, offset);
            }

        return best;
    }

    private static bool Crosses(PlanShape a, PlanShape b, GeometryTolerance tol)
    {
        foreach (var s in a.Segments)
            foreach (var t in b.Segments)
                if (s.Bounds.IntersectsXY(t.Bounds, tol.PointEquality) && GeometryMath.CrossesProperlyXY(s, t, tol.PointEquality, out _)) return true;
        return false;
    }

    private sealed class UnionFind(int size)
    {
        private readonly int[] _parent = Enumerable.Range(0, size).ToArray();

        public int Find(int x)
        {
            while (_parent[x] != x) { _parent[x] = _parent[_parent[x]]; x = _parent[x]; }
            return x;
        }

        public void Join(int a, int b)
        {
            var (ra, rb) = (Find(a), Find(b));
            if (ra != rb) _parent[Math.Max(ra, rb)] = Math.Min(ra, rb);
        }
    }
}
