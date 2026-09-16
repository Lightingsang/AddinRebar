using HPAutoCad.Aec.Geometry;
using HPAutoCad.Aec.Spatial;

namespace HPAutoCad.Aec.Architecture;

/// <summary>
///     The planar graph behind <see cref="RoomLoopFinder"/>: wall pieces cut at every crossing, T-junction and collinear overlap,
///     endpoints merged within the point tolerance, one undirected edge per node pair (with every wall handle that drew it).
///     Dangling chains are peeled before the faces are walked.
/// </summary>
internal sealed class WallGraph
{
    private readonly List<Pt> _nodes = [];
    private readonly Dictionary<(long X, long Y), List<int>> _cells = [];
    private readonly Dictionary<(int U, int V), Edge> _edges = [];
    private readonly Dictionary<int, HashSet<int>> _adjacent = [];
    private readonly double _tol;

    private WallGraph(double tol) => _tol = tol;

    public int NodeCount => _nodes.Count;

    private sealed record Edge(int U, int V, List<string> Handles);

    public sealed record Face(IReadOnlyList<Pt> Ring, IReadOnlyList<string> Handles);

    public static WallGraph Build(List<RoomLoopFinder.Piece> pieces, GeometryTolerance tol, CancellationToken ct)
    {
        var graph = new WallGraph(tol.PointEquality);
        var cuts = new List<double>[pieces.Count];
        var index = new SpatialIndex<int>();
        for (var i = 0; i < pieces.Count; i++)
        {
            cuts[i] = [0, 1];
            index.Insert(Box.Of(pieces[i].A, pieces[i].B), i);
        }

        // Every crossing, touch and overlap becomes a cut parameter on both pieces.
        for (var i = 0; i < pieces.Count; i++)
        {
            ct.ThrowIfCancellationRequested();
            var s = new Seg(pieces[i].A, pieces[i].B);
            foreach (var j in index.Query(Box.Of(s.A, s.B), tol.PointEquality))
            {
                if (j <= i) continue;
                var t = new Seg(pieces[j].A, pieces[j].B);
                switch (GeometryMath.IntersectXY(s, t, tol.PointEquality, out var point))
                {
                    case IntersectionKind.Point:
                        cuts[i].Add(s.ClosestParameterXY(point));
                        cuts[j].Add(t.ClosestParameterXY(point));
                        break;
                    case IntersectionKind.Overlap when GeometryMath.CollinearOverlapXY(s, t, tol.PointEquality, out var from, out var to):
                        cuts[i].Add(s.ClosestParameterXY(from)); cuts[i].Add(s.ClosestParameterXY(to));
                        cuts[j].Add(t.ClosestParameterXY(from)); cuts[j].Add(t.ClosestParameterXY(to));
                        break;
                }
            }
        }

        for (var i = 0; i < pieces.Count; i++)
        {
            var s = new Seg(pieces[i].A, pieces[i].B);
            var parameters = cuts[i].Select(p => Math.Clamp(p, 0, 1)).Distinct().OrderBy(p => p).ToList();
            var previous = graph.NodeAt(s.A);
            for (var k = 1; k < parameters.Count; k++)
            {
                var node = graph.NodeAt(Pt.Lerp(s.A, s.B, parameters[k]));
                graph.AddEdge(previous, node, pieces[i].Handle);
                previous = node;
            }
        }

        return graph;
    }

