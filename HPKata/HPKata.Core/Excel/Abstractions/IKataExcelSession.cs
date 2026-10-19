using System;

namespace HPKata.Core.Excel.Abstractions
{
    /// <summary>
    /// Represents an active Excel workbook session (either live via COM or headless via ClosedXML).
    /// </summary>
    public interface IKataExcelSession : IDisposable
    {
        bool IsComSession { get; }
        string WorkbookPath { get; }

        bool SheetExists(string sheetName);
        void EnsureSheet(string sheetName);

        string? GetCellValue(string sheetName, int row, int col);
        double GetCellDouble(string sheetName, int row, int col, double defaultValue = 0.0);
        int GetCellInt(string sheetName, int row, int col, int defaultValue = 0);

        void SetCellValue(string sheetName, int row, int col, object? value);
        void ClearRange(string sheetName, int startRow, int startCol, int endRow, int endCol);
        void Save();
    }
}
