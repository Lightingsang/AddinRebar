using HPAutoCad.Aec.Geometry;

namespace HPAutoCad.Aec.Spatial;

/// <summary>
///     Uniform grid over plan bounding boxes: the broad phase that keeps pairwise work near O(n) on a
///     floor plan. Cell size defaults to the median box size of the inserted items (clamped), so a drawing
///     in metres and one in millimetres both get a few items per cell. Items straddling cells are listed in
///     each; items that would span more than <see cref="MaxCellsPerItem"/> cells (a site boundary among
///     door swings) are kept in a separate list every query checks by box, so nothing is ever dropped.
///     <see cref="Query"/> de-duplicates and falls back to a linear scan when the query box itself is huge.
/// </summary>
public sealed class SpatialIndex<T>
{
    /// <summary>Above this many cells an item goes to the oversized list instead of the grid.</summary>
    public const int MaxCellsPerItem = 4096;

    /// <summary>A query spanning more cells than this is cheaper as a linear scan of every item.</summary>
    public const int MaxCellsPerQuery = 4096;

    private const double MinCellSizeMm = 10;
    private const double MaxCellSizeMm = 1_000_000;
    private const double FallbackCellSizeMm = 1000;

    private readonly Dictionary<(long, long), List<int>> _cells = new();
    private readonly List<(Box Bounds, T Item)> _items = [];
    private readonly List<int> _oversized = [];
    private double _cellSize;
    private bool _built;

    public int Count => _items.Count;

    public double CellSize => _cellSize;

    public void Insert(Box bounds, T item)
    {
        if (bounds.IsEmpty) return;
        _items.Add((bounds, item));
        _built = false;
    }

    /// <summary>Everything whose box intersects <paramref name="area"/> (expanded by the tolerance), each item once.</summary>
    public IEnumerable<T> Query(Box area, double tolerance = 0)
    {
        if (area.IsEmpty || _items.Count == 0) return [];
        if (!_built) Build();

        var expanded = area.Expand(tolerance);
        var (x0, y0) = CellOf(expanded.Min);
        var (x1, y1) = CellOf(expanded.Max);
        if ((x1 - x0 + 1) * (y1 - y0 + 1) > MaxCellsPerQuery)
            return _items.Where(i => i.Bounds.IntersectsXY(expanded)).Select(i => i.Item).ToList();

        var seen = new HashSet<int>();
        var hits = new List<T>();
        for (var x = x0; x <= x1; x++)
        for (var y = y0; y <= y1; y++)
        {
            if (!_cells.TryGetValue((x, y), out var bucket)) continue;
            foreach (var index in bucket)
            {
                if (!seen.Add(index)) continue;
                if (_items[index].Bounds.IntersectsXY(expanded)) hits.Add(_items[index].Item);
            }
        }

        foreach (var index in _oversized)
        {
            if (seen.Add(index) && _items[index].Bounds.IntersectsXY(expanded)) hits.Add(_items[index].Item);
        }

        return hits;
    }

    /// <summary>Every item, in insertion order — for callers that need the full set after using the index for pairs.</summary>
    public IEnumerable<T> All() => _items.Select(i => i.Item);

    private void Build()
    {
        _cells.Clear();
        _oversized.Clear();
        var sizes = _items.Select(i => Math.Max(i.Bounds.Width, i.Bounds.Height)).Where(s => s > 0).OrderBy(s => s).ToArray();
        var median = sizes.Length == 0 ? FallbackCellSizeMm : sizes[sizes.Length / 2];
        _cellSize = Math.Clamp(median * 2, MinCellSizeMm, MaxCellSizeMm);

        for (var index = 0; index < _items.Count; index++)
        {
            var (x0, y0) = CellOf(_items[index].Bounds.Min);
            var (x1, y1) = CellOf(_items[index].Bounds.Max);
            if ((x1 - x0 + 1) * (y1 - y0 + 1) > MaxCellsPerItem)
            {
                _oversized.Add(index);
                continue;
            }

            for (var x = x0; x <= x1; x++)
            for (var y = y0; y <= y1; y++)
            {
                if (!_cells.TryGetValue((x, y), out var bucket)) _cells[(x, y)] = bucket = [];
                bucket.Add(index);
            }
        }

        _built = true;
    }

    private (long, long) CellOf(Pt p) => ((long)Math.Floor(p.X / _cellSize), (long)Math.Floor(p.Y / _cellSize));
}
