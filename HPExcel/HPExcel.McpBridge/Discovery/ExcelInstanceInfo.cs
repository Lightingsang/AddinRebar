using System;

namespace HPExcel.McpBridge.Discovery;

/// <summary>
///     Information about a detected Microsoft Excel instance on the system.
/// </summary>
public sealed record ExcelInstanceInfo(
    int ProcessId,
    string ProcessName,
    DateTime? StartTime,
    string MainWindowTitle,
    IntPtr MainWindowHandle,
    bool IsResponding,
    bool HasActiveWorkbook)
{
    public string DisplayText => string.IsNullOrWhiteSpace(MainWindowTitle)
        ? $"PID {ProcessId} (No Title)"
        : $"PID {ProcessId} — {MainWindowTitle}";
}
