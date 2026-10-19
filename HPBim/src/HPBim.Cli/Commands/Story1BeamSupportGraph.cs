namespace HPBim.Cli.Commands;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

/// <summary>
///     Represents a structural support node (Column, Wall, or Beam intersection).
/// </summary>
public sealed record SupportNode(
    string Id,
    string Type,
    double X,
    double Y,
    double WidthMm,
    double HeightMm,
    string GridX = "",
    string GridY = "");

/// <summary>
///     Builds the topological support graph for Story 1 beams from Columns, Walls, and Grids.
/// </summary>
public static class Story1BeamSupportGraph
{
    public static List<SupportNode> LoadAllSupports(string columnsPath, string wallsPath, GridSystem grids)
    {
        var result = new List<SupportNode>();

        if (File.Exists(columnsPath))
        {
            using var doc = JsonDocument.Parse(File.ReadAllText(columnsPath));
            if (doc.RootElement.TryGetProperty("columns", out var cols))
            {
                foreach (var c in cols.EnumerateArray())
                {
                    int idx = c.GetProperty("index").GetInt32();
                    double x = c.GetProperty("xMm").GetDouble();
                    double y = c.GetProperty("yMm").GetDouble();
                    double w = c.GetProperty("widthMm").GetDouble();
                    double h = c.GetProperty("heightMm").GetDouble();

                    var gx = grids.XGrids.OrderBy(g => Math.Abs(g.Coord - x)).FirstOrDefault();
                    var gy = grids.YGrids.OrderBy(g => Math.Abs(g.Coord - y)).FirstOrDefault();

                    result.Add(new SupportNode(
                        Id: $"COL_{idx:D2}",
                        Type: "Column",
                        X: x,
                        Y: y,
                        WidthMm: w,
                        HeightMm: h,
                        GridX: gx != null && Math.Abs(gx.Coord - x) <= 400.0 ? gx.Name : "",
                        GridY: gy != null && Math.Abs(gy.Coord - y) <= 400.0 ? gy.Name : ""));
                }
            }
        }

        if (File.Exists(wallsPath))
        {
            using var doc = JsonDocument.Parse(File.ReadAllText(wallsPath));
            if (doc.RootElement.TryGetProperty("walls", out var walls))
            {
                foreach (var w in walls.EnumerateArray())
                {
                    string panel = w.GetProperty("panel").GetString() ?? "";
                    double x0 = w.GetProperty("x0Mm").GetDouble();
                    double y0 = w.GetProperty("y0Mm").GetDouble();
                    double x1 = w.GetProperty("x1Mm").GetDouble();
                    double y1 = w.GetProperty("y1Mm").GetDouble();
                    double thick = w.GetProperty("thicknessMm").GetDouble();

                    // Add endpoints of walls as support nodes
                    AddWallSupportNode(result, $"{panel}_P0", x0, y0, thick, grids);
                    AddWallSupportNode(result, $"{panel}_P1", x1, y1, thick, grids);
                }
            }
        }

        return result;
    }

    private static void AddWallSupportNode(List<SupportNode> list, string id, double x, double y, double thick, GridSystem grids)
    {
        if (list.Any(s => Math.Abs(s.X - x) <= 100.0 && Math.Abs(s.Y - y) <= 100.0))
            return;

        var gx = grids.XGrids.OrderBy(g => Math.Abs(g.Coord - x)).FirstOrDefault();
        var gy = grids.YGrids.OrderBy(g => Math.Abs(g.Coord - y)).FirstOrDefault();

        list.Add(new SupportNode(
            Id: id,
            Type: "Wall",
            X: x,
            Y: y,
            WidthMm: thick,
            HeightMm: thick,
            GridX: gx != null && Math.Abs(gx.Coord - x) <= 300.0 ? gx.Name : "",
            GridY: gy != null && Math.Abs(gy.Coord - y) <= 300.0 ? gy.Name : ""));
    }

    public static List<SupportNode> GetSupportsOnGridY(List<SupportNode> allSupports, double gy, double tolMm = 350.0)
    {
        return allSupports
            .Where(s => Math.Abs(s.Y - gy) <= tolMm)
            .OrderBy(s => s.X)
            .ToList();
    }

    public static List<SupportNode> GetSupportsOnGridX(List<SupportNode> allSupports, double gx, double tolMm = 350.0)
    {
        return allSupports
            .Where(s => Math.Abs(s.X - gx) <= tolMm)
            .OrderBy(s => s.Y)
            .ToList();
    }
}
