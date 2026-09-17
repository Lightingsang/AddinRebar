using HPAutoCad.Aec.Model;

namespace HPAutoCad.Aec.ChangeSets;

/// <summary>What the drawing says about a committed (or rolled-back) set's handles, class by class — the input of the verifier's rule.</summary>
public sealed record UndoEvidence(int Created, int CreatedGone, int Deleted, int DeletedBack, int Modified, int ModifiedAsSnapshot);

/// <summary>
///     The rule that tells a commit or a rollback undone by its request (dryRun, <c>U</c>) from one that held: every class of handle
///     the run touched must agree — created entities all gone, erased ones all back, modified ones all reading as their snapshot. One
///     class still showing the work means the work is there. A set that touched nothing cannot be told either way. Pure.
/// </summary>
public static class UndoRule
{
    public static bool WorkIsGone(UndoEvidence e)
    {
        if (e.Created + e.Deleted + e.Modified == 0) return false;
        if (e.Created > 0 && e.CreatedGone < e.Created) return false;
        if (e.Deleted > 0 && e.DeletedBack < e.Deleted) return false;
        if (e.Modified > 0 && e.ModifiedAsSnapshot < e.Modified) return false;
        return true;
    }
}

/// <summary>The edit envelopes of commit and rollback, shaped under the result cap whatever the set did. Pure.</summary>
public static class ChangeSetEnvelopes
{
    /// <summary>Failed handles listed per rollback before "+N more"; the counts by code carry the rest.</summary>
    public const int MaxListedFailures = 20;

    public sealed record CommitInput(ChangeSet Set, IReadOnlyList<OpOutcome> Outcomes, IReadOnlyList<(int Op, string Warning)> Warnings, IReadOnlyList<(int Op, ToolError Error)> Errors,
        IReadOnlyList<string> Created, IReadOnlyList<string> Modified, IReadOnlyList<string> Deleted, IReadOnlyList<string> LayersCreated, IReadOnlyList<string> BlocksCreated,
        int Snapshots, bool SnapshotsComplete, int Unsnapshotted, bool Atomic);

    public static EditResult Commit(CommitInput c)
    {
        var result = new EditResult();
        foreach (var o in c.Outcomes)
        {
            result.CreatedCount += o.Created;
            result.ModifiedCount += o.Modified;
            result.DeletedCount += o.Deleted;
        }

        foreach (var (op, warning) in c.Warnings) result.WarnItem(op - 1, warning);
        result.AffectedHandles.AddRange(c.Created.Concat(c.Modified).Concat(c.Deleted).Take(ChangeSetLedger.MaxListedHandles));
        var firstError = c.Errors.GroupBy(e => e.Op).ToDictionary(g => g.Key, g => g.First().Error);
        result.Settle(c.Outcomes.Select(o => new ItemOutcome(o.Index - 1, o.Ok, null, o.Tool, [$"op {o.Index}", $"created {o.Created}", $"modified {o.Modified}", $"deleted {o.Deleted}"],
            o.Ok ? null : firstError.GetValueOrDefault(o.Index) ?? ToolError.Argument(o.Error ?? "the op did not succeed"))).ToArray());
        if (!c.SnapshotsComplete) result.Warn(c.Unsnapshotted > 0
            ? $"{c.Unsnapshotted} modified entit(ies) could not be snapshotted: a rollback erases what was created but cannot restore them."
            : $"more than {c.Snapshots} entities were modified: a rollback erases what was created but cannot restore every modification.");
        if (c.LayersCreated.Count > 0) result.Warn($"layer(s) created: {string.Join(", ", c.LayersCreated)} — a rollback keeps them.");
        if (c.BlocksCreated.Count > 0) result.Warn($"block definition(s) created: {string.Join(", ", c.BlocksCreated.Take(20))}{(c.BlocksCreated.Count > 20 ? $" +{c.BlocksCreated.Count - 20} more" : "")} — a rollback keeps them.");
        result.Summary = new
        {
            changeSetId = c.Set.Id, label = c.Set.Label, committed = true, ops = c.Set.Ops.Count, failedOps = c.Outcomes.Count(o => !o.Ok), atomic = c.Atomic,
            created = c.Created.Count, modified = c.Modified.Count, deleted = c.Deleted.Count, layersCreated = c.LayersCreated, blocksCreated = c.BlocksCreated.Take(20).ToArray(),
            snapshots = c.Snapshots, snapshotsComplete = c.SnapshotsComplete,
            byTool = c.Outcomes.GroupBy(o => o.Tool).ToDictionary(g => g.Key, g => new { ops = g.Count(), created = g.Sum(o => o.Created), modified = g.Sum(o => o.Modified), deleted = g.Sum(o => o.Deleted) }),
        };
        return result;
    }

