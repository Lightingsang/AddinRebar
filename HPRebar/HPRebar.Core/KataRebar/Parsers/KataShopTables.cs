using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace HPRebar.Core.KataRebar.Parsers;

/// <summary>One row of Kata's lap / anchorage table (mm): bar diameter, lap and anchorage in compression and tension.</summary>
public sealed record KataLapRow(double Diameter, double LapCompression, double LapTension, double AnchorCompression, double AnchorTension);

/// <summary>
/// The two tables of Kata's tab "Thông số đặc thù" as the settings file keeps them: rows joined by ';', each
/// "diameter:value" (unit mass, kg/m) or "diameter:lap compression,lap tension,anchor compression,anchor tension".
/// A row that does not read (a missing value, a negative or zero diameter, text) is dropped; diameters are unique.
/// </summary>
public static class KataShopTables
{
    public static IReadOnlyList<(double Diameter, double KgPerM)> ReadMass(string? text) =>
        Rows(text, 1).Select(r => (r[0], r[1])).ToList();

    public static string WriteMass(IEnumerable<(double Diameter, double KgPerM)> rows) =>
        string.Join(";", Unique(rows, r => r.Diameter).Select(r => $"{N(r.Diameter)}:{N(r.KgPerM)}"));

    public static IReadOnlyList<KataLapRow> ReadLaps(string? text) =>
        Rows(text, 4).Select(r => new KataLapRow(r[0], r[1], r[2], r[3], r[4])).ToList();

    public static string WriteLaps(IEnumerable<KataLapRow> rows) =>
        string.Join(";", Unique(rows, r => r.Diameter)
            .Select(r => $"{N(r.Diameter)}:{N(r.LapCompression)},{N(r.LapTension)},{N(r.AnchorCompression)},{N(r.AnchorTension)}"));

    /// <summary>The text with every row that does not read dropped.</summary>
    public static string NormalizeMass(string? text) => WriteMass(ReadMass(text));

    public static string NormalizeLaps(string? text) => WriteLaps(ReadLaps(text));

    private static IEnumerable<double[]> Rows(string? text, int values)
    {
        if (string.IsNullOrWhiteSpace(text)) yield break;
        var seen = new HashSet<double>();
        foreach (var raw in text!.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries))
        {
            var parts = raw.Split(':');
            if (parts.Length != 2 || !TryNumber(parts[0], out double d) || d <= 0.0 || seen.Contains(d)) continue;
            var rest = parts[1].Split(',');
            if (rest.Length != values) continue;
            var numbers = new double[values + 1];
            numbers[0] = d;
            bool ok = true;
            for (int i = 0; i < values && ok; i++)
                ok = TryNumber(rest[i], out numbers[i + 1]) && numbers[i + 1] >= 0.0;
            if (!ok) continue;
            seen.Add(d);
            yield return numbers;
        }
    }

    private static IEnumerable<T> Unique<T>(IEnumerable<T> rows, Func<T, double> key) =>
        rows.Where(r => key(r) > 0.0 && !double.IsInfinity(key(r))).GroupBy(key).Select(g => g.First()).OrderBy(key);

    private static bool TryNumber(string text, out double value) =>
        double.TryParse(text.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out value) && !double.IsNaN(value) && !double.IsInfinity(value);

    private static string N(double value) => value.ToString("0.###", CultureInfo.InvariantCulture);
}
