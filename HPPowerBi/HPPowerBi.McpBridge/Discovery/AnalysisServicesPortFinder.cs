using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using Serilog;

namespace HPPowerBi.McpBridge.Discovery;

/// <summary>
///     Discovers and reads the TCP port file written by Power BI Desktop's local Analysis Services engine (msmdsrv.exe).
///     The port file is located at %LocalAppData%\Microsoft\Power BI Desktop\AnalysisServicesWorkspaces\&lt;guid&gt;\Data\msmdsrv.port.txt.
///     Crucially, SSAS encodes this file in UTF-16 LE (Unicode) and may hold an open handle, requiring FileShare.ReadWrite.
/// </summary>
public static class AnalysisServicesPortFinder
{
    public const string PortFileName = "msmdsrv.port.txt";
    public const int MinPort = 1024;
    public const int MaxPort = 65535;

    /// <summary>Gets the default root directory where Power BI Desktop stores local Analysis Services workspaces.</summary>
    public static string GetDefaultWorkspacesRoot()
    {
        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Microsoft",
            "Power BI Desktop",
            "AnalysisServicesWorkspaces");
    }

    /// <summary>
    ///     Reads the port number from a specified msmdsrv.port.txt file.
    ///     Uses UTF-16LE encoding, FileShare.ReadWrite, and retry semantics to handle file locks.
    /// </summary>
    public static int ReadPortFile(string portFilePath, int maxRetries = 3, int retryDelayMs = 100)
    {
        if (string.IsNullOrWhiteSpace(portFilePath))
            throw new ArgumentException("Port file path cannot be null or empty.", nameof(portFilePath));

        if (!File.Exists(portFilePath))
            throw new FileNotFoundException($"Port file not found at '{portFilePath}'.", portFilePath);

        for (var attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                using var stream = new FileStream(portFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                using var reader = new StreamReader(stream, Encoding.Unicode, detectEncodingFromByteOrderMarks: true);

                var content = reader.ReadToEnd();
                // Clean null chars, whitespace, newlines that may be present in raw memory buffer
                var clean = content.Replace("\0", string.Empty).Trim();

                if (int.TryParse(clean, out var port))
                {
                    if (port is >= MinPort and <= MaxPort)
                        return port;

                    throw new InvalidDataException($"Port {port} in '{portFilePath}' is outside valid range [{MinPort}..{MaxPort}].");
                }

                throw new FormatException($"Cannot parse port number from content '{clean}' in file '{portFilePath}'.");
            }
            catch (IOException ex)
            {
                if (attempt == maxRetries)
                    throw new IOException($"Failed to read port file '{portFilePath}' after {maxRetries} attempts.", ex);

                Log.Debug(ex, "Port file read attempt {Attempt} for '{Path}' encountered IO lock, retrying in {Delay}ms", attempt, portFilePath, retryDelayMs);
                Thread.Sleep(retryDelayMs);
            }
            catch (Exception ex) when (ex is FormatException or InvalidDataException)
            {
                if (attempt == maxRetries)
                    throw;

                Log.Debug(ex, "Port file read attempt {Attempt} for '{Path}' encountered transient parse/data error, retrying in {Delay}ms", attempt, portFilePath, retryDelayMs);
                Thread.Sleep(retryDelayMs);
            }
        }

        throw new IOException($"Failed to read port file '{portFilePath}' after {maxRetries} attempts.");
    }

    /// <summary>
    ///     Finds the port file inside a specific workspace directory (e.g. workspace\Data\msmdsrv.port.txt).
    /// </summary>
    public static int? FindPortForWorkspace(string workspacePath)
    {
        if (string.IsNullOrWhiteSpace(workspacePath) || !Directory.Exists(workspacePath))
            return null;

        var portPath = Path.Combine(workspacePath, "Data", PortFileName);
        if (!File.Exists(portPath))
        {
            // Some versions place it directly in workspace root
            portPath = Path.Combine(workspacePath, PortFileName);
            if (!File.Exists(portPath))
                return null;
        }

        try
        {
            return ReadPortFile(portPath);
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Failed to read port from workspace '{Path}'", workspacePath);
            return null;
        }
    }

    /// <summary>
    ///     Scans the workspaces root directory and returns all active workspace paths and their ports.
    /// </summary>
    public static IReadOnlyList<(string workspacePath, int port)> FindAllActivePorts(string? customRootPath = null)
    {
        var root = customRootPath ?? GetDefaultWorkspacesRoot();
        if (!Directory.Exists(root))
            return Array.Empty<(string, int)>();

        var results = new List<(string, int)>();
        try
        {
            var directories = Directory.GetDirectories(root, "AnalysisServicesWorkspace*");
            foreach (var dir in directories)
            {
                var port = FindPortForWorkspace(dir);
                if (port.HasValue)
                    results.Add((dir, port.Value));
            }
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Failed to enumerate workspaces in '{Root}'", root);
        }

        return results;
    }

    /// <summary>
    ///     Finds the newest active workspace and its port based on last write time of the directory.
    /// </summary>
    public static (string workspacePath, int port)? FindNewestPort(string? customRootPath = null)
    {
        var root = customRootPath ?? GetDefaultWorkspacesRoot();
        if (!Directory.Exists(root))
            return null;

        try
        {
            var directories = Directory.GetDirectories(root, "AnalysisServicesWorkspace*");
            string? newestDir = null;
            var newestTime = DateTime.MinValue;

            foreach (var dir in directories)
            {
                var writeTime = Directory.GetLastWriteTimeUtc(dir);
                if (writeTime > newestTime)
                {
                    newestTime = writeTime;
                    newestDir = dir;
                }
            }

            if (newestDir != null)
            {
                var port = FindPortForWorkspace(newestDir);
                if (port.HasValue)
                    return (newestDir, port.Value);
            }
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Failed to find newest workspace in '{Root}'", root);
        }

        return null;
    }
}