    public sealed record RollbackInput(ChangeSet Set, IReadOnlyList<string> Erased, IReadOnlyList<string> Restored, IReadOnlyList<string> Unerased, IReadOnlyList<string> Missing,
        IReadOnlyList<ToolError> Failures, IReadOnlyList<string> LayersKept, IReadOnlyList<string> BlocksKept, bool SnapshotsComplete);

    public static EditResult Rollback(RollbackInput r)
    {
        var result = new EditResult { DeletedCount = r.Erased.Count, ModifiedCount = r.Restored.Count, CreatedCount = r.Unerased.Count };
        result.AffectedHandles.AddRange(r.Erased.Concat(r.Unerased).Concat(r.Restored).Take(ChangeSetLedger.MaxListedHandles));
        foreach (var failure in r.Failures.Take(MaxListedFailures)) result.Errors.Add(failure);
        var byCode = r.Failures.GroupBy(f => f.Code).OrderByDescending(g => g.Count()).ThenBy(g => g.Key, StringComparer.Ordinal).ToDictionary(g => g.Key, g => g.Count());
        if (r.Failures.Count > MaxListedFailures) result.Errors.Add(ToolError.Argument($"{r.Failures.Count - MaxListedFailures} more handle(s) failed ({string.Join(", ", byCode.Select(p => $"{p.Key} {p.Value}"))})."));
        result.Success = r.Failures.Count == 0;
        if (r.Failures.Count > 0) result.Warn($"{r.Failures.Count} handle(s) could not be undone; the set stays committed with its snapshots — fix the cause (a locked layer?) and roll back again.");
        if (r.Missing.Count > 0) result.Warn($"{r.Missing.Count} handle(s) of the commit no longer resolve or have no snapshot and were skipped.");
        if (r.LayersKept.Count > 0) result.Warn($"layer(s) the commit created stay: {string.Join(", ", r.LayersKept)}.");
        if (r.BlocksKept.Count > 0) result.Warn($"block definition(s) the commit created stay: {string.Join(", ", r.BlocksKept.Take(20))}.");
        if (!r.SnapshotsComplete) result.Warn("the commit had more modifications than the snapshot cap: not every modification was restored.");
        result.Summary = new
        {
            changeSetId = r.Set.Id, label = r.Set.Label, rolledBack = r.Failures.Count == 0, ops = r.Set.Ops.Count,
            erased = r.Erased.Take(ChangeSetLedger.MaxListedHandles).ToArray(), erasedCount = r.Erased.Count,
            restored = r.Restored.Take(ChangeSetLedger.MaxListedHandles).ToArray(), restoredCount = r.Restored.Count,
            unerased = r.Unerased.Take(ChangeSetLedger.MaxListedHandles).ToArray(), unerasedCount = r.Unerased.Count,
            missing = r.Missing.Take(ChangeSetLedger.MaxListedHandles).ToArray(), missingCount = r.Missing.Count,
            layersKept = r.LayersKept, blocksKept = r.BlocksKept.Take(20).ToArray(), failed = r.Failures.Count, failedByCode = byCode,
        };
        return result;
    }
}
