using System;
using System.Collections.Generic;

namespace HPDcePro.Core.Geometry
{
    /// <summary>
    /// Uniform 2D spatial hash grid for fast nearest-neighbor and range queries on points and segments.
    /// </summary>
    public class SpatialGrid2D<T>
    {
        private readonly double _cellSize;
        private readonly Dictionary<(int, int), List<(Point2D Point, T Item)>> _pointGrid;
        private readonly Dictionary<(int, int), List<(Segment2D Segment, T Item)>> _segmentGrid;

        public double CellSize => _cellSize;
        public int PointCount { get; private set; }
        public int SegmentCount { get; private set; }

        public SpatialGrid2D(double cellSize = 500.0)
        {
            if (cellSize <= 0.0)
            {
                throw new ArgumentOutOfRangeException(nameof(cellSize), "Cell size must be positive.");
            }
            _cellSize = cellSize;
            _pointGrid = new Dictionary<(int, int), List<(Point2D, T)>>();
            _segmentGrid = new Dictionary<(int, int), List<(Segment2D, T)>>();
        }

        private (int X, int Y) GetCellIndex(double x, double y)
        {
            int cx = (int)Math.Floor(x / _cellSize);
            int cy = (int)Math.Floor(y / _cellSize);
            return (cx, cy);
        }

        public void InsertPoint(Point2D pt, T item)
        {
            var cell = GetCellIndex(pt.X, pt.Y);
            if (!_pointGrid.TryGetValue(cell, out var list))
            {
                list = new List<(Point2D, T)>();
                _pointGrid[cell] = list;
            }
            list.Add((pt, item));
            PointCount++;
        }

        public void InsertSegment(Segment2D seg, T item)
        {
            var startCell = GetCellIndex(seg.Start.X, seg.Start.Y);
            var endCell = GetCellIndex(seg.End.X, seg.End.Y);

            int minX = Math.Min(startCell.X, endCell.X);
            int maxX = Math.Max(startCell.X, endCell.X);
            int minY = Math.Min(startCell.Y, endCell.Y);
            int maxY = Math.Max(startCell.Y, endCell.Y);

            for (int x = minX; x <= maxX; x++)
            {
                for (int y = minY; y <= maxY; y++)
                {
                    var cell = (x, y);
                    if (!_segmentGrid.TryGetValue(cell, out var list))
                    {
                        list = new List<(Segment2D, T)>();
                        _segmentGrid[cell] = list;
                    }
                    list.Add((seg, item));
                }
            }
            SegmentCount++;
        }

        public IEnumerable<T> QueryRadius(Point2D center, double radius)
        {
            double rSq = radius * radius;
            int minX = (int)Math.Floor((center.X - radius) / _cellSize);
            int maxX = (int)Math.Floor((center.X + radius) / _cellSize);
            int minY = (int)Math.Floor((center.Y - radius) / _cellSize);
            int maxY = (int)Math.Floor((center.Y + radius) / _cellSize);

            for (int x = minX; x <= maxX; x++)
            {
                for (int y = minY; y <= maxY; y++)
                {
                    if (_pointGrid.TryGetValue((x, y), out var list))
                    {
                        foreach (var entry in list)
                        {
                            if (center.DistanceToSquared(entry.Point) <= rSq)
                            {
                                yield return entry.Item;
                            }
                        }
                    }
                }
            }
        }

        public bool FindNearest(Point2D query, double maxDistance, out T? result, out double nearestDist)
        {
            result = default;
            nearestDist = maxDistance;
            bool found = false;

            double searchDist = maxDistance;
            int minX = (int)Math.Floor((query.X - searchDist) / _cellSize);
            int maxX = (int)Math.Floor((query.X + searchDist) / _cellSize);
            int minY = (int)Math.Floor((query.Y - searchDist) / _cellSize);
            int maxY = (int)Math.Floor((query.Y + searchDist) / _cellSize);

            for (int x = minX; x <= maxX; x++)
            {
                for (int y = minY; y <= maxY; y++)
                {
                    if (_pointGrid.TryGetValue((x, y), out var list))
                    {
                        foreach (var entry in list)
                        {
                            double d = query.DistanceTo(entry.Point);
                            if (d < nearestDist)
                            {
                                nearestDist = d;
                                result = entry.Item;
                                found = true;
                            }
                        }
                    }
                }
            }
            return found;
        }

        public void Clear()
        {
            _pointGrid.Clear();
            _segmentGrid.Clear();
            PointCount = 0;
            SegmentCount = 0;
        }
    }
}
