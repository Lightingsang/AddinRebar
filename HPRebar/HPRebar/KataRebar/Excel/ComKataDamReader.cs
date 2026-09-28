using System;
using HPRebar.Core.KataRebar.Parsers;
using HPRebar.KataExport.Service;

namespace HPRebar.KataRebar.Excel;

/// <summary>
/// Reads sheet 'Dam' directly from an active Microsoft Excel instance using
/// Windows ROT late binding (ole32/oleaut32). Performs a single batch COM call
/// across Range["A1:BZ30"] for sub-5ms extraction without Office Interop PIA.
/// </summary>
public static class ComKataDamReader
{
    private const string TargetSheetName = "Dam";
    private const string TargetRangeAddress = "A1:BZ30";

    /// <summary>
    /// Attempts to read the 'Dam' sheet from the currently active workbook in Microsoft Excel.
    /// </summary>
    /// <param name="table">Populated <see cref="KataCellTable"/> if successful, otherwise null.</param>
    /// <param name="errorMessage">Detailed reason if the operation failed.</param>
    /// <returns>True if successfully read; otherwise false.</returns>
    public static bool TryReadActiveSheet(out KataCellTable? table, out string errorMessage)
    {
        table = null;
        if (!ExcelComAttach.TryGetRunningExcel(out var app, out var attachReason))
        {
            errorMessage = attachReason;
            return false;
        }

        try
        {
            var wb = ComLateBinding.Get(app!, "ActiveWorkbook");
            if (wb is null)
            {
                errorMessage = "Không tìm thấy ActiveWorkbook nào trong tiến trình Microsoft Excel đang chạy.";
                return false;
            }

            var sheets = ComLateBinding.Get(wb, "Worksheets");
            if (sheets is null)
            {
                errorMessage = "Không thể truy cập danh sách Worksheets từ workbook đang mở.";
                return false;
            }

            object? ws = null;
            try
            {
                ws = ComLateBinding.Get(sheets, "Item", TargetSheetName);
            }
            catch
            {
                // Sheet "Dam" not found in current workbook
            }

            if (ws is null)
            {
                errorMessage = $"Workbook đang mở không chứa sheet '{TargetSheetName}'. Vui lòng mở đúng file Kata.";
                return false;
            }

            var range = ComLateBinding.Get(ws, "Range", TargetRangeAddress);
            if (range is null)
            {
                errorMessage = $"Không thể truy cập vùng Range['{TargetRangeAddress}'] của sheet '{TargetSheetName}'.";
                return false;
            }

            var raw2D = ComLateBinding.Get(range, "Value2") as object[,];
            if (raw2D is null)
            {
                errorMessage = $"Không đọc được mảng dữ liệu Value2 từ Range['{TargetRangeAddress}'].";
                return false;
            }

            table = new KataCellTable(raw2D, startRow: 1, startCol: 1);
            errorMessage = string.Empty;
            return true;
        }
        catch (Exception ex)
        {
            errorMessage = $"Lỗi khi đọc sheet '{TargetSheetName}' từ Excel qua COM: {ex.Message}";
            return false;
        }
        finally
        {
            ComLateBinding.Release(app);
        }
    }

    /// <summary>
    /// Reads the 'Dam' sheet from the active workbook in Microsoft Excel, throwing if unsuccessful.
    /// </summary>
    public static KataCellTable ReadActiveSheet()
    {
        if (!TryReadActiveSheet(out var table, out var error))
        {
            throw new InvalidOperationException(error);
        }
        return table!;
    }
}
