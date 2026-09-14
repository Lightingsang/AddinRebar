using System.Text;
using HPRebar.Mcp.Server.Registry.Model;
using Microsoft.Extensions.Logging;

namespace HPRebar.Mcp.Server.Registry;

/// <summary>
///     The tools library on disk: one folder per tool under its category, three files each. Humans edit
///     these files (approve, fix code, retire) and so does the server (publish, new version); a watcher
///     turns either into a reload so a running server never needs restarting. Writes are atomic
///     (temp file + move) because several server processes may share one library.
/// </summary>
public sealed class ToolLibraryStore : IDisposable
{
    public const string ToolFile = "tool.json";
    public const string CodeFile = "code.cs";
    public const string ExamplesFile = "examples.json";

    private static readonly TimeSpan Debounce = TimeSpan.FromMilliseconds(400);

    private readonly ILogger<ToolLibraryStore> _logger;
    private FileSystemWatcher? _watcher;
    private Timer? _debounce;
    private int _suppress;

    public ToolLibraryStore(string root, ILogger<ToolLibraryStore> logger)
    {
        Root = Path.GetFullPath(root);
        _logger = logger;
    }

    public string Root { get; }

    /// <summary>Raised (debounced, on a thread-pool thread) after files under <see cref="Root"/> changed and the change was not one of our own writes.</summary>
    public event Action? Changed;

    public void EnsureRoot() => Directory.CreateDirectory(Root);

    public string FolderFor(string category, string name) => Path.Combine(Root, Sanitize(category), Sanitize(name));

    /// <summary>Folder of an existing tool, whatever its category, or null.</summary>
    public string? FindFolder(string name)
    {
        if (!Directory.Exists(Root)) return null;
        var safe = Sanitize(name);
        return Directory.EnumerateDirectories(Root)
            .Select(category => Path.Combine(category, safe))
            .FirstOrDefault(folder => File.Exists(Path.Combine(folder, ToolFile)));
    }

    public bool Exists(string name) => FindFolder(name) is not null;

    /// <summary>Every readable tool; unreadable folders are logged and skipped so one bad file cannot take the library down.</summary>
    public IReadOnlyList<ToolRecord> ReadAll()
    {
        if (!Directory.Exists(Root)) return [];

        var records = new List<ToolRecord>();
        foreach (var category in Directory.EnumerateDirectories(Root))
        {
            if (Path.GetFileName(category).StartsWith('_')) continue; // _review, _pending: not tools
            foreach (var folder in Directory.EnumerateDirectories(category))
            {
                var record = TryRead(folder);
                if (record is not null) records.Add(record);
            }
        }

        return records;
    }

    public ToolRecord? TryRead(string folder)
    {
        var toolPath = Path.Combine(folder, ToolFile);
        if (!File.Exists(toolPath)) return null;

        try
        {
            var toolJson = ReadWithRetry(toolPath);
            var code = ReadWithRetry(Path.Combine(folder, CodeFile));
            var examplesPath = Path.Combine(folder, ExamplesFile);
            var examplesJson = File.Exists(examplesPath) ? ReadWithRetry(examplesPath) : "[]";

            var record = RegistryJson.Deserialize<ToolRecord>(toolJson);
            if (record is null || string.IsNullOrWhiteSpace(record.Name))
            {
                _logger.LogWarning("Tool folder {Folder}: tool.json has no name; skipped", folder);
                return null;
            }

            record.Code = code;
            record.Examples = RegistryJson.Deserialize<List<ToolExample>>(examplesJson) ?? [];
            record.Folder = folder;
            record.Checksum = RegistryJson.Sha256(toolJson + "\n" + code + "\n" + examplesJson);
            return record;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or System.Text.Json.JsonException)
        {
            _logger.LogWarning(exception, "Tool folder {Folder} could not be read; skipped", folder);
            return null;
        }
    }

