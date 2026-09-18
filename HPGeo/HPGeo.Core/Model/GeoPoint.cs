namespace HPGeo.Core.Model;

/// <summary>A WGS84 geodetic position in decimal degrees. Height is ignored throughout (KML is clamped to ground).</summary>
public readonly record struct GeoPoint(double LatDeg, double LonDeg)
{
    public bool IsFinite => double.IsFinite(LatDeg) && double.IsFinite(LonDeg);
}
