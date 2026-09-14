using System.Text.Json;
using HPRebar.Mcp.Contracts.JsonRpc;
using HPRebar.Mcp.Contracts.Messages;
using HPRebar.Mcp.Server.Hosts;
using HPRebar.Mcp.Server.Models;
using HPRebar.Mcp.Server.Registry.Model;
using HPRebar.Mcp.Server.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HPRebar.Mcp.Server.Registry;

/// <summary>A search result: enough for the AI to decide whether to call the tool.</summary>
public sealed record SearchHit(string Name, string? Title, string Description, string Category, string Status, int Version,
    double Stability, int Runs, double SuccessRate, double Score, string Transaction, bool Destructive, JsonElement InputSchema);

/// <summary>Everything known about one tool, for `get_tool`.</summary>
public sealed record ToolDetails(ToolRecord Record, RunStats Stats, double Stability, IReadOnlyList<RunRecord> RecentRuns);

/// <summary>
///     The registry's brain: keeps the in-memory snapshot of the library in sync with disk and the
///     database, answers searches, runs stored tools through the bridge and records every run. Nothing
///     here knows about MCP — <see cref="DynamicToolRegistrar"/> and the meta tools sit on top.
/// </summary>
public sealed class ToolManager
{
    private readonly RegistryOptions _options;
    private readonly BridgeOptions _bridgeOptions;
    private readonly ToolLibraryStore _store;
    private readonly ToolRegistryDb _db;
    private readonly IRevitBridgeClient _bridge;
    private readonly ILogger<ToolManager> _logger;
    private readonly SemaphoreSlim _reload = new(1, 1);
    private volatile Dictionary<string, ToolRecord> _tools = new(StringComparer.OrdinalIgnoreCase);

    public ToolManager(IOptions<RegistryOptions> options, IOptions<BridgeOptions> bridgeOptions, ToolLibraryStore store, ToolRegistryDb db,
        IRevitBridgeClient bridge, ILogger<ToolManager> logger)
    {
        _options = options.Value;
        _bridgeOptions = bridgeOptions.Value;
        _store = store;
        _db = db;
        _bridge = bridge;
        _logger = logger;
    }

    public RegistryOptions Options => _options;

    /// <summary>The host this exe serves; every text and rule that names the host reads it from here.</summary>
    public IHostProfile Profile => _bridge.Profile;

    public ToolLibraryStore Store => _store;

    public ToolRegistryDb Db => _db;

    /// <summary>Raised after the snapshot changed (load, publish, quarantine…). Listeners re-register MCP tools.</summary>
    public event Action? Changed;

    public IReadOnlyCollection<ToolRecord> Tools => _tools.Values;

    public bool TryGet(string name, out ToolRecord record) => _tools.TryGetValue(name, out record!);

    /// <summary>Files → database → snapshot. Safe to call repeatedly; the watcher calls it on every change.</summary>
    public async Task<int> LoadAllAsync(CancellationToken cancellationToken = default)
    {
        await _reload.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var records = _store.ReadAll();
            var previous = _tools;
            var next = new Dictionary<string, ToolRecord>(StringComparer.OrdinalIgnoreCase);

            foreach (var record in records)
            {
                // A file copied by hand from another host's library: its code cannot run here.
                record.Host ??= _bridge.Profile.HostId;
                if (!string.Equals(record.Host, _bridge.Profile.HostId, StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogWarning("Tool {Name} targets host {Host}; this server serves {Ours} — skipped", record.Name, record.Host, _bridge.Profile.HostId);
                    continue;
                }

                if (next.ContainsKey(record.Name))
                {
                    _logger.LogWarning("Tool {Name} exists in two folders ({A}, {B}); keeping the first", record.Name, next[record.Name].Folder, record.Folder);
                    continue;
                }

                next[record.Name] = record;
                if (previous.TryGetValue(record.Name, out var old) && old.Checksum == record.Checksum) continue;

                // A status edited by hand in tool.json (the documented second approval path) is a lifecycle event
                // like an approve: without it a quarantined tool set back to published would be re-quarantined by
                // the failures that got it quarantined.
                var stored = old?.Status ?? _db.StoredStatus(record.Name);
                _db.UpsertTool(record);
                if (stored is { } was && was != record.Status)
                    _db.InsertEvent(record.Name, "status_changed", "file", $"{ToolRegistryDb.StatusText(was)} -> {ToolRegistryDb.StatusText(record.Status)}");
            }

            _db.RemoveToolsNotIn(next.Keys);
            _tools = next;
            _logger.LogInformation("Tools library loaded: {Count} tools from {Root}", next.Count, _store.Root);
        }
        finally
        {
            _reload.Release();
        }

        Changed?.Invoke();
        return _tools.Count;
    }

