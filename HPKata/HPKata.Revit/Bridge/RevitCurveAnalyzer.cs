using Autodesk.Revit.DB;
using HPKata.Revit.Units;

namespace HPKata.Revit.Bridge;

/// <summary>
/// Geometric fallback analyzer evaluating physical centerline curves to detect topology
/// and segment dimensions for freeform or unmapped rebars.
/// </summary>
public static class RevitCurveAnalyzer
{
    private const double ToleranceMm = 1.0;

    /// <summary>
    /// Analyzes an array of Revit curves (in decimal feet) and extracts the Kata shape code and millimeter parameters.
    /// </summary>
    public static (string ShapeCode, double[] ParametersMm) AnalyzeCurves(IList<Curve> curves)
    {
        if (curves == null || curves.Count == 0)
        {
            return ("dang00", []);
        }

        var pointsMm = new List<(double X, double Y, double Z)>();
        foreach (var curve in curves)
        {
            var p0 = RevitUnitConverter.PointFeetToMm(curve.GetEndPoint(0));
            var p1 = RevitUnitConverter.PointFeetToMm(curve.GetEndPoint(1));

            if (pointsMm.Count == 0)
            {
                pointsMm.Add(p0);
            }
            pointsMm.Add(p1);
        }

        return AnalyzePoints(pointsMm);
    }

    /// <summary>
    /// Analyzes ordered polyline points (in millimeters) to deduce rebar shape and dimensions.
    /// </summary>
    public static (string ShapeCode, double[] ParametersMm) AnalyzePoints(
        IReadOnlyList<(double X, double Y, double Z)> pointsMm)
    {
        if (pointsMm.Count < 2)
        {
            return ("dang00", []);
        }

        int segmentCount = pointsMm.Count - 1;
        var lengths = new double[segmentCount];
        for (int i = 0; i < segmentCount; i++)
        {
            lengths[i] = Distance(pointsMm[i], pointsMm[i + 1]);
        }

        if (segmentCount == 1)
        {
            return ("dang01", lengths);
        }

        if (segmentCount == 2)
        {
            return ("dang03", lengths);
        }

        if (segmentCount == 3)
        {
            double diff = Math.Abs(lengths[0] - lengths[2]);
            string code = diff < ToleranceMm ? "dang02" : "dang04";
            return (code, lengths);
        }

        if (segmentCount == 4)
        {
            bool isClosed = Distance(pointsMm[0], pointsMm[4]) < ToleranceMm * 5.0;
            return isClosed ? ("dang22", lengths) : ("dang00", lengths);
        }

        return ("dang00", lengths);
    }

    private static double Distance(
        (double X, double Y, double Z) a,
        (double X, double Y, double Z) b)
    {
        double dx = a.X - b.X;
        double dy = a.Y - b.Y;
        double dz = a.Z - b.Z;
        return Math.Sqrt(dx * dx + dy * dy + dz * dz);
    }
}
