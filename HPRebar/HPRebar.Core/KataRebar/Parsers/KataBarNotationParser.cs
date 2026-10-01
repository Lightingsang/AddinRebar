using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using HPRebar.Core.KataRebar.Models;

namespace HPRebar.Core.KataRebar.Parsers;

/// <summary>
/// High-performance, robust parser for Vietnamese structural reinforcing bar notation strings.
/// Parses notations such as '2f18', '3f20', '6f25', '2d8', '2f20;2f16', '-50;5f20', 'a100/200/50', '50/25'.
/// </summary>
public static class KataBarNotationParser
{
    private static readonly Regex BarRegex = new(
        @"^(?<count>\d+)?\s*(?:f|d|phi|ø|Ø|%%c|Φ)\s*(?<dia>\d+(?:\.\d+)?)$",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    /// <summary>
    /// The parts of a bar cell <see cref="ParseBarList"/> drops because they read as no bar
    /// ("2x18" in "2f20+2x18"); the placeholders 0, - and * are not counted.
    /// </summary>
    public static IReadOnlyList<string> UnreadableTokens(string? text)
    {
        var bad = new List<string>();
        if (string.IsNullOrWhiteSpace(text)) return bad;

        foreach (var raw in text!.Split(new[] { ';', '+', ',' }, StringSplitOptions.RemoveEmptyEntries))
        {
            string token = raw.Trim();
            if (token.Length == 0 || token == "0" || token == "-" || token == "*") continue;
            var item = ParseSingleBar(token);
            if (item is null || item.IsEmpty) bad.Add(token);
        }

        return bad;
    }

    /// <summary>A cell holding one positive number gives it; anything else (empty, "-300;11700", text, 0) gives 0.</summary>
    public static double ParsePositive(string? text) =>
        double.TryParse(text?.Trim(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out double v)
        && v > 0.0 && !double.IsInfinity(v) ? v : 0.0;

    /// <summary>
    /// Parses a bar notation string into a list of <see cref="KataBarItem"/>s.
    /// Supports compound notations separated by ';' or '+' or ',' (e.g. '2f20;2f16', '6f20;0', '2f20+1f18').
    /// </summary>
    public static IReadOnlyList<KataBarItem> ParseBarList(string? text, int defaultLayer = 1, bool allowZeroCount = false)
    {
        if (string.IsNullOrWhiteSpace(text))
            return Array.Empty<KataBarItem>();

        string trimmed = text!.Trim();
        if (trimmed == "-" || trimmed == "*")
            return Array.Empty<KataBarItem>();

        if (trimmed == "0")
        {
            return allowZeroCount
                ? new[] { new KataBarItem(0, 0.0, defaultLayer, 0.0, "0") }
                : Array.Empty<KataBarItem>();
        }

        string[] tokens = trimmed.Split(new[] { ';', '+', ',' }, StringSplitOptions.RemoveEmptyEntries);
        var result = new List<KataBarItem>(tokens.Length);

        foreach (var rawToken in tokens)
        {
            string token = rawToken.Trim();
            if (string.IsNullOrEmpty(token) || token == "-")
                continue;

            if (token == "0")
            {
                if (allowZeroCount)
                    result.Add(new KataBarItem(0, 0.0, defaultLayer, 0.0, "0"));
                continue;
            }

            var item = ParseSingleBar(token, defaultLayer);
            if (item is not null)
            {
                if (!item.IsEmpty || (allowZeroCount && item.Count == 0))
                {
                    result.Add(item);
                }
            }
        }

        return result;
    }

    /// <summary>
    /// Parses an additional-bar cell over a support: "left;right" (either side may be "0" or empty), or one
    /// group list for both sides. '+' and ',' still join groups on one side.
    /// </summary>
    public static KataSideBars ParseSides(string? text, int layer)
    {
        if (string.IsNullOrWhiteSpace(text))
            return KataSideBars.None;

        string trimmed = text!.Trim();
        int split = trimmed.IndexOf(';');
        if (split < 0)
        {
            var both = ParseBarList(trimmed, layer);
            return new KataSideBars(both, both, trimmed);
        }

        return new KataSideBars(
            ParseBarList(trimmed.Substring(0, split), layer),
            ParseBarList(trimmed.Substring(split + 1), layer),
            trimmed);
    }

    /// <summary>
    /// Parses a single bar token (e.g. '2f18', 'f10', '3d20', '6f25') into a <see cref="KataBarItem"/>.
    /// </summary>
    public static KataBarItem? ParseSingleBar(string? token, int defaultLayer = 1)
    {
        if (string.IsNullOrWhiteSpace(token))
            return null;

        string clean = token!.Trim();
        var match = BarRegex.Match(clean);
        if (!match.Success)
            return null;

        int count = 1;
        if (match.Groups["count"].Success && !string.IsNullOrEmpty(match.Groups["count"].Value))
        {
            if (!int.TryParse(match.Groups["count"].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out count))
                count = 1;
        }

        if (!double.TryParse(match.Groups["dia"].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out double dia) || dia <= 0)
            return null;

        return new KataBarItem(count, dia, defaultLayer, 0.0, clean);
    }

    /// <summary>
    /// Parses stirrup spacing notations such as 'a150', '@150', '150', 'a100/200', 'a100/200/50'.
    /// Returns (SupportDense, MidspanSparse, EndDense).
    /// </summary>
    public static (double DenseStart, double SparseMid, double? DenseEnd) ParseStirrupSpacing(
        string? text,
        double defaultDense = 150.0,
        double defaultSparse = 200.0)
    {
        if (string.IsNullOrWhiteSpace(text))
            return (defaultDense, defaultSparse, null);

        string clean = text!.Trim().ToLowerInvariant()
            .Replace("a", "")
            .Replace("@", "")
            .Replace(" ", "");

        if (string.IsNullOrEmpty(clean) || clean == "0" || clean == "-")
            return (defaultDense, defaultSparse, null);

        string[] parts = clean.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);

        if (parts.Length == 1)
        {
            if (double.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out double s) && s > 0)
                return (s, s, null);
        }
        else if (parts.Length == 2)
        {
            double s1 = double.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out double p0) && p0 > 0 ? p0 : defaultDense;
            double s2 = double.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out double p1) && p1 > 0 ? p1 : defaultSparse;
            return (s1, s2, null);
        }
        else if (parts.Length >= 3)
        {
            double s1 = double.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out double p0) && p0 > 0 ? p0 : defaultDense;
            double s2 = double.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out double p1) && p1 > 0 ? p1 : defaultSparse;
            double s3 = double.TryParse(parts[2], NumberStyles.Float, CultureInfo.InvariantCulture, out double p2) && p2 > 0 ? p2 : defaultDense;
            return (s1, s2, s3);
        }

        return (defaultDense, defaultSparse, null);
    }

    /// <summary>
    /// Parses step drop and bar override strings from rows 19 & 21 (e.g. '-50', '100;5f25', '-100;5f20', '5f20').
    /// </summary>
    public static (double OffsetMm, IReadOnlyList<KataBarItem> Bars) ParseOffsetAndBars(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return (0.0, Array.Empty<KataBarItem>());

        string trimmed = text!.Trim();
        if (trimmed == "0" || trimmed == "-")
            return (0.0, Array.Empty<KataBarItem>());

        string[] tokens = trimmed.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);

        double offset = 0.0;
        var bars = new List<KataBarItem>();

        foreach (var raw in tokens)
        {
            string t = raw.Trim();
            if (string.IsNullOrEmpty(t)) continue;

            // Check if it's pure numeric (offset)
            if (double.TryParse(t, NumberStyles.Float, CultureInfo.InvariantCulture, out double num))
            {
                offset = num;
            }
            else
            {
                // Otherwise try parsing as bar notation (e.g. 5f25)
                var parsedBars = ParseBarList(t);
                if (parsedBars.Count > 0)
                {
                    bars.AddRange(parsedBars);
                }
            }
        }

        return (offset, bars);
    }

    /// <summary>
    /// Parses cell J9 ("50/25", "30/20", "30"): the distance from the concrete face to the centre of the main
    /// bars, then the clear cover of the stirrups. A number that is missing or not positive comes back as 0,
    /// so the detailing rules can tell "not given" from a value.
    /// </summary>
    public static (double MainBarCentre, double StirrupCover) ParseCover(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return (0.0, 0.0);

        string[] parts = text!.Trim().Replace(" ", "").Split(new[] { '/' }, StringSplitOptions.None);
        double main = parts.Length > 0 ? PositiveOrZero(parts[0]) : 0.0;
        double stirrup = parts.Length > 1 ? PositiveOrZero(parts[1]) : 0.0;
        return (main, stirrup);
    }

    private static double PositiveOrZero(string token) =>
        double.TryParse(token, NumberStyles.Float, CultureInfo.InvariantCulture, out double value) && value > 0 ? value : 0.0;

    /// <summary>
    /// Parses support dimension or section string from row 11 (e.g. '400', '300x500', '300*500').
    /// Returns (Width, Height). If single number, Height is 0.
    /// </summary>
    public static (double Width, double Height) ParseSupportDimension(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return (0.0, 0.0);

        string clean = text!.Trim().ToLowerInvariant().Replace(" ", "");
        string[] parts = clean.Split(new[] { 'x', '*', '/' }, StringSplitOptions.RemoveEmptyEntries);

        if (parts.Length == 1)
        {
            if (double.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out double w))
                return (w, 0.0);
        }
        else if (parts.Length >= 2)
        {
            double w = double.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out double pw) ? pw : 0.0;
            double h = double.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out double ph) ? ph : 0.0;
            return (w, h);
        }

        return (0.0, 0.0);
    }

    /// <summary>
    /// Parses a coordinate/dimension pair separated by ';' or ',' (e.g. '350;0', '250;790').
    /// </summary>
    public static (double First, double Second) ParsePair(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return (0.0, 0.0);

        string[] parts = text!.Trim().Split(new[] { ';', ',' }, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 1)
        {
            if (double.TryParse(parts[0].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out double f))
                return (f, 0.0);
        }
        else if (parts.Length >= 2)
        {
            double f = double.TryParse(parts[0].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out double pf) ? pf : 0.0;
            double s = double.TryParse(parts[1].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out double ps) ? ps : 0.0;
            return (f, s);
        }

        return (0.0, 0.0);
    }
}
