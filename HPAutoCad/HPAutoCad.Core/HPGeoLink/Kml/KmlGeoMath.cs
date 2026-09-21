using System.Globalization;
using HPAutoCad.Core.HPGeoLink.Model;

namespace HPAutoCad.Core.HPGeoLink.Kml;

/// <summary>
/// Metre offsets on the WGS84 sphere and coordinate formatting, exactly as the reference tool draws its
/// survey marker: 1° of latitude = 111 320 m, longitude scaled by cos(lat) floored at 0.01.
/// </summary>
public static class KmlGeoMath
{
    private const double MetersPerDegree = 111320.0;

    public static double MetersToLatDeg(double meters) => meters / MetersPerDegree;

    public static double MetersToLonDeg(double meters, double latDeg)
    {
        var c = Math.Cos(latDeg * Math.PI / 180.0);
        return meters / (MetersPerDegree * Math.Max(Math.Abs(c), 0.01));
    }

    public static GeoPoint Offset(GeoPoint p, double eastMeters, double northMeters) =>
        new(p.LatDeg + MetersToLatDeg(northMeters), p.LonDeg + MetersToLonDeg(eastMeters, p.LatDeg));

    /// <summary>"lon,lat,0" with the given number of decimals (8 for markers, 7 for rings in the reference output).</summary>
    public static string Coordinate(GeoPoint p, int decimals)
    {
        var f = "F" + decimals.ToString(CultureInfo.InvariantCulture);
        return string.Create(CultureInfo.InvariantCulture, $"{p.LonDeg.ToString(f, CultureInfo.InvariantCulture)},{p.LatDeg.ToString(f, CultureInfo.InvariantCulture)},0");
    }
}
