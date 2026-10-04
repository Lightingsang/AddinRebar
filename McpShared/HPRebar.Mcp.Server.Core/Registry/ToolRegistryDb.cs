using HPRebar.Mcp.Server.Registry.Model;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;

namespace HPRebar.Mcp.Server.Registry;

/// <summary>
///     SQLite index of the library plus the run history. Everything here can be rebuilt from the files
///     except <c>runs</c> and <c>registry_events</c>, which are the tool memory proper. Search uses FTS5
///     when the bundled SQLite has it and a token-count fallback otherwise, so the server never depends
///     on a compile option it cannot see at build time. WAL + busy timeout let several server processes
///     (several AI sessions) share one file.
/// </summary>
public sealed partial class ToolRegistryDb
{
    private readonly string _connectionString;
    private readonly ILogger<ToolRegistryDb> _logger;
    private bool _fts;

    public ToolRegistryDb(string path, ILogger<ToolRegistryDb> logger)
    {
        Path = System.IO.Path.GetFullPath(path);
        _logger = logger;
        _connectionString = new SqliteConnectionStringBuilder { DataSource = Path, Mode = SqliteOpenMode.ReadWriteCreate, Cache = SqliteCacheMode.Shared }.ToString();
    }

    public string Path { get; }

    public bool HasFullTextSearch => _fts;

    public void Initialize()
    {
        Directory.CreateDirectory(System.IO.Path.GetDirectoryName(Path)!);
        using var connection = Open();

        Exec(connection, """
            CREATE TABLE IF NOT EXISTS tools (
                name TEXT PRIMARY KEY, category TEXT NOT NULL, title TEXT, description TEXT NOT NULL, tags TEXT,
                status TEXT NOT NULL, version INTEGER NOT NULL, checksum TEXT NOT NULL, folder TEXT,
                record_json TEXT NOT NULL, examples_text TEXT, updated_at INTEGER NOT NULL);
            CREATE TABLE IF NOT EXISTS tool_versions (
                name TEXT NOT NULL, version INTEGER NOT NULL, code_sha TEXT NOT NULL, input_schema TEXT, code TEXT NOT NULL,
                created_at INTEGER NOT NULL, PRIMARY KEY (name, version));
            CREATE TABLE IF NOT EXISTS runs (
                id INTEGER PRIMARY KEY AUTOINCREMENT, tool_name TEXT, version INTEGER, kind TEXT NOT NULL,
                args_sha TEXT, code_sha TEXT, dry_run INTEGER NOT NULL, success INTEGER NOT NULL, duration_ms INTEGER,
                error TEXT, revit_version TEXT, doc_title TEXT, ts INTEGER NOT NULL, code TEXT, args_json TEXT);
            CREATE INDEX IF NOT EXISTS idx_runs_tool_ts ON runs (tool_name, ts DESC);
            CREATE INDEX IF NOT EXISTS idx_runs_kind_ts ON runs (kind, ts DESC);
            CREATE TABLE IF NOT EXISTS registry_events (
                id INTEGER PRIMARY KEY AUTOINCREMENT, ts INTEGER NOT NULL, tool_name TEXT, event TEXT NOT NULL, actor TEXT, detail TEXT);
            CREATE INDEX IF NOT EXISTS idx_events_tool_event_ts ON registry_events (tool_name, event, ts DESC);
            """);

        try
        {
            Exec(connection, "CREATE VIRTUAL TABLE IF NOT EXISTS tools_fts USING fts5(name, title, description, tags, examples, tokenize='unicode61');");
            _fts = true;
        }
        catch (SqliteException exception)
        {
            _fts = false;
            _logger.LogWarning("SQLite has no FTS5 ({Message}); search falls back to token matching", exception.Message);
        }
    }

    // ---- tools index -----------------------------------------------------------------------------

    /// <summary>Status the index last saw for a tool, or null when it is new — lets a reload notice a hand edit.</summary>
    public ToolStatus? StoredStatus(string name)
    {
        using var connection = Open();
        var rows = Query(connection, "SELECT status FROM tools WHERE name = @name;", r => r.GetString(0), ("@name", name));
        return rows.Count == 0 ? null : ParseStatus(rows[0]);
    }

