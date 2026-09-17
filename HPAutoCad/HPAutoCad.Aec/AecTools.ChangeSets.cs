using System.Diagnostics;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using HPAutoCad.Aec.Cad;
using HPAutoCad.Aec.ChangeSets;
using HPAutoCad.Aec.Model;
using HPRebar.McpBridge.Core.Scripting;

namespace HPAutoCad.Aec;

/// <summary>
///     The change-set tools: a logical set of write-tool calls recorded instead of applied (<c>changeSetId</c> on any write tool), listed,
///     replayed in one run, and undone. The store lives in the bridge process per drawing (<see cref="ChangeSetStore"/>).
/// </summary>
public static partial class AecTools
{
    /// <summary>Ops per preview page: an op with its arguments cut at <see cref="MaxOpArgsChars"/> and up to 100 handles is ~1.5 KB.</summary>
    public const int MaxChangeOpLimit = 30;

    /// <summary>Characters of an op's raw arguments shown in a preview.</summary>
    public const int MaxOpArgsChars = 500;

    public static AnalysisResult<Dictionary<string, object?>> BeginChangeSet(Database db, Editor ed, Transaction tr, ScriptUnits units, CancellationToken ct, Action<string> log, string? label)
    {
        var ledger = ChangeSetStore.For(db);
        foreach (var s in ledger.Sets) ChangeSetVerifier.Verify(db, tr, ledger, s);
        var set = ledger.Begin(label, DateTimeOffset.Now, out var closed);
        log($"begin_change_set: {set.Id} in {ledger.Document}");
        var result = new AnalysisResult<Dictionary<string, object?>>
        {
            Items = [set.Describe()], Count = 1,
            Summary = new { changeSetId = set.Id, label = set.Label, document = ledger.Document, byState = ledger.ByState(), maxOps = ChangeSetLedger.MaxOps, maxLiveSets = ChangeSetLedger.MaxSets, closed = closed?.Id, writeTools = WriteToolTable.Names },
        };
        if (closed is not null) result.Warn($"{closed.Id} was closed to make room (the drawing had {ChangeSetLedger.MaxSets} live sets): its work stays, its undo is no longer available.");
        return result;
    }

    public static AnalysisResult<Dictionary<string, object?>> GetChangeSummary(Database db, Editor ed, Transaction tr, ScriptUnits units, CancellationToken ct, Action<string> log, string? changeSetId)
    {
        var ledger = ChangeSetStore.For(db);
        foreach (var s in ledger.Sets) ChangeSetVerifier.Verify(db, tr, ledger, s);
        var sets = string.IsNullOrWhiteSpace(changeSetId) ? ledger.Sets : [ledger.Get(changeSetId)];
        return new AnalysisResult<Dictionary<string, object?>>
        {
            Items = sets.Select(s => s.Describe()).ToArray(), Count = sets.Count,
            Summary = new { document = ledger.Document, sets = ledger.Sets.Count, byState = ledger.ByState(), live = ledger.Sets.Count(s => s.IsLive), maxSets = ChangeSetLedger.MaxSets },
        };
    }

    public static AnalysisResult<Dictionary<string, object?>> PreviewChangeSet(Database db, Editor ed, Transaction tr, ScriptUnits units, CancellationToken ct, Action<string> log, string? changeSetId, int limit, int offset)
    {
        var result = new AnalysisResult<Dictionary<string, object?>>();
        (limit, offset) = PageBounds(limit, offset, MaxChangeOpLimit, result);
        var ledger = ChangeSetStore.For(db);
        var set = ledger.Get(changeSetId);
        ChangeSetVerifier.Verify(db, tr, ledger, set);
        result.Items = set.Ops.Skip(offset).Take(limit).Select(o => o.Describe(MaxOpArgsChars)).ToArray();
        result.Count = set.Ops.Count;
        result.Offset = offset;
        result.Truncated = set.Ops.Count > offset + result.Items.Count;
        result.Summary = new { document = ledger.Document, set = set.Describe(), handles = set.Ops.SelectMany(o => o.Handles).Distinct(StringComparer.OrdinalIgnoreCase).Count() };
        if (set.Note is not null) result.Warn(set.Note);
        if (set.State == ChangeSetState.Pending && set.Ops.Count == 0) result.Warn("the set holds no ops yet: call any write tool with changeSetId to record into it.");
        return result;
    }

    public static EditResult CommitChangeSet(Database db, Editor ed, Transaction tr, ScriptUnits units, CancellationToken ct, Action<string> log, string? changeSetId, bool atomic)
    {
        var watch = Stopwatch.StartNew();
        var ledger = ChangeSetStore.For(db);
        var set = ledger.Get(changeSetId);
        var result = ChangeSetCommitter.Commit(new WriteContext(db, ed, tr, units, ct, log), ledger, set, atomic);
        log($"commit_change_set: {set.Id} {set.Ops.Count} op(s) → +{result.CreatedCount} ~{result.ModifiedCount} -{result.DeletedCount}, {watch.ElapsedMilliseconds} ms");
        return result;
    }

    public static EditResult RollbackChangeSet(Database db, Editor ed, Transaction tr, ScriptUnits units, CancellationToken ct, Action<string> log, string? changeSetId, bool keep)
    {
        var watch = Stopwatch.StartNew();
        var ledger = ChangeSetStore.For(db);
        var set = ledger.Get(changeSetId);
        var result = ChangeSetRollback.Rollback(new WriteContext(db, ed, tr, units, ct, log), ledger, set, keep);
        log($"rollback_change_set: {set.Id} → {set.State}, erased {result.DeletedCount}, restored {result.ModifiedCount}, unerased {result.CreatedCount}, {watch.ElapsedMilliseconds} ms");
        return result;
    }
}
