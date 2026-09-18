namespace HPGeo.Core.Projection;

/// <summary>
/// Transverse Mercator, Snyder's series (USGS PP 1395, eqs. 8-9/8-10 forward, 8-12..8-25 inverse).
/// The inverse is the reference tool's <c>tmInverse</c> term for term; the forward is the matching
/// series so a round trip closes to well under a millimetre inside a 3° zone.
/// Angles in radians in and out; eastings/northings in metres.
/// </summary>
public static class TransverseMercator
{
    /// <summary>Grid (E, N) → geodetic (φ, λ) on the ellipsoid of the projection.</summary>
    public static (double LatRad, double LonRad) Inverse(double easting, double northing, TmParameters p, Ellipsoid ell)
    {
        var a = ell.SemiMajorAxis;
        var e2 = ell.E2;
        var ep2 = ell.Ep2;

        var m = (northing - p.FalseNorthing) / p.ScaleFactor;
        var mu = m / (a * (1 - e2 / 4 - 3 * e2 * e2 / 64 - 5 * e2 * e2 * e2 / 256));

        var sq = Math.Sqrt(1 - e2);
        var e1 = (1 - sq) / (1 + sq);
        var phi1 = mu
            + (3 * e1 / 2 - 27 * Pow3(e1) / 32) * Math.Sin(2 * mu)
            + (21 * e1 * e1 / 16 - 55 * Pow4(e1) / 32) * Math.Sin(4 * mu)
            + (151 * Pow3(e1) / 96) * Math.Sin(6 * mu)
            + (1097 * Pow4(e1) / 512) * Math.Sin(8 * mu);

        var sp = Math.Sin(phi1);
        var cp = Math.Cos(phi1);
        var tp = Math.Tan(phi1);
        var n1 = a / Math.Sqrt(1 - e2 * sp * sp);
        var r1 = a * (1 - e2) / Math.Pow(1 - e2 * sp * sp, 1.5);
        var t1 = tp * tp;
        var c1 = ep2 * cp * cp;
        var d = (easting - p.FalseEasting) / (n1 * p.ScaleFactor);

        var lat = phi1 - (n1 * tp / r1) * (
            d * d / 2
            - (5 + 3 * t1 + 10 * c1 - 4 * c1 * c1 - 9 * ep2) * Pow4(d) / 24
            + (61 + 90 * t1 + 298 * c1 + 45 * t1 * t1 - 252 * ep2 - 3 * c1 * c1) * Pow6(d) / 720);

        var lon = DegToRad(p.CentralMeridianDeg) + (1 / cp) * (
            d
            - (1 + 2 * t1 + c1) * Pow3(d) / 6
            + (5 - 2 * c1 + 28 * t1 - 3 * c1 * c1 + 8 * ep2 + 24 * t1 * t1) * Pow5(d) / 120);

        return (lat, lon);
    }

    /// <summary>Largest correction accepted as converged, in metres; a few steps reach it from the series guess.</summary>
    private const double ForwardConvergenceM = 1e-9;
    private const int ForwardMaxIterations = 6;

    /// <summary>
    /// Geodetic (φ, λ) → grid (E, N). New code (the reference tool has no forward). Snyder's forward series
    /// is the initial guess; it is then refined until <see cref="Inverse"/> maps the result back onto the
    /// input, so a round trip closes to floating-point precision instead of the ~1 mm the two truncated
    /// series (meridian arc vs footpoint latitude) leave between them. Pinned against proj4.
    /// </summary>
    public static (double Easting, double Northing) Forward(double latRad, double lonRad, TmParameters p, Ellipsoid ell)
    {
        var target = ForwardSeries(latRad, lonRad, p, ell);
        var easting = target.Easting;
        var northing = target.Northing;
        for (var i = 0; i < ForwardMaxIterations; i++)
        {
            var (lat, lon) = Inverse(easting, northing, p, ell);
            var back = ForwardSeries(lat, lon, p, ell);
            var dE = target.Easting - back.Easting;
            var dN = target.Northing - back.Northing;
            easting += dE;
            northing += dN;
            if (Math.Abs(dE) < ForwardConvergenceM && Math.Abs(dN) < ForwardConvergenceM) break;
        }
        return (easting, northing);
    }

    /// <summary>Snyder's forward series (USGS PP 1395, 8-9/8-10) on its own.</summary>
    internal static (double Easting, double Northing) ForwardSeries(double latRad, double lonRad, TmParameters p, Ellipsoid ell)
    {
        var a = ell.SemiMajorAxis;
        var e2 = ell.E2;
        var ep2 = ell.Ep2;

        var sinLat = Math.Sin(latRad);
        var cosLat = Math.Cos(latRad);
        var tanLat = Math.Tan(latRad);

        var n = a / Math.Sqrt(1 - e2 * sinLat * sinLat);
        var t = tanLat * tanLat;
        var c = ep2 * cosLat * cosLat;
        var aa = (lonRad - DegToRad(p.CentralMeridianDeg)) * cosLat;

        var m = MeridianArc(latRad, a, e2); // latitude of origin is 0, so M0 = 0

        var easting = p.FalseEasting + p.ScaleFactor * n * (
            aa
            + (1 - t + c) * Pow3(aa) / 6
            + (5 - 18 * t + t * t + 72 * c - 58 * ep2) * Pow5(aa) / 120);

        var northing = p.FalseNorthing + p.ScaleFactor * (m + n * tanLat * (
            aa * aa / 2
            + (5 - t + 9 * c + 4 * c * c) * Pow4(aa) / 24
            + (61 - 58 * t + t * t + 600 * c - 330 * ep2) * Pow6(aa) / 720));

        return (easting, northing);
    }

    /// <summary>Meridian arc length from the equator (Snyder 3-21).</summary>
    private static double MeridianArc(double latRad, double a, double e2)
    {
        var e4 = e2 * e2;
        var e6 = e4 * e2;
        return a * (
            (1 - e2 / 4 - 3 * e4 / 64 - 5 * e6 / 256) * latRad
            - (3 * e2 / 8 + 3 * e4 / 32 + 45 * e6 / 1024) * Math.Sin(2 * latRad)
            + (15 * e4 / 256 + 45 * e6 / 1024) * Math.Sin(4 * latRad)
            - (35 * e6 / 3072) * Math.Sin(6 * latRad));
    }

    private static double DegToRad(double deg) => deg * Math.PI / 180.0;
    private static double Pow3(double x) => x * x * x;
    private static double Pow4(double x) => x * x * x * x;
    private static double Pow5(double x) => Pow4(x) * x;
    private static double Pow6(double x) => Pow3(x) * Pow3(x);
}
