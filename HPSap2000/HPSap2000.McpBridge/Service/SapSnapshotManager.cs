using System.IO;
using System.Text.RegularExpressions;
using HPRebar.Mcp.Contracts.JsonRpc;
using HPRebar.McpBridge.Core.Pipe;
using Serilog;

namespace HPSap2000.McpBridge.Service;

/// <summary>What the snapshot step produced.</summary>
public sealed record SnapshotOutcome(string? FileName, string? PresaveFileName, string? Failure, bool TimedOut)
{
    public bool Succeeded => FileName is not null;
}

/// <summary>
///     Safety net for writing scripts in SAP2000: saves the model and copies `.SDB` into
///     `%LocalAppData%\HPSap2000\McpBridge\snapshots\<model>\prerun\`.
/// </summary>
public sealed class SapSnapshotManager
{
    public const int PresaveKeep = 5;
    public const int PrerunKeep = 10;
    public const string TimestampFormat = "yyyyMMdd-HHmmss";

    private static readonly Regex LabelCleaner = new("[^A-Za-z0-9_-]", RegexOptions.Compiled);

    private readonly Func<DateTime> _now;
    private readonly Dictionary<string, (DateTime mtime, long size)> _lastWritten = new(StringComparer.OrdinalIgnoreCase);
    private readonly object _gate = new();

    public SapSnapshotManager(string rootDirectory, Func<DateTime>? now = null)
    {
        RootDirectory = rootDirectory;
        _now = now ?? (() => DateTime.Now);
    }

    public string RootDirectory { get; }

    /// <summary>
    ///     The model must be a local, existing `.SDB`.
    /// </summary>
    public static void EnsureSnapshotable(string? modelPath)
    {
        if (string.IsNullOrWhiteSpace(modelPath) || !Path.IsPathRooted(modelPath))
            throw new BridgeRequestException(BridgeErrorCode.NoActiveDocument, "No model (.SDB) with a file path is open in SAP2000 — save the model in SAP2000 first; writing scripts need a file to snapshot.");
        if (modelPath.StartsWith(@"\\", StringComparison.Ordinal))
            throw new BridgeRequestException(BridgeErrorCode.NoActiveDocument, "The model is on a UNC share — copy it to a local folder and open that copy first; the bridge does not snapshot over the network.");
        if (!File.Exists(modelPath))
            throw new BridgeRequestException(BridgeErrorCode.NoActiveDocument, "The model's .SDB file does not exist on disk yet — save the model in SAP2000 first.");
    }

    /// <summary>Folder for one model's snapshots: `<root>\<model name>\`.</summary>
    public string ModelDirectory(string modelPath) => Path.Combine(RootDirectory, SanitizeLabel(Path.GetFileNameWithoutExtension(modelPath), 60));

    /// <summary>Label as it appears in a snapshot file name: `[A-Za-z0-9_-]{1,40}`.</summary>
    public static string SanitizeLabel(string? label, int maxLength = 40)
    {
        var cleaned = LabelCleaner.Replace(label ?? string.Empty, "_").Trim('_', '-');
        if (cleaned.Length == 0) cleaned = "script";
        return cleaned.Length > maxLength ? cleaned[..maxLength] : cleaned;
    }

    public SnapshotOutcome Prepare(string modelPath, string? label, Func<int> save, CancellationToken budget)
    {
        EnsureSnapshotable(modelPath);
        var directory = ModelDirectory(modelPath);
        var stamp = _now().ToString(TimestampFormat);
        var name = SanitizeLabel(label);

        lock (_gate)
        {
            if (budget.IsCancellationRequested) return TimedOut("before the pre-run save");

            string? presave = null;
            if (!WrittenByBridge(modelPath))
            {
                presave = Copy(modelPath, Path.Combine(directory, "presave"), $"{stamp}-{name}-presave.SDB");
                Prune(Path.Combine(directory, "presave"), PresaveKeep);
            }

            var ret = save();
            if (ret != 0) return new SnapshotOutcome(null, presave, $"SAP2000 returned {ret} from File.Save — snapshot impossible, nothing ran.", false);
            Remember(modelPath);
            if (budget.IsCancellationRequested) return TimedOut("while saving the model for the snapshot", presave);

            var prerun = Copy(modelPath, Path.Combine(directory, "prerun"), $"{stamp}-{name}.SDB");
            Prune(Path.Combine(directory, "prerun"), PrerunKeep);
            if (budget.IsCancellationRequested) return TimedOut("while copying the snapshot", presave);

            Log.Information("MCP snapshot {File} taken for '{Label}'{Presave}", prerun, name, presave is null ? "" : " (presave " + presave + ")");
            return new SnapshotOutcome(prerun, presave, null, false);
        }
    }

    public bool WrittenByBridge(string modelPath)
    {
        lock (_gate)
        {
            if (!_lastWritten.TryGetValue(modelPath, out var last)) return false;
            var info = new FileInfo(modelPath);
            return info.Exists && info.LastWriteTimeUtc == last.mtime && info.Length == last.size;
        }
    }

    private void Remember(string modelPath)
    {
        var info = new FileInfo(modelPath);
        info.Refresh();
        _lastWritten[modelPath] = (info.LastWriteTimeUtc, info.Length);
    }

    private static SnapshotOutcome TimedOut(string when, string? presave = null) =>
        new(null, presave, $"Timed out {when}; the script did not run. Raise timeoutSeconds — the save counts against it.", true);

    private static string Copy(string source, string bucket, string fileName)
    {
        Directory.CreateDirectory(bucket);
        var destination = Path.Combine(bucket, fileName);
        var fullBucket = Path.GetFullPath(bucket).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        if (!Path.GetFullPath(destination).StartsWith(fullBucket, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("snapshot destination escaped its bucket: " + fileName);

        var attempt = 1;
        while (File.Exists(destination))
            destination = Path.Combine(bucket, Path.GetFileNameWithoutExtension(fileName) + "-" + ++attempt + ".SDB");

        File.Copy(source, destination);
        return Path.GetFileName(destination);
    }

    private static void Prune(string bucket, int keep)
    {
        if (!Directory.Exists(bucket)) return;
        var stale = new DirectoryInfo(bucket).GetFiles("*.SDB").OrderByDescending(f => f.Name, StringComparer.OrdinalIgnoreCase).Skip(keep);
        foreach (var file in stale)
        {
            try { file.Delete(); }
            catch (IOException exception) { Log.Warning(exception, "MCP snapshot prune could not delete {File}", file.Name); }
        }
    }
}
