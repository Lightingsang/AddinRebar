using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ClosedXML.Excel;
using Serilog;

namespace HPExcel.McpBridge.Headless;

/// <summary>
///     Headless workbook engine powered by ClosedXML.
///     Enables reading, batch writing, formula evaluation, and table creation on closed .xlsx files
///     without requiring Microsoft Excel or COM registration.
/// </summary>
public sealed class ClosedXmlWorkbookService
{
    /// <summary>
    ///     Reads cell values from a worksheet range in a .xlsx file.
    /// </summary>
    public HeadlessReadResult ReadRange(
        string filePath,
        string? sheetName = null,
        string? rangeAddress = null,
        bool hasHeaders = false,
        int maxRows = 1000)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException($"Excel file not found: {filePath}", filePath);

        using var workbook = new XLWorkbook(filePath);
        var worksheet = string.IsNullOrEmpty(sheetName)
            ? workbook.Worksheets.FirstOrDefault() ?? throw new InvalidOperationException("Workbook contains no worksheets.")
            : workbook.Worksheet(sheetName);

        var range = string.IsNullOrEmpty(rangeAddress)
            ? worksheet.RangeUsed()
            : worksheet.Range(rangeAddress);

        if (range == null)
        {
            return new HeadlessReadResult(worksheet.Name, "$A$1", 0, 0, false, null, Array.Empty<object>());
        }

        var totalRows = range.RowCount();
        var rowCount = Math.Min(totalRows, maxRows);
        var colCount = range.ColumnCount();
        var actualAddress = range.RangeAddress?.ToString() ?? "$A$1";

        var headers = new List<string>();
        var rows = new List<object>();

        if (hasHeaders && rowCount > 1)
        {
            for (var c = 1; c <= colCount; c++)
            {
                var headerVal = range.Cell(1, c).GetString();
                headers.Add(string.IsNullOrWhiteSpace(headerVal) ? $"Col_{c}" : headerVal);
            }

            for (var r = 2; r <= rowCount; r++)
            {
                var rowDict = new Dictionary<string, object?>();
                for (var c = 1; c <= colCount; c++)
                {
                    rowDict[headers[c - 1]] = GetCellValue(range.Cell(r, c));
                }
                rows.Add(rowDict);
            }
        }
        else
        {
            for (var r = 1; r <= rowCount; r++)
            {
                var rowList = new List<object?>();
                for (var c = 1; c <= colCount; c++)
                {
                    rowList.Add(GetCellValue(range.Cell(r, c)));
                }
                rows.Add(rowList);
            }
        }

