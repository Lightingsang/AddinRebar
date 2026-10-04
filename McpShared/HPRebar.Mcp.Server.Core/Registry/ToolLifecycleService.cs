using System.Text;
using System.Text.Json;
using HPRebar.Mcp.Contracts.JsonRpc;
using HPRebar.Mcp.Contracts.Messages;
using HPRebar.Mcp.Server.Registry.Model;
using HPRebar.Mcp.Server.Services;
using Microsoft.Extensions.Logging;

namespace HPRebar.Mcp.Server.Registry;

public sealed record ProposeInput(string Name, string? Title, string Description, string Category, IReadOnlyList<string> Tags, JsonElement InputSchema,
    string Code, IReadOnlyList<ToolExample> Examples, string Transaction, int TimeoutSeconds, long? SourceRunId, bool NewVersion);

public sealed record ProposeOutcome(bool Accepted, ValidationReport Report, ToolRecord? Record, string Next);

public sealed record TestCaseOutcome(string Title, bool Success, bool DryRun, long DurationMs, string? Error, JsonElement? Value, long? RunId, ChangedCounts? Changed);

public sealed record TestOutcome(string Name, string Status, int Passed, int Failed, IReadOnlyList<TestCaseOutcome> Cases, string Next);

public sealed record PublishOutcome(string Name, string Status, string Message, string? ReviewFile);

/// <summary>
///     The write side of the registry: propose → test → publish, plus the human gate (approve / reject)
///     and manual management. Every transition is a file write (so it is reviewable and survives
///     restarts) and a registry event (so it is auditable). The AI can get a tool to
///     <c>pending_approval</c>; only a person — through the CLI or by editing tool.json — can publish
///     under the default policy.
/// </summary>
public sealed partial class ToolLifecycleService
{
    public const string ReviewFolder = "_review";
    private static readonly TimeSpan AnalyzeTimeout = TimeSpan.FromSeconds(30);

    private readonly ToolManager _manager;
    private readonly IRevitBridgeClient _bridge;
    private readonly ILogger<ToolLifecycleService> _logger;

    public ToolLifecycleService(ToolManager manager, IRevitBridgeClient bridge, ILogger<ToolLifecycleService> logger)
    {
        _manager = manager;
        _bridge = bridge;
        _logger = logger;
    }

