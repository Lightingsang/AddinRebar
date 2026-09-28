using System;
using System.Collections.Generic;
using System.Globalization;

namespace HPRebar.Core.KataRebar.Parsers;

/// <summary>
/// In-memory 2D table implementing <see cref="IKataDamCellAccessor"/>.
/// Supports initialization from Excel COM 2D array, ClosedXML dictionary, or programmatic tests.
/// </summary>
public sealed class KataCellTable : IKataDamCellAccessor
{
    private readonly Dictionary<(int Row, int Col), object?> _cells = new();

    public KataCellTable()
    {
    }

    /// <summary>
    /// Initializes table from a 2D object array (such as returned by Excel COM Range.Value2).
    /// Handles both 1-based bounds (typical of COM) and 0-based bounds.
    /// </summary>
    public KataCellTable(object[,] raw2D, int startRow = 1, int startCol = 1)
    {
        if (raw2D is null) return;

        int rowLower = raw2D.GetLowerBound(0);
        int rowUpper = raw2D.GetUpperBound(0);
        int colLower = raw2D.GetLowerBound(1);
        int colUpper = raw2D.GetUpperBound(1);

        for (int r = rowLower; r <= rowUpper; r++)
        {
            int targetRow = (rowLower == 1) ? r : (r - rowLower + startRow);
            for (int c = colLower; c <= colUpper; c++)
            {
                int targetCol = (colLower == 1) ? c : (c - colLower + startCol);
                var val = raw2D[r, c];
                if (val is not null)
                {
                    _cells[(targetRow, targetCol)] = val;
                }
            }
        }
    }

    /// <summary>
    /// Initializes table from an address-to-value map (e.g. "B3" -> "B01").
    /// </summary>
    public KataCellTable(IDictionary<string, object?> cells)
    {
        if (cells is null) return;

        foreach (var kvp in cells)
        {
            if (KataDamCellAccessorExtensions.TryParseAddress(kvp.Key, out int row, out int col))
            {
                _cells[(row, col)] = kvp.Value;
            }
        }
    }

    /// <summary>Sets a cell value at 1-based (row, col).</summary>
    public void Set(int row, int col, object? value)
    {
        _cells[(row, col)] = value;
    }

    /// <summary>Sets a cell value by A1 address (e.g. "B3").</summary>
    public void Set(string address, object? value)
    {
        if (KataDamCellAccessorExtensions.TryParseAddress(address, out int row, out int col))
        {
            _cells[(row, col)] = value;
        }
    }

    /// <summary>Gets the raw object value at (row, col).</summary>
    public object? Get(int row, int col)
    {
        _cells.TryGetValue((row, col), out var value);
        return value;
    }

    /// <inheritdoc />
    public string? GetText(int row, int col)
    {
        if (!_cells.TryGetValue((row, col), out var val) || val is null)
            return null;

        if (val is string str)
        {
            string trimmed = str.Trim();
            return string.IsNullOrEmpty(trimmed) ? null : trimmed;
        }

        if (val is double d)
        {
            // If whole number, format without decimals (e.g. 10400 instead of 10400.0)
            if (Math.Abs(d - Math.Round(d)) < 1e-9)
                return Math.Round(d).ToString("0", CultureInfo.InvariantCulture);
            return d.ToString(CultureInfo.InvariantCulture);
        }

        if (val is float f)
        {
            if (Math.Abs(f - Math.Round(f)) < 1e-6)
                return Math.Round(f).ToString("0", CultureInfo.InvariantCulture);
            return f.ToString(CultureInfo.InvariantCulture);
        }

        return Convert.ToString(val, CultureInfo.InvariantCulture)?.Trim();
    }

    /// <inheritdoc />
    public double? GetDouble(int row, int col)
    {
        if (!_cells.TryGetValue((row, col), out var val) || val is null)
            return null;

        if (val is double d) return d;
        if (val is float f) return f;
        if (val is int i) return i;
        if (val is long l) return l;
        if (val is decimal m) return (double)m;

        if (val is string s)
        {
            string clean = s.Trim().Replace("'", "");
            if (double.TryParse(clean, NumberStyles.Any, CultureInfo.InvariantCulture, out double parsed))
                return parsed;
        }

        return null;
    }

    /// <inheritdoc />
    public int? GetInt(int row, int col)
    {
        if (!_cells.TryGetValue((row, col), out var val) || val is null)
            return null;

        if (val is int i) return i;
        if (val is double d) return (int)Math.Round(d);
        if (val is float f) return (int)Math.Round(f);
        if (val is long l) return (int)l;
        if (val is decimal m) return (int)Math.Round(m);

        if (val is string s)
        {
            string clean = s.Trim().Replace("'", "");
            if (int.TryParse(clean, NumberStyles.Any, CultureInfo.InvariantCulture, out int parsed))
                return parsed;
            if (double.TryParse(clean, NumberStyles.Any, CultureInfo.InvariantCulture, out double parsedDouble))
                return (int)Math.Round(parsedDouble);
        }

        return null;
    }
}
