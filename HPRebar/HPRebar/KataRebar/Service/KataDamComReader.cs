using System;
using System.Runtime.InteropServices;
using HPRebar.Core.KataRebar.Parsers;
using HPRebar.KataExport.Service;

namespace HPRebar.KataRebar.Service;

/// <summary>What was read from Excel: the cells of sheet 'Dam' and the workbook they came from.</summary>
public sealed record KataDamReadResult(KataCellTable? Cells, string WorkbookName, string Error)
{
    public bool IsSuccess => Cells is not null;
}

/// <summary>
/// Reads sheet 'Dam' of the workbook active in the running Excel, unsaved edits included, in one COM call
/// over A1:BZ44 (header, rows 10-23 and the stirrup sections of rows 24-44). Every COM object it touches is
/// released; nothing is written and no macro runs.
/// </summary>
public static class KataDamComReader
{
    private const string SheetName = "Dam";
    private const string RangeAddress = "A1:BZ44";

    private const uint RpcCallRejected = 0x80010001;
    private const uint RpcRetryLater = 0x8001010A;
    private const uint ExcelBusy = 0x800AC472;

    public static KataDamReadResult Read()
    {
        object? app = null, workbook = null, sheets = null, sheet = null, range = null;
        try
        {
            if (!ExcelComAttach.TryGetRunningExcel(out app, out var reason))
                return new KataDamReadResult(null, "", reason);

            workbook = ComLateBinding.Get(app!, "ActiveWorkbook");
            if (workbook is null)
                return new KataDamReadResult(null, "", "Excel đang mở nhưng không có workbook nào được kích hoạt.");

            string name = ComLateBinding.Get(workbook, "Name")?.ToString() ?? "";
            sheets = ComLateBinding.Get(workbook, "Worksheets");
            try
            {
                sheet = ComLateBinding.Get(sheets!, "Item", SheetName);
            }
            catch (COMException ex) when (!IsBusy(ex))
            {
                sheet = null;
            }

            if (sheet is null)
                return new KataDamReadResult(null, name, $"Workbook '{name}' không có sheet '{SheetName}' — hãy kích hoạt workbook Kata.");

            range = ComLateBinding.Get(sheet, "Range", RangeAddress);
            if (range is null || ComLateBinding.Get(range, "Value2") is not object[,] values)
                return new KataDamReadResult(null, name, $"Không đọc được vùng {RangeAddress} của sheet '{SheetName}'.");

            return new KataDamReadResult(new KataCellTable(WithoutErrorCells(values), startRow: 1, startCol: 1), name, "");
        }
        catch (COMException ex) when (IsBusy(ex))
        {
            return new KataDamReadResult(null, "", "Excel đang bận (đang sửa ô hoặc có hộp thoại mở?). Bấm Enter/Esc trong Excel rồi đọc lại.");
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            // Late-bound COM can fail in many ways (RPC, bad index, a dialog open in Excel); the window
            // shows the reason instead of failing to open.
            return new KataDamReadResult(null, "", $"Lỗi đọc Excel: {ex.Message}");
        }
        finally
        {
            ComLateBinding.Release(range);
            ComLateBinding.Release(sheet);
            ComLateBinding.Release(sheets);
            ComLateBinding.Release(workbook);
            ComLateBinding.Release(app);
        }
    }

    /// <summary>
    /// Value2 returns numbers as double, text as string and an error cell (#N/A, #DIV/0!...) as an Int32
    /// error code; an error code is not a value, so it is read as an empty cell.
    /// </summary>
    private static object[,] WithoutErrorCells(object[,] values)
    {
        for (int r = values.GetLowerBound(0); r <= values.GetUpperBound(0); r++)
        for (int c = values.GetLowerBound(1); c <= values.GetUpperBound(1); c++)
        {
            if (values[r, c] is int) values[r, c] = null!;
        }

        return values;
    }

    private static bool IsBusy(COMException ex) => (uint)ex.ErrorCode is RpcCallRejected or RpcRetryLater or ExcelBusy;
}
