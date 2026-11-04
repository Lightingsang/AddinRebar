using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using HPRebar.Core.KataExport.Calculators;
using HPRebar.Core.KataExport.ColumnStack;

namespace HPRebar.KataExport.Service;

/// <summary>Outcome of writing one column stack into sheet "Cot".</summary>
/// <param name="RowsWithoutBars">Storeys whose bar count (E) is still empty: Kata refuses to draw until it is typed.</param>
public sealed record KataColumnWriteResult(
    bool Success,
    int StoreysWritten,
    string WorkbookName,
    string ErrorMessage,
    IReadOnlyList<string> RowsWithoutBars);

/// <summary>
/// Writes the geometry cells of a <see cref="KataColumnSheet"/> into sheet "Cot" of the workbook active in the
/// running Excel. Bars, hoops, ties, splice positions and the option boxes stay as the user typed them; nothing
/// is saved.
/// </summary>
public static class KataColumnExcelWriter
{
    private const int NameColumn = 1;      // A
    private const int LastSizeColumn = 4;  // D: A name, B height, C b, D h
    private const int BeamDepthColumn = 11; // K
    private const int BarCountColumn = 5;  // E: Kata refuses a storey without it

    public static KataExcelProbeResult Probe()
    {
        using var target = KataWorkbookTarget.Open(KataColumnSheet.SheetName);
        return new KataExcelProbeResult(target.Error is null, target.WorkbookName, target.WorkbookFullName, target.Error ?? string.Empty);
    }

    /// <param name="expectedWorkbook">
    /// Full name of the workbook the window showed; the write is refused when another workbook became active.
    /// </param>
    public static KataColumnWriteResult Write(KataColumnSheet sheet, string expectedWorkbook)
    {
        if (sheet is null)
        {
            throw new ArgumentNullException(nameof(sheet));
        }

        using var target = KataWorkbookTarget.Open(KataColumnSheet.SheetName);
        if (target.Error is not null)
        {
            return Failed(target.WorkbookName, target.Error);
        }

        if (!string.Equals(target.WorkbookFullName, expectedWorkbook, StringComparison.OrdinalIgnoreCase))
        {
            return Failed(target.WorkbookName,
                $"Workbook đang kích hoạt đã đổi thành '{target.WorkbookName}'. Bấm \"Kiểm tra Excel\" rồi ghi lại.");
        }

        bool started = false;
        try
        {
            var ws = target.Sheet!;
            started = true;
            WriteHeader(ws, sheet);
            WriteStoreys(ws, sheet);
            ClearRowsBelow(ws, sheet.Storeys.Count);
            return new KataColumnWriteResult(true, sheet.Storeys.Count, target.WorkbookName, string.Empty, RowsWithoutBars(ws, sheet));
        }
        catch (Exception ex)
        {
            string reason = ex is COMException com && KataWorkbookTarget.IsBusy(com)
                ? KataWorkbookTarget.BusyMessage
                : $"Lỗi ghi sang Excel: {ex.Message}";
            string partial = started ? " Sheet Cot đã bị ghi dở — ghi lại để ghi đè đầy đủ." : string.Empty;
            return Failed(target.WorkbookName, reason + partial);
        }
    }

    private static KataColumnWriteResult Failed(string workbook, string error) =>
        new(false, 0, workbook, error, Array.Empty<string>());

    private static void WriteHeader(object ws, KataColumnSheet sheet)
    {
        foreach (var cell in sheet.Header)
        {
            if (cell.Value is not null)
            {
                KataExcelRange.Set(ws, cell.Key, "Value2", KataExcelCell.ToCellValue(cell.Value));
            }
        }
    }

    private static void WriteStoreys(object ws, KataColumnSheet sheet)
    {
        int count = sheet.Storeys.Count;
        var sizes = new object[count, LastSizeColumn];
        var beams = new object[count, 1];
        for (int i = 0; i < count; i++)
        {
            var row = sheet.Storeys[i];
            sizes[i, 0] = KataExcelCell.ToCellValue(row.Name);
            sizes[i, 1] = row.HeightM;
            sizes[i, 2] = row.SizeB;
            sizes[i, 3] = row.SizeH;
            beams[i, 0] = row.BeamDepthMm;
        }

        int first = KataColumnSheet.FirstStoreyRow;
        int last = first + count - 1;
        KataExcelRange.WithBlock(ws, first, NameColumn, last, LastSizeColumn, range => ComLateBinding.Set(range, "Value2", sizes));
        KataExcelRange.WithBlock(ws, first, BeamDepthColumn, last, BeamDepthColumn, range => ComLateBinding.Set(range, "Value2", beams));
    }

    /// <summary>
    /// Kata reads storeys until column A is empty, so the cells this tool writes are cleared in the rows a taller
    /// column left below the stack. The user's bars, hoops and ties in those rows (E–J, L–V) are kept: they may be the
    /// only copy of that other column's input.
    /// </summary>
    private static void ClearRowsBelow(object ws, int storeys)
    {
        int first = KataColumnSheet.FirstStoreyRow + storeys;
        if (first > KataColumnSheet.LastStoreyRow)
        {
            return;
        }

        int last = KataColumnSheet.LastStoreyRow;
        KataExcelRange.WithBlock(ws, first, NameColumn, last, LastSizeColumn, range => ComLateBinding.Call(range, "ClearContents"));
        KataExcelRange.WithBlock(ws, first, BeamDepthColumn, last, BeamDepthColumn, range => ComLateBinding.Call(range, "ClearContents"));
    }

    private static IReadOnlyList<string> RowsWithoutBars(object ws, KataColumnSheet sheet)
    {
        int first = KataColumnSheet.FirstStoreyRow;
        int count = sheet.Storeys.Count;
        object? read = null;
        KataExcelRange.WithBlock(ws, first, BarCountColumn, first + count - 1, BarCountColumn, range => read = ComLateBinding.Get(range, "Value2"));

        var missing = new List<string>();
        for (int i = 0; i < count; i++)
        {
            object? cell = read is object[,] cells ? cells[cells.GetLowerBound(0) + i, cells.GetLowerBound(1)] : read;
            if (cell is null || (cell is string text && text.Trim().Length == 0))
            {
                missing.Add(sheet.Storeys[i].Name.Value);
            }
        }

        return missing;
    }
}
