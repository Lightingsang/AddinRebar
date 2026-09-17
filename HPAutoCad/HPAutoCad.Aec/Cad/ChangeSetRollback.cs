using Autodesk.AutoCAD.DatabaseServices;
using HPAutoCad.Aec.ChangeSets;
using HPAutoCad.Aec.Model;

namespace HPAutoCad.Aec.Cad;

/// <summary>
///     <c>rollback_change_set</c>: a pending set is discarded (nothing was ever written); a committed set is undone in the caller's run —
///     the entities it created are erased, the ones it erased are un-erased (same handles: AutoCAD keeps erased objects in the open
///     drawing), and the ones it modified get their original state back from the snapshots (<c>CopyFrom</c>, same handle, so nothing
///     that references them breaks). Layers and block definitions it added stay and are reported. A handle that cannot be undone (a
///     locked layer) is an error, the rest is still restored — and the set stays committed with its snapshots for another try. With
///     <c>keep</c> the committed work stays and only the undo is released: the set is closed.
/// </summary>
public static class ChangeSetRollback
{
    public static EditResult Rollback(WriteContext cx, ChangeSetLedger ledger, ChangeSet set, bool keep)
    {
        ChangeSetVerifier.Verify(cx.Db, cx.Tr, ledger, set);
        var result = new EditResult();
        switch (set.State)
        {
            case ChangeSetState.Pending:
                ledger.MarkDiscarded(set, DateTimeOffset.Now);
                result.Summary = new { changeSetId = set.Id, label = set.Label, discarded = true, ops = set.Ops.Count, note = set.Note };
                result.Warn($"change set {set.Id} was pending: discarded, nothing to undo{(set.Note is null ? "" : $" ({set.Note})")}.");
                return result;
            case ChangeSetState.Committed when keep:
                ledger.Close(set, DateTimeOffset.Now);
                result.Summary = new { changeSetId = set.Id, label = set.Label, closed = true, ops = set.Ops.Count, created = set.Commit!.CreatedHandles.Count, modified = set.Commit.ModifiedHandles.Count, deleted = set.Commit.DeletedHandles.Count };
                result.Warn($"change set {set.Id} closed: its work stays, its undo is released.");
                return result;
            case ChangeSetState.Committed when set.Commit is { } commit:
                return Undo(cx, ledger, set, commit);
            default:
                throw new ArgumentException($"change set {set.Id} is {set.State}; nothing to roll back{(set.Note is null ? "" : $" ({set.Note})")}.");
        }
    }

    private static EditResult Undo(WriteContext cx, ChangeSetLedger ledger, ChangeSet set, CommitRecord commit)
    {
        var bag = commit.Snapshots as SnapshotBag;
        var erased = new List<string>();
        var restored = new List<string>();
        var unerased = new List<string>();
        var missing = new List<string>();
        var failures = new List<ToolError>();

        // Undo in reverse order of effect: what was created goes first (it may reference what was modified), then the erased, then the modified.
        foreach (var handle in commit.CreatedHandles.AsEnumerable().Reverse())
        {
            cx.Ct.ThrowIfCancellationRequested();
            var id = HandleResolver.Resolve(cx.Db, handle, out var error);
            if (id.IsNull) { if (error!.Code == ToolErrorCode.Erased) erased.Add(handle); else missing.Add(handle); continue; }
            if (Try(failures, handle, () => ((Entity)cx.Tr.GetObject(id, OpenMode.ForWrite)).Erase())) erased.Add(handle);
        }

        foreach (var handle in commit.DeletedHandles)
        {
            cx.Ct.ThrowIfCancellationRequested();
            if (!TryGetId(cx.Db, handle, out var id)) { missing.Add(handle); continue; }
            var clone = bag is not null && bag.Clones.TryGetValue(handle, out var c) ? c : null;
            if (Try(failures, handle, () =>
                {
                    var live = (Entity)cx.Tr.GetObject(id, OpenMode.ForWrite, true);
                    if (live.IsErased) live.Erase(false);
                    Restore(live, clone);
                })) unerased.Add(handle);
        }

        foreach (var handle in commit.ModifiedHandles)
        {
            cx.Ct.ThrowIfCancellationRequested();
            if (bag is null || !bag.Clones.TryGetValue(handle, out var clone)) { missing.Add(handle); continue; }
            var id = HandleResolver.Resolve(cx.Db, handle, out _);
            if (id.IsNull) { missing.Add(handle); continue; }
            if (Try(failures, handle, () => Restore((Entity)cx.Tr.GetObject(id, OpenMode.ForWrite), clone))) restored.Add(handle);
        }

        var result = ChangeSetEnvelopes.Rollback(new ChangeSetEnvelopes.RollbackInput(set, erased, restored, unerased, missing, failures, commit.LayersCreated, commit.BlocksCreated, commit.SnapshotsComplete));
        if (failures.Count > 0) ledger.MarkRollbackIncomplete(set, $"a rollback left {failures.Count} handle(s) undone ({string.Join(", ", failures.Select(f => f.Code).Distinct())}); roll back again once fixed");
        else ledger.MarkRolledBack(set, DateTimeOffset.Now, commit.SnapshotsComplete ? null : "not every modification could be restored (snapshot cap)");
        return result;
    }

    /// <summary>The original state back onto the live entity, same handle; a dimension or hatch then recomputes what it draws.</summary>
    private static void Restore(Entity live, Entity? clone)
    {
        if (clone is null) return;
        live.CopyFrom(clone);
        switch (live)
        {
            case Dimension d: d.RecomputeDimensionBlock(true); break;
            case Hatch h: h.EvaluateHatch(true); break;
        }
    }

    private static bool TryGetId(Database db, string handle, out ObjectId id)
    {
        id = ObjectId.Null;
        try
        {
            var normalized = HandleResolver.Normalize(handle);
            return normalized is not null && db.TryGetObjectId(new Handle(long.Parse(normalized, System.Globalization.NumberStyles.HexNumber, System.Globalization.CultureInfo.InvariantCulture)), out id) && !id.IsNull;
        }
        catch
        {
            return false;
        }
    }

    private static bool Try(List<ToolError> failures, string handle, Action action)
    {
        try
        {
            action();
            return true;
        }
        catch (Autodesk.AutoCAD.Runtime.Exception exception)
        {
            var code = exception.ErrorStatus == Autodesk.AutoCAD.Runtime.ErrorStatus.OnLockedLayer ? ToolErrorCode.LayerLocked : ToolErrorCode.Internal;
            failures.Add(ToolError.ForHandle(code, handle, $"{handle}: {exception.ErrorStatus} while undoing."));
            return false;
        }
    }
}
