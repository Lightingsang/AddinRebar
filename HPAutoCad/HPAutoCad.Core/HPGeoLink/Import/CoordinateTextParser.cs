using System.Globalization;
using System.Text.RegularExpressions;
using HPAutoCad.Core.HPGeoLink.Model;
using HPAutoCad.Core.HPGeoLink.Validation;

namespace HPAutoCad.Core.HPGeoLink.Import;

/// <summary>Which pair of numbers a pasted line carries.</summary>
public enum PastedCoordinateKind
{
    /// <summary>WGS84 "lat, lon" (or "lon, lat" — detected by the Viet Nam envelope).</summary>
    Wgs84,
    /// <summary>VN-2000 grid pair; the order is <see cref="PairOrder"/>.</summary>
    Vn2000,
}

/// <summary>How to read a VN-2000 pair — the reference tool's <c>coordOrder</c>.</summary>
public enum PairOrder
{
    /// <summary>Decide per line by the plausible ranges; ambiguous pairs are kept as E,N and flagged.</summary>
    Auto,
    /// <summary>First value Easting, second Northing (AutoCAD / E,N lists).</summary>
    EastingNorthing,
    /// <summary>First value X = Northing, second Y = Easting (cadastral paperwork).</summary>
    CadastralXY,
}

/// <summary>One parsed line: the pair as read, how it was interpreted, and whether that was a guess.</summary>
public sealed record PastedCoordinate(int LineNumber, string Label, double First, double Second, string Mode, bool Ambiguous)
{
    /// <summary>For VN-2000 lines: the pair as Easting/Northing after the order rule.</summary>
    public PlanePoint? Grid { get; init; }

    /// <summary>For WGS84 lines: the pair as lat/lon after the swap rule.</summary>
    public GeoPoint? Geo { get; init; }
}

/// <summary>
/// Reads pasted coordinate text one line at a time: "POINT 588940.2481,1251366.2704", "588940.2481 1251366.2704",
/// "1;10.77;106.70", "P1 10.7769, 106.7009", "3<tab>600125.887<tab>1231608.428"… — the first two numbers on a
/// line are the pair, anything before them is the label, and a leading integer followed by two more numbers is a
/// row number. The VN-2000 order rules are the reference tool's <c>classifyPair</c>: the
/// plausible ranges decide when they can, an explicit order always wins, and a pair that fits both ways is kept
/// in the order given and flagged — never silently swapped.
/// </summary>
public static partial class CoordinateTextParser
{
    public const int MaxLines = 5000;

    // Decimal separator is the dot, as in the reference tool; a comma is a separator ("600125,1231608" is two integers).
    // The lookbehind keeps a label's trailing digit ("M2 600124.894 …") from being read as the first number.
    [GeneratedRegex(@"(?<![\w.])([-+]?\d+(?:\.\d+)?)[,;\s]+([-+]?\d+(?:\.\d+)?)(?![\w.])")]
    private static partial Regex PairPattern();

    [GeneratedRegex(@"^\s*POINT\b", RegexOptions.IgnoreCase)]
    private static partial Regex PointKeyword();

    [GeneratedRegex(@"\G[,;\s]+([-+]?\d+(?:\.\d+)?)(?![\w.])")]
    private static partial Regex TrailingNumber();

