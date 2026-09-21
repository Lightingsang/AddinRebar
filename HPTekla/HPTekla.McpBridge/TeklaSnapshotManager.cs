using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Serilog;
using Tekla.Structures.Model;

namespace HPTekla.McpBridge;

/// <summary>
///     Manages automatic pre-mutation snapshot backups of Tekla Structures models
///     before Tier W (Write) or Tier D (Destructive/Heavy) operations.
///     Copies core database files (.db1, .db2, environment.db) into a timestamped
///     subfolder inside `.hptekla_snapshots` (or %TEMP%\.hptekla_snapshots if read-only),
///     and prunes older snapshots to retain the newest 20.
/// </summary>
public sealed class TeklaSnapshotManager
{
    public const int DefaultMaxRetained = 20;
    public const string SnapshotFolder = ".hptekla_snapshots";
    public const string TimestampFormat = "yyyyMMdd_HHmmss_fff";

    private static readonly Regex SafeNameRegex = new(@"[^A-Za-z0-9_-]", RegexOptions.Compiled);
    private readonly string? _overrideRootDirectory;

    public TeklaSnapshotManager(string? overrideRootDirectory = null)
    {
        _overrideRootDirectory = overrideRootDirectory;
    }

    /// <summary>
    ///     Resolves the parent directory where snapshots for the active model should reside.
    ///     Prefers a local `.hptekla_snapshots` directory inside the model folder;
    ///     falls back to %TEMP%\.hptekla_snapshots if unsaved, read-only, or invalid.
    /// </summary>
    public string ResolveSnapshotDirectory(string? modelPath, string modelName = "Model")
    {
        if (!string.IsNullOrEmpty(_overrideRootDirectory))
        {
            Directory.CreateDirectory(_overrideRootDirectory);
            return _overrideRootDirectory!;
        }

        if (string.IsNullOrWhiteSpace(modelPath) ||
            !Path.IsPathRooted(modelPath!) ||
            modelPath!.StartsWith(@"\\", StringComparison.Ordinal))
        {
            var tempDir = Path.Combine(Path.GetTempPath(), SnapshotFolder, Sanitize(modelName));
            Directory.CreateDirectory(tempDir);
            return tempDir;
        }

        try
        {
            if (Directory.Exists(modelPath))
            {
                var localSnapDir = Path.Combine(modelPath, SnapshotFolder);
                Directory.CreateDirectory(localSnapDir);
                return localSnapDir;
            }
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Failed to resolve adjacent snapshot directory for '{Path}'. Falling back to TEMP.", modelPath);
        }

        var fallbackDir = Path.Combine(Path.GetTempPath(), SnapshotFolder, Sanitize(modelName));
        Directory.CreateDirectory(fallbackDir);
        return fallbackDir;
    }

    /// <summary>
    ///     Creates a pre-mutation snapshot backup of the current Tekla model databases (.db1, .db2, etc.).
    ///     Returns the path of the created snapshot folder.
    /// </summary>
    public string CreateSnapshot(Model? model, string? label = null)
    {
        var modelInfo = model != null && model.GetConnectionStatus() ? model.GetInfo() : null;
        var modelPath = modelInfo?.ModelPath;
        var modelName = !string.IsNullOrWhiteSpace(modelInfo?.ModelName) ? modelInfo!.ModelName : "Model";

        var rootSnapshotDir = ResolveSnapshotDirectory(modelPath, modelName);
        var timestamp = DateTime.Now.ToString(TimestampFormat);
        var safeLabel = !string.IsNullOrWhiteSpace(label) ? "_" + Sanitize(label!) : string.Empty;
        var snapshotSubDir = Path.Combine(rootSnapshotDir, $"{timestamp}{safeLabel}");

        Directory.CreateDirectory(snapshotSubDir);

        if (!string.IsNullOrWhiteSpace(modelPath) && Directory.Exists(modelPath))
        {
            try
            {
                // Core Tekla database files to back up
                var extensionsToCopy = new[] { "*.db1", "*.db2", "environment.db", "options_model.db" };
                foreach (var pattern in extensionsToCopy)
                {
                    foreach (var sourceFile in Directory.EnumerateFiles(modelPath, pattern, SearchOption.TopDirectoryOnly))
                    {
                        var fileName = Path.GetFileName(sourceFile);
                        var destFile = Path.Combine(snapshotSubDir, fileName);
                        CopyFileShared(sourceFile, destFile);
                    }
                }

                PruneOldSnapshots(rootSnapshotDir, DefaultMaxRetained);
                Log.Information("Tekla model snapshot created at '{SnapshotPath}'", snapshotSubDir);
                return snapshotSubDir;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to copy database files to snapshot directory '{SnapshotPath}'", snapshotSubDir);
            }
        }

        return snapshotSubDir;
    }

    /// <summary>
    ///     Copies a file open by Tekla with FileShare.ReadWrite without blocking or failing on lock.
    /// </summary>
    private static void CopyFileShared(string sourceFile, string destinationFile)
    {
        try
        {
            using var sourceStream = new FileStream(sourceFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var destStream = new FileStream(destinationFile, FileMode.Create, FileAccess.Write, FileShare.None);
            sourceStream.CopyTo(destStream);
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Shared copy failed for '{Source}'. Attempting standard File.Copy.", sourceFile);
            File.Copy(sourceFile, destinationFile, overwrite: true);
        }
    }

    /// <summary>
    ///     Prunes snapshot folders beyond maxRetained count.
    /// </summary>
    public static void PruneOldSnapshots(string rootDir, int maxRetained)
    {
        try
        {
            if (!Directory.Exists(rootDir)) return;

            var snapshotDirs = new DirectoryInfo(rootDir)
                .GetDirectories()
                .OrderByDescending(d => d.CreationTimeUtc)
                .ToList();

            if (snapshotDirs.Count > maxRetained)
            {
                foreach (var dir in snapshotDirs.Skip(maxRetained))
                {
                    try
                    {
                        dir.Delete(recursive: true);
                    }
                    catch (Exception ex)
                    {
                        Log.Warning(ex, "Failed to prune snapshot directory '{Dir}'", dir.FullName);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Failed to prune snapshots in '{Dir}'", rootDir);
        }
    }

    public static string Sanitize(string name)
    {
        return SafeNameRegex.Replace(name, "_");
    }
}
