using System;
using System.IO;
using System.Linq;
using ClosedXML.Excel;
using HPRebar.Core.KataRebar.Parsers;

namespace HPRebar.KataRebar.Excel;

/// <summary>
/// Offline file reader for sheet 'Dam' of Kata .xlsm / .xlsx files using ClosedXML.
/// Operates without requiring Microsoft Excel or Windows ROT registration.
/// Uses FileShare.ReadWrite so files currently open in Excel can still be inspected.
/// </summary>
public static class ClosedXmlKataDamReader
{
    private const string TargetSheetName = "Dam";
    private const int MaxRow = 30;
    private const int MaxCol = 78; // BZ

    /// <summary>
    /// Attempts to open a Kata spreadsheet file from disk and parse sheet 'Dam' into a <see cref="KataCellTable"/>.
    /// </summary>
    /// <param name="filePath">Absolute or relative path to the .xlsm or .xlsx file.</param>
    /// <param name="table">Populated <see cref="KataCellTable"/> if successful, otherwise null.</param>
    /// <param name="errorMessage">Detailed explanation if the operation failed.</param>
    /// <returns>True if successfully read; otherwise false.</returns>
    public static bool TryReadFromFile(string filePath, out KataCellTable? table, out string errorMessage)
    {
        table = null;
        if (string.IsNullOrWhiteSpace(filePath))
        {
            errorMessage = "Đường dẫn file không được để trống.";
            return false;
        }

        if (!File.Exists(filePath))
        {
            errorMessage = $"Không tìm thấy file tại đường dẫn: '{filePath}'.";
            return false;
        }

        try
        {
            // Open with FileShare.ReadWrite to permit reading even if user has the file open in Excel
            using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var workbook = new XLWorkbook(stream);

            var ws = workbook.Worksheets.FirstOrDefault(w => w.Name.Equals(TargetSheetName, StringComparison.OrdinalIgnoreCase));
            if (ws is null)
            {
                errorMessage = $"File '{Path.GetFileName(filePath)}' không chứa sheet '{TargetSheetName}'.";
                return false;
            }

            var cellTable = new KataCellTable();
            for (int r = 1; r <= MaxRow; r++)
            {
                for (int c = 1; c <= MaxCol; c++)
                {
                    var cell = ws.Cell(r, c);
                    var val = GetCellValue(cell);
                    if (val is not null)
                    {
                        cellTable.Set(r, c, val);
                    }
                }
            }

            table = cellTable;
            errorMessage = string.Empty;
            return true;
        }
        catch (Exception ex)
        {
            errorMessage = $"Lỗi khi đọc file '{Path.GetFileName(filePath)}' qua ClosedXML: {ex.Message}";
            return false;
        }
    }

    /// <summary>
    /// Reads sheet 'Dam' from the specified file on disk, throwing if unsuccessful.
    /// </summary>
    public static KataCellTable ReadFromFile(string filePath)
    {
        if (!TryReadFromFile(filePath, out var table, out var error))
        {
            throw new InvalidOperationException(error);
        }
        return table!;
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
}
