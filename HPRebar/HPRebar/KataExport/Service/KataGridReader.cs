using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using HPRebar.Core.KataExport.Models;
using HPRebar.KataExport.Model;
using RevitView = Autodesk.Revit.DB.View;

namespace HPRebar.KataExport.Service;

/// <summary>
/// Straight grids of the active view: those crossing the run give stations (rows 22/23), the one running
/// along the beam gives the axis name and offset (B8/B9). Arc grids are counted and skipped.
/// </summary>
public static class KataGridReader
{
    private const double ParallelDegrees = 1.0;

    public sealed record Result(
        IReadOnlyList<KataGridCrossing> Crossings,
        string? AxisGridName,
        double? AxisOffsetMm,
        IReadOnlyList<string> Warnings);

    public static Result Read(Document doc, RevitView view, KataRunGeometry run)
    {
        var crossings = new List<KataGridCrossing>();
        var along = new List<(string Name, double OffsetMm)>();
        int arcs = 0;
        double axisReachMm = run.Pieces.Max(p => p.WidthMm);

        foreach (var grid in new FilteredElementCollector(doc, view.Id).OfClass(typeof(Grid)).Cast<Grid>())
        {
            if (grid.Curve is not Line line)
            {
                arcs++;
                continue;
            }

            if (run.Frame.IsParallel(line.Direction, ParallelDegrees))
            {
                double offset = run.Frame.Offset(line.GetEndPoint(0)) - run.CenterOffsetMm;
                if (Math.Abs(offset) <= axisReachMm) along.Add((grid.Name, offset));
                continue;
            }

            if (Station(run.Frame, line) is { } station) crossings.Add(new KataGridCrossing(grid.Name, station));
        }

        var warnings = new List<string>();
        if (arcs > 0) warnings.Add($"{arcs} arc grid(s) in the view were ignored; only straight grids are read.");
        if (crossings.Count == 0) warnings.Add("No straight grid in the active view crosses the beam; rows 22 and 23 stay empty.");

        var axis = along.OrderBy(a => Math.Abs(a.OffsetMm)).Select(a => ((string, double)?)a).FirstOrDefault();
        return new Result(crossings, axis?.Item1, axis?.Item2, warnings);
    }

    /// <summary>
    /// Station where the (unbounded) grid line crosses the run axis in plan: grids stand for infinite axes,
    /// so a grid drawn short of the beam still names the support it passes through.
    /// </summary>
    private static double? Station(KataAxisFrame frame, Line grid)
    {
        var p = grid.GetEndPoint(0) - frame.Origin;
        var d = grid.Direction;
        var a = frame.Axis;
        double cross = a.X * d.Y - a.Y * d.X;
        if (Math.Abs(cross) < Math.Sin(ParallelDegrees * Math.PI / 180.0)) return null;

        double s = (p.X * d.Y - p.Y * d.X) / cross;
        return RevitUnits.FtToMm(s);
    }
}
