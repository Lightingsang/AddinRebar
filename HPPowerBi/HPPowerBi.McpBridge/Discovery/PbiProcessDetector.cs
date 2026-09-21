using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Management;
using System.Text.RegularExpressions;
using Serilog;

namespace HPPowerBi.McpBridge.Discovery;

/// <summary>
///     Detects running Power BI Desktop instances, correlates them with child msmdsrv.exe SSAS engines,
///     and resolves their local TCP ports for AMO-TOM and ADOMD.NET connections.
/// </summary>
public static class PbiProcessDetector
{
    public const string PbiProcessName = "PBIDesktop";
    public const string MsmdsrvProcessName = "msmdsrv";

    /// <summary>
    ///     Gets all currently running PBIDesktop processes.
    /// </summary>
    public static IReadOnlyList<Process> GetPbiProcesses()
    {
        try
        {
            return Process.GetProcessesByName(PbiProcessName);
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Failed to enumerate PBIDesktop processes");
            return Array.Empty<Process>();
        }
    }

    /// <summary>
    ///     Detects all active Power BI Desktop instances with their associated local Analysis Services ports.
    /// </summary>
    public static IReadOnlyList<PbiInstanceInfo> DetectInstances(string? customWorkspaceRoot = null)
    {
        var results = new List<PbiInstanceInfo>();
        var pbiProcesses = GetPbiProcesses();

        if (pbiProcesses.Count == 0)
        {
            // If PBIDesktop process cannot be enumerated (e.g. permission or headless mode),
            // check active workspace directories directly
            var activeWorkspaces = AnalysisServicesPortFinder.FindAllActivePorts(customWorkspaceRoot);
            foreach (var (wsPath, port) in activeWorkspaces)
            {
                var dirName = Path.GetFileName(wsPath);
                results.Add(new PbiInstanceInfo(
                    ProcessId: 0,
                    WindowTitle: dirName,
                    ReportName: dirName,
                    Port: port,
                    WorkspacePath: wsPath,
                    DiscoveredAt: DateTime.UtcNow));
            }
            return results;
        }

        // Map parent PBIDesktop PID -> child msmdsrv command line or workspace path
        var childWorkspaces = FindMsmdsrvWorkspacesViaWmi();

        foreach (var process in pbiProcesses)
        {
            try
            {
                var pid = process.Id;
                var windowTitle = process.MainWindowTitle;
                var reportName = PbiInstanceInfo.ExtractReportName(windowTitle);
                string? workspacePath = null;
                int? port = null;

                // 1. Try finding workspace mapped via WMI
                if (childWorkspaces.TryGetValue(pid, out var wmiPath) && Directory.Exists(wmiPath))
                {
                    workspacePath = wmiPath;
                    port = AnalysisServicesPortFinder.FindPortForWorkspace(wmiPath);
                }

                // 2. Fallback: If only 1 PBIDesktop is running, match to newest workspace port
                if (!port.HasValue && pbiProcesses.Count == 1)
                {
                    var newest = AnalysisServicesPortFinder.FindNewestPort(customWorkspaceRoot);
                    if (newest.HasValue)
                    {
                        workspacePath = newest.Value.workspacePath;
                        port = newest.Value.port;
                    }
                }

                // 3. Fallback: Scan active workspaces and pick first available
                if (!port.HasValue)
                {
                    var allActive = AnalysisServicesPortFinder.FindAllActivePorts(customWorkspaceRoot);
                    if (allActive.Count > 0)
                    {
                        workspacePath = allActive[0].workspacePath;
                        port = allActive[0].port;
                    }
                }

                if (port.HasValue)
                {
                    results.Add(new PbiInstanceInfo(
                        ProcessId: pid,
                        WindowTitle: string.IsNullOrWhiteSpace(windowTitle) ? reportName : windowTitle,
                        ReportName: reportName,
                        Port: port.Value,
                        WorkspacePath: workspacePath,
                        DiscoveredAt: DateTime.UtcNow));
                }
                else
                {
                    Log.Debug("Found PBIDesktop PID {Pid} ('{Title}'), but no active Analysis Services port was resolved", pid, windowTitle);
                }
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Error inspecting PBIDesktop process {Pid}", process.Id);
            }
        }

        return results;
    }

    /// <summary>
    ///     Uses WMI to query Win32_Process for msmdsrv.exe child processes and extracts the workspace directory from -s argument.
    ///     Returns a dictionary mapping ParentProcessId (PBIDesktop) -> WorkspacePath.
    /// </summary>
    private static Dictionary<int, string> FindMsmdsrvWorkspacesViaWmi()
    {
        var map = new Dictionary<int, string>();
        try
        {
            using var searcher = new ManagementObjectSearcher(
                "SELECT ProcessId, ParentProcessId, CommandLine FROM Win32_Process WHERE Name = 'msmdsrv.exe'");

            foreach (ManagementObject obj in searcher.Get())
            {
                try
                {
                    var parentId = Convert.ToInt32(obj["ParentProcessId"]);
                    var commandLine = obj["CommandLine"]?.ToString();

                    if (string.IsNullOrWhiteSpace(commandLine))
                        continue;

                    // Match -s "path" or -s path
                    var match = Regex.Match(commandLine, @"-s\s+[""]?([^""]+)[""]?", RegexOptions.IgnoreCase);
                    if (match.Success)
                    {
                        var dataDir = match.Groups[1].Value.Trim();
                        // -s points to the Data directory inside the workspace; parent is the workspace directory
                        var workspaceDir = Path.GetDirectoryName(dataDir.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar))
                                           ?? dataDir;

                        map[parentId] = workspaceDir;
                    }
                }
                catch (Exception ex)
                {
                    Log.Debug(ex, "Error parsing WMI msmdsrv process row");
                }
            }
        }
        catch (Exception ex)
        {
            Log.Debug(ex, "WMI query for msmdsrv.exe failed (may require elevation or non-WMI environment)");
        }

        return map;
    }
}
