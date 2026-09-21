using System;
using System.Runtime.InteropServices;
using Serilog;

namespace HPExcel.McpBridge.Com;

/// <summary>
///     COM Interop helper for Excel automation:
///     - P/Invoke oleaut32!GetActiveObject and ole32!CLSIDFromProgID.
///     - Custom IOleMessageFilter to automatically retry on SERVERCALL_RETRYLATER / busy Excel states.
///     - RCW release utilities.
/// </summary>
public static class ComInteropHelper
{
    public const string ExcelProgId = "Excel.Application";

    [DllImport("ole32.dll", CharSet = CharSet.Unicode)]
    private static extern int CLSIDFromProgID(string lpszProgID, out Guid lpclsid);

    [DllImport("oleaut32.dll", PreserveSig = true)]
    private static extern int GetActiveObject(ref Guid rclsid, IntPtr pvReserved, [MarshalAs(UnmanagedType.IUnknown)] out object? ppunk);

    [DllImport("ole32.dll")]
    private static extern int CoRegisterMessageFilter(IOleMessageFilter? newFilter, out IOleMessageFilter? oldFilter);

    /// <summary>
    ///     Attempts to get the active running Excel.Application COM object from the ROT.
    /// </summary>
    public static object? GetActiveExcelObject()
    {
        var hrClsid = CLSIDFromProgID(ExcelProgId, out var clsid);
        if (hrClsid != 0)
        {
            Log.Debug("CLSIDFromProgID for '{ProgId}' returned 0x{Hr:X8}", ExcelProgId, hrClsid);
            return null;
        }

        var hrActive = GetActiveObject(ref clsid, IntPtr.Zero, out var punk);
        if (hrActive != 0 || punk == null)
        {
            Log.Debug("GetActiveObject for '{ProgId}' returned 0x{Hr:X8}, punk is null={Null}",
                ExcelProgId, hrActive, punk == null);
            return null;
        }

        return punk;
    }

    /// <summary>
    ///     Registers a message filter for the current thread to handle Excel busy/retry states.
    ///     Must be called on the STA thread. Returns an IDisposable that restores the previous filter.
    /// </summary>
    public static IDisposable RegisterMessageFilter()
    {
        var filter = new ExcelOleMessageFilter();
        CoRegisterMessageFilter(filter, out var oldFilter);
        return new FilterScope(oldFilter);
    }

    /// <summary>
    ///     Safely releases a COM object reference.
    /// </summary>
    public static void ReleaseComObject(object? obj)
    {
        if (obj == null) return;
        try
        {
            if (Marshal.IsComObject(obj))
            {
                Marshal.ReleaseComObject(obj);
            }
        }
        catch (Exception ex)
        {
            Log.Debug(ex, "Error releasing COM object");
        }
    }

    /// <summary>
    ///     Safely releases all references to a COM object.
    /// </summary>
    public static void FinalReleaseComObject(object? obj)
    {
        if (obj == null) return;
        try
        {
            if (Marshal.IsComObject(obj))
            {
                Marshal.FinalReleaseComObject(obj);
            }
        }
        catch (Exception ex)
        {
            Log.Debug(ex, "Error final-releasing COM object");
        }
    }

    private sealed class FilterScope : IDisposable
    {
        private readonly IOleMessageFilter? _previousFilter;
        private bool _disposed;

        public FilterScope(IOleMessageFilter? previousFilter)
        {
            _previousFilter = previousFilter;
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            try
            {
                CoRegisterMessageFilter(_previousFilter, out _);
            }
            catch (Exception ex)
            {
                Log.Debug(ex, "Error restoring previous OLE message filter");
            }
        }
    }

    [ComImport, Guid("00000016-0000-0000-C000-000000000046"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IOleMessageFilter
    {
        [PreserveSig]
        int HandleInComingCall(int dwCallType, IntPtr htaskCaller, int dwTickCount, IntPtr lpInterfaceInfo);

        [PreserveSig]
        int RetryRejectedCall(IntPtr htaskCallee, int dwTickCount, int dwRejectType);

        [PreserveSig]
        int MessagePending(IntPtr htaskCallee, int dwTickCount, int dwPendingType);
    }

    private sealed class ExcelOleMessageFilter : IOleMessageFilter
    {
        private const int SERVERCALL_ISHANDLED = 0;
        private const int SERVERCALL_RETRYLATER = 2;
        private const int PENDINGMSG_WAITDEFPROCESS = 2;
        private const int MaxRetryMilliseconds = 15000;
        private const int RetryIntervalMilliseconds = 250;

        public int HandleInComingCall(int dwCallType, IntPtr htaskCaller, int dwTickCount, IntPtr lpInterfaceInfo)
        {
            return SERVERCALL_ISHANDLED;
        }

        public int RetryRejectedCall(IntPtr htaskCallee, int dwTickCount, int dwRejectType)
        {
            // If the server says retry later (busy/modal dialog) and we haven't exceeded max time
            if (dwRejectType == SERVERCALL_RETRYLATER && dwTickCount < MaxRetryMilliseconds)
            {
                return RetryIntervalMilliseconds; // Retry after 250 ms
            }

            // Cancel the call (-1) if timed out or not retry later
            return -1;
        }

        public int MessagePending(IntPtr htaskCallee, int dwTickCount, int dwPendingType)
        {
            return PENDINGMSG_WAITDEFPROCESS;
        }
    }
}