    /// <summary>
    ///     Removes chains that end in a free node. A free end of the original graph is an open end — unless the chain is a short tail of a
    ///     wall past the junction it crosses (the wall's own piece runs through the node the chain hangs off, no farther than a wall is
    ///     thick): an overshoot, counted, not reported. A wall that runs on past a T-junction and stops is a real end.
    /// </summary>
    public List<OpenEnd> PeelDangling(List<RoomLoopFinder.Piece> pieces, GeometryTolerance tol, double reachMm, double maxOvershootMm, CancellationToken ct, out int overshoots)
    {
        var tips = new List<(int Tip, string Handle)>();
        var origin = new Dictionary<int, int>();
        var attachment = new Dictionary<int, int?>(); // tip → the node its chain hangs off, null when the chain came loose entirely
        var free = new Queue<int>(_adjacent.Where(kv => kv.Value.Count == 1).Select(kv => kv.Key));
        foreach (var n in free) { origin[n] = n; attachment[n] = null; }
        while (free.Count > 0)
        {
            ct.ThrowIfCancellationRequested();
            var node = free.Dequeue();
            if (!_adjacent.TryGetValue(node, out var links) || links.Count != 1) continue;
            var other = links.First();
            var edge = _edges[Key(node, other)];
            var tip = origin[node];
            if (tip == node) tips.Add((tip, edge.Handles[0]));
            RemoveEdge(node, other);
            if (_adjacent.TryGetValue(other, out var remaining))
            {
                attachment[tip] = other;
                if (remaining.Count == 1 && !origin.ContainsKey(other)) { origin[other] = tip; free.Enqueue(other); }
            }
            else
            {
                attachment[tip] = null;
                // the chain came loose at another free tip (a detached wall): that tip is an open end too
                if (origin.TryGetValue(other, out var otherTip) && otherTip == other && tips.All(t => t.Tip != other)) { tips.Add((other, edge.Handles[0])); attachment[other] = null; }
            }
        }

        var index = new SpatialIndex<RoomLoopFinder.Piece>();
        foreach (var p in pieces) if (!p.Virtual) index.Insert(Box.Of(p.A, p.B), p);
        var tipPoints = tips.Select(t => _nodes[t.Tip]).ToList();
        overshoots = 0;
        var open = new List<OpenEnd>();
        foreach (var (tip, handle) in tips.Where(t => !OpeningBridger.IsVirtual(t.Handle)))
        {
            ct.ThrowIfCancellationRequested();
            // a chain only hangs off the graph when the node it stopped at is still in it; two chains meeting in the middle of a detached wall are not attached to anything
            var hangsOff = attachment[tip] is { } at && _adjacent.ContainsKey(at) ? at : (int?)null;
            if (hangsOff is { } junction && _nodes[tip].DistanceXY(_nodes[junction]) <= maxOvershootMm && RunsThrough(index, handle, _nodes[tip], _nodes[junction], tol)) { overshoots++; continue; }
            open.Add(Describe(index, tip, hangsOff is { } j ? _nodes[j] : null, handle, tipPoints, tol, reachMm));
        }

        return open.OrderBy(o => o.Handle, StringComparer.Ordinal).ThenBy(o => o.PointMm.X).ThenBy(o => o.PointMm.Y).ToList();
    }

    /// <summary>Every face of the remaining graph, walked with the leftmost turn so bounded faces come out counter-clockwise.</summary>
    public IEnumerable<Face> Faces(CancellationToken ct)
    {
        var order = new Dictionary<int, List<int>>();
        foreach (var (node, links) in _adjacent)
            order[node] = links.OrderBy(v => Angle(node, v)).ToList();
        var visited = new HashSet<(int, int)>();
        foreach (var (u, links) in order.OrderBy(kv => kv.Key))
            foreach (var v in links)
            {
                if (visited.Contains((u, v))) continue;
                ct.ThrowIfCancellationRequested();
                var ring = new List<Pt>();
                var handles = new HashSet<string>(StringComparer.Ordinal);
                var (a, b) = (u, v);
                while (visited.Add((a, b)))
                {
                    ring.Add(_nodes[a]);
                    handles.UnionWith(_edges[Key(a, b)].Handles);
                    var around = order[b];
                    var back = around.IndexOf(a);
                    var next = around[(back + around.Count - 1) % around.Count]; // the next edge clockwise from the one we arrived by
                    (a, b) = (b, next);
                }

                yield return new Face(ring, handles.OrderBy(h => h.Length).ThenBy(h => h, StringComparer.Ordinal).ToArray());
            }
    }

    private double Angle(int from, int to) => Math.Atan2(_nodes[to].Y - _nodes[from].Y, _nodes[to].X - _nodes[from].X);

