namespace HPRebar.Core.KataRebar.Parsers;

/// <summary>
/// Abstraction interface for accessing cell values from sheet 'Dam' of Kata.xlsm.
/// Decouples the sheet parsing logic in HPRebar.Core from Excel COM and file readers.
/// Row and column coordinates are 1-based (Excel standard).
/// </summary>
public interface IKataDamCellAccessor
{
    /// <summary>Gets the raw text value of a cell at (row, col) (1-based).</summary>
    string? GetText(int row, int col);

    /// <summary>Gets the numeric double value of a cell at (row, col) (1-based).</summary>
    double? GetDouble(int row, int col);

    /// <summary>Gets the integer value of a cell at (row, col) (1-based).</summary>
    int? GetInt(int row, int col);
}

/// <summary>
/// Extension methods for <see cref="IKataDamCellAccessor"/> supporting A1-style cell addresses.
/// </summary>
public static class KataDamCellAccessorExtensions
{
    /// <summary>Gets the raw text value of a cell by A1 address (e.g. "B3", "G6").</summary>
    public static string? GetText(this IKataDamCellAccessor accessor, string cellAddress)
    {
        if (TryParseAddress(cellAddress, out int row, out int col))
            return accessor.GetText(row, col);
        return null;
    }

    /// <summary>Gets the numeric double value of a cell by A1 address (e.g. "B5", "G2").</summary>
    public static double? GetDouble(this IKataDamCellAccessor accessor, string cellAddress)
    {
        if (TryParseAddress(cellAddress, out int row, out int col))
            return accessor.GetDouble(row, col);
        return null;
    }

    /// <summary>Gets the integer value of a cell by A1 address (e.g. "B4", "I8").</summary>
    public static int? GetInt(this IKataDamCellAccessor accessor, string cellAddress)
    {
        if (TryParseAddress(cellAddress, out int row, out int col))
            return accessor.GetInt(row, col);
        return null;
    }

    /// <summary>Parses an A1-style cell reference (e.g. "C11", "BZ23") into 1-based (row, col).</summary>
    public static bool TryParseAddress(string cellAddress, out int row, out int col)
    {
        row = 0;
        col = 0;
        if (string.IsNullOrWhiteSpace(cellAddress))
            return false;

        string trimmed = cellAddress.Trim().Replace("$", "");
        int splitIndex = 0;
        while (splitIndex < trimmed.Length && char.IsLetter(trimmed[splitIndex]))
            splitIndex++;

        if (splitIndex == 0 || splitIndex == trimmed.Length)
            return false;

        string colPart = trimmed.Substring(0, splitIndex).ToUpperInvariant();
        string rowPart = trimmed.Substring(splitIndex);

        if (!int.TryParse(rowPart, out row) || row < 1)
            return false;

        int c = 0;
        foreach (char ch in colPart)
        {
            if (ch < 'A' || ch > 'Z') return false;
            c = c * 26 + (ch - 'A' + 1);
        }

        col = c;
        return true;
    }
}