    /// <summary>Writes a record to disk + db and refreshes the snapshot entry without a full reload.</summary>
    public void Save(ToolRecord record, string @event, string? actor = null, string? detail = null)
    {
        _store.Write(record);
        _db.UpsertTool(record);
        _db.InsertEvent(record.Name, @event, actor, detail);

        var next = new Dictionary<string, ToolRecord>(_tools, StringComparer.OrdinalIgnoreCase) { [record.Name] = record };
        _tools = next;
        Changed?.Invoke();
    }

    // ---- search ----------------------------------------------------------------------------------

    public IReadOnlyList<SearchHit> Search(string? query, string? category, int limit, bool includeUnpublished)
    {
        limit = Math.Clamp(limit <= 0 ? _options.SearchTopK : limit, 1, 50);
        var stats = _db.AllStats(_options.RunWindow);
        var snapshot = _tools;

        IEnumerable<(ToolRecord Record, double Text)> candidates;
        if (string.IsNullOrWhiteSpace(query))
            candidates = snapshot.Values.Select(r => (r, 1.0));
        else
            candidates = _db.Search(query, limit * 6).Where(hit => snapshot.ContainsKey(hit.Name)).Select(hit => (snapshot[hit.Name], hit.Score));

        return candidates
            .Where(c => includeUnpublished ? c.Record.IsRunnable : c.Record.IsPublished)
            .Where(c => string.IsNullOrWhiteSpace(category) || string.Equals(c.Record.Category, category, StringComparison.OrdinalIgnoreCase))
            .Select(c =>
            {
                var s = stats.TryGetValue(c.Record.Name, out var st) ? st : RunStats.Empty;
                return ToHit(c.Record, s, StabilityScorer.Rank(c.Text, c.Record.Status, s));
            })
            .OrderByDescending(h => h.Score).ThenBy(h => h.Name, StringComparer.OrdinalIgnoreCase)
            .Take(limit)
            .ToList();
    }

    public ToolDetails? Get(string name, int recentRuns = 10)
    {
        if (!TryGet(name, out var record)) return null;
        var stats = _db.Stats(name, _options.RunWindow);
        return new ToolDetails(record, stats, StabilityScorer.Score(stats), recentRuns > 0 ? _db.RecentRuns(name, recentRuns) : []);
    }

    // ---- run -------------------------------------------------------------------------------------

    /// <summary>
    ///     Executes a stored tool: its code + the caller's args down the ordinary execute path. Only
    ///     outcomes the tool is responsible for (script error, timeout, success) enter its run history;
    ///     a closed host or a missing opt-in is reported but not counted against the tool.
    /// </summary>
    public async Task<ExecuteResult> RunAsync(string name, JsonElement? args, bool dryRun, bool allowUnpublished, string kind, CancellationToken cancellationToken)
    {
        if (!TryGet(name, out var record)) throw new ToolNotFoundException(name);
        if (!record.IsRunnable) throw new ToolNotRunnableException($"Tool '{name}' is deprecated.");
        if (!record.IsPublished && !allowUnpublished) throw new ToolNotRunnableException($"Tool '{name}' is {ToolRegistryDb.StatusText(record.Status)}; pass allowUnpublished=true to run it anyway (it has not been approved).");

        var timeout = Math.Clamp(record.TimeoutSeconds, 5, 120);
        var request = new ExecuteRequest(record.Code, record.Transaction, dryRun, timeout, name, args);
        var started = DateTimeOffset.UtcNow;

        ExecuteResult result;
        try
        {
            result = await _bridge.SendAsync<ExecuteResult>(_bridge.Profile.Method(JsonRpcMethods.ExecuteSuffix), request, TimeSpan.FromSeconds(timeout + _bridgeOptions.ExtraTimeoutSeconds), null, cancellationToken).ConfigureAwait(false);
        }
        catch (BridgeTimeoutException exception)
        {
            Record(record, args, dryRun, kind, false, (long)(DateTimeOffset.UtcNow - started).TotalMilliseconds, "timeout: " + exception.Message);
            throw;
        }

        result.RunId = Record(record, args, dryRun, kind, !result.IsError, result.DurationMs, result.IsError ? result.Message : null);
        return result;
    }

