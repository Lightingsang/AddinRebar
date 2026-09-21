using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using RobotOM;
using Serilog;

namespace HPRobot.McpBridge.Safety;

/// <summary>
///     Manages automatic pre-mutation snapshot backups of Autodesk Robot (.rtd) models before
///     Tier W (Write) or Tier D (Delete/Heavy) operations.
///     Saves timestamped copies into a local .hprobot_snapshots directory (or %TEMP%\.hprobot_snapshots),
///     and prunes older snapshots to retain the newest 20.
/// </summary>
public sealed class RobotSnapshotManager
{
    public const int DefaultMaxRetained = 20;
    public const string SnapshotFolder = ".hprobot_snapshots";
    public const string TimestampFormat = "yyyyMMdd-HHmmss";

    private static readonly Regex SafeNameRegex = new(@"[^A-Za-z0-9_-]", RegexOptions.Compiled);
    private readonly string? _overrideRootDirectory;

    public RobotSnapshotManager(string? overrideRootDirectory = null)
    {
        _overrideRootDirectory = overrideRootDirectory;
    }

    /// <summary>
    ///     Resolves the directory where snapshots for the specified model should reside.
    ///     Prefers a local `.hprobot_snapshots` directory adjacent to the file;
    ///     falls back to %TEMP%\.hprobot_snapshots if unsaved, read-only, or on a UNC path.
    /// </summary>
    public string ResolveSnapshotDirectory(string? modelPath)
    {
        if (!string.IsNullOrEmpty(_overrideRootDirectory))
        {
            Directory.CreateDirectory(_overrideRootDirectory);
            return _overrideRootDirectory;
        }

        if (string.IsNullOrWhiteSpace(modelPath) ||
            !Path.IsPathRooted(modelPath) ||
            modelPath.StartsWith(@"\\", StringComparison.Ordinal))
        {
            var tempDir = Path.Combine(Path.GetTempPath(), SnapshotFolder);
            Directory.CreateDirectory(tempDir);
            return tempDir;
        }

        try
        {
            var dir = Path.GetDirectoryName(modelPath);
            if (!string.IsNullOrEmpty(dir) && Directory.Exists(dir))
            {
                var localSnapDir = Path.Combine(dir, SnapshotFolder);
                Directory.CreateDirectory(localSnapDir);
                return localSnapDir;
            }
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Failed to resolve adjacent snapshot directory for '{Path}'. Falling back to TEMP.", modelPath);
        }

        var fallbackDir = Path.Combine(Path.GetTempPath(), SnapshotFolder);
        Directory.CreateDirectory(fallbackDir);
        return fallbackDir;
    }

    /// <summary>
    ///     Creates a pre-mutation snapshot backup of the current Robot model.
    ///     Returns the absolute path of the created snapshot file.
    /// </summary>
    public string CreateSnapshot(IRobotApplication? robot, string? modelPath, string? label = null)
    {
        var targetDir = ResolveSnapshotDirectory(modelPath);
        Directory.CreateDirectory(targetDir);

        var timestamp = DateTime.Now.ToString(TimestampFormat);
        string baseName = "RobotModel";

        if (!string.IsNullOrEmpty(modelPath))
        {
            baseName = Path.GetFileNameWithoutExtension(modelPath);
        }

        var safeBaseName = Sanitize(baseName);
        var safeLabel = !string.IsNullOrWhiteSpace(label) ? "_" + Sanitize(label) : string.Empty;
        var snapshotFileName = $"{timestamp}_{safeBaseName}{safeLabel}.rtd";
        var snapshotFullPath = Path.Combine(targetDir, snapshotFileName);

        // 1. If Robot COM application is live
        if (robot != null && robot.Project != null && robot.Project.IsActive != 0)
        {
            try
            {
                if (!string.IsNullOrEmpty(modelPath) && File.Exists(modelPath))
                {
                    // Save active changes to current model file, then copy
                    try { robot.Project.Save(); } catch { }
                    File.Copy(modelPath, snapshotFullPath, overwrite: true);
                    Log.Information("Captured model snapshot via File.Copy: '{Path}'", snapshotFullPath);
                    Prune(targetDir, DefaultMaxRetained);
                    return snapshotFullPath;
                }

                // If unsaved model, call SaveAs
                robot.Project.SaveAs(snapshotFullPath);
                Log.Information("Captured model snapshot via Project.SaveAs: '{Path}'", snapshotFullPath);
                Prune(targetDir, DefaultMaxRetained);
                return snapshotFullPath;
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Robot Project Save/SaveAs failed while taking snapshot.");
            }
        }

        // 2. Headless file-copy fallback
        if (!string.IsNullOrEmpty(modelPath) && File.Exists(modelPath))
        {
            File.Copy(modelPath, snapshotFullPath, overwrite: true);
            Log.Information("Captured file-copy model snapshot: '{Path}'", snapshotFullPath);
            Prune(targetDir, DefaultMaxRetained);
            return snapshotFullPath;
        }

        throw new InvalidOperationException("Cannot capture Robot model snapshot: no active model path and Robot SaveAs unavailable.");
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
                .GetFiles("*.rtd")
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
