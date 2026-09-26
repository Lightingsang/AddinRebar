using System;
using System.Runtime.InteropServices;

namespace HPRebar.KataExport.Service;

/// <summary>
/// Connects to running Microsoft Excel instance via ROT (Running Object Table).
/// Compatible with .NET 8 (where Marshal.GetActiveObject is unavailable) and .NET Framework.
/// </summary>
public static class ExcelComAttach
{
    public const string ExcelProgId = "Excel.Application";

    [DllImport("ole32.dll", CharSet = CharSet.Unicode)]
    private static extern int CLSIDFromProgID(string lpszProgID, out Guid lpclsid);

    [DllImport("oleaut32.dll", PreserveSig = true)]
    private static extern int GetActiveObject(ref Guid rclsid, IntPtr pvReserved, [MarshalAs(UnmanagedType.IUnknown)] out object? ppunk);

    /// <summary>
    /// Attempts to attach to a currently running Excel application instance.
    /// </summary>
    /// <param name="app">The Excel.Application COM object if successful.</param>
    /// <param name="reason">Failure explanation if unsuccessful.</param>
    /// <returns>True if connected; otherwise false.</returns>
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
