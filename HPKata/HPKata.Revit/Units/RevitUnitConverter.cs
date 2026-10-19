using Autodesk.Revit.DB;

namespace HPKata.Revit.Units;

/// <summary>
/// Quarantine boundary for all unit conversions between Revit internal units (decimal feet)
/// and HPKata domain units (millimeters).
/// Enforces Rule R5: This is the ONLY file in HPKata.Revit containing the 304.8 factor.
/// </summary>
public static class RevitUnitConverter
{
    public const double MmPerFoot = 304.8;
    public const double FeetPerMm = 1.0 / MmPerFoot;
    public const double DefaultToleranceMm = 0.5;
    public const double DefaultToleranceFt = DefaultToleranceMm * FeetPerMm;

    /// <summary>Converts decimal feet to millimeters.</summary>
    public static double FeetToMm(double feet) => feet * MmPerFoot;

    /// <summary>Converts millimeters to decimal feet.</summary>
    public static double MmToFeet(double mm) => mm * FeetPerMm;

    /// <summary>Converts an array of decimal feet to millimeters.</summary>
    public static double[] FeetToMm(double[] feet)
    {
        var result = new double[feet.Length];
        for (int i = 0; i < feet.Length; i++)
        {
            result[i] = feet[i] * MmPerFoot;
        }
        return result;
    }

    /// <summary>Converts an array of millimeters to decimal feet.</summary>
    public static double[] MmToFeet(double[] mm)
    {
        var result = new double[mm.Length];
        for (int i = 0; i < mm.Length; i++)
        {
            result[i] = mm[i] * FeetPerMm;
        }
        return result;
    }

    /// <summary>Converts an XYZ coordinate in feet to millimeter tuple.</summary>
    public static (double X, double Y, double Z) PointFeetToMm(XYZ pt) =>
        (pt.X * MmPerFoot, pt.Y * MmPerFoot, pt.Z * MmPerFoot);

    /// <summary>Converts millimeter coordinates to an XYZ in decimal feet.</summary>
    public static XYZ PointMmToFeet(double xMm, double yMm, double zMm) =>
        new(xMm * FeetPerMm, yMm * FeetPerMm, zMm * FeetPerMm);

    /// <summary>Converts coordinate tuple in feet to millimeters without host API dependency.</summary>
    public static (double X, double Y, double Z) CoordinatesFeetToMm(double xFt, double yFt, double zFt) =>
        (xFt * MmPerFoot, yFt * MmPerFoot, zFt * MmPerFoot);

    /// <summary>Converts coordinate tuple in millimeters to feet without host API dependency.</summary>
    public static (double X, double Y, double Z) CoordinatesMmToFeet(double xMm, double yMm, double zMm) =>
        (xMm * FeetPerMm, yMm * FeetPerMm, zMm * FeetPerMm);

    /// <summary>Checks whether two values in millimeters are within tolerance.</summary>
    public static bool AreCloseMm(double aMm, double bMm, double toleranceMm = DefaultToleranceMm) =>
        Math.Abs(aMm - bMm) <= toleranceMm;

    /// <summary>Checks whether two values in feet are within tolerance.</summary>
    public static bool AreCloseFeet(double aFt, double bFt, double toleranceFt = DefaultToleranceFt) =>
        Math.Abs(aFt - bFt) <= toleranceFt;

    /// <summary>Converts cubic feet to cubic meters.</summary>
    public static double CubicFeetToCubicMeters(double cuFt) =>
        cuFt * Math.Pow(MmPerFoot * 1e-3, 3);

    /// <summary>Converts square feet to square meters.</summary>
    public static double SquareFeetToSquareMeters(double sqFt) =>
        sqFt * Math.Pow(MmPerFoot * 1e-3, 2);
}
