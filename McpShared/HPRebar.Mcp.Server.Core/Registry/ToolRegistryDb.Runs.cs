using HPRebar.Mcp.Server.Registry.Model;
using Microsoft.Data.Sqlite;

namespace HPRebar.Mcp.Server.Registry;

/// <summary>
///     The run history half of the index: every tool and ad-hoc run, the stability window that decides
///     quarantine, and the code retention of ad-hoc runs. Schema and shared helpers live in
///     <c>ToolRegistryDb.cs</c>.
/// </summary>
public sealed partial class ToolRegistryDb
{
    // ---- runs ------------------------------------------------------------------------------------

    public long InsertRun(RunRecord run)
    {
        using var connection = Open();
        using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO runs (tool_name, version, kind, args_sha, code_sha, dry_run, success, duration_ms, error, revit_version, doc_title, ts, code, args_json)
            VALUES (@tool, @version, @kind, @args, @code_sha, @dry, @ok, @ms, @error, @revit, @doc, @ts, @code, @args_json);
            SELECT last_insert_rowid();
            """;
        command.Parameters.AddWithValue("@tool", (object?)run.ToolName ?? DBNull.Value);
        command.Parameters.AddWithValue("@version", (object?)run.Version ?? DBNull.Value);
        command.Parameters.AddWithValue("@kind", run.Kind);
        command.Parameters.AddWithValue("@args", (object?)run.ArgsSha ?? DBNull.Value);
        command.Parameters.AddWithValue("@code_sha", (object?)run.CodeSha ?? DBNull.Value);
        command.Parameters.AddWithValue("@dry", run.DryRun ? 1 : 0);
        command.Parameters.AddWithValue("@ok", run.Success ? 1 : 0);
        command.Parameters.AddWithValue("@ms", run.DurationMs);
        command.Parameters.AddWithValue("@error", (object?)run.Error ?? DBNull.Value);
        command.Parameters.AddWithValue("@revit", (object?)run.RevitVersion ?? DBNull.Value);
        command.Parameters.AddWithValue("@doc", (object?)run.DocTitle ?? DBNull.Value);
        command.Parameters.AddWithValue("@ts", run.Timestamp.ToUnixTimeMilliseconds());
        command.Parameters.AddWithValue("@code", (object?)run.Code ?? DBNull.Value);
        command.Parameters.AddWithValue("@args_json", (object?)run.ArgsJson ?? DBNull.Value);
        run.Id = (long)command.ExecuteScalar()!;
        return run.Id;
    }

    public RunRecord? GetRun(long id)
    {
        using var connection = Open();
        return Query(connection, RunSelect + " WHERE id = @id;", ReadRun, ("@id", id)).FirstOrDefault();
    }

    public IReadOnlyList<RunRecord> RecentRuns(string toolName, int limit)
    {
        using var connection = Open();
        return Query(connection, RunSelect + " WHERE tool_name = @tool ORDER BY ts DESC LIMIT @n;", ReadRun, ("@tool", toolName), ("@n", limit));
    }

    /// <summary>
    ///     Runs that count towards a tool's stability: not tests; not refusals of the caller's own arguments (a
    ///     script that throws an ArgumentException — "block 'X' is not defined" — reports a wrong call, not a broken
    ///     tool, and counting those would quarantine every seed that validates its inputs); and nothing older than
    ///     the tool's last approval, restore or new version — otherwise the failures that quarantined a tool would
    ///     quarantine it again on the first run after a human restored it.
    /// </summary>
    private const string StabilityRunFilter =
        "kind <> 'test' AND (error IS NULL OR error NOT LIKE 'Argument%Exception:%') " +
        "AND ts >= COALESCE((SELECT MAX(e.ts) FROM registry_events e WHERE e.tool_name = runs.tool_name AND e.event IN ('approved', 'published', 'restore', 'proposed_version', 'imported', 'status_changed')), 0)";

    public RunStats Stats(string toolName, int window)
    {
        using var connection = Open();
        var rows = Query(connection, $"SELECT success, ts, error FROM runs WHERE tool_name = @tool AND {StabilityRunFilter} ORDER BY ts DESC LIMIT @n;",
            r => (Success: r.GetInt64(0) == 1, Ts: r.GetInt64(1), Error: r.IsDBNull(2) ? null : r.GetString(2)), ("@tool", toolName), ("@n", window));
        if (rows.Count == 0) return RunStats.Empty;
        return new RunStats(rows.Count, rows.Count(r => r.Success), DateTimeOffset.FromUnixTimeMilliseconds(rows[0].Ts), rows.FirstOrDefault(r => !r.Success).Error);
    }

    public IReadOnlyDictionary<string, RunStats> AllStats(int window)
    {
        using var connection = Open();
        var rows = Query(connection, $"""
            SELECT tool_name, success, ts, error FROM (
                SELECT tool_name, success, ts, error, ROW_NUMBER() OVER (PARTITION BY tool_name ORDER BY ts DESC) AS rn
                FROM runs WHERE tool_name IS NOT NULL AND {StabilityRunFilter}) WHERE rn <= @n ORDER BY tool_name, ts DESC;
            """,
            r => (Tool: r.GetString(0), Success: r.GetInt64(1) == 1, Ts: r.GetInt64(2), Error: r.IsDBNull(3) ? null : r.GetString(3)), ("@n", window));

        return rows.GroupBy(r => r.Tool).ToDictionary(
            g => g.Key,
            g => new RunStats(g.Count(), g.Count(r => r.Success), DateTimeOffset.FromUnixTimeMilliseconds(g.First().Ts), g.FirstOrDefault(r => !r.Success).Error),
            StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>Drops the stored code of ad-hoc runs beyond the newest <paramref name="keep"/>; the rows stay for history.</summary>
    public int PruneAdhocCode(int keep)
    {
        using var connection = Open();
        using var command = connection.CreateCommand();
        command.CommandText = "UPDATE runs SET code = NULL WHERE kind = 'adhoc' AND code IS NOT NULL AND id NOT IN (SELECT id FROM runs WHERE kind = 'adhoc' AND code IS NOT NULL ORDER BY ts DESC LIMIT @keep);";
        command.Parameters.AddWithValue("@keep", keep);
        return command.ExecuteNonQuery();
    }

    private const string RunSelect = "SELECT id, tool_name, version, kind, args_sha, code_sha, dry_run, success, duration_ms, error, revit_version, doc_title, ts, code, args_json FROM runs";

    private static RunRecord ReadRun(SqliteDataReader r) => new()
    {
        Id = r.GetInt64(0),
        ToolName = r.IsDBNull(1) ? null : r.GetString(1),
        Version = r.IsDBNull(2) ? null : (int)r.GetInt64(2),
        Kind = r.GetString(3),
        ArgsSha = r.IsDBNull(4) ? null : r.GetString(4),
        CodeSha = r.IsDBNull(5) ? null : r.GetString(5),
        DryRun = r.GetInt64(6) == 1,
        Success = r.GetInt64(7) == 1,
        DurationMs = r.IsDBNull(8) ? 0 : r.GetInt64(8),
        Error = r.IsDBNull(9) ? null : r.GetString(9),
        RevitVersion = r.IsDBNull(10) ? null : r.GetString(10),
        DocTitle = r.IsDBNull(11) ? null : r.GetString(11),
        Timestamp = DateTimeOffset.FromUnixTimeMilliseconds(r.GetInt64(12)),
        Code = r.IsDBNull(13) ? null : r.GetString(13),
        ArgsJson = r.IsDBNull(14) ? null : r.GetString(14),
    };
}
