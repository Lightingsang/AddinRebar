namespace HPBim.Core.Qc;

using System;
using System.Collections.Generic;
using HPBim.Core.Geometry;

/// <summary>
/// Extracts spatial intersection nodes from structural grids.
/// Used to identify column and framing support nodes at grid crossings.
/// </summary>
public static class GridIntersectionExtractor
{
    public static IReadOnlyList<Point2D> Extract(IReadOnlyList<GridAuditItem> grids)
    {
        if (grids == null || grids.Count == 0)
        {
            return Array.Empty<Point2D>();
        }

        var xCoords = new List<double>();
        var yCoords = new List<double>();

        foreach (var g in grids)
        {
            double dx = Math.Abs(g.P1X - g.P0X);
            double dy = Math.Abs(g.P1Y - g.P0Y);
            if (dy > dx)
            {
                xCoords.Add((g.P0X + g.P1X) / 2.0);
            }
            else
            {
                yCoords.Add((g.P0Y + g.P1Y) / 2.0);
            }
        }

        var nodes = new List<Point2D>(xCoords.Count * yCoords.Count);
        for (int i = 0; i < xCoords.Count; i++)
        {
            for (int j = 0; j < yCoords.Count; j++)
            {
                nodes.Add(new Point2D(xCoords[i], yCoords[j]));
            }
        }

        return nodes;
    }
}
