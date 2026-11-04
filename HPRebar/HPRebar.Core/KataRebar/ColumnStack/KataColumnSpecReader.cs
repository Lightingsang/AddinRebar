using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using HPRebar.Core.KataRebar.Parsers;

namespace HPRebar.Core.KataRebar.ColumnStack;

/// <summary>
/// Reads sheet "Cot" of the Kata workbook (cell contract confirmed by drawing variants with Kata's VC command):
/// header C3/C4/C5, F4 "cover/starter lift", F5 bend factor, H4 hoops, H5 ties, option boxes G3/H3/I3/K3, pile cap
/// height in B9 and one storey per row from row 10 until column A is empty.
/// </summary>
public static class KataColumnSpecReader
{
    public const int FirstStoreyRow = 10;
    public const int LastStoreyRow = 216;

    private static readonly Regex Notation = new(@"^\s*[dDΦφØø]?\s*(\d+(?:\.\d+)?)\s*[aA@]\s*([\d./]+)\s*$", RegexOptions.CultureInvariant);
    private static readonly Regex CountAndDiameter = new(@"^\s*(\d+)\s*(?:[fFΦφØø]\s*(\d+))?\s*$", RegexOptions.CultureInvariant);

    public static KataColumnSpecReading Read(IKataDamCellAccessor cells)
    {
        if (cells is null)
        {
            throw new ArgumentNullException(nameof(cells));
        }

        var errors = new List<string>();
        var (cover, lift) = CoverAndLift(cells.GetText(4, 6), errors);
        var hoops = Hoops(cells.GetText(4, 8), errors);
        var ties = Ties(cells.GetText(5, 8), errors);
        var rows = Rows(cells, errors);
        if (rows.Count < 2)
        {
            errors.Add("Sheet Cot needs at least one storey and the row above it (from row 10).");
        }

        if (errors.Count > 0)
        {
            return new KataColumnSpecReading(null, errors);
        }

        var spec = new KataColumnSpec(
            cells.GetText(3, 3)?.Trim() ?? string.Empty,
            Math.Max(1, cells.GetInt(4, 3) ?? 1),
            (cells.GetDouble(5, 3) ?? 0) * 1000,
            (cells.GetDouble(9, 2) ?? 0) * 1000,
            cover,
            lift,
            cells.GetDouble(5, 6) ?? 15,
            hoops!,
            ties!,
            AutoTies: Flag(cells, 3, 9),
            HoopsThroughBeam: Flag(cells, 3, 11),
            DenseAtLaps: Flag(cells, 3, 8),
            TightInnerLayer: Flag(cells, 3, 7),
            rows);
        return new KataColumnSpecReading(spec, errors);
    }

    private static List<KataColumnStoreyRow> Rows(IKataDamCellAccessor cells, List<string> errors)
    {
        var rows = new List<KataColumnStoreyRow>();
        for (int row = FirstStoreyRow; row <= LastStoreyRow; row++)
        {
            string? name = cells.GetText(row, 1);
            if (string.IsNullOrWhiteSpace(name))
            {
                break;
            }

            var storey = Storey(cells, row, name!.Trim(), errors);
            if (storey is not null)
            {
                rows.Add(storey);
            }
        }

        return rows;
    }

    private static KataColumnStoreyRow? Storey(IKataDamCellAccessor cells, int row, string name, List<string> errors)
    {
        int before = errors.Count;
        double height = Required(cells.GetDouble(row, 2), $"B{row}", "storey height", errors) * 1000;
        double sizeB = Required(cells.GetDouble(row, 3), $"C{row}", "b", errors);
        double sizeH = cells.GetDouble(row, 4) ?? 0;
        var (alongB, diameterB) = Count(cells.GetText(row, 5), $"E{row}", required: true, errors);
        var (innerB, _) = Count(cells.GetText(row, 6), $"F{row}", required: false, errors);
        var (alongH, _) = Count(cells.GetText(row, 7), $"G{row}", required: sizeH != 0, errors);
        var (innerH, _) = Count(cells.GetText(row, 8), $"H{row}", required: false, errors);
        var (outer, inner) = Diameters(cells.GetText(row, 9), diameterB, $"I{row}", errors);
        double? lapStart = cells.GetDouble(row, 10);
        double beam = cells.GetDouble(row, 11) ?? 0;
        if (errors.Count > before)
        {
            return null;
        }

        return new KataColumnStoreyRow(row, name, height, sizeB, sizeH, alongB, innerB, alongH, innerH, outer, inner, lapStart, beam);
    }

