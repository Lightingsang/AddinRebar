using System.Globalization;
using HPAutoCad.Core.HPGeoLink.Catalog;
using HPAutoCad.Core.HPGeoLink.Conversion;
using HPAutoCad.Core.HPGeoLink.Units;

namespace HPAutoCad.Core.HPGeoLink.Imagery;

/// <summary>
/// The <c>key=value</c> line <c>-HPGEOIMAGE</c> takes, e.g. <c>cm=105.75 layer=RANH res=0.3 margin=30</c>. Keys:
/// cm, k0, fe, fn (all optional here — the drawing's stored zone, then the user's last one, fill in what is
/// missing; see <see cref="ImageZoneResolver"/>), handle (one closed LWPOLYLINE) or layer (AutoCAD wildcard;
/// default = every closed LWPOLYLINE of model space), res (target m/px) or zoom (tile zoom ≤ the provider's
/// maximum; the two exclude each other), area (the image covers this many times the boundary's bounding-box area,
/// default 10) or margin (metres around the boundary; the two exclude each other), unit (overrides INSUNITS), out
/// (PNG path; the world file goes beside it), provider (esri). Unknown keys and non-finite numbers are refused.
/// </summary>
public sealed record ImageArguments(
    double? CentralMeridianDeg,
    double? ScaleFactor,
    double? FalseEasting,
    double? FalseNorthing,
    string? Handle,
    string? LayerFilter,
    double? ResolutionMPerPx,
    int? Zoom,
    double? MarginM,
    double? AreaRatio,
    DrawingUnit? UnitOverride,
    string? OutputPath,
    string ProviderId)
{
    private static readonly HashSet<string> KnownKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        "cm", "k0", "fe", "fn", "handle", "layer", "res", "zoom", "margin", "area", "unit", "out", "provider",
    };

    public static ImageArguments Parse(string line)
    {
        var values = ExportArguments.Tokenize(line);
        foreach (var key in values.Keys)
        {
            if (!KnownKeys.Contains(key))
                throw new ArgumentException($"Tham số '{key}' không hợp lệ. Cho phép: {string.Join(", ", KnownKeys)}.");
        }

        var cmText = Get(values, "cm");
        double? cm = null;
        if (cmText is not null)
            cm = CentralMeridian.Parse(cmText) ?? throw new ArgumentException($"cm='{cmText}' không phải kinh tuyến trục hợp lệ (vd cm=105.75 hoặc cm=105°45′).");
        var k0 = Number(values, "k0");
        if (k0 is { } k && !(k > 0)) throw new ArgumentException("k0= (hệ số tỉ lệ) phải > 0.");
        var handle = Get(values, "handle");
        var layer = Get(values, "layer");
        if (handle is not null && layer is not null) throw new ArgumentException("Chỉ dùng một trong handle= hoặc layer=.");

        var provider = ImageryProviders.Resolve(Get(values, "provider")); // unknown ids refused here, before any work
        var res = Number(values, "res");
        var zoomText = Get(values, "zoom");
        int? zoom = null;
        if (zoomText is not null)
        {
            if (!int.TryParse(zoomText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var z) || z < 0 || z > provider.MaxZoom)
                throw new ArgumentException($"zoom='{zoomText}' phải là số nguyên 0–{provider.MaxZoom} ({provider.DisplayName}).");
            zoom = z;
        }
        if (res is not null && zoom is not null) throw new ArgumentException("Chỉ dùng một trong res= (m/px) hoặc zoom=.");
        if (res is { } r && !(r > 0)) throw new ArgumentException("res= (m/px) phải > 0.");
        var margin = Number(values, "margin");
        if (margin is { } mg && !(mg >= 0)) throw new ArgumentException("margin= (m) phải ≥ 0.");
        var area = Number(values, "area");
        if (area is { } ar && !(ar >= 1)) throw new ArgumentException("area= (tỉ lệ diện tích) phải ≥ 1.");
        if (margin is not null && area is not null) throw new ArgumentException("Chỉ dùng một trong margin= (m) hoặc area= (tỉ lệ diện tích).");

        var output = Get(values, "out");
        if (output is not null && !output.EndsWith(".png", StringComparison.OrdinalIgnoreCase)) output += ".png";

        return new ImageArguments(
            cm,
            k0,
            Number(values, "fe"),
            Number(values, "fn"),
            handle,
            layer,
            res,
            zoom,
            margin,
            area,
            ExportArguments.ParseUnit(Get(values, "unit")),
            output,
            provider.Id);
    }

    private static string? Get(Dictionary<string, string> values, string key) =>
        values.TryGetValue(key, out var v) && v.Length > 0 ? v : null;

    /// <summary>A number that is present must be finite — "1e400" is ∞ to the parser and NaN to the projection.</summary>
    private static double? Number(Dictionary<string, string> values, string key)
    {
        var text = Get(values, key);
        if (text is null) return null;
        if (double.TryParse(text.Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out var v) && double.IsFinite(v)) return v;
        throw new ArgumentException($"{key}='{text}' không phải số hữu hạn.");
    }
}
