using System.Globalization;
using HPAutoCad.Core.HPGeoLink.Catalog;
using HPAutoCad.Core.HPGeoLink.Kml;
using HPAutoCad.Core.HPGeoLink.Projection;
using HPAutoCad.Core.HPGeoLink.Units;

namespace HPAutoCad.Core.HPGeoLink.Conversion;

/// <summary>
/// The <c>key=value</c> line the script command takes, e.g.
/// <c>cm=105.75 type=both out=C:\kmz\site.kmz</c>. Keys: cm (required; also accepts 105°45′), k0, fe, fn,
/// type (both|points|boundaries), out (required), unit (m|mm|cm|dm|km|ft|in — overrides INSUNITS), layer
/// (AutoCAD wildcard), name (document name), pcolor / lcolor (aabbggrr). Unknown keys are refused so a
/// typo never silently changes the result.
/// </summary>
public sealed record ExportArguments(
    double CentralMeridianDeg,
    double ScaleFactor,
    double FalseEasting,
    double FalseNorthing,
    KmlOutput Output,
    string OutputPath,
    DrawingUnit? UnitOverride,
    string? LayerFilter,
    string? DocumentName,
    string PointColor,
    string LineColor)
{
    public TmParameters Tm => new(CentralMeridianDeg, ScaleFactor, FalseEasting, FalseNorthing);

    private static readonly HashSet<string> KnownKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        "cm", "k0", "fe", "fn", "type", "out", "unit", "layer", "name", "pcolor", "lcolor",
    };

    public static ExportArguments Parse(string line)
    {
        var values = Tokenize(line);
        foreach (var key in values.Keys)
        {
            if (!KnownKeys.Contains(key))
                throw new ArgumentException($"Tham số '{key}' không hợp lệ. Cho phép: {string.Join(", ", KnownKeys)}.");
        }

        var cm = CentralMeridian.Parse(Get(values, "cm"))
            ?? throw new ArgumentException("Thiếu cm=<kinh tuyến trục> (vd cm=105.75 hoặc cm=105°45′).");
        var output = Get(values, "out") ?? throw new ArgumentException("Thiếu out=<đường dẫn .kmz>.");
        if (!output.EndsWith(".kmz", StringComparison.OrdinalIgnoreCase)) output += ".kmz";

        return new ExportArguments(
            cm,
            Number(values, "k0", TmParameters.Tm3ScaleFactor),
            Number(values, "fe", TmParameters.DefaultFalseEasting),
            Number(values, "fn", TmParameters.DefaultFalseNorthing),
            KmlExportOptions.ParseOutput(Get(values, "type")),
            output,
            ParseUnit(Get(values, "unit")),
            Get(values, "layer"),
            Get(values, "name"),
            KmlColor.Normalize(Get(values, "pcolor") ?? KmlColor.DefaultPoint, "điểm"),
            KmlColor.Normalize(Get(values, "lcolor") ?? KmlColor.DefaultLine, "đường"));
    }

    public static DrawingUnit? ParseUnit(string? text) => (text ?? "").Trim().ToLowerInvariant() switch
    {
        "" => null,
        "m" or "meter" or "meters" or "metre" or "metres" => DrawingUnit.Meters,
        "mm" or "millimeter" or "millimeters" => DrawingUnit.Millimeters,
        "cm" or "centimeter" or "centimeters" => DrawingUnit.Centimeters,
        "dm" => DrawingUnit.Decimeters,
        "km" => DrawingUnit.Kilometers,
        "ft" or "feet" or "foot" => DrawingUnit.Feet,
        "in" or "inch" or "inches" => DrawingUnit.Inches,
        var other => throw new ArgumentException($"Đơn vị '{other}' không hợp lệ (m | mm | cm | dm | km | ft | in)."),
    };

    /// <summary>Splits <c>key=value</c> tokens; a value may be quoted ("...") to carry spaces.</summary>
    public static Dictionary<string, string> Tokenize(string line)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var i = 0;
        while (i < line.Length)
        {
            while (i < line.Length && char.IsWhiteSpace(line[i])) i++;
            if (i >= line.Length) break;
            var eq = line.IndexOf('=', i);
            if (eq < 0) throw new ArgumentException($"Tham số '{line[i..].Trim()}' phải có dạng key=value.");
            var key = line[i..eq].Trim();
            if (key.Length == 0 || key.Any(char.IsWhiteSpace)) throw new ArgumentException($"Tham số gần '{line[i..eq]}' phải có dạng key=value.");
            i = eq + 1;
            string value;
            if (i < line.Length && line[i] == '"')
            {
                var close = line.IndexOf('"', i + 1);
                if (close < 0) throw new ArgumentException($"Thiếu dấu \" đóng cho {key}.");
                value = line[(i + 1)..close];
                i = close + 1;
            }
            else
            {
                var end = i;
                while (end < line.Length && !char.IsWhiteSpace(line[end])) end++;
                value = line[i..end];
                i = end;
            }
            result[key] = value;
        }
        return result;
    }

    private static string? Get(Dictionary<string, string> values, string key) =>
        values.TryGetValue(key, out var v) && v.Length > 0 ? v : null;

    private static double Number(Dictionary<string, string> values, string key, double fallback)
    {
        var text = Get(values, key);
        if (text is null) return fallback;
        if (double.TryParse(text.Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out var v)) return v;
        throw new ArgumentException($"{key}='{text}' không phải số.");
    }
}
