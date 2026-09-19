using HPGeo.Core.Model;

namespace HPGeo.Core.Imagery;

/// <summary>E = A·x + B·y + C, N = D·x + E·y + F — a mosaic pixel → VN-2000 metres.</summary>
public sealed record AffineTransform(double A, double B, double C, double D, double E, double F)
{
    public PlanePoint Apply(double x, double y) => new(A * x + B * y + C, D * x + E * y + F);
}

/// <summary>Least-squares affine fit of the control grid and the largest residual — the number that says how far a plain (unwarped, rotated) placement would be off.</summary>
public sealed record AffineFitResult(AffineTransform Transform, double MaxResidualM, double RmsResidualM);

/// <summary>
/// Fits an affine transform from mosaic pixels to VN-2000 metres over the control grid. The warp does not use
/// it — it exists to report the distortion (a Web Mercator tile stack is neither north-up nor uniformly
/// scaled in TM-3) and to sanity-check the control grid: a residual of a few decimetres on a one-kilometre
/// plot is expected, metres mean something is wrong.
/// </summary>
public static class AffineFit
{
    /// <summary>Residual above which the import warns the user (metres).</summary>
    public const double WarnResidualM = 0.5;

    public static AffineFitResult Fit(ControlGrid grid, OutputRaster output)
    {
        var points = grid.Points().Select(p => (p.SrcX, p.SrcY, Target: output.PixelCenter(p.OutCol, p.OutRow))).ToList();
        if (points.Count < 3) throw new ArgumentException("Cần ít nhất 3 điểm khống chế để khớp affine.");

        // Normal equations for [x y 1] · [a b c]ᵀ = E (and the same matrix for N).
        double sxx = 0, sxy = 0, sx = 0, syy = 0, sy = 0, n = points.Count;
        double sxe = 0, sye = 0, se = 0, sxn = 0, syn = 0, sn = 0;
        foreach (var (x, y, t) in points)
        {
            sxx += x * x; sxy += x * y; sx += x; syy += y * y; sy += y;
            sxe += x * t.Easting; sye += y * t.Easting; se += t.Easting;
            sxn += x * t.Northing; syn += y * t.Northing; sn += t.Northing;
        }
        var m = new[,] { { sxx, sxy, sx }, { sxy, syy, sy }, { sx, sy, n } };
        var east = Solve3(m, new[] { sxe, sye, se });
        var north = Solve3(m, new[] { sxn, syn, sn });
        var transform = new AffineTransform(east[0], east[1], east[2], north[0], north[1], north[2]);

        double max = 0, sumSq = 0;
        foreach (var (x, y, t) in points)
        {
            var p = transform.Apply(x, y);
            var d = Math.Sqrt((p.Easting - t.Easting) * (p.Easting - t.Easting) + (p.Northing - t.Northing) * (p.Northing - t.Northing));
            max = Math.Max(max, d);
            sumSq += d * d;
        }
        return new AffineFitResult(transform, max, Math.Sqrt(sumSq / points.Count));
    }

    /// <summary>Cramer's rule on a symmetric 3×3 system; the normal matrix of a non-degenerate grid is well conditioned.</summary>
    private static double[] Solve3(double[,] m, double[] b)
    {
        var det = Det(m[0, 0], m[0, 1], m[0, 2], m[1, 0], m[1, 1], m[1, 2], m[2, 0], m[2, 1], m[2, 2]);
        if (Math.Abs(det) < 1e-12) throw new ArgumentException("Điểm khống chế thẳng hàng — không khớp được affine.");
        var x = Det(b[0], m[0, 1], m[0, 2], b[1], m[1, 1], m[1, 2], b[2], m[2, 1], m[2, 2]) / det;
        var y = Det(m[0, 0], b[0], m[0, 2], m[1, 0], b[1], m[1, 2], m[2, 0], b[2], m[2, 2]) / det;
        var z = Det(m[0, 0], m[0, 1], b[0], m[1, 0], m[1, 1], b[1], m[2, 0], m[2, 1], b[2]) / det;
        return new[] { x, y, z };
    }

    private static double Det(double a, double b, double c, double d, double e, double f, double g, double h, double i) =>
        a * (e * i - f * h) - b * (d * i - f * g) + c * (d * h - e * g);
}
