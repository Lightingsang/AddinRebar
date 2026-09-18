namespace HPGeo.Core.Projection;

/// <summary>Geodetic (φ, λ, h) ↔ geocentric Cartesian (X, Y, Z) on an ellipsoid. Radians and metres.</summary>
public static class GeocentricConverter
{
    /// <summary>Iteration count and threshold of the reference tool's <c>ecefToGeodetic</c>.</summary>
    private const int MaxIterations = 12;
    private const double Threshold = 1e-14;

    public static (double X, double Y, double Z) ToGeocentric(double latRad, double lonRad, double heightM, Ellipsoid ell)
    {
        var sinLat = Math.Sin(latRad);
        var cosLat = Math.Cos(latRad);
        var sinLon = Math.Sin(lonRad);
        var cosLon = Math.Cos(lonRad);
        var n = ell.PrimeVerticalRadius(latRad);
        return (
            (n + heightM) * cosLat * cosLon,
            (n + heightM) * cosLat * sinLon,
            (n * (1 - ell.E2) + heightM) * sinLat);
    }

    public static (double LatRad, double LonRad, double HeightM) ToGeodetic(double x, double y, double z, Ellipsoid ell)
    {
        var e2 = ell.E2;
        var lon = Math.Atan2(y, x);
        var p = Math.Sqrt(x * x + y * y);
        var lat = Math.Atan2(z, p * (1 - e2));
        var h = 0.0;
        for (var i = 0; i < MaxIterations; i++)
        {
            var n = ell.PrimeVerticalRadius(lat);
            h = p / Math.Cos(lat) - n;
            var next = Math.Atan2(z, p * (1 - e2 * n / (n + h)));
            if (Math.Abs(next - lat) < Threshold)
            {
                lat = next;
                break;
            }
            lat = next;
        }
        return (lat, lon, h);
    }
}
