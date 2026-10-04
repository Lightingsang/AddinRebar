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

    private const uint RpcCallRejected = 0x80010001;
    private const uint RpcRetryLater = 0x8001010A;
    private const uint ExcelBusy = 0x800AC472;
    private const string BusyMessage = "Excel đang bận (đang sửa ô hoặc có hộp thoại mở?). Bấm Enter/Esc trong Excel rồi thử lại.";


    public static KataExcelProbeResult Probe()
    {
        using var target = Target.Open();
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

        using var target = Target.Open();
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
            SetRange(ws, "B10", "NumberFormat", "@");
            started = true;
            SetRange(ws, "B3:B10", "Value2", Column(sheet.HeaderColumn));

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
            string reason = ex is COMException com && IsBusy(com) ? BusyMessage : $"Lỗi ghi sang Excel: {ex.Message}";
            // Every step overwrites the same cells, so exporting again repairs a half-written sheet.
            string partial = started ? " Sheet Dam đã bị ghi dở — xuất lại để ghi đè đầy đủ." : string.Empty;
            return new KataExcelWriteResult(false, 0, target.WorkbookName, reason + partial);
        }
    }

    private static bool IsBusy(COMException ex) => (uint)ex.ErrorCode is RpcCallRejected or RpcRetryLater or ExcelBusy;

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

        WithBlock(ws, row, FirstColumn, row, FirstColumn + values.Count - 1, range => ComLateBinding.Set(range, "Value2", array));
    }

    /// <summary>
    /// Row 20: support cells as built, or as the user typed them where Revit shows no crossing beam (null); span
    /// cells keep the side bars the user typed there, with Revit's span width in front (<see cref="KataRow20.ComposeSpanCell"/>).
    /// </summary>
    private static void WriteRow20(object ws, IReadOnlyList<object?> values)
    {
        if (values.Count == 0) return;

        var existing = new string?[values.Count];
        WithBlock(ws, Row20, FirstColumn, Row20, FirstColumn + values.Count - 1, range =>
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
        WithBlock(ws, firstRow, firstColumn, lastRow, lastColumn, range => ComLateBinding.Call(range, "ClearContents"));

    private static void WithBlock(object ws, int firstRow, int firstColumn, int lastRow, int lastColumn, Action<object> action)
    {
        object? start = null, end = null, range = null;
        try
        {
            start = ComLateBinding.Get(ws, "Cells", firstRow, firstColumn);
            end = ComLateBinding.Get(ws, "Cells", lastRow, lastColumn);
            range = ComLateBinding.Get(ws, "Range", start, end);
            if (range is not null) action(range);
        }
        finally
        {
            ComLateBinding.Release(range);
            ComLateBinding.Release(end);
            ComLateBinding.Release(start);
        }
    }

    private static void SetRange(object ws, string address, string property, object value)
    {
        object? range = null;
        try
        {
            range = ComLateBinding.Get(ws, "Range", address);
            if (range is not null) ComLateBinding.Set(range, property, value);
        }
        finally
        {
            ComLateBinding.Release(range);
        }
    }

    /// <summary>The running Excel, its active workbook and sheet "Dam", released together.</summary>
    private sealed class Target : IDisposable
    {
        private object? _app, _workbook, _sheets;

        public object? Sheet { get; private set; }
        public string WorkbookName { get; private set; } = string.Empty;
        public string WorkbookFullName { get; private set; } = string.Empty;
        public string? Error { get; private set; }

        public static Target Open()
        {
            var target = new Target();
            try
            {
                if (!ExcelComAttach.TryGetRunningExcel(out target._app, out var reason))
                {
                    target.Error = reason;
                    return target;
                }

                target._workbook = ComLateBinding.Get(target._app!, "ActiveWorkbook");
                if (target._workbook is null)
                {
                    target.Error = "Excel đang mở nhưng không có workbook nào được kích hoạt.";
                    return target;
                }

                target.WorkbookName = ComLateBinding.Get(target._workbook, "Name")?.ToString() ?? string.Empty;
                target.WorkbookFullName = ComLateBinding.Get(target._workbook, "FullName")?.ToString() ?? target.WorkbookName;
                target._sheets = ComLateBinding.Get(target._workbook, "Worksheets");
                try
                {
                    target.Sheet = ComLateBinding.Get(target._sheets!, "Item", TargetSheetName);
                }
                catch (COMException)
                {
                    target.Sheet = null;
                }

                if (target.Sheet is null)
                    target.Error = $"Workbook '{target.WorkbookName}' không có sheet '{TargetSheetName}' — hãy kích hoạt workbook Kata.";
            }
            catch (COMException ex) when (IsBusy(ex))
            {
                target.Error = BusyMessage;
            }
            catch (Exception ex)
            {
                target.Error = $"Lỗi kết nối Excel: {ex.Message}";
            }

            return target;
        }

        public void Dispose()
        {
            ComLateBinding.Release(Sheet);
            ComLateBinding.Release(_sheets);
            ComLateBinding.Release(_workbook);
            ComLateBinding.Release(_app);
        }
    }
}