    /// <summary>Writes all three files; a tool that moved category is removed from its old folder.</summary>
    public string Write(ToolRecord record)
    {
        if (string.IsNullOrWhiteSpace(record.Name)) throw new ArgumentException("Tool name is required.", nameof(record));

        var folder = FolderFor(record.Category, record.Name);
        var previous = FindFolder(record.Name);

        Interlocked.Increment(ref _suppress);
        try
        {
            Directory.CreateDirectory(folder);
            record.UpdatedAt = DateTimeOffset.UtcNow;
            record.CreatedAt ??= record.UpdatedAt;

            var toolJson = RegistryJson.Serialize(record) + "\n";
            var code = record.Code.TrimEnd('\r', '\n') + "\n";
            var examplesJson = RegistryJson.Serialize(record.Examples) + "\n";

            WriteAtomic(Path.Combine(folder, ToolFile), toolJson);
            WriteAtomic(Path.Combine(folder, CodeFile), code);
            WriteAtomic(Path.Combine(folder, ExamplesFile), examplesJson);

            if (previous is not null && !string.Equals(Path.GetFullPath(previous), Path.GetFullPath(folder), StringComparison.OrdinalIgnoreCase))
                Directory.Delete(previous, recursive: true);

            record.Folder = folder;
            record.Checksum = RegistryJson.Sha256(toolJson + "\n" + code + "\n" + examplesJson);
            return folder;
        }
        finally
        {
            Interlocked.Decrement(ref _suppress);
        }
    }

    /// <summary>Writes a loose file beside the tools (review notes, pending lists) without triggering a reload.</summary>
    public string WriteAux(string relativePath, string content)
    {
        var path = Path.Combine(Root, relativePath);
        Interlocked.Increment(ref _suppress);
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            WriteAtomic(path, content);
            return path;
        }
        finally
        {
            Interlocked.Decrement(ref _suppress);
        }
    }

    public void StartWatching()
    {
        if (_watcher is not null) return;
        EnsureRoot();

        _watcher = new FileSystemWatcher(Root)
        {
            IncludeSubdirectories = true,
            NotifyFilter = NotifyFilters.FileName | NotifyFilters.DirectoryName | NotifyFilters.LastWrite | NotifyFilters.Size,
        };
        _watcher.Created += OnFileEvent;
        _watcher.Changed += OnFileEvent;
        _watcher.Deleted += OnFileEvent;
        _watcher.Renamed += OnFileEvent;
        _watcher.Error += (_, e) => _logger.LogWarning(e.GetException(), "Library watcher error; edits on disk may not be picked up until restart");
        _watcher.EnableRaisingEvents = true;
    }

    private void OnFileEvent(object sender, FileSystemEventArgs e)
    {
        if (Volatile.Read(ref _suppress) > 0) return;
        var name = Path.GetFileName(e.FullPath);
        if (name.EndsWith(".tmp", StringComparison.OrdinalIgnoreCase)) return;
        if (name is not (ToolFile or CodeFile or ExamplesFile) && File.Exists(e.FullPath)) return; // only our three files, or folder events

        _debounce ??= new Timer(_ => Changed?.Invoke(), null, Timeout.Infinite, Timeout.Infinite);
        _debounce.Change(Debounce, Timeout.InfiniteTimeSpan);
    }

    private static void WriteAtomic(string path, string content)
    {
        var temp = path + ".tmp";
        File.WriteAllText(temp, content, new UTF8Encoding(false));
        File.Move(temp, path, overwrite: true);
    }

    private static string ReadWithRetry(string path)
    {
        for (var attempt = 0; ; attempt++)
        {
            try
            {
                return File.ReadAllText(path, Encoding.UTF8);
            }
            catch (IOException) when (attempt < 5)
            {
                Thread.Sleep(50); // another process is mid-write; the move is atomic, the wait is short
            }
        }
    }

    /// <summary>Folder names come from tool metadata; keep them portable and inside the root.</summary>
    public static string Sanitize(string segment)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var cleaned = new string(segment.Trim().Select(c => invalid.Contains(c) || c is '/' or '\\' ? '_' : c).ToArray());
        return cleaned is "" or "." or ".." ? "_" : cleaned;
    }

    public void Dispose()
    {
        _watcher?.Dispose();
        _debounce?.Dispose();
    }
}
