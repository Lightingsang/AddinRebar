using System;
using Autodesk.Revit.DB;
using HPRebar.Core.KataExport.Models;

namespace HPRebar.KataExport.Service;

/// <summary>
/// Plan frame of a beam run: stations along <see cref="Axis"/> and offsets along <see cref="Transverse"/>
/// (left of the run direction), both in millimetres from <see cref="Origin"/>. The axis is oriented so its
/// dominant plan component is positive, which is the left-to-right / bottom-to-top order Kata reads.
/// </summary>
public sealed class KataAxisFrame
{
    private KataAxisFrame(XYZ origin, XYZ axis)
    {
        Origin = origin;
        Axis = axis;
        Transverse = XYZ.BasisZ.CrossProduct(axis).Normalize();
    }

    public XYZ Origin { get; }

    public XYZ Axis { get; }

    public XYZ Transverse { get; }

    public static KataAxisFrame FromLine(Line line)
    {
        var d = line.Direction;
        var plan = new XYZ(d.X, d.Y, 0.0);
        if (plan.GetLength() < 1e-9) throw new InvalidOperationException("A vertical beam cannot form a Kata run.");

        plan = plan.Normalize();
        bool xDominant = Math.Abs(plan.X) >= Math.Abs(plan.Y);
        if ((xDominant && plan.X < 0) || (!xDominant && plan.Y < 0)) plan = plan.Negate();

        var p = line.GetEndPoint(0);
        return new KataAxisFrame(new XYZ(p.X, p.Y, 0.0), plan);
    }

    /// <summary>Station of a point along the axis (mm).</summary>
    public double Station(XYZ point) => RevitUnits.FtToMm((point - Origin).DotProduct(Axis));

    /// <summary>Plan offset of a point to the left of the axis (mm).</summary>
    public double Offset(XYZ point) => RevitUnits.FtToMm((point - Origin).DotProduct(Transverse));

    public Interval1D Stations(Line line) => new(Station(line.GetEndPoint(0)), Station(line.GetEndPoint(1)));

    /// <summary>World point at a station, plan offset and absolute elevation (all mm, elevation in feet space).</summary>
    public XYZ Point(double stationMm, double offsetMm, double elevationFt) =>
        new XYZ(Origin.X, Origin.Y, 0.0)
        + Axis * RevitUnits.MmToFt(stationMm)
        + Transverse * RevitUnits.MmToFt(offsetMm)
        + XYZ.BasisZ * elevationFt;

    /// <summary>Horizontal line along the axis between two stations, at a plan offset and elevation.</summary>
    public Line Probe(double fromMm, double toMm, double offsetMm, double elevationFt) =>
        Line.CreateBound(Point(fromMm, offsetMm, elevationFt), Point(toMm, offsetMm, elevationFt));

    /// <summary>True when a plan direction is within <paramref name="degrees"/> of the axis (either way).</summary>
    public bool IsParallel(XYZ direction, double degrees) => Math.Abs(PlanCos(direction)) >= Math.Cos(degrees * Math.PI / 180.0);

    /// <summary>|cos| of the plan angle between a direction and the axis.</summary>
    public double PlanCos(XYZ direction)
    {
        var plan = new XYZ(direction.X, direction.Y, 0.0);
        double length = plan.GetLength();
        return length < 1e-9 ? 0.0 : Math.Abs(plan.DotProduct(Axis) / length);
    }
}
