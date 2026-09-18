using HPGeo.Core.Model;

namespace HPGeo.Core.Projection;

/// <summary>
/// VN-2000 TM-3 grid ↔ WGS84 geodetic. Forward = the reference tool's chain (TM inverse on the WGS84
/// ellipsoid → geocentric → coordinate-frame Helmert → geodetic); reverse = the exact inverse chain.
/// Ellipsoidal height is taken as 0 on the VN-2000 side, as the reference tool does.
/// </summary>
public sealed class Vn2000Wgs84Transform
{
    private readonly Helmert7Parameters _helmert;
    private readonly Ellipsoid _ellipsoid;

    public Vn2000Wgs84Transform() : this(Helmert7Parameters.Vn2000ToWgs84, Ellipsoid.Wgs84) { }

    public Vn2000Wgs84Transform(Helmert7Parameters helmert, Ellipsoid ellipsoid)
    {
        _helmert = helmert;
        _ellipsoid = ellipsoid;
    }

    public static Vn2000Wgs84Transform Default { get; } = new();

    /// <summary>VN-2000 grid → WGS84 lat/lon (degrees).</summary>
    public GeoPoint ToWgs84(PlanePoint vn2000, TmParameters tm)
    {
        var (lat, lon) = TransverseMercator.Inverse(vn2000.Easting, vn2000.Northing, tm, _ellipsoid);
        var wgs = Vn2000GeodeticToWgs84(lat, lon);
        return new GeoPoint(RadToDeg(wgs.LatRad), RadToDeg(wgs.LonRad));
    }

    /// <summary>
    /// WGS84 lat/lon (degrees) → VN-2000 grid. The forward chain fixes the ellipsoidal height at 0 on the
    /// VN-2000 side, which leaves the WGS84 side tens of metres off the ellipsoid (−35 m in the north-west);
    /// the reverse therefore solves for the WGS84 height that lands at h = 0 on VN-2000 — dropping it would
    /// shift the grid position by ~1 mm per 30 m of height because the two datums' normals differ.
    /// </summary>
    public PlanePoint ToVn2000(GeoPoint wgs84, TmParameters tm)
    {
        var latW = DegToRad(wgs84.LatDeg);
        var lonW = DegToRad(wgs84.LonDeg);
        var heightW = 0.0;
        double lat = 0, lon = 0;
        for (var i = 0; i < HeightIterations; i++)
        {
            var geocentric = GeocentricConverter.ToGeocentric(latW, lonW, heightW, _ellipsoid);
            var source = Helmert7.Inverse(geocentric, _helmert);
            double heightVn;
            (lat, lon, heightVn) = GeocentricConverter.ToGeodetic(source.X, source.Y, source.Z, _ellipsoid);
            if (Math.Abs(heightVn) < HeightToleranceM) break;
            heightW -= heightVn;
        }
        var (e, n) = TransverseMercator.Forward(lat, lon, tm, _ellipsoid);
        return new PlanePoint(e, n);
    }

    private const int HeightIterations = 5;
    private const double HeightToleranceM = 1e-6;

    /// <summary>VN-2000 geodetic → WGS84 geodetic (radians in, radians out), the Helmert step alone.</summary>
    public (double LatRad, double LonRad, double HeightM) Vn2000GeodeticToWgs84(double latRad, double lonRad, double heightM = 0)
    {
        var geocentric = GeocentricConverter.ToGeocentric(latRad, lonRad, heightM, _ellipsoid);
        var shifted = Helmert7.Forward(geocentric, _helmert);
        return GeocentricConverter.ToGeodetic(shifted.X, shifted.Y, shifted.Z, _ellipsoid);
    }

    private static double DegToRad(double deg) => deg * Math.PI / 180.0;
    private static double RadToDeg(double rad) => rad * 180.0 / Math.PI;
}
