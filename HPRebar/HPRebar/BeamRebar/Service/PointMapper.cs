using Autodesk.Revit.DB;
using HPRebar.Core.BeamRebar.Models;

namespace HPRebar.BeamRebar.Service;

/// <summary>
/// Converts 3D points between continuous beam local millimetre space (X longitudinal, Y transverse, Z vertical)
/// and Revit model world coordinates (decimal feet).
/// </summary>
public sealed class PointMapper
{
    private readonly XYZ _origin;
    private readonly XYZ _axisX;
    private readonly XYZ _axisY;
    private readonly XYZ _axisZ;

    public PointMapper(XYZ origin, XYZ axisX, XYZ axisY, XYZ axisZ)
    {
        _origin = origin;
        _axisX = axisX.Normalize();
        _axisY = axisY.Normalize();
        _axisZ = axisZ.Normalize();
    }

    public PointMapper(XYZ origin, XYZ axisX, XYZ axisY)
        : this(origin, axisX, axisY, XYZ.BasisZ)
    {
    }

    public XYZ Origin => _origin;
    public XYZ AxisX => _axisX;
    public XYZ AxisY => _axisY;
    public XYZ AxisZ => _axisZ;

    public XYZ ToXyz(Point3 point) =>
        _origin
        + RevitUnits.MmToFt(point.X) * _axisX
        + RevitUnits.MmToFt(point.Y) * _axisY
        + RevitUnits.MmToFt(point.Z) * _axisZ;

    public XYZ ToXyz(double xMm, double yMm, double zMm) =>
        _origin
        + RevitUnits.MmToFt(xMm) * _axisX
        + RevitUnits.MmToFt(yMm) * _axisY
        + RevitUnits.MmToFt(zMm) * _axisZ;

    public Point3 ToLocal(XYZ worldPoint)
    {
        var diff = worldPoint - _origin;
        double xMm = RevitUnits.FtToMm(diff.DotProduct(_axisX));
        double yMm = RevitUnits.FtToMm(diff.DotProduct(_axisY));
        double zMm = RevitUnits.FtToMm(diff.DotProduct(_axisZ));
        return new Point3(xMm, yMm, zMm);
    }
}