    public void UpsertTool(ToolRecord record)
    {
        using var connection = Open();
        using var transaction = connection.BeginTransaction();

        var examplesText = string.Join(" ", record.Examples.Select(e => e.Title));
        using (var command = connection.CreateCommand())
        {
            command.CommandText = """
                INSERT INTO tools (name, category, title, description, tags, status, version, checksum, folder, record_json, examples_text, updated_at)
                VALUES (@name, @category, @title, @description, @tags, @status, @version, @checksum, @folder, @record, @examples, @updated)
                ON CONFLICT(name) DO UPDATE SET category = excluded.category, title = excluded.title, description = excluded.description,
                    tags = excluded.tags, status = excluded.status, version = excluded.version, checksum = excluded.checksum,
                    folder = excluded.folder, record_json = excluded.record_json, examples_text = excluded.examples_text, updated_at = excluded.updated_at;
                """;
            command.Parameters.AddWithValue("@name", record.Name);
            command.Parameters.AddWithValue("@category", record.Category);
            command.Parameters.AddWithValue("@title", (object?)record.Title ?? DBNull.Value);
            command.Parameters.AddWithValue("@description", record.Description);
            command.Parameters.AddWithValue("@tags", string.Join(" ", record.Tags));
            command.Parameters.AddWithValue("@status", StatusText(record.Status));
            command.Parameters.AddWithValue("@version", record.Version);
            command.Parameters.AddWithValue("@checksum", record.Checksum);
            command.Parameters.AddWithValue("@folder", (object?)record.Folder ?? DBNull.Value);
            command.Parameters.AddWithValue("@record", RegistryJson.Serialize(record));
            command.Parameters.AddWithValue("@examples", examplesText);
            command.Parameters.AddWithValue("@updated", Now());
            command.ExecuteNonQuery();
        }

        if (_fts)
        {
            Exec(connection, "DELETE FROM tools_fts WHERE name = @name;", ("@name", record.Name));
            Exec(connection, "INSERT INTO tools_fts (name, title, description, tags, examples) VALUES (@name, @title, @description, @tags, @examples);",
                ("@name", record.Name), ("@title", record.Title ?? record.Name.Replace('_', ' ')), ("@description", record.Description),
                ("@tags", string.Join(" ", record.Tags)), ("@examples", examplesText));
        }

        transaction.Commit();
    }

    /// <summary>Drops index rows whose folder vanished from the library. Runs and events are kept.</summary>
    public int RemoveToolsNotIn(IEnumerable<string> names)
    {
        var keep = names.ToHashSet(StringComparer.OrdinalIgnoreCase);
        using var connection = Open();
        var stale = Query(connection, "SELECT name FROM tools;", r => r.GetString(0)).Where(n => !keep.Contains(n)).ToList();
        foreach (var name in stale)
        {
            Exec(connection, "DELETE FROM tools WHERE name = @name;", ("@name", name));
            if (_fts) Exec(connection, "DELETE FROM tools_fts WHERE name = @name;", ("@name", name));
        }

        return stale.Count;
    }

    public IReadOnlyList<string> ToolNames()
    {
        using var connection = Open();
        return Query(connection, "SELECT name FROM tools ORDER BY name;", r => r.GetString(0));
    }

    /// <summary>Full-text (or token) match → (name, textScore ≥ 0). Status/stability weighting happens in the manager.</summary>
    public IReadOnlyList<(string Name, double Score)> Search(string query, int limit)
    {
        var tokens = Tokenize(query);
        if (tokens.Count == 0) return [];
        using var connection = Open();

        if (_fts)
        {
            var match = string.Join(" OR ", tokens.Select(t => $"\"{t}\"*"));
            try
            {
                return Query(connection, "SELECT name, bm25(tools_fts) FROM tools_fts WHERE tools_fts MATCH @q ORDER BY bm25(tools_fts) LIMIT @n;",
                    r => (r.GetString(0), -r.GetDouble(1)), ("@q", match), ("@n", limit));
            }
            catch (SqliteException exception)
            {
                _logger.LogDebug(exception, "FTS query failed for {Query}; falling back to token matching", query);
            }
        }

        var rows = Query(connection, "SELECT name, title, description, tags, examples_text FROM tools;",
            r => (Name: r.GetString(0), Text: string.Join(" ", r.GetString(0).Replace('_', ' '), r.IsDBNull(1) ? "" : r.GetString(1), r.GetString(2), r.IsDBNull(3) ? "" : r.GetString(3), r.IsDBNull(4) ? "" : r.GetString(4)).ToLowerInvariant()));

        return rows
            .Select(row => (row.Name, Score: (double)tokens.Count(t => row.Text.Contains(t, StringComparison.OrdinalIgnoreCase))))
            .Where(x => x.Score > 0)
            .OrderByDescending(x => x.Score)
            .Take(limit)
            .ToList();
    }

