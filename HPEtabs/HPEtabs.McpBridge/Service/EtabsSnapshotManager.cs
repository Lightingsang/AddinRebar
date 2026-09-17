using System.IO;
using System.Text.RegularExpressions;
using HPRebar.Mcp.Contracts.JsonRpc;
using HPRebar.McpBridge.Core.Pipe;
using Serilog;

namespace HPEtabs.McpBridge.Service;

/// <summary>What the snapshot step produced: the prerun copy's file name, a presave copy when the user's own save was preserved, or why it stopped.</summary>
public sealed record SnapshotOutcome(string? FileName, string? PresaveFileName, string? Failure, bool TimedOut)
{
    public bool Succeeded => FileName is not null;
}

/// <summary>
///     The only safety net a writing script gets, since ETABS has no transaction: before the script runs the bridge
///     saves the model with <c>File.Save()</c> and copies the <c>.EDB</c> into
///     <c>%LocalAppData%\HPEtabs\McpBridge\snapshots\&lt;model&gt;\prerun\</c>. The forced save overwrites the
///     user's file with whatever is in memory, so when the file on disk was last written by someone other than
///     this bridge (the user pressed Save, or nothing was saved yet) a copy of it goes to <c>presave\</c> first.
///     Buckets are pruned to the newest 5 (presave) / 10 (prerun). Everything runs inside the request's timeout:
///     a save that outlasts the budget fails the run before the script starts. Only the file name leaves the
///     bridge — the folder carries the user name and is shown in the window instead.
/// </summary>
public sealed class EtabsSnapshotManager
{
    public const int PresaveKeep = 5;
    public const int PrerunKeep = 10;
    public const string TimestampFormat = "yyyyMMdd-HHmmss";

    private static readonly Regex LabelCleaner = new("[^A-Za-z0-9_-]", RegexOptions.Compiled);

    private readonly Func<DateTime> _now;
    private readonly Dictionary<string, (DateTime mtime, long size)> _lastWritten = new(StringComparer.OrdinalIgnoreCase);
    private readonly object _gate = new();

    public EtabsSnapshotManager(string rootDirectory, Func<DateTime>? now = null)
    {
        RootDirectory = rootDirectory;
        _now = now ?? (() => DateTime.Now);
    }

    public string RootDirectory { get; }

    /// <summary>
    ///     The model must be a local, existing `.EDB`; otherwise the request is refused with the no-document code — an
    ///     environment state, not a tool failure, so it never counts against a stored tool.
    /// </summary>
    public static void EnsureSnapshotable(string? modelPath)
    {
        if (string.IsNullOrWhiteSpace(modelPath) || !Path.IsPathRooted(modelPath))
            throw new BridgeRequestException(BridgeErrorCode.NoActiveDocument, "No model (.EDB) with a file path is open in ETABS — save the model in ETABS first; writing scripts need a file to snapshot.");
        if (modelPath.StartsWith(@"\\", StringComparison.Ordinal))
            throw new BridgeRequestException(BridgeErrorCode.NoActiveDocument, "The model is on a UNC share — copy it to a local folder and open that copy first; the bridge does not snapshot over the network.");
        if (!File.Exists(modelPath))
            throw new BridgeRequestException(BridgeErrorCode.NoActiveDocument, "The model's .EDB file does not exist on disk yet — save the model in ETABS first.");
    }

    /// <summary>Folder for one model's snapshots: `<root>\<model name>\`.</summary>
    public string ModelDirectory(string modelPath) => Path.Combine(RootDirectory, SanitizeLabel(Path.GetFileNameWithoutExtension(modelPath), 60));

    /// <summary>Label as it appears in a snapshot file name: `[A-Za-z0-9_-]{1,40}`, never a path element.</summary>
    public static string SanitizeLabel(string? label, int maxLength = 40)
    {
        var cleaned = LabelCleaner.Replace(label ?? string.Empty, "_").Trim('_', '-');
        if (cleaned.Length == 0) cleaned = "script";
        return cleaned.Length > maxLength ? cleaned[..maxLength] : cleaned;
    }

    /// <summary>
    ///     presave? → save → prerun copy, each step checked against the budget. <paramref name="save"/> is the
    ///     OAPI call (`sapModel.File.Save()`), injected so the sequence is testable without ETABS.
    /// </summary>
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
                presave = Copy(modelPath, Path.Combine(directory, "presave"), $"{stamp}-{name}-presave.EDB");
                Prune(Path.Combine(directory, "presave"), PresaveKeep);
            }

            var ret = save();
            if (ret != 0) return new SnapshotOutcome(null, presave, $"ETABS returned {ret} from File.Save — snapshot impossible, nothing ran.", false);
            Remember(modelPath);
            if (budget.IsCancellationRequested) return TimedOut("while saving the model for the snapshot", presave);

            var prerun = Copy(modelPath, Path.Combine(directory, "prerun"), $"{stamp}-{name}.EDB");
            Prune(Path.Combine(directory, "prerun"), PrerunKeep);
            if (budget.IsCancellationRequested) return TimedOut("while copying the snapshot", presave);

            Log.Information("MCP snapshot {File} taken for '{Label}'{Presave}", prerun, name, presave is null ? "" : " (presave " + presave + ")");
            return new SnapshotOutcome(prerun, presave, null, false);
        }
    }

    /// <summary>True when the file on disk still is the one this bridge wrote last (same mtime and size).</summary>
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

    /// <summary>Copies into the bucket, never outside it (the label is sanitized, and the destination is asserted under the bucket).</summary>
    private static string Copy(string source, string bucket, string fileName)
    {
        Directory.CreateDirectory(bucket);
        var destination = Path.Combine(bucket, fileName);
        var fullBucket = Path.GetFullPath(bucket).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        if (!Path.GetFullPath(destination).StartsWith(fullBucket, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("snapshot destination escaped its bucket: " + fileName);

        // A second run inside the same second keeps both copies.
        var attempt = 1;
        while (File.Exists(destination))
            destination = Path.Combine(bucket, Path.GetFileNameWithoutExtension(fileName) + "-" + ++attempt + ".EDB");

        File.Copy(source, destination);
        return Path.GetFileName(destination);
    }

    private static void Prune(string bucket, int keep)
    {
        if (!Directory.Exists(bucket)) return;
        // By name: the timestamp prefix is when the copy was taken; File.Copy keeps the source's mtime, which may be older than an earlier copy.
        var stale = new DirectoryInfo(bucket).GetFiles("*.EDB").OrderByDescending(f => f.Name, StringComparer.OrdinalIgnoreCase).Skip(keep);
        foreach (var file in stale)
        {
            try { file.Delete(); }
            catch (IOException exception) { Log.Warning(exception, "MCP snapshot prune could not delete {File}", file.Name); }
        }
    }
}
