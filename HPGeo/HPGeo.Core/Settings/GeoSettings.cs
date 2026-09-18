using HPGeo.Core.Kml;
using HPGeo.Core.Units;

namespace HPGeo.Core.Settings;

/// <summary>
/// What the dialogs remember: the coordinate system and the export presentation. Stored per drawing (named
/// object dictionary "HPGEO") and per user (settings.json); the drawing's copy wins, then the user's last
/// values, then the first-run defaults. Everything is optional so a partial record still applies.
/// </summary>
public sealed record GeoSettings
{
    public const string Key = "HPGEO";

    public bool UseCurrentCatalog { get; init; } = true;
    public string? ProvinceName { get; init; }
    public double? CentralMeridianDeg { get; init; }
    public double? ScaleFactor { get; init; }
    public double? FalseEasting { get; init; }
    public double? FalseNorthing { get; init; }
    /// <summary>The unit the user chose when INSUNITS was unknown; null when the drawing's own unit was used.</summary>
    public DrawingUnit? Unit { get; init; }
    public KmlOutput Output { get; init; } = KmlOutput.Both;
    public string PointColor { get; init; } = KmlColor.DefaultPoint;
    public string LineColor { get; init; } = KmlColor.DefaultLine;
    /// <summary>Last export folder (per user only).</summary>
    public string? ExportDirectory { get; init; }
    public string? SavedBy { get; init; }
    /// <summary>Per user only: false keeps the satellite map panel off (nothing leaves the machine before an export).</summary>
    public bool MapEnabled { get; init; } = true;

    public bool HasCentralMeridian => CentralMeridianDeg is { } cm && double.IsFinite(cm);
}
