using System;
using System.Collections.Generic;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using HPRebar.Core.ColumnRebar;
using HPRebar.Core.ColumnRebar.Models;
using Serilog;

namespace HPRebar.ColumnRebar.Service;

/// <summary>Turns a computed main-bar centre-line into a free-form Rebar element.</summary>
public static class MainBarCreator
{
    /// <summary>Segments shorter than this are dropped: Revit rejects a curve loop containing them.</summary>
    private const double MinimumSegmentMm = 1.0;

    public static Rebar Create(
        Document document,
        Element host,
        RebarBarType barType,
        BarPolyline polyline,
        PointMapper mapper,
        string partitionName)
    {
        var curves = BuildCurves(polyline, mapper);
        var loops = new List<CurveLoop> { CurveLoop.Create(curves) };

        // Multi-version: Rebar.CreateFreeForm overloads.
        // Up to Revit 2025 the only overload reports failure through an out parameter. Revit 2026 added a
        // RebarStyle overload and deprecated the old one, and Revit 2027 removed the old one outright, so
        // there is no single call that compiles across the whole range.
#if REVIT2026_OR_GREATER
        var result = Rebar.CreateFreeForm(document, barType, host, loops, RebarStyle.Standard);

        if (result.Error != RebarFreeFormValidationResult.Success || result.Rebar is null)
        {
            Log.Error(
                "Revit rejected the free-form bar {BarNumber} with {Validation}; {PointCount} points, {CurveCount} segments",
                polyline.BarNumber, result.Error, polyline.Points.Count, curves.Count);

            throw new InvalidOperationException(
                $"Revit could not create main bar {polyline.BarNumber}: {result.Error}.");
        }

        var rebar = result.Rebar;
#else
        var rebar = Rebar.CreateFreeForm(document, barType, host, loops, out var validation);

        if (validation != RebarFreeFormValidationResult.Success)
        {
            Log.Error(
                "Revit rejected the free-form bar {BarNumber} with {Validation}; {PointCount} points, {CurveCount} segments",
                polyline.BarNumber, validation, polyline.Points.Count, curves.Count);

            throw new InvalidOperationException(
                $"Revit could not create main bar {polyline.BarNumber}: {validation}.");
        }
#endif

        SetPartition(rebar, partitionName);

        return rebar;
    }

    /// <summary>
    ///     The bar as Revit curves: one line per bend. Points that carry no bend are dropped first, so a bar
    ///     that never moves in plan comes out as a single line however many points it was calculated from,
    ///     and the cross-over at the top reads as one sloped segment rather than a kink — while every point
    ///     that does turn still gets its own segment.
    /// </summary>
    private static IList<Curve> BuildCurves(BarPolyline polyline, PointMapper mapper)
    {
        var simplified = Simplify(polyline.Points);

        if (simplified.Count < 2)
        {
            throw new InvalidOperationException($"Main bar {polyline.BarNumber} collapsed to a single point.");
        }

        var points = BarPolylineBuilder.Corners(simplified);
        var curves = new List<Curve>();

        for (var i = 1; i < points.Count; i++)
        {
            curves.Add(Line.CreateBound(mapper.ToXyz(points[i - 1]), mapper.ToXyz(points[i])));
        }

        return curves;
    }

    /// <summary>
    ///     Drops points sitting within a millimetre of the one kept before them, which is Revit's limit for
    ///     a curve it will accept. A trailing point can go this way too: an anchorage that short changes the
    ///     bar by less than the tolerance the model is drawn to.
    /// </summary>
    private static IReadOnlyList<Point3> Simplify(IReadOnlyList<Point3> points)
    {
        var kept = new List<Point3> { points[0] };

        for (var i = 1; i < points.Count; i++)
        {
            if (BarPolylineBuilder.Distance(kept[kept.Count - 1], points[i]) >= MinimumSegmentMm)
            {
                kept.Add(points[i]);
            }
        }

        if (kept.Count < points.Count)
        {
            Log.Debug("Dropped {Count} zero-length segment(s) from a main bar", points.Count - kept.Count);
        }

        return kept;
    }

    /// <summary>Writes the partition name if the project defines that parameter; not every template does.</summary>
    internal static void SetPartition(Element rebar, string partitionName)
    {
        var parameter = rebar.LookupParameter("Partition");

        if (parameter is { IsReadOnly: false })
        {
            parameter.Set(partitionName);
        }
    }
}