    /// <summary>A bar count as Kata takes it: "7" or "20f30" (20 bars Ø30, the cell's own note).</summary>
    private static (int Count, double? DiameterMm) Count(string? text, string cell, bool required, List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            if (required)
            {
                errors.Add($"{cell}: no bar count (Kata refuses a storey without bars).");
            }

            return (0, null);
        }

        var match = CountAndDiameter.Match(text);
        if (!match.Success)
        {
            errors.Add($"{cell}: '{text}' is not a bar count (\"7\" or \"20f30\").");
            return (0, null);
        }

        int count = int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
        double? diameter = match.Groups[2].Success ? double.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture) : null;
        if (required && count < 2)
        {
            errors.Add($"{cell}: a face needs at least its two corner bars (got {count}).");
        }

        return (count, diameter);
    }

    private static (double Outer, double Inner) Diameters(string? text, double? fromCount, string cell, List<string> errors)
    {
        string? clean = text?.Trim();
        if (string.IsNullOrEmpty(clean))
        {
            if (fromCount is { } d)
            {
                return (d, d);
            }

            errors.Add($"{cell}: no bar diameter.");
            return (0, 0);
        }

        var parts = clean!.Split('/');
        if (!TryNumber(parts[0], out double outer) || (parts.Length > 1 && !TryNumber(parts[1], out _)))
        {
            errors.Add($"{cell}: '{text}' is not a diameter (\"25\" or \"25/20\").");
            return (0, 0);
        }

        double inner = parts.Length > 1 && TryNumber(parts[1], out double second) ? second : outer;
        return (outer, inner);
    }

    private static (double Cover, double Lift) CoverAndLift(string? text, List<string> errors)
    {
        var parts = (text ?? string.Empty).Split('/');
        if (!TryNumber(parts[0], out double cover) || cover <= 0)
        {
            errors.Add($"F4: '{text}' is not a cover (\"30/100\").");
            return (0, 0);
        }

        double lift = parts.Length > 1 && TryNumber(parts[1], out double l) ? l : 100;
        return (cover, lift);
    }

    private static KataColumnHoopSpec? Hoops(string? text, List<string> errors)
    {
        var match = Notation.Match(text ?? string.Empty);
        if (!match.Success)
        {
            errors.Add($"H4: '{text}' is not a hoop notation (\"d10a100/200\").");
            return null;
        }

        double diameter = double.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
        var (dense, middle, beam) = KataBarNotationParser.ParseStirrupSpacing(match.Groups[2].Value);
        return new KataColumnHoopSpec(diameter, dense, middle, beam ?? dense);
    }

    private static KataColumnTieSpec? Ties(string? text, List<string> errors)
    {
        var match = Notation.Match(text ?? string.Empty);
        if (!match.Success)
        {
            errors.Add($"H5: '{text}' is not a tie notation (\"d8a400\").");
            return null;
        }

        var (spacing, _, _) = KataBarNotationParser.ParseStirrupSpacing(match.Groups[2].Value);
        return new KataColumnTieSpec(double.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture), spacing);
    }

    private static bool Flag(IKataDamCellAccessor cells, int row, int column) =>
        string.Equals(cells.GetText(row, column)?.Trim(), "true", StringComparison.OrdinalIgnoreCase)
        || cells.GetDouble(row, column) is { } value && value != 0;

    private static double Required(double? value, string cell, string what, List<string> errors)
    {
        if (value is { } v && v != 0)
        {
            return v;
        }

        errors.Add($"{cell}: no {what}.");
        return 0;
    }

    private static bool TryNumber(string text, out double value) =>
        double.TryParse(text.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out value);
}