    private long Record(ToolRecord record, JsonElement? args, bool dryRun, string kind, bool success, long durationMs, string? error)
    {
        var argsJson = args is { ValueKind: not JsonValueKind.Undefined } a ? RegistryJson.Canonical(a) : null;
        var run = new RunRecord
        {
            ToolName = record.Name,
            Version = record.Version,
            Kind = kind,
            ArgsSha = argsJson is null ? null : RegistryJson.Sha256(argsJson),
            ArgsJson = argsJson,
            CodeSha = RegistryJson.Sha256(record.Code),
            DryRun = dryRun,
            Success = success,
            DurationMs = durationMs,
            Error = error,
            RevitVersion = _bridge.LastStatus?.RevitVersion,
            DocTitle = _bridge.LastStatus?.DocTitle,
        };
        var id = _db.InsertRun(run);

        if (kind == RunRecord.KindTest || record.Status != ToolStatus.Published) return id;

        var stats = _db.Stats(record.Name, _options.RunWindow);
        if (!StabilityScorer.ShouldQuarantine(stats, _options.QuarantineMinRuns, _options.QuarantineMaxFailureRate)) return id;

        record.Status = ToolStatus.Quarantined;
        record.Notes = $"Quarantined {DateTimeOffset.UtcNow:u}: {stats.Failures}/{stats.Runs} recent runs failed. Last error: {stats.LastError}";
        _logger.LogWarning("Tool {Name} quarantined: {Failures}/{Runs} recent runs failed", record.Name, stats.Failures, stats.Runs);
        Save(record, "quarantined", "registry", record.Notes);
        return id;
    }

    /// <summary>Records an ad-hoc execute run so a successful one can later become a tool.</summary>
    public long RecordAdhoc(string code, JsonElement? args, bool dryRun, ExecuteResult result)
    {
        var argsJson = args is { ValueKind: not JsonValueKind.Undefined } a ? RegistryJson.Canonical(a) : null;
        var id = _db.InsertRun(new RunRecord
        {
            Kind = RunRecord.KindAdhoc,
            CodeSha = RegistryJson.Sha256(code),
            ArgsSha = argsJson is null ? null : RegistryJson.Sha256(argsJson),
            ArgsJson = argsJson,
            DryRun = dryRun,
            Success = !result.IsError,
            DurationMs = result.DurationMs,
            Error = result.IsError ? result.Message : null,
            RevitVersion = _bridge.LastStatus?.RevitVersion,
            DocTitle = _bridge.LastStatus?.DocTitle,
            Code = result.IsError ? null : code,
        });
        if (!result.IsError && _options.KeepAdhocRuns > 0) _db.PruneAdhocCode(_options.KeepAdhocRuns);
        return id;
    }

    private static SearchHit ToHit(ToolRecord r, RunStats s, double score) => new(
        r.Name, r.Title, r.Description, r.Category, ToolRegistryDb.StatusText(r.Status), r.Version,
        StabilityScorer.Score(s), s.Runs, Math.Round(s.SuccessRate, 3), score, r.Transaction, r.Destructive, r.InputSchema);
}

public sealed class ToolNotFoundException(string name) : Exception($"No tool named '{name}' in the registry. Use search_tools to find the right name.");

public sealed class ToolNotRunnableException(string message) : Exception(message);
