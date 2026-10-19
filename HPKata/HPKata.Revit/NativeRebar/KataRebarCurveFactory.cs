using Autodesk.Revit.DB;
using HPKata.Revit.Units;

namespace HPKata.Revit.NativeRebar;

/// <summary>
/// Factory creating Revit curve geometries from millimeter coordinates and segment vectors.
/// Enforces Rule R5 by delegating all coordinate conversions to RevitUnitConverter.
/// </summary>
public static class KataRebarCurveFactory
{
    /// <summary>
    /// Creates a bound line segment from two endpoints in millimeter coordinates.
    /// </summary>
    public static Line CreateLineMm(
        double startX, double startY, double startZ,
        double endX, double endY, double endZ)
    {
        var p0 = RevitUnitConverter.PointMmToFeet(startX, startY, startZ);
        var p1 = RevitUnitConverter.PointMmToFeet(endX, endY, endZ);
        return Line.CreateBound(p0, p1);
    }

    /// <summary>
    /// Creates continuous line segments from a sequence of polyline vertices specified in millimeters.
    /// </summary>
    public static IList<Curve> CreatePolylineCurvesMm(IReadOnlyList<(double X, double Y, double Z)> verticesMm)
    {
        if (verticesMm == null || verticesMm.Count < 2)
        {
            return [];
        }

        var curves = new List<Curve>(verticesMm.Count - 1);
        for (int i = 0; i < verticesMm.Count - 1; i++)
        {
            var p0 = RevitUnitConverter.PointMmToFeet(verticesMm[i].X, verticesMm[i].Y, verticesMm[i].Z);
            var p1 = RevitUnitConverter.PointMmToFeet(verticesMm[i + 1].X, verticesMm[i + 1].Y, verticesMm[i + 1].Z);
            curves.Add(Line.CreateBound(p0, p1));
        }

        return curves;
    }
}
