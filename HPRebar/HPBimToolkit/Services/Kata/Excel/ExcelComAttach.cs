using System.Runtime.InteropServices;

namespace HPBimToolkit.Services.Kata.Excel;

/// <summary>
///     Attaches to the Excel the user already runs, through the Running Object Table (.NET 8 has no
///     <c>Marshal.GetActiveObject</c>). Ported from <c>HPRebar/HPRebar/KataExport/Service</c> (ADR-0009).
/// </summary>
public static class ExcelComAttach
{
    public const string ExcelProgId = "Excel.Application";

    [DllImport("ole32.dll", CharSet = CharSet.Unicode)]
    private static extern int CLSIDFromProgID(string lpszProgID, out Guid lpclsid);

    [DllImport("oleaut32.dll", PreserveSig = true)]
    private static extern int GetActiveObject(ref Guid rclsid, IntPtr pvReserved, [MarshalAs(UnmanagedType.IUnknown)] out object? ppunk);

    /// <returns>True with the <c>Excel.Application</c> COM object; false with a reason the page can show.</returns>
    public static bool TryGetRunningExcel(out object? app, out string reason)
    {
        app = null;
        var hrClsid = CLSIDFromProgID(ExcelProgId, out var clsid);
        if (hrClsid != 0)
        {
            reason = $"Không thể lấy CLSID cho {ExcelProgId} (0x{hrClsid:X8}).";
            return false;
        }

        var hrActive = GetActiveObject(ref clsid, IntPtr.Zero, out var punk);
        if (hrActive != 0 || punk is null)
        {
            reason = "Microsoft Excel hiện không chạy hoặc không đăng ký trong hệ thống ROT.";
            return false;
        }

        app = punk;
        reason = string.Empty;
        return true;
    }
}
