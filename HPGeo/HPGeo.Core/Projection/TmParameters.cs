namespace HPGeo.Core.Projection;

/// <summary>
/// Transverse Mercator projection parameters. VN-2000 "TM-3" (the 3° zone used by every province's
/// cadastral records) is k0 = 0.9999, false easting 500 000 m, false northing 0, latitude of origin 0,
/// central meridian per province — the tool never falls back to a default central meridian.
/// </summary>
public sealed record TmParameters(double CentralMeridianDeg, double ScaleFactor, double FalseEasting, double FalseNorthing)
{
    public const double Tm3ScaleFactor = 0.9999;
    public const double DefaultFalseEasting = 500000.0;
    public const double DefaultFalseNorthing = 0.0;

    /// <summary>TM-3 with the given central meridian and the standard VN-2000 constants.</summary>
    public static TmParameters Tm3(double centralMeridianDeg) =>
        new(centralMeridianDeg, Tm3ScaleFactor, DefaultFalseEasting, DefaultFalseNorthing);

    public bool IsValid =>
        double.IsFinite(CentralMeridianDeg) && CentralMeridianDeg is >= -180 and <= 180 &&
        double.IsFinite(ScaleFactor) && ScaleFactor > 0 &&
        double.IsFinite(FalseEasting) && double.IsFinite(FalseNorthing);
}
