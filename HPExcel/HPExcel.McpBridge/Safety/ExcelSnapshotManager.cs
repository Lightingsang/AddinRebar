using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Serilog;

namespace HPExcel.McpBridge.Safety;

/// <summary>
///     Manages automatic pre-mutation snapshot backups of Excel workbooks before Write or Destructive operations.
///     Saves timestamped copies into a local .hpexcel_snapshots directory (or %TEMP%\.hpexcel_snapshots),
///     and prunes older snapshots to retain the newest 20.
/// </summary>
public sealed class ExcelSnapshotManager
{
    public const int DefaultMaxRetained = 20;
    public const string SnapshotFolder = ".hpexcel_snapshots";
    private static readonly Regex SafeNameRegex = new(@"[^A-Za-z0-9_-]", RegexOptions.Compiled);
    private readonly string? _overrideRootDirectory;

    public ExcelSnapshotManager(string? overrideRootDirectory = null)
    {
        _overrideRootDirectory = overrideRootDirectory;
    }

    /// <summary>
    ///     Resolves the directory where snapshots for the specified workbook should reside.
    ///     Prefers a local `.hpexcel_snapshots` directory adjacent to the file;
    ///     falls back to %TEMP%\.hpexcel_snapshots if unsaved, read-only, or on a UNC path.
    /// </summary>
    public string ResolveSnapshotDirectory(string? workbookPath)
    {
        if (!string.IsNullOrEmpty(_overrideRootDirectory))
        {
            Directory.CreateDirectory(_overrideRootDirectory);
            return _overrideRootDirectory;
        }

        if (string.IsNullOrWhiteSpace(workbookPath) ||
            !Path.IsPathRooted(workbookPath) ||
            workbookPath.StartsWith(@"\\", StringComparison.Ordinal))
        {
            var tempDir = Path.Combine(Path.GetTempPath(), SnapshotFolder);
            Directory.CreateDirectory(tempDir);
            return tempDir;
        }

        try
        {
            var dir = Path.GetDirectoryName(workbookPath);
            if (!string.IsNullOrEmpty(dir) && Directory.Exists(dir))
            {
                var localSnapDir = Path.Combine(dir, SnapshotFolder);
                Directory.CreateDirectory(localSnapDir);
                return localSnapDir;
            }
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Failed to use adjacent snapshot directory for '{Path}'. Falling back to TEMP.", workbookPath);
        }

        var fallbackDir = Path.Combine(Path.GetTempPath(), SnapshotFolder);
        Directory.CreateDirectory(fallbackDir);
        return fallbackDir;
    }

    /// <summary>
    ///     Creates a snapshot backup of a workbook before a mutating operation.
    ///     Supports live COM workbooks (via SaveCopyAs) and ClosedXML files.
    /// </summary>
    public string CreateSnapshot(dynamic? comWorkbook, string? workbookPath, string? label = null)
    {
        var targetDir = ResolveSnapshotDirectory(workbookPath);
        Directory.CreateDirectory(targetDir);

        var timestamp = DateTime.Now.ToString("yyyyMMdd-HHmmss");
        string baseName = "Workbook";

        if (!string.IsNullOrEmpty(workbookPath))
        {
            baseName = Path.GetFileNameWithoutExtension(workbookPath);
        }
        else if (comWorkbook != null)
        {
            try { baseName = (string)comWorkbook.Name; } catch { }
            if (baseName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase) ||
                baseName.EndsWith(".xlsm", StringComparison.OrdinalIgnoreCase) ||
                baseName.EndsWith(".xls", StringComparison.OrdinalIgnoreCase))
            {
                baseName = Path.GetFileNameWithoutExtension(baseName);
            }
        }

        var safeBaseName = Sanitize(baseName);
        var safeLabel = !string.IsNullOrWhiteSpace(label) ? "_" + Sanitize(label) : string.Empty;
        var snapshotFileName = $"{timestamp}_{safeBaseName}{safeLabel}.xlsx";
        var snapshotFullPath = Path.Combine(targetDir, snapshotFileName);

        // 1. If live COM workbook is available, invoke SaveCopyAs
        if (comWorkbook != null)
        {
            try
            {
                comWorkbook.SaveCopyAs(snapshotFullPath);
                Log.Information("Captured live COM workbook snapshot: '{Path}'", snapshotFullPath);
                Prune(targetDir, DefaultMaxRetained);
                return snapshotFullPath;
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "COM SaveCopyAs failed. Attempting file-copy fallback.");
            }
        }

        // 2. Headless file-copy fallback
        if (!string.IsNullOrEmpty(workbookPath) && File.Exists(workbookPath))
        {
            File.Copy(workbookPath, snapshotFullPath, overwrite: true);
            Log.Information("Captured file-copy snapshot: '{Path}'", snapshotFullPath);
            Prune(targetDir, DefaultMaxRetained);
            return snapshotFullPath;
        }

        throw new InvalidOperationException("Cannot capture workbook snapshot: workbook has no file path and COM SaveCopyAs was unavailable.");
    }

    public static string Sanitize(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return "snapshot";
        var cleaned = SafeNameRegex.Replace(text, "_").Trim('_');
        return cleaned.Length > 40 ? cleaned[..40] : (cleaned.Length == 0 ? "snapshot" : cleaned);
    }

    public static int Prune(string directory, int maxRetained = DefaultMaxRetained)
    {
        if (!Directory.Exists(directory)) return 0;

        try
        {
            var files = new DirectoryInfo(directory)
                .GetFiles("*.xlsx")
                .OrderByDescending(f => f.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (files.Count <= maxRetained) return 0;

            var toDelete = files.Skip(maxRetained).ToList();
            var count = 0;
            foreach (var f in toDelete)
            {
                try
                {
                    f.Delete();
                    count++;
                }
                catch (Exception ex)
                {
                    Log.Debug(ex, "Failed to delete old snapshot '{File}'", f.FullName);
                }
            }
            return count;
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Failed to prune snapshots in '{Dir}'", directory);
            return 0;
        }
    }
}
