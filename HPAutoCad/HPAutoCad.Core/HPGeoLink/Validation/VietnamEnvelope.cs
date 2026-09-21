using HPAutoCad.Core.HPGeoLink.Model;

namespace HPAutoCad.Core.HPGeoLink.Validation;

/// <summary>
/// Broad national envelope with a margin for islands and coastal data — the reference tool's
/// <c>isVietnamWGS84</c>. A converted point outside it is an error, not a warning: it means the
/// central meridian, the E/N order or the unit is wrong, never a real position.
/// </summary>
public static class VietnamEnvelope
{
    public const double MinLat = 7.5;
    public const double MaxLat = 24.0;
    public const double MinLon = 101.5;
    public const double MaxLon = 110.5;

    public static bool Contains(GeoPoint p) =>
        p.IsFinite && p.LatDeg >= MinLat && p.LatDeg <= MaxLat && p.LonDeg >= MinLon && p.LonDeg <= MaxLon;
}