    /// <summary>
    ///     Guard + compile + literals from the bridge; null when the host is not reachable (validation degrades to
    ///     warnings). <paramref name="transaction"/> is the mode a proposal declares, so a bridge that tells reads
    ///     from writes statically can refuse a `none` tool that writes; ad-hoc callers leave it null.
    /// </summary>
    public async Task<AnalyzeResult?> AnalyzeAsync(string code, CancellationToken cancellationToken, string? transaction = null)
    {
        try
        {
            return await _bridge.SendAsync<AnalyzeResult>(_bridge.Profile.Method(JsonRpcMethods.AnalyzeSuffix), new AnalyzeRequest(code, transaction), AnalyzeTimeout, null, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception) when (exception is BridgeUnavailableException or BridgeTimeoutException or BridgeErrorException)
        {
            _logger.LogInformation("{Method} unavailable ({Message}); validating without the compiler", _bridge.Profile.Method(JsonRpcMethods.AnalyzeSuffix), exception.Message);
            return null;
        }
    }

    public async Task<ProposeOutcome> ProposeAsync(ProposeInput input, CancellationToken cancellationToken)
    {
        var name = (input.Name ?? string.Empty).Trim().ToLowerInvariant();
        _manager.TryGet(name, out var existing);

        var record = new ToolRecord
        {
            Name = name,
            Title = string.IsNullOrWhiteSpace(input.Title) ? name.Replace('_', ' ') : input.Title.Trim(),
            Description = (input.Description ?? string.Empty).Trim(),
            Category = _bridge.Profile.Categories.FirstOrDefault(c => string.Equals(c, input.Category?.Trim(), StringComparison.OrdinalIgnoreCase)) ?? (input.Category ?? "Generic"),
            Tags = input.Tags.Where(t => !string.IsNullOrWhiteSpace(t)).Select(t => t.Trim().ToLowerInvariant()).Distinct().ToList(),
            InputSchema = input.InputSchema,
            Code = (input.Code ?? string.Empty).Trim(),
            Examples = input.Examples.ToList(),
            Transaction = TransactionModes.Normalize(input.Transaction) ?? input.Transaction,
            TimeoutSeconds = input.TimeoutSeconds,
            Destructive = TransactionModes.Normalize(input.Transaction) != TransactionModes.None,
            Status = ToolStatus.Draft,
            Version = input.NewVersion && existing is not null ? existing.Version + 1 : 1,
            Author = "ai",
            CreatedFromRunId = input.SourceRunId,
            CreatedAt = input.NewVersion && existing is not null ? existing.CreatedAt : null,
            RevitVersions = _bridge.LastStatus?.RevitVersion is { } v ? [v] : [],
            HostVersions = _bridge.LastStatus?.RevitVersion is { } hv ? [hv] : [],
            Host = _bridge.Profile.HostId,
        };

        var analysis = string.IsNullOrWhiteSpace(record.Code) ? null : await AnalyzeAsync(record.Code, cancellationToken, record.Transaction).ConfigureAwait(false);
        var report = ToolValidator.Validate(record, analysis, _manager.Tools, input.NewVersion, _bridge.Profile);
        if (!report.IsValid)
            return new ProposeOutcome(false, report, null, "Fix the errors and call propose_tool again.");

        _manager.Save(record, input.NewVersion ? "proposed_version" : "proposed", "ai", input.SourceRunId is null ? null : $"from run {input.SourceRunId}");
        _manager.Db.SaveVersion(record);
        _manager.Db.InsertEvent(record.Name, QualityEvent, "ai", QualityRecord(record, analysis));

        return new ProposeOutcome(true, report, record,
            $"Draft v{record.Version} saved. Next: test_tool {{name: \"{record.Name}\"}} runs its examples with dryRun; then publish_tool.");
    }

    public async Task<TestOutcome> TestAsync(string name, IReadOnlyList<ToolExample>? cases, bool realRun, CancellationToken cancellationToken)
    {
        if (!_manager.TryGet(name, out var record)) throw new ToolNotFoundException(name);
        if (!record.IsRunnable) throw new ToolNotRunnableException($"Tool '{name}' is deprecated; restore it first (manage_tool).");

        var useExamples = cases is null || cases.Count == 0;
        var toRun = useExamples ? record.Examples : cases!;
        if (toRun.Count == 0) throw new ToolNotRunnableException($"Tool '{name}' has no examples and no cases were given.");

        var outcomes = new List<TestCaseOutcome>();
        for (var i = 0; i < toRun.Count; i++)
        {
            var example = toRun[i];
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                var result = await _manager.RunAsync(name, example.Args, dryRun: !realRun, allowUnpublished: true, RunRecord.KindTest, cancellationToken).ConfigureAwait(false);
                outcomes.Add(new TestCaseOutcome(example.Title, !result.IsError, !realRun, result.DurationMs, result.Message, result.Value, result.RunId, result.Changed));
                if (useExamples && !result.IsError && result.RunId is { } runId) record.Examples[i].VerifiedRunId = runId;
            }
            catch (BridgeTimeoutException exception)
            {
                outcomes.Add(new TestCaseOutcome(example.Title, false, !realRun, 0, exception.Message, null, null, null));
            }
        }

        var failed = outcomes.Count(o => !o.Success);
        var passed = outcomes.Count - failed;
        string next;
        if (failed == 0)
        {
            if (record.Status is ToolStatus.Draft or ToolStatus.Tested or ToolStatus.Quarantined) record.Status = ToolStatus.Tested;
            _manager.Save(record, "tested", "ai", $"{passed}/{outcomes.Count} cases passed{(realRun ? " (real run)" : " (dry run)")}");
            next = record.Status == ToolStatus.Published ? "Still published." : $"All cases passed. Next: publish_tool {{name: \"{name}\"}}.";
        }
        else
        {
            _manager.Db.InsertEvent(name, "test_failed", "ai", $"{failed}/{outcomes.Count} cases failed");
            next = "Fix the code (propose_tool with newVersion=true) or the examples, then test again.";
        }

        return new TestOutcome(name, ToolRegistryDb.StatusText(record.Status), passed, failed, outcomes, next);
    }