        return new HeadlessReadResult(
            worksheet.Name,
            actualAddress,
            rowCount,
            colCount,
            hasHeaders,
            hasHeaders ? headers : null,
            rows);
    }

    /// <summary>
    ///     Batch writes 2D values into a .xlsx file starting at targetCell.
    ///     Creates the file if it does not exist.
    /// </summary>
    public HeadlessWriteResult WriteRange(
        string filePath,
        string? sheetName,
        string startCell,
        object[,] values,
        bool isFormula = false,
        bool autoFit = false)
    {
        var rowCount = values.GetLength(0);
        var colCount = values.GetLength(1);
        if (rowCount == 0 || colCount == 0)
            throw new ArgumentException("Values array cannot be empty.");

        XLWorkbook workbook;
        var isNewFile = !File.Exists(filePath);
        if (isNewFile)
        {
            var dir = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
            workbook = new XLWorkbook();
        }
        else
        {
            workbook = new XLWorkbook(filePath);
        }

        using (workbook)
        {
            var targetSheetName = string.IsNullOrEmpty(sheetName) ? "Sheet1" : sheetName;
            var worksheet = workbook.Worksheets.FirstOrDefault(w => w.Name.Equals(targetSheetName, StringComparison.OrdinalIgnoreCase))
                            ?? workbook.Worksheets.Add(targetSheetName);

            var origin = worksheet.Cell(startCell);
            var startRow = origin.Address.RowNumber;
            var startCol = origin.Address.ColumnNumber;

            for (var r = 0; r < rowCount; r++)
            {
                for (var c = 0; c < colCount; c++)
                {
                    var cell = worksheet.Cell(startRow + r, startCol + c);
                    var val = values[r, c];

                    if (val == null)
                    {
                        cell.Clear();
                    }
                    else if (isFormula && val is string strVal && strVal.StartsWith("=", StringComparison.Ordinal))
                    {
                        cell.FormulaA1 = strVal[1..];
                    }
                    else
                    {
                        SetCellValue(cell, val);
                    }
                }
            }

            if (autoFit)
            {
                worksheet.Columns().AdjustToContents();
            }

            var endCell = worksheet.Cell(startRow + rowCount - 1, startCol + colCount - 1);
            var writtenRange = $"{startCell}:{endCell.Address.ToStringRelative(false)}";

            workbook.SaveAs(filePath);
            Log.Information("ClosedXML wrote {Rows}x{Cols} to '{File}': {Sheet}!{Range}",
                rowCount, colCount, filePath, worksheet.Name, writtenRange);

            return new HeadlessWriteResult(worksheet.Name, writtenRange, rowCount, colCount, true);
        }
    }

    /// <summary>
    ///     Inspects worksheet metadata and dimensions in a .xlsx file.
    /// </summary>
    public IReadOnlyList<HeadlessSheetInfo> GetWorksheetInfo(string filePath, string? sheetName = null)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException($"Excel file not found: {filePath}", filePath);

        using var workbook = new XLWorkbook(filePath);
        var sheets = new List<HeadlessSheetInfo>();

        var targetWorksheets = string.IsNullOrEmpty(sheetName)
            ? (IEnumerable<IXLWorksheet>)workbook.Worksheets
            : workbook.Worksheets.Where(w => w.Name.Equals(sheetName, StringComparison.OrdinalIgnoreCase));

        foreach (var ws in targetWorksheets)
        {
            var used = ws.RangeUsed();
            var usedAddress = used?.RangeAddress.ToString() ?? "$A$1";
            var rowCount = used?.RowCount() ?? 0;
            var colCount = used?.ColumnCount() ?? 0;
            var tables = ws.Tables.Select(t => t.Name).ToList();

            var visibility = ws.Visibility switch
            {
                XLWorksheetVisibility.Visible => "Visible",
                XLWorksheetVisibility.Hidden => "Hidden",
                XLWorksheetVisibility.VeryHidden => "VeryHidden",
                _ => "Visible"
            };

            sheets.Add(new HeadlessSheetInfo(
                ws.Name,
                ws.Position,
                visibility,
                usedAddress,
                rowCount,
                colCount,
                tables,
                ws.IsProtected));
        }

        return sheets;
    }

    /// <summary>
    ///     Converts a range into an Excel table (ListObject) in a .xlsx file.
    /// </summary>
    public HeadlessTableResult CreateTable(
        string filePath,
        string? sheetName,
        string rangeAddress,
        string tableName,
        string? styleName = null,
        bool showTotals = false)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException($"Excel file not found: {filePath}", filePath);

        using var workbook = new XLWorkbook(filePath);
        var ws = string.IsNullOrEmpty(sheetName)
            ? workbook.Worksheets.FirstOrDefault() ?? throw new InvalidOperationException("No worksheet found.")
            : workbook.Worksheet(sheetName);

        var range = ws.Range(rangeAddress);
        var table = range.CreateTable(tableName);

        if (!string.IsNullOrEmpty(styleName))
        {
            var themeProp = typeof(XLTableTheme).GetProperty(
                styleName,
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.IgnoreCase);
            if (themeProp != null && themeProp.GetValue(null) is XLTableTheme theme)
            {
                table.Theme = theme;
            }
        }

        table.ShowTotalsRow = showTotals;
        workbook.Save();

        return new HeadlessTableResult(
            table.Name,
            ws.Name,
            table.RangeAddress?.ToString() ?? rangeAddress,
            table.Fields.Count(),
            table.DataRange?.RowCount() ?? 0,
            showTotals);
    }

    /// <summary>
    ///     Evaluates an Excel formula dynamically using ClosedXML's calculation engine.
    /// </summary>
    public object? EvaluateFormula(string filePath, string? sheetName, string formula, string? contextCell = null)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException($"Excel file not found: {filePath}", filePath);

        using var workbook = new XLWorkbook(filePath);
        var ws = string.IsNullOrEmpty(sheetName)
            ? workbook.Worksheets.FirstOrDefault() ?? throw new InvalidOperationException("No worksheet found.")
            : workbook.Worksheet(sheetName);

        var cleanFormula = formula.TrimStart('=');
        if (!string.IsNullOrEmpty(contextCell))
        {
            var cell = ws.Cell(contextCell);
            cell.FormulaA1 = cleanFormula;
            return ConvertCellValue(cell.Value);
        }

        var evalResult = ws.Evaluate(cleanFormula);
        return ConvertCellValue(evalResult);
    }

    private static object? ConvertCellValue(XLCellValue val)
    {
        return val.Type switch
        {
            XLDataType.Blank => null,
            XLDataType.Boolean => val.GetBoolean(),
            XLDataType.Number => val.GetNumber(),
            XLDataType.Text => val.GetText(),
            XLDataType.DateTime => val.GetDateTime().ToString("yyyy-MM-dd HH:mm:ss"),
            XLDataType.TimeSpan => val.GetTimeSpan().ToString(),
            XLDataType.Error => val.GetError().ToString(),
            _ => val.ToString()
        };
    }

    private static object? GetCellValue(IXLCell cell)
    {
        return cell.DataType switch
        {
            XLDataType.Boolean => cell.GetBoolean(),
            XLDataType.Number => cell.GetDouble(),
            XLDataType.DateTime => cell.GetDateTime().ToString("yyyy-MM-dd HH:mm:ss"),
            XLDataType.TimeSpan => cell.GetTimeSpan().ToString(),
            XLDataType.Text => cell.GetString(),
            XLDataType.Blank => null,
            _ => cell.GetString()
        };
    }

    private static void SetCellValue(IXLCell cell, object val)
    {
        switch (val)
        {
            case bool b: cell.SetValue(b); break;
            case int i: cell.SetValue(i); break;
            case long l: cell.SetValue(l); break;
            case double d: cell.SetValue(d); break;
            case decimal m: cell.SetValue(m); break;
            case DateTime dt: cell.SetValue(dt); break;
            case string s: cell.SetValue(s); break;
            default: cell.SetValue(val.ToString()); break;
        }
    }
}

public sealed record HeadlessReadResult(
    string Sheet,
    string Address,
    int RowCount,
    int ColCount,
    bool HasHeaders,
    IReadOnlyList<string>? Headers,
    IReadOnlyList<object> Data);

public sealed record HeadlessWriteResult(
    string Sheet,
    string TargetRange,
    int RowsWritten,
    int ColsWritten,
    bool Success);

public sealed record HeadlessSheetInfo(
    string Name,
    int Position,
    string Visibility,
    string UsedRange,
    int RowCount,
    int ColCount,
    IReadOnlyList<string> Tables,
    bool IsProtected);

public sealed record HeadlessTableResult(
    string TableName,
    string Sheet,
    string Range,
    int ColumnsCount,
    int RowsCount,
    bool HasTotalsRow);
