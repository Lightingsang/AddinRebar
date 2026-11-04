using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using HPRebar.Core.KataExport.Calculators;
using HPRebar.Core.KataExport.Models;

namespace HPRebar.KataExport.Service;

/// <summary>Whether the running Excel has an active workbook with sheet "Dam".</summary>
public sealed record KataExcelProbeResult(bool Success, string WorkbookName, string WorkbookFullName, string ErrorMessage);

/// <summary>Outcome of writing one beam run into sheet "Dam".</summary>
public sealed record KataExcelWriteResult(bool Success, int ColumnsWritten, string WorkbookName, string ErrorMessage);

/// <summary>
/// Writes a <see cref="KataSheet"/> into sheet "Dam" of the workbook active in the running Excel, the Kata
/// workbook the user has open. Nothing is saved: the user reviews the sheet and runs Kata from there.
/// </summary>
public static class KataExcelWriter
{
    private const string TargetSheetName = "Dam";
    private const int FirstDataRow = 11;
    private const int LastDataRow = 23;
    private const int Row20 = 20;
    private const int FirstColumn = 3; // C
    private const int LastColumn = 78; // BZ


    public static KataExcelProbeResult Probe()
    {
        using var target = KataWorkbookTarget.Open(TargetSheetName);
        return target.Error is null
            ? new KataExcelProbeResult(true, target.WorkbookName, target.WorkbookFullName, string.Empty)
            : new KataExcelProbeResult(false, target.WorkbookName, target.WorkbookFullName, target.Error);
    }

    /// <param name="expectedWorkbook">
    /// Full name of the workbook the window showed; the write is refused when another workbook became active,
    /// because any workbook with a "Dam" sheet would otherwise be overwritten.
    /// </param>
    public static KataExcelWriteResult Write(KataSheet sheet, string expectedWorkbook)
    {
        if (sheet is null) throw new ArgumentNullException(nameof(sheet));

        using var target = KataWorkbookTarget.Open(TargetSheetName);
        if (target.Error is not null) return new KataExcelWriteResult(false, 0, target.WorkbookName, target.Error);
        if (!string.Equals(target.WorkbookFullName, expectedWorkbook, StringComparison.OrdinalIgnoreCase))
        {
            return new KataExcelWriteResult(false, 0, target.WorkbookName,
                $"Workbook đang kích hoạt đã đổi thành '{target.WorkbookName}'. Bấm \"Kiểm tra lại Excel\" rồi xuất lại.");
        }

        bool started = false;
        try
        {
            var ws = target.Sheet!;

            // Kata keeps B10 as text ("+3.300"); without the text format Excel would store the number 3.3.
            KataExcelRange.Set(ws, "B10", "NumberFormat", "@");
            started = true;
            KataExcelRange.Set(ws, "B3:B10", "Value2", Column(sheet.HeaderColumn));

            // Columns past this run still hold the previous, longer beam in rows 11–23 (sizes, the user's
            // rebar entries for those spans, grid names): clear them so Kata does not draw stale spans.
            int columns = sheet.ColumnCount;
            if (FirstColumn + columns <= LastColumn)
                ClearBlock(ws, FirstDataRow, FirstColumn + columns, LastDataRow, LastColumn);

            foreach (var (row, values) in sheet.Rows())
                WriteRow(ws, row, values);
            WriteRow20(ws, sheet.Row20);

            return new KataExcelWriteResult(true, columns, target.WorkbookName, string.Empty);
        }
        catch (Exception ex)
        {
            string reason = ex is COMException com && KataWorkbookTarget.IsBusy(com) ? KataWorkbookTarget.BusyMessage : $"Lỗi ghi sang Excel: {ex.Message}";
            // Every step overwrites the same cells, so exporting again repairs a half-written sheet.
            string partial = started ? " Sheet Dam đã bị ghi dở — xuất lại để ghi đè đầy đủ." : string.Empty;
            return new KataExcelWriteResult(false, 0, target.WorkbookName, reason + partial);
        }
    }

    private static object[,] Column(IReadOnlyList<object?> values)
    {
        var array = new object[values.Count, 1];
        for (int i = 0; i < values.Count; i++) array[i, 0] = Cell(values[i]);
        return array;
    }

    /// <summary>COM only marshals primitives and strings; the text/number decision is <see cref="KataExcelCell"/>.</summary>
    private static object Cell(object? value) => KataExcelCell.ToCellValue(value);

    private static void WriteRow(object ws, int row, IReadOnlyList<object?> values)
    {
        if (values.Count == 0) return;

        var array = new object[1, values.Count];
        for (int c = 0; c < values.Count; c++) array[0, c] = Cell(values[c]);

        KataExcelRange.WithBlock(ws, row, FirstColumn, row, FirstColumn + values.Count - 1, range => ComLateBinding.Set(range, "Value2", array));
    }

    /// <summary>
    /// Row 20: support cells as built, or as the user typed them where Revit shows no crossing beam (null); span
    /// cells keep the side bars the user typed there, with Revit's span width in front (<see cref="KataRow20.ComposeSpanCell"/>).
    /// </summary>
    private static void WriteRow20(object ws, IReadOnlyList<object?> values)
    {
        if (values.Count == 0) return;

        var existing = new string?[values.Count];
        KataExcelRange.WithBlock(ws, Row20, FirstColumn, Row20, FirstColumn + values.Count - 1, range =>
        {
            object? read = ComLateBinding.Get(range, "Value2");
            if (read is object[,] cells)
                for (int c = 0; c < values.Count; c++) existing[c] = Text(cells[cells.GetLowerBound(0), cells.GetLowerBound(1) + c]);
            else
                existing[0] = Text(read);
        });

        var merged = new object?[values.Count];
        for (int c = 0; c < values.Count; c++)
        {
            merged[c] = values[c] switch
            {
                KataSpanWidthCell span => KataRow20.ComposeSpanCell(existing[c], span.WidthMm),
                null => existing[c] ?? string.Empty,
                var cell => cell
            };
        }

        WriteRow(ws, Row20, merged);
    }

    /// <summary>A cell value as invariant text: a number read back from Excel must not pick up the decimal comma.</summary>
    private static string? Text(object? value) => value is null ? null : KataColumnLetters.CellText(value);

    private static void ClearBlock(object ws, int firstRow, int firstColumn, int lastRow, int lastColumn) =>
        KataExcelRange.WithBlock(ws, firstRow, firstColumn, lastRow, lastColumn, range => ComLateBinding.Call(range, "ClearContents"));
}
