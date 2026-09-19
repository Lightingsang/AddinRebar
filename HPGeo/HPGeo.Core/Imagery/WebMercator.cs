using HPGeo.Core.Model;

namespace HPGeo.Core.Imagery;

/// <summary>A tile address in the XYZ scheme (origin top-left, y grows south).</summary>
public readonly record struct TileAddress(int Zoom, int X, int Y);

/// <summary>A position in the global pixel grid of one zoom level (256 px tiles, origin top-left).</summary>
public readonly record struct GlobalPixel(int Zoom, double X, double Y);

/// <summary>
/// Web Mercator (EPSG:3857) as the tile servers use it: spherical, R = 6 378 137 m, XYZ tiles of 256 px with
/// the origin at the top-left (y grows south). Pinned against proj4 by <c>golden-webmercator.json</c>.
/// </summary>
public static class WebMercator
{
    public const double EarthRadiusM = 6378137.0;
    public const int TileSizePx = 256;
    public const double MaxLatitudeDeg = 85.05112878;
    private const double OriginShift = Math.PI * EarthRadiusM;

    /// <summary>Lon/lat (degrees) → metres east/north of the origin.</summary>
    public static (double X, double Y) ToMeters(GeoPoint p)
    {
        var lat = Math.Clamp(p.LatDeg, -MaxLatitudeDeg, MaxLatitudeDeg) * Math.PI / 180.0;
        return (EarthRadiusM * p.LonDeg * Math.PI / 180.0, EarthRadiusM * Math.Log(Math.Tan(Math.PI / 4 + lat / 2)));
    }

    public static GeoPoint FromMeters(double x, double y) =>
        new((2 * Math.Atan(Math.Exp(y / EarthRadiusM)) - Math.PI / 2) * 180.0 / Math.PI, x / EarthRadiusM * 180.0 / Math.PI);

    /// <summary>Metres per pixel at a latitude and zoom: 156 543.03 · cos φ / 2^z.</summary>
    public static double ResolutionMPerPx(double latDeg, int zoom) =>
        2 * OriginShift / TileSizePx / Math.Pow(2, zoom) * Math.Cos(latDeg * Math.PI / 180.0);

    /// <summary>Fractional global pixel of a position at a zoom (x east, y south from the top-left of the world).</summary>
    public static GlobalPixel ToPixel(GeoPoint p, int zoom)
    {
        var (mx, my) = ToMeters(p);
        var worldPx = TileSizePx * Math.Pow(2, zoom);
        return new GlobalPixel(zoom, (mx + OriginShift) / (2 * OriginShift) * worldPx, (OriginShift - my) / (2 * OriginShift) * worldPx);
    }

    public static GeoPoint FromPixel(GlobalPixel px)
    {
        var worldPx = TileSizePx * Math.Pow(2, px.Zoom);
        var mx = px.X / worldPx * 2 * OriginShift - OriginShift;
        var my = OriginShift - px.Y / worldPx * 2 * OriginShift;
        return FromMeters(mx, my);
    }

    /// <summary>The tile containing a position.</summary>
    public static TileAddress ToTile(GeoPoint p, int zoom)
    {
        var px = ToPixel(p, zoom);
        var n = (int)Math.Pow(2, zoom);
        return new TileAddress(zoom, Math.Clamp((int)Math.Floor(px.X / TileSizePx), 0, n - 1), Math.Clamp((int)Math.Floor(px.Y / TileSizePx), 0, n - 1));
    }

    /// <summary>Top-left global pixel of a tile.</summary>
    public static GlobalPixel TileOrigin(TileAddress t) => new(t.Zoom, (double)t.X * TileSizePx, (double)t.Y * TileSizePx);
}
