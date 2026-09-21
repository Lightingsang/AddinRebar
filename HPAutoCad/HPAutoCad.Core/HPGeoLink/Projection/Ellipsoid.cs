namespace HPAutoCad.Core.HPGeoLink.Projection;

/// <summary>
/// Reference ellipsoid. VN-2000 is defined on the WGS84 ellipsoid (a = 6 378 137 m, 1/f = 298.257223563),
/// so both datums of this tool share one figure and differ only by the Helmert shift.
/// </summary>
public sealed record Ellipsoid(double SemiMajorAxis, double InverseFlattening)
{
    public static readonly Ellipsoid Wgs84 = new(6378137.0, 298.257223563);

    public double Flattening => 1.0 / InverseFlattening;

    public double SemiMinorAxis => SemiMajorAxis * (1.0 - Flattening);

    /// <summary>First eccentricity squared, e² = f(2 − f).</summary>
    public double E2 => Flattening * (2.0 - Flattening);

    /// <summary>Second eccentricity squared, e'² = e² / (1 − e²).</summary>
    public double Ep2 => E2 / (1.0 - E2);

    /// <summary>Prime vertical radius of curvature N(φ) = a / √(1 − e² sin²φ).</summary>
    public double PrimeVerticalRadius(double latRad)
    {
        var s = Math.Sin(latRad);
        return SemiMajorAxis / Math.Sqrt(1.0 - E2 * s * s);
    }
}