    // ---- versions --------------------------------------------------------------------------------

    public void SaveVersion(ToolRecord record)
    {
        using var connection = Open();
        Exec(connection, """
            INSERT INTO tool_versions (name, version, code_sha, input_schema, code, created_at) VALUES (@name, @version, @sha, @schema, @code, @ts)
            ON CONFLICT(name, version) DO UPDATE SET code_sha = excluded.code_sha, input_schema = excluded.input_schema, code = excluded.code;
            """,
            ("@name", record.Name), ("@version", record.Version), ("@sha", RegistryJson.Sha256(record.Code)),
            ("@schema", RegistryJson.Canonical(record.InputSchema)), ("@code", record.Code), ("@ts", Now()));
    }

    // ---- events ----------------------------------------------------------------------------------

    public void InsertEvent(string? toolName, string @event, string? actor, string? detail)
    {
        using var connection = Open();
        Exec(connection, "INSERT INTO registry_events (ts, tool_name, event, actor, detail) VALUES (@ts, @tool, @event, @actor, @detail);",
            ("@ts", Now()), ("@tool", toolName), ("@event", @event), ("@actor", actor), ("@detail", detail));
    }

    /// <summary>The detail of the newest <paramref name="event"/> recorded for a tool, or null when there is none.</summary>
    public string? LatestEventDetail(string toolName, string @event)
    {
        using var connection = Open();
        return Query(connection, "SELECT detail FROM registry_events WHERE tool_name = @tool AND event = @event ORDER BY rowid DESC LIMIT 1;",
            reader => reader.IsDBNull(0) ? null : reader.GetString(0), ("@tool", toolName), ("@event", @event)).FirstOrDefault();
    }

    // ---- internals -------------------------------------------------------------------------------

    private SqliteConnection Open()
    {
        var connection = new SqliteConnection(_connectionString);
        connection.Open();
        using var pragma = connection.CreateCommand();
        pragma.CommandText = "PRAGMA journal_mode = WAL; PRAGMA busy_timeout = 5000; PRAGMA foreign_keys = ON;";
        pragma.ExecuteNonQuery();
        return connection;
    }

    private static void Exec(SqliteConnection connection, string sql, params (string Name, object? Value)[] parameters)
    {
        using var command = connection.CreateCommand();
        command.CommandText = sql;
        foreach (var (name, value) in parameters) command.Parameters.AddWithValue(name, value ?? DBNull.Value);
        command.ExecuteNonQuery();
    }

    private static List<T> Query<T>(SqliteConnection connection, string sql, Func<SqliteDataReader, T> map, params (string Name, object? Value)[] parameters)
    {
        using var command = connection.CreateCommand();
        command.CommandText = sql;
        foreach (var (name, value) in parameters) command.Parameters.AddWithValue(name, value ?? DBNull.Value);
        using var reader = command.ExecuteReader();
        var list = new List<T>();
        while (reader.Read()) list.Add(map(reader));
        return list;
    }

    private static long Now() => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

    /// <summary>Inverse of <see cref="StatusText"/>; unknown text reads as draft (never runnable as published by accident).</summary>
    public static ToolStatus ParseStatus(string text) =>
        text == "pending_approval" ? ToolStatus.PendingApproval : Enum.TryParse<ToolStatus>(text, true, out var status) ? status : ToolStatus.Draft;

    public static string StatusText(ToolStatus status) => status switch
    {
        ToolStatus.PendingApproval => "pending_approval",
        _ => status.ToString().ToLowerInvariant(),
    };

    /// <summary>Letters/digits runs of ≥ 2 chars, lower-cased; punctuation and FTS operators never reach the query.</summary>
    public static IReadOnlyList<string> Tokenize(string query)
    {
        var tokens = new List<string>();
        var current = new System.Text.StringBuilder();
        foreach (var ch in (query ?? string.Empty).ToLowerInvariant())
        {
            if (char.IsLetterOrDigit(ch)) current.Append(ch);
            else if (current.Length > 0) { if (current.Length >= 2) tokens.Add(current.ToString()); current.Clear(); }
        }
        if (current.Length >= 2) tokens.Add(current.ToString());
        return tokens.Distinct().Take(12).ToList();
    }
}
