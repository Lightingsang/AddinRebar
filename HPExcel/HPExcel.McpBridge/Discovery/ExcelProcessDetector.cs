using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Serilog;

namespace HPExcel.McpBridge.Discovery;

/// <summary>
///     Detects and enumerates running Microsoft Excel processes on the machine.
/// </summary>
public static class ExcelProcessDetector
{
    private const string ExcelProcessName = "EXCEL";

    /// <summary>
    ///     Scans the system for running EXCEL.EXE processes.
    /// </summary>
    public static IReadOnlyList<ExcelInstanceInfo> DetectRunningInstances()
    {
        var result = new List<ExcelInstanceInfo>();

        try
        {
            var processes = Process.GetProcessesByName(ExcelProcessName);
            foreach (var process in processes)
            {
                try
                {
                    var pid = process.Id;
                    var title = process.MainWindowTitle;
                    var handle = process.MainWindowHandle;
                    var responding = process.Responding;
                    DateTime? startTime = null;
                    try { startTime = process.StartTime; } catch { }

                    var hasActiveWorkbook = !string.IsNullOrWhiteSpace(title) &&
                                            !title.Equals("Excel", StringComparison.OrdinalIgnoreCase);

                    result.Add(new ExcelInstanceInfo(
                        pid,
                        process.ProcessName,
                        startTime,
                        title,
                        handle,
                        responding,
                        hasActiveWorkbook));
                }
                catch (Exception ex)
                {
                    Log.Debug(ex, "Failed to read process info for Excel process {Pid}", process.Id);
                }
                finally
                {
                    process.Dispose();
                }
            }
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Failed to detect running Excel processes");
        }

        return result.OrderByDescending(p => p.StartTime ?? DateTime.MinValue).ToList();
    }

    /// <summary>
    ///     Checks whether an Excel process with the given PID is still running.
    /// </summary>
    public static bool IsProcessRunning(int pid)
    {
        try
        {
            using var p = Process.GetProcessById(pid);
            return !p.HasExited;
        }
        catch
        {
            return false;
        }
    }
}
