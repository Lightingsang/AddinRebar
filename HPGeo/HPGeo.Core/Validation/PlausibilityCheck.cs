using HPGeo.Core.Model;

namespace HPGeo.Core.Validation;

/// <summary>
/// Diagnostic only. VN-2000 TM-3 eastings fall in 100 000–900 000 m and northings in 800 000–2 600 000 m
/// for the whole country; a pair outside those bands usually means swapped axes or a drawing in
/// millimetres. The check produces hints — it never changes the data.
/// </summary>
public static class PlausibilityCheck
{
    public const double MinEasting = 100000;
    public const double MaxEasting = 900000;
    public const double MinNorthing = 800000;
    public const double MaxNorthing = 2600000;

    public static bool PlausibleEasting(double v) => v >= MinEasting && v <= MaxEasting;
    public static bool PlausibleNorthing(double v) => v >= MinNorthing && v <= MaxNorthing;

    public static bool IsPlausible(PlanePoint p) => PlausibleEasting(p.Easting) && PlausibleNorthing(p.Northing);

    /// <summary>A short Vietnamese hint about what looks wrong, or null when the pair is plausible.</summary>
    public static string? Diagnose(PlanePoint p)
    {
        if (IsPlausible(p)) return null;
        if (PlausibleEasting(p.Northing) && PlausibleNorthing(p.Easting))
            return "có vẻ X/Y bị đảo (giá trị X giống Northing, Y giống Easting)";
        if (PlausibleEasting(p.Easting / 1000) && PlausibleNorthing(p.Northing / 1000))
            return "có vẻ bản vẽ theo mm (chia 1000 thì hợp lý) — kiểm tra đơn vị";
        if (PlausibleEasting(p.Easting * 1000) && PlausibleNorthing(p.Northing * 1000))
            return "giá trị quá nhỏ — có thể là km hoặc toạ độ cục bộ";
        return "E/N ngoài dải VN-2000 thông thường (E 100 000–900 000, N 800 000–2 600 000 m)";
    }
}