    public static IReadOnlyList<PastedCoordinate> Parse(string text, PastedCoordinateKind kind, PairOrder order = PairOrder.Auto)
    {
        var result = new List<PastedCoordinate>();
        if (string.IsNullOrWhiteSpace(text)) return result;
        var lines = text.Replace("\r\n", "\n").Split('\n');
        for (var i = 0; i < lines.Length && result.Count < MaxLines; i++)
        {
            var line = lines[i].Trim();
            if (line.Length == 0) continue;
            var m = PairPattern().Match(line);
            if (!m.Success) continue;
            var first = m.Groups[1].Value;
            var a = Number(first);
            var b = Number(m.Groups[2].Value);
            if (a is null || b is null) continue;
            var prefix = line[..m.Index].Trim();

            // "1 600125.887 1231608.428" / "1;10.77;106.70": a leading integer followed by two more numbers is the row
            // number, not a coordinate (a coordinate always carries decimals or is far larger than a row index).
            var tail = TrailingNumber().Match(line, m.Index + m.Length);
            if (tail.Success && !first.Contains('.') && prefix.Length == 0)
            {
                prefix = first;
                a = b;
                b = Number(tail.Groups[1].Value);
                if (b is null) continue;
            }
            var label = PointKeyword().IsMatch(prefix) || prefix.Length == 0 ? (result.Count + 1).ToString(CultureInfo.InvariantCulture) : prefix.TrimEnd(':', ',', ';');

            result.Add(kind == PastedCoordinateKind.Wgs84
                ? ClassifyGeo(i + 1, label, a.Value, b.Value)
                : ClassifyGrid(i + 1, label, a.Value, b.Value, PointKeyword().IsMatch(line), order));
        }
        return result;
    }

    private static PastedCoordinate ClassifyGrid(int lineNumber, string label, double a, double b, bool isPointKeyword, PairOrder order)
    {
        PlanePoint grid;
        string mode;
        var ambiguous = false;
        var aE = PlausibilityCheck.PlausibleEasting(a);
        var aN = PlausibilityCheck.PlausibleNorthing(a);
        var bE = PlausibilityCheck.PlausibleEasting(b);
        var bN = PlausibilityCheck.PlausibleNorthing(b);
        switch (order)
        {
            case PairOrder.CadastralXY:
                grid = new PlanePoint(b, a);
                mode = "X,Y địa chính → E=Y; N=X";
                break;
            case PairOrder.EastingNorthing:
                grid = new PlanePoint(a, b);
                mode = "E,N / AutoCAD → E=giá trị 1; N=giá trị 2";
                break;
            default:
                if (isPointKeyword && aE && bN)
                {
                    grid = new PlanePoint(a, b);
                    mode = "AUTO: nhận dạng POINT/AutoCAD → E,N";
                }
                else if (aE && bN && !(aN && bE))
                {
                    grid = new PlanePoint(a, b);
                    mode = "AUTO: giá trị 1 = Easting; giá trị 2 = Northing";
                }
                else if (aN && bE && !(aE && bN))
                {
                    grid = new PlanePoint(b, a);
                    mode = "AUTO: giá trị 1 = Northing; giá trị 2 = Easting";
                }
                else
                {
                    grid = new PlanePoint(a, b);
                    mode = "AUTO: cặp mơ hồ, tạm hiểu theo E,N; hãy chọn thứ tự thủ công nếu cần";
                    ambiguous = true;
                }
                break;
        }
        return new PastedCoordinate(lineNumber, label, a, b, mode, ambiguous) { Grid = grid };
    }

    private static PastedCoordinate ClassifyGeo(int lineNumber, string label, double a, double b)
    {
        var asIs = new GeoPoint(a, b);
        var swapped = new GeoPoint(b, a);
        if (VietnamEnvelope.Contains(asIs) && !VietnamEnvelope.Contains(swapped))
            return new PastedCoordinate(lineNumber, label, a, b, "lat, lon", false) { Geo = asIs };
        if (VietnamEnvelope.Contains(swapped) && !VietnamEnvelope.Contains(asIs))
            return new PastedCoordinate(lineNumber, label, a, b, "lon, lat (đã đảo về lat, lon)", false) { Geo = swapped };
        return new PastedCoordinate(lineNumber, label, a, b, "lat, lon (ngoài Việt Nam — kiểm tra lại)", true) { Geo = asIs };
    }

    private static double? Number(string text) =>
        double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var v) && double.IsFinite(v) ? v : null;
}