    /// <summary>The wall the tip belongs to passes through the node its chain hangs off: the tail beyond a crossing, at any angle.</summary>
    private static bool RunsThrough(SpatialIndex<RoomLoopFinder.Piece> index, string handle, Pt tip, Pt attachment, GeometryTolerance tol) =>
        index.Query(Box.Of(attachment, attachment), tol.PointEquality).Any(p => p.Handle == handle && (p.A.AlmostEqualsXY(tip, tol.PointEquality) || p.B.AlmostEqualsXY(tip, tol.PointEquality))
            && new Seg(p.A, p.B).DistanceXY(attachment) <= tol.PointEquality && new Seg(p.A, p.B).ClosestParameterXY(attachment) is > 0 and < 1);

    /// <summary>
    ///     The nearest other wall within reach — other than the pieces meeting the tip and those at the junction its chain hangs off,
    ///     so a stub is measured to the wall it misses, not the one it leaves. A piece of the tip's own entity counts only through its
    ///     other free end (an unclosed polyline's gap), never through the neighbouring pieces of a sampled arc.
    /// </summary>
    private OpenEnd Describe(SpatialIndex<RoomLoopFinder.Piece> index, int node, Pt? attachment, string handle, List<Pt> tipPoints, GeometryTolerance tol, double reachMm)
    {
        var at = _nodes[node];
        var own = index.Query(Box.Of(at, at), tol.PointEquality).Where(p => new Seg(p.A, p.B).DistanceXY(at) <= tol.PointEquality).ToList();
        if (attachment is { } junction) own.AddRange(index.Query(Box.Of(junction, junction), tol.PointEquality).Where(p => new Seg(p.A, p.B).DistanceXY(junction) <= tol.PointEquality));
        RoomLoopFinder.Piece? nearest = null;
        var distance = double.PositiveInfinity;
        foreach (var p in index.Query(Box.Of(at, at), reachMm))
        {
            if (own.Contains(p)) continue;
            var d = new Seg(p.A, p.B).DistanceXY(at);
            if (p.Handle == handle)
            {
                var otherEnd = tipPoints.Where(t => !t.AlmostEqualsXY(at, tol.PointEquality) && (t.AlmostEqualsXY(p.A, tol.PointEquality) || t.AlmostEqualsXY(p.B, tol.PointEquality))).Select(t => (Pt?)t).FirstOrDefault();
                if (otherEnd is not { } end || Math.Abs(end.DistanceXY(at) - d) > tol.PointEquality) continue;
            }

            if (d < distance) (nearest, distance) = (p, d);
        }

        var within = nearest is not null && distance <= reachMm;
        return new OpenEnd(at.Rounded(), handle, within ? Math.Round(distance, 1) : null, within ? nearest!.Handle : null);
    }

    private int NodeAt(Pt p)
    {
        var cx = (long)Math.Floor(p.X / _tol);
        var cy = (long)Math.Floor(p.Y / _tol);
        for (var dx = -1; dx <= 1; dx++)
            for (var dy = -1; dy <= 1; dy++)
                if (_cells.TryGetValue((cx + dx, cy + dy), out var ids))
                    foreach (var id in ids)
                        if (_nodes[id].AlmostEqualsXY(p, _tol)) return id;
        _nodes.Add(new Pt(p.X, p.Y));
        var index = _nodes.Count - 1;
        if (!_cells.TryGetValue((cx, cy), out var cell)) _cells[(cx, cy)] = cell = [];
        cell.Add(index);
        return index;
    }

    private static (int, int) Key(int u, int v) => u < v ? (u, v) : (v, u);

    private void AddEdge(int u, int v, string handle)
    {
        if (u == v) return;
        if (_edges.TryGetValue(Key(u, v), out var edge)) { if (!edge.Handles.Contains(handle)) edge.Handles.Add(handle); return; }
        _edges[Key(u, v)] = new Edge(u, v, [handle]);
        if (!_adjacent.TryGetValue(u, out var au)) _adjacent[u] = au = [];
        if (!_adjacent.TryGetValue(v, out var av)) _adjacent[v] = av = [];
        au.Add(v);
        av.Add(u);
    }

    private void RemoveEdge(int u, int v)
    {
        _edges.Remove(Key(u, v));
        _adjacent[u].Remove(v);
        _adjacent[v].Remove(u);
        if (_adjacent[u].Count == 0) _adjacent.Remove(u);
        if (_adjacent[v].Count == 0) _adjacent.Remove(v);
    }
}
