using System;
using System.Globalization;
using System.IO;
using ClosedXML.Excel;
using HPKata.Core.Excel.Abstractions;

namespace HPKata.Core.Excel.Session
{
    /// <summary>
    /// Headless Excel session implemented via ClosedXML for hermetic file I/O.
    /// </summary>
    public sealed class KataClosedXmlSession : IKataExcelSession
    {
        private readonly XLWorkbook _workbook;
        private bool _isDisposed;

        public bool IsComSession => false;
        public string WorkbookPath { get; }

        public KataClosedXmlSession(string workbookPath)
        {
            if (string.IsNullOrWhiteSpace(workbookPath))
                throw new ArgumentException("Workbook path cannot be empty.", nameof(workbookPath));

            WorkbookPath = workbookPath;
            if (File.Exists(workbookPath))
            {
                using var fs = new FileStream(workbookPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                var ms = new MemoryStream();
                fs.CopyTo(ms);
                ms.Position = 0;
                _workbook = new XLWorkbook(ms);
            }
            else
            {
                _workbook = new XLWorkbook();
            }
        }

        public bool SheetExists(string sheetName) =>
            _workbook.Worksheets.Contains(sheetName);

        public void EnsureSheet(string sheetName)
        {
            if (!SheetExists(sheetName))
            {
                _workbook.Worksheets.Add(sheetName);
            }
        }

        public string? GetCellValue(string sheetName, int row, int col)
        {
            if (!_workbook.Worksheets.TryGetWorksheet(sheetName, out var ws))
                return null;

            var cell = ws.Cell(row, col);
            if (cell.IsEmpty()) return null;
            return cell.GetString();
        }

        public double GetCellDouble(string sheetName, int row, int col, double defaultValue = 0.0)
        {
            if (!_workbook.Worksheets.TryGetWorksheet(sheetName, out var ws))
                return defaultValue;

            var cell = ws.Cell(row, col);
            if (cell.IsEmpty()) return defaultValue;

            if (cell.TryGetValue(out double doubleVal))
                return doubleVal;

            string text = cell.GetString().Trim();
            if (double.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out double parsedVal))
                return parsedVal;

            if (double.TryParse(text, NumberStyles.Any, CultureInfo.CurrentCulture, out parsedVal))
                return parsedVal;

            return defaultValue;
        }

        public int GetCellInt(string sheetName, int row, int col, int defaultValue = 0)
        {
            double val = GetCellDouble(sheetName, row, col, defaultValue);
            return (int)Math.Round(val);
        }

        public void SetCellValue(string sheetName, int row, int col, object? value)
        {
            EnsureSheet(sheetName);
            var ws = _workbook.Worksheet(sheetName);
            var cell = ws.Cell(row, col);

            if (value == null)
            {
                cell.Clear();
                return;
            }

            switch (value)
            {
                case int i:
                    cell.Value = i;
                    break;
                case double d:
                    cell.Value = d;
                    break;
                case string s:
                    cell.Value = s;
                    break;
                case bool b:
                    cell.Value = b;
                    break;
                default:
                    cell.Value = value.ToString();
                    break;
            }
        }

        public void ClearRange(string sheetName, int startRow, int startCol, int endRow, int endCol)
        {
            if (!_workbook.Worksheets.TryGetWorksheet(sheetName, out var ws))
                return;

            ws.Range(startRow, startCol, endRow, endCol).Clear();
        }

        public void Save()
        {
            string? dir = Path.GetDirectoryName(WorkbookPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            SanitizeAutoFilters();
            _workbook.SaveAs(WorkbookPath);
        }

        private void SanitizeAutoFilters()
        {
            foreach (var ws in _workbook.Worksheets)
            {
                try
                {
                    if (ws.AutoFilter.IsEnabled)
                    {
                        ws.AutoFilter.Clear();
                    }
                }
                catch
                {
                    // Suppress if reading autofilter fails
                }
            }
        }

        public void Dispose()
        {
            if (!_isDisposed)
            {
                _workbook.Dispose();
                _isDisposed = true;
            }
        }
    }
}