    public PublishOutcome Publish(string name)
    {
        if (!_manager.TryGet(name, out var record)) throw new ToolNotFoundException(name);
        var policy = _manager.Options.PublishPolicy;

        switch (record.Status)
        {
            case ToolStatus.Deprecated:
                throw new ToolNotRunnableException($"Tool '{name}' is deprecated; restore it first.");
            case ToolStatus.Published:
                return new PublishOutcome(name, "published", "Already published.", null);
            case ToolStatus.Draft:
                throw new ToolNotRunnableException($"Tool '{name}' has not been tested. Run test_tool first.");
            case ToolStatus.Quarantined:
                throw new ToolNotRunnableException($"Tool '{name}' is quarantined ({record.Notes}). Fix it, test_tool, then publish again.");
        }

        if (policy == RegistryOptions.PolicyAuto)
        {
            record.Status = ToolStatus.Published;
            record.ApprovedBy = "policy:auto";
            record.PublishedAt = DateTimeOffset.UtcNow;
            _manager.Save(record, "published", "policy:auto");
            return new PublishOutcome(name, "published", "Published under the auto policy; it is now an MCP tool." + QualityNoteForPublish(record), null);
        }

        var reviewFile = WriteReview(record);
        if (record.Status != ToolStatus.PendingApproval)
        {
            record.Status = ToolStatus.PendingApproval;
            _manager.Save(record, "pending_approval", "ai", reviewFile);
        }

        return new PublishOutcome(name, "pending_approval",
            $"Waiting for a human. Ask the user to review {reviewFile} and run: {_bridge.Profile.CliExecutable} registry approve {name} --by <their name>. " +
            "Until then the tool runs only through run_tool with allowUnpublished=true.", reviewFile);
    }

    /// <summary>Human gate (CLI / tool.json edit). Publishes a tested or pending tool; a draft is refused unless forced.</summary>
    public ToolRecord Approve(string name, string by, bool force = false)
    {
        if (!_manager.TryGet(name, out var record)) throw new ToolNotFoundException(name);
        if (record.Status == ToolStatus.Deprecated) throw new ToolNotRunnableException($"Tool '{name}' is deprecated.");
        if (record.Status is ToolStatus.Draft or ToolStatus.Quarantined && !force)
            throw new ToolNotRunnableException($"Tool '{name}' is {ToolRegistryDb.StatusText(record.Status)}; test it first or pass --force.");

        record.Status = ToolStatus.Published;
        record.ApprovedBy = by;
        record.PublishedAt = DateTimeOffset.UtcNow;
        _manager.Save(record, "approved", by, force ? "forced" : null);
        return record;
    }

    public ToolRecord Reject(string name, string by, string reason)
    {
        if (!_manager.TryGet(name, out var record)) throw new ToolNotFoundException(name);
        record.Status = ToolStatus.Draft;
        record.Notes = $"Rejected by {by} {DateTimeOffset.UtcNow:u}: {reason}";
        _manager.Save(record, "rejected", by, reason);
        return record;
    }

    public ToolRecord Manage(string name, string action, string? reason, string actor)
    {
        if (!_manager.TryGet(name, out var record)) throw new ToolNotFoundException(name);
        var stamp = $"{DateTimeOffset.UtcNow:u} by {actor}: {reason}";
        var normalized = action.Trim().ToLowerInvariant();

        switch (normalized)
        {
            case "deprecate":
                record.Status = ToolStatus.Deprecated;
                record.Notes = "Deprecated " + stamp;
                break;
            case "quarantine":
                record.Status = ToolStatus.Quarantined;
                record.Notes = "Quarantined " + stamp;
                break;
            case "restore":
                if (record.Status is not (ToolStatus.Quarantined or ToolStatus.Deprecated)) throw new ToolNotRunnableException($"Tool '{name}' is {ToolRegistryDb.StatusText(record.Status)}; nothing to restore.");
                record.Status = ToolStatus.Draft; // must be tested again before it can be published
                record.Notes = "Restored " + stamp;
                break;
            default:
                throw new ArgumentException("action must be deprecate, quarantine or restore.");
        }

        _manager.Save(record, normalized, actor, reason);
        return record;
    }
}
