using System;
using System.Globalization;
using HPKata.Core.Excel.Abstractions;

namespace HPKata.Core.Excel.Session
{
    /// <summary>
    /// Live Excel session communicating with desktop Excel application via late-binding COM reflection.
    /// </summary>
    public sealed class KataComExcelSession : IKataExcelSession
    {
        private readonly object _excelApp;
        private readonly object _workbook;
        private readonly bool _ownsApp;
        private bool _isDisposed;

        public bool IsComSession => true;
        public string WorkbookPath { get; }

        public KataComExcelSession(object excelApp, object workbook, string workbookPath, bool ownsApp = false)
        {
            _excelApp = excelApp ?? throw new ArgumentNullException(nameof(excelApp));
            _workbook = workbook ?? throw new ArgumentNullException(nameof(workbook));
            WorkbookPath = workbookPath ?? throw new ArgumentNullException(nameof(workbookPath));
            _ownsApp = ownsApp;
        }

        public bool SheetExists(string sheetName)
        {
            try
            {
                object? sheets = ComLateBindingHelper.GetProperty(_workbook, "Worksheets");
                if (sheets == null) return false;
                object? sheet = ComLateBindingHelper.GetProperty(sheets, "Item", sheetName);
                bool exists = sheet != null;
                ComLateBindingHelper.Release(sheet);
                ComLateBindingHelper.Release(sheets);
                return exists;
            }
            catch
            {
                // Sheet Item indexer throws when sheet not found
                return false;
            }
        }

        public void EnsureSheet(string sheetName)
        {
            if (SheetExists(sheetName)) return;

            object? sheets = ComLateBindingHelper.GetProperty(_workbook, "Worksheets");
            if (sheets != null)
            {
                object? newSheet = ComLateBindingHelper.InvokeMethod(sheets, "Add");
                if (newSheet != null)
                {
                    ComLateBindingHelper.SetProperty(newSheet, "Name", sheetName);
                    ComLateBindingHelper.Release(newSheet);
                }
                ComLateBindingHelper.Release(sheets);
            }
        }

        public string? GetCellValue(string sheetName, int row, int col)
        {
            object? cell = GetCellCom(sheetName, row, col);
            if (cell == null) return null;

            try
            {
                object? val = ComLateBindingHelper.GetProperty(cell, "Value2");
                return val?.ToString();
            }
            finally
            {
                ComLateBindingHelper.Release(cell);
            }
        }

        public double GetCellDouble(string sheetName, int row, int col, double defaultValue = 0.0)
        {
            string? str = GetCellValue(sheetName, row, col);
            if (string.IsNullOrWhiteSpace(str)) return defaultValue;

            if (double.TryParse(str, NumberStyles.Any, CultureInfo.InvariantCulture, out double d))
                return d;
            if (double.TryParse(str, NumberStyles.Any, CultureInfo.CurrentCulture, out d))
                return d;

            return defaultValue;
        }

        public int GetCellInt(string sheetName, int row, int col, int defaultValue = 0)
        {
            double d = GetCellDouble(sheetName, row, col, defaultValue);
            return (int)Math.Round(d);
        }

        public void SetCellValue(string sheetName, int row, int col, object? value)
        {
            EnsureSheet(sheetName);
            object? cell = GetCellCom(sheetName, row, col);
            if (cell == null) return;

            try
            {
                ComLateBindingHelper.SetProperty(cell, "Value2", value ?? string.Empty);
            }
            finally
            {
                ComLateBindingHelper.Release(cell);
            }
        }

        public void ClearRange(string sheetName, int startRow, int startCol, int endRow, int endCol)
        {
            object? sheet = GetSheetCom(sheetName);
            if (sheet == null) return;

            try
            {
                object? cell1 = ComLateBindingHelper.GetProperty(sheet, "Cells", startRow, startCol);
                object? cell2 = ComLateBindingHelper.GetProperty(sheet, "Cells", endRow, endCol);
                if (cell1 != null && cell2 != null)
                {
                    object? range = ComLateBindingHelper.GetProperty(sheet, "Range", cell1, cell2);
                    if (range != null)
                    {
                        ComLateBindingHelper.InvokeMethod(range, "ClearContents");
                        ComLateBindingHelper.Release(range);
                    }
                }
                ComLateBindingHelper.Release(cell1);
                ComLateBindingHelper.Release(cell2);
            }
            finally
            {
                ComLateBindingHelper.Release(sheet);
            }
        }

        public void Save()
        {
            ComLateBindingHelper.InvokeMethod(_workbook, "Save");
        }

        public void Dispose()
        {
            if (!_isDisposed)
            {
                ComLateBindingHelper.Release(_workbook);
                if (_ownsApp)
                {
                    try { ComLateBindingHelper.InvokeMethod(_excelApp, "Quit"); } catch (Exception) { /* Best-effort quit on dispose: ignore errors if Excel is busy or closed */ }
                    ComLateBindingHelper.Release(_excelApp);
                }
                _isDisposed = true;
            }
        }

        private object? GetSheetCom(string sheetName)
        {
            try
            {
                object? sheets = ComLateBindingHelper.GetProperty(_workbook, "Worksheets");
                if (sheets == null) return null;
                object? sheet = ComLateBindingHelper.GetProperty(sheets, "Item", sheetName);
                ComLateBindingHelper.Release(sheets);
                return sheet;
            }
            catch
            {
                // Sheet may not exist
                return null;
            }
        }

        private object? GetCellCom(string sheetName, int row, int col)
        {
            object? sheet = GetSheetCom(sheetName);
            if (sheet == null) return null;

            try
            {
                return ComLateBindingHelper.GetProperty(sheet, "Cells", row, col);
            }
            finally
            {
                ComLateBindingHelper.Release(sheet);
            }
        }
    }
}
