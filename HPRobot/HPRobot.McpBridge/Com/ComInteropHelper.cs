using System;
using System.Runtime.InteropServices;
using Serilog;

namespace HPRobot.McpBridge.Com;

/// <summary>
///     COM Interop helper for Autodesk Robot Structural Analysis automation:
///     - Custom IOleMessageFilter to automatically retry on SERVERCALL_RETRYLATER / modal dialogs.
///     - RCW release utilities.
/// </summary>
public static class ComInteropHelper
{
    public const string RobotProgId = "Robot.Application";

    [DllImport("ole32.dll")]
    private static extern int CoRegisterMessageFilter(IOleMessageFilter? newFilter, out IOleMessageFilter? oldFilter);

    /// <summary>
    ///     Registers a message filter for the current STA thread to handle COM busy/retry states.
    ///     Returns an IDisposable that restores the previous filter upon disposal.
    /// </summary>
    public static IDisposable RegisterMessageFilter()
    {
        var filter = new RobotOleMessageFilter();
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

    private sealed class RobotOleMessageFilter : IOleMessageFilter
    {
        private const int SERVERCALL_ISHANDLED = 0;
        private const int SERVERCALL_RETRYLATER = 2;
        private const int PENDINGMSG_WAITDEFPROCESS = 2;
        private const int MaxRetryMilliseconds = 30000;
        private const int RetryIntervalMilliseconds = 250;

        public int HandleInComingCall(int dwCallType, IntPtr htaskCaller, int dwTickCount, IntPtr lpInterfaceInfo)
        {
            return SERVERCALL_ISHANDLED;
        }

        public int RetryRejectedCall(IntPtr htaskCallee, int dwTickCount, int dwRejectType)
        {
            if (dwRejectType == SERVERCALL_RETRYLATER && dwTickCount < MaxRetryMilliseconds)
            {
                return RetryIntervalMilliseconds;
            }

            return -1;
        }

        public int MessagePending(IntPtr htaskCallee, int dwTickCount, int dwPendingType)
        {
            return PENDINGMSG_WAITDEFPROCESS;
        }
    }
}
