using System;
using System.Runtime.InteropServices;

namespace HPRebar.KataExport.Service;

/// <summary>
/// The running Excel, its active workbook and one Kata sheet ("Dam" for beams, "Cot" for columns), released
/// together. <see cref="Error"/> holds the user-facing reason when any of them is missing.
/// </summary>
internal sealed class KataWorkbookTarget : IDisposable
{
    public const string BusyMessage = "Excel đang bận (đang sửa ô hoặc có hộp thoại mở?). Bấm Enter/Esc trong Excel rồi thử lại.";

    private const uint RpcCallRejected = 0x80010001;
    private const uint RpcRetryLater = 0x8001010A;
    private const uint ExcelBusy = 0x800AC472;

    private object? _app, _workbook, _sheets;

    private KataWorkbookTarget()
    {
    }

    public object? Sheet { get; private set; }
    public string WorkbookName { get; private set; } = string.Empty;
    public string WorkbookFullName { get; private set; } = string.Empty;
    public string? Error { get; private set; }

    public static bool IsBusy(COMException ex) => (uint)ex.ErrorCode is RpcCallRejected or RpcRetryLater or ExcelBusy;

    public static KataWorkbookTarget Open(string sheetName)
    {
        var target = new KataWorkbookTarget();
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
                target.Sheet = ComLateBinding.Get(target._sheets!, "Item", sheetName);
            }
            catch (COMException)
            {
                target.Sheet = null;
            }

            if (target.Sheet is null)
            {
                target.Error = $"Workbook '{target.WorkbookName}' không có sheet '{sheetName}' — hãy kích hoạt workbook Kata.";
            }
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

/// <summary>Range access on a worksheet through late-bound COM, every intermediate object released.</summary>
internal static class KataExcelRange
{
    public static void WithBlock(object ws, int firstRow, int firstColumn, int lastRow, int lastColumn, Action<object> action)
    {
        object? start = null, end = null, range = null;
        try
        {
            start = ComLateBinding.Get(ws, "Cells", firstRow, firstColumn);
            end = ComLateBinding.Get(ws, "Cells", lastRow, lastColumn);
            range = ComLateBinding.Get(ws, "Range", start, end);
            if (range is not null)
            {
                action(range);
            }
        }
        finally
        {
            ComLateBinding.Release(range);
            ComLateBinding.Release(end);
            ComLateBinding.Release(start);
        }
    }

    public static void Set(object ws, string address, string property, object value)
    {
        object? range = null;
        try
        {
            range = ComLateBinding.Get(ws, "Range", address);
            if (range is not null)
            {
                ComLateBinding.Set(range, property, value);
            }
        }
        finally
        {
            ComLateBinding.Release(range);
        }
    }
}
