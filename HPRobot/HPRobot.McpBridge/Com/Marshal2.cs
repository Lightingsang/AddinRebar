using System;
using System.Runtime.InteropServices;
using Serilog;

namespace HPRobot.McpBridge.Com;

/// <summary>
///     Provides P/Invoke wrappers for COM operations missing in modern .NET (.NET 5+).
///     Enables GetActiveObject lookup against the Windows Running Object Table (ROT).
/// </summary>
public static class Marshal2
{
    [DllImport("ole32.dll", CharSet = CharSet.Unicode)]
    private static extern int CLSIDFromProgID(string lpszProgID, out Guid lpclsid);

    [DllImport("oleaut32.dll", PreserveSig = true)]
    private static extern int GetActiveObject(ref Guid rclsid, IntPtr pvReserved, [MarshalAs(UnmanagedType.IUnknown)] out object? ppunk);

    /// <summary>
    ///     Attempts to resolve an active COM object registered in the ROT by its ProgID.
    ///     Returns null if not found or if the host is not running.
    /// </summary>
    public static object? GetActiveObject(string progId)
    {
        if (string.IsNullOrWhiteSpace(progId)) return null;

        var hrClsid = CLSIDFromProgID(progId, out var clsid);
        if (hrClsid != 0)
        {
            Log.Debug("CLSIDFromProgID for '{ProgId}' returned 0x{Hr:X8}", progId, hrClsid);
            return null;
        }

        var hrActive = GetActiveObject(ref clsid, IntPtr.Zero, out var punk);
        if (hrActive != 0 || punk == null)
        {
            Log.Debug("GetActiveObject for '{ProgId}' returned 0x{Hr:X8}, punk is null={Null}", progId, hrActive, punk == null);
            return null;
        }

        return punk;
    }
}
