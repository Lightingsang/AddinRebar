namespace HPBim.Core.Qc;

using System;
using System.Collections.Generic;
using HPBim.Core.Geometry;

/// <summary>
/// Fast 2D spatial grid hash for O(1) candidate lookup in geometric pre-flight checks.
/// Matches vncad O_LUOI = 2000.0mm grid cell architecture.
/// </summary>
public sealed class SpatialGridIndex<T> where T : notnull
{
    public const double DefaultCellSizeMm = 2000.0;
    public double CellSizeMm { get; }
    private readonly Dictionary<(int X, int Y), List<T>> _grid = new();
    private readonly Dictionary<T, BoundingBox2D> _bounds = new();

    public int Count => _bounds.Count;

    public SpatialGridIndex(double cellSizeMm = DefaultCellSizeMm)
    {
        CellSizeMm = cellSizeMm > 0 ? cellSizeMm : DefaultCellSizeMm;
    }

    public void Insert(T item, BoundingBox2D bounds)
    {
        _bounds[item] = bounds;
        int minX = (int)Math.Floor(bounds.Min.X / CellSizeMm);
        int maxX = (int)Math.Floor(bounds.Max.X / CellSizeMm);
        int minY = (int)Math.Floor(bounds.Min.Y / CellSizeMm);
        int maxY = (int)Math.Floor(bounds.Max.Y / CellSizeMm);

        for (int x = minX; x <= maxX; x++)
        {
            for (int y = minY; y <= maxY; y++)
            {
                var cellKey = (x, y);
                if (!_grid.TryGetValue(cellKey, out var list))
                {
                    list = new List<T>();
                    _grid[cellKey] = list;
                }
                list.Add(item);
            }
        }
    }

    public IEnumerable<T> Query(BoundingBox2D queryBounds)
    {
        var candidates = new HashSet<T>();
        int minX = (int)Math.Floor(queryBounds.Min.X / CellSizeMm);
        int maxX = (int)Math.Floor(queryBounds.Max.X / CellSizeMm);
        int minY = (int)Math.Floor(queryBounds.Min.Y / CellSizeMm);
        int maxY = (int)Math.Floor(queryBounds.Max.Y / CellSizeMm);

        for (int x = minX; x <= maxX; x++)
        {
            for (int y = minY; y <= maxY; y++)
            {
                if (_grid.TryGetValue((x, y), out var list))
                {
                    foreach (var item in list)
                    {
                        if (_bounds.TryGetValue(item, out var itemBounds) && itemBounds.Intersects(queryBounds))
                        {
                            candidates.Add(item);
                        }
                    }
                }
            }
        }

        return candidates;
    }

    public void Clear()
    {
        _grid.Clear();
        _bounds.Clear();
    }
}
