using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.AnalysisServices.Tabular;
using Serilog;

namespace HPPowerBi.McpBridge.Safety;

/// <summary>
///     Manages pre-mutation TMSL JSON snapshots of Power BI Tabular databases.
///     Snapshots are stored in %LocalAppData%\HPPowerBi\Snapshots and automatically pruned to the newest 50.
///     Supports rollback / restore in case a mutation produces model or formula errors.
/// </summary>
public sealed class PbiSnapshotManager
{
    public const int DefaultMaxRetained = 50;
    private readonly string _snapshotDirectory;

    public PbiSnapshotManager(string? snapshotDirectory = null)
    {
        _snapshotDirectory = snapshotDirectory ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "HPPowerBi",
            "Snapshots");
    }

    public string SnapshotDirectory => _snapshotDirectory;

    /// <summary>
    ///     Captures a complete TMSL JSON snapshot of the database before applying mutations.
    ///     Returns the filename of the saved snapshot.
    /// </summary>
    public string CreateSnapshot(Database database, string? label = null)
    {
        ArgumentNullException.ThrowIfNull(database);

        Directory.CreateDirectory(_snapshotDirectory);

        var timestamp = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture);
        var safeDbName = SanitizeFileName(database.Name ?? "Model");
        var safeLabel = !string.IsNullOrWhiteSpace(label) ? "_" + SanitizeFileName(label) : string.Empty;
        var fileName = $"Snapshot_{safeDbName}_{timestamp}{safeLabel}.json";
        var fullPath = Path.Combine(_snapshotDirectory, fileName);

        var json = Microsoft.AnalysisServices.Tabular.JsonSerializer.SerializeDatabase(database, new SerializeOptions
        {
            IncludeRestrictedInformation = false,
        });

        File.WriteAllText(fullPath, json, Encoding.UTF8);
        Log.Information("Saved pre-mutation model snapshot: '{FileName}' ({Bytes} bytes)", fileName, json.Length);

        PruneOldSnapshots(DefaultMaxRetained);

        return fileName;
    }

    /// <summary>
    ///     Reads and deserializes a previously captured snapshot into a Database object.
    /// </summary>
    public Database RestoreSnapshot(string snapshotFileNameOrPath)
    {
        var fullPath = File.Exists(snapshotFileNameOrPath)
            ? snapshotFileNameOrPath
            : Path.Combine(_snapshotDirectory, snapshotFileNameOrPath);

        if (!File.Exists(fullPath))
            throw new FileNotFoundException($"Snapshot file not found at '{fullPath}'.", fullPath);

        var json = File.ReadAllText(fullPath, Encoding.UTF8);

        var db = Microsoft.AnalysisServices.Tabular.JsonSerializer.DeserializeDatabase(json);
        Log.Information("Deserialized database '{Db}' from snapshot '{Snapshot}'", db.Name, Path.GetFileName(fullPath));
        return db;
    }

    /// <summary>
    ///     Prunes older snapshots, retaining only the most recent count.
    /// </summary>
    public int PruneOldSnapshots(int maxRetained = DefaultMaxRetained)
    {
        if (!Directory.Exists(_snapshotDirectory))
            return 0;

        try
        {
            var files = Directory.GetFiles(_snapshotDirectory, "Snapshot_*.json")
                .Select(f => new FileInfo(f))
                .OrderByDescending(f => f.CreationTimeUtc)
                .ToList();

            if (files.Count <= maxRetained)
                return 0;

            var toDelete = files.Skip(maxRetained).ToList();
            var deletedCount = 0;

            foreach (var file in toDelete)
            {
                try
                {
                    file.Delete();
                    deletedCount++;
                }
                catch (Exception ex)
                {
                    Log.Debug(ex, "Failed to delete pruned snapshot file '{File}'", file.FullName);
                }
            }

            if (deletedCount > 0)
                Log.Debug("Pruned {Count} old snapshots from '{Dir}'", deletedCount, _snapshotDirectory);

            return deletedCount;
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Failed to prune snapshot directory '{Dir}'", _snapshotDirectory);
            return 0;
        }
    }

    /// <summary>
    ///     Sanitizes a string for safe usage in file paths.
    /// </summary>
    public static string SanitizeFileName(string name)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var chars = name.Select(c => invalid.Contains(c) ? '_' : c).ToArray();
        var result = new string(chars).Trim();
        return string.IsNullOrWhiteSpace(result) ? "snapshot" : result;
    }
}
