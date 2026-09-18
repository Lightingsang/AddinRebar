namespace HPGeo.Core.Projection;

/// <summary>
/// Seven-parameter Helmert transformation in the geocentric domain, <b>coordinate-frame</b> rotation
/// convention (EPSG method 1032) — the convention the reference tool applies. Rotations in arc-seconds,
/// scale in ppm, translations in metres.
/// </summary>
public sealed record Helmert7Parameters(
    double DxM, double DyM, double DzM,
    double RxArcSec, double RyArcSec, double RzArcSec,
    double ScalePpm)
{
    /// <summary>
    /// VN-2000 → WGS 84, the parameters the reference tool ships (Vietnamese national values, applied in the
    /// coordinate-frame convention). Note: PROJ's <c>+towgs84</c> is the position-vector convention, so the same
    /// numbers there need their three rotation signs flipped; see tools/gen-golden-wgs84-to-vn2000.js.
    /// </summary>
    public static readonly Helmert7Parameters Vn2000ToWgs84 = new(
        -191.90441429, -39.30318279, -111.45032835,
        -0.00928836, 0.01975479, -0.00427372,
        0.252906278);
}

public static class Helmert7
{
    private const double ArcSecToRad = Math.PI / (180.0 * 3600.0);

    /// <summary>Source frame → target frame: X' = T + M·R·X with R the coordinate-frame small-angle matrix.</summary>
    public static (double X, double Y, double Z) Forward((double X, double Y, double Z) v, Helmert7Parameters p)
    {
        var (rx, ry, rz, m) = Coefficients(p);
        return (
            p.DxM + m * v.X + rz * v.Y - ry * v.Z,
            p.DyM - rz * v.X + m * v.Y + rx * v.Z,
            p.DzM + ry * v.X - rx * v.Y + m * v.Z);
    }

    /// <summary>
    /// Target frame → source frame. Exact inverse of <see cref="Forward"/>: the 3×3 matrix is inverted
    /// analytically instead of negating the parameters, so a round trip closes to floating-point precision
    /// (negating the parameters would leave second-order residuals of the rotations × scale).
    /// </summary>
    public static (double X, double Y, double Z) Inverse((double X, double Y, double Z) v, Helmert7Parameters p)
    {
        var (rx, ry, rz, m) = Coefficients(p);
        var bx = v.X - p.DxM;
        var by = v.Y - p.DyM;
        var bz = v.Z - p.DzM;

        // Matrix A = [[m, rz, -ry], [-rz, m, rx], [ry, -rx, m]]; solve A·x = b by the adjugate.
        var det = m * (m * m + rx * rx) - rz * (-rz * m - rx * ry) + (-ry) * (rz * rx - m * ry);
        var c00 = m * m + rx * rx;
        var c01 = -(-rz * m - rx * ry);
        var c02 = rz * rx - m * ry;
        var c10 = -(rz * m - (-ry) * (-rx));
        var c11 = m * m + ry * ry;
        var c12 = -(m * (-rx) - rz * ry);
        var c20 = rz * rx - (-ry) * m;
        var c21 = -(m * rx - (-ry) * (-rz));
        var c22 = m * m + rz * rz;

        // adj(A) = transpose of the cofactor matrix
        var x = (c00 * bx + c10 * by + c20 * bz) / det;
        var y = (c01 * bx + c11 * by + c21 * bz) / det;
        var z = (c02 * bx + c12 * by + c22 * bz) / det;
        return (x, y, z);
    }

    private static (double Rx, double Ry, double Rz, double M) Coefficients(Helmert7Parameters p) =>
        (p.RxArcSec * ArcSecToRad, p.RyArcSec * ArcSecToRad, p.RzArcSec * ArcSecToRad, 1.0 + p.ScalePpm * 1e-6);
}
