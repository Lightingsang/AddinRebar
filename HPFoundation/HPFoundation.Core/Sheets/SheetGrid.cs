namespace HPFoundation.Core.Sheets;

/// <summary>A snapshot of one worksheet, free of Excel, so the sheet rules can run in tests.</summary>
public sealed class SheetGrid(string sheetName, IReadOnlyDictionary<CellAddress, SheetCell> cells)
{
    public string SheetName { get; } = sheetName;

    public IReadOnlyDictionary<CellAddress, SheetCell> Cells { get; } = cells;

    public SheetCell? CellAt(CellAddress address) => Cells.GetValueOrDefault(address);

    public string TextAt(CellAddress address) => CellAt(address)?.Text?.Trim() ?? string.Empty;

    /// <summary>Cells in reading order, so the first match is the topmost one.</summary>
    public IEnumerable<CellAddress> FindAddresses(Func<SheetCell, bool> isMatch) =>
        Cells
            .Where(entry => isMatch(entry.Value))
            .Select(entry => entry.Key)
            .OrderBy(address => address.Row)
            .ThenBy(address => address.Column);
}
