using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using HPAutoCad.Aec.ChangeSets;
using HPAutoCad.Aec.Model;
using HPRebar.McpBridge.Core.Scripting;

namespace HPAutoCad.Aec.Cad;

/// <summary>The bridge's globals a write tool runs with, bundled so a recorded call can be replayed.</summary>
public sealed record WriteContext(Database Db, Editor Ed, Transaction Tr, ScriptUnits Units, CancellationToken Ct, Action<string> Log);

/// <summary>
///     Every write tool a change set may hold, by seed name, reading its arguments exactly as the seed's shim does (so a recorded call
///     replays as the call would have run) and always with <c>changeSetId</c> null — the replay is the write. <see cref="Validate"/> refuses
///     at record time what a rollback could not undo.
/// </summary>
public static class WriteToolTable
{
    private static readonly Dictionary<string, Func<WriteContext, ScriptArgs, object>> Readers = new(StringComparer.Ordinal)
    {
        ["create_entities_batch"] = (c, a) => AecTools.CreateEntitiesBatch(c.Db, c.Ed, c.Tr, c.Units, c.Ct, c.Log, a.List("items"), a.Str("space"), a.Bool("atomic", true), null, a),
        ["update_entities_batch"] = (c, a) => AecTools.UpdateEntitiesBatch(c.Db, c.Ed, c.Tr, c.Units, c.Ct, c.Log, a.List("items"), a.Strings("handles"), a.Obj("set"), a.Bool("atomic", true), null, a),
        ["manage_blocks_attributes"] = (c, a) => AecTools.ManageBlocksAttributes(c.Db, c.Ed, c.Tr, c.Units, c.Ct, c.Log, a.Str("op"), a.Str("namePattern"), a.Bool("includeAnonymous", false), a.Obj("filter"), a.Strings("handles"), a.List("items"),
            a.Obj("attributes"), a.Obj("properties"), a.Obj("insert"), a.Str("space"), a.Bool("atomic", true), a.Int("limit", 100), a.Int("offset", 0), a.Int("maxCandidates", 5000), null, a),
        ["manage_annotations"] = (c, a) => AecTools.ManageAnnotations(c.Db, c.Ed, c.Tr, c.Units, c.Ct, c.Log, a.Str("op"), a.Obj("annotation"), a.Strings("handles"), a.List("items"), a.Obj("set"), a.Str("space"), a.Bool("atomic", true), null, a),
        ["manage_hatches"] = (c, a) => AecTools.ManageHatches(c.Db, c.Ed, c.Tr, c.Units, c.Ct, c.Log, a.Str("op"), a.Obj("hatch"), a.Strings("handles"), a.Obj("set"), a.Obj("seedPoint"), a.Str("space"), a.Bool("atomic", true), a.Int("limit", 20), a.Int("maxCandidates", 5000), null, a),
        ["manage_xrefs"] = (c, a) => AecTools.ManageXrefs(c.Db, c.Ed, c.Tr, c.Units, c.Ct, c.Log, a.Str("op"), a.Strings("names"), a.Str("namePattern"), a.Obj("attach"), a.Bool("insertBind", false), a.Str("space"), null, a),
        ["create_issue_markup"] = (c, a) => AecTools.CreateIssueMarkup(c.Db, c.Ed, c.Tr, c.Units, c.Ct, c.Log, a.List("issues"), a.Str("style"), a.Str("layer"), a.Double("radiusMm", 500), a.Double("textHeightMm", 150), a.Bool("withLeader", true), a.Bool("colorBySeverity", true), a.Str("space"), a.Bool("atomic", true), null, a),
        ["structural_tag_members"] = (c, a) => AecTools.StructuralTagMembers(c.Db, c.Ed, c.Tr, c.Units, c.Ct, c.Log, a.Obj("filter"), a.Strings("kinds"), a.Str("ruleSet"), a.Obj("prefixes"), a.Int("start", 1), a.Int("digits", 1), a.Str("sortBy"), a.Bool("overwrite", false), a.Str("layer"), a.Double("textHeightMm", 200), a.Str("space"), a.Bool("apply", true), null, a),
        ["structural_generate_member_schedule"] = (c, a) => AecTools.StructuralGenerateMemberSchedule(c.Db, c.Ed, c.Tr, c.Units, c.Ct, c.Log, a.Obj("filter"), a.Strings("kinds"), a.Str("ruleSet"), a.Obj("prefixes"), a.Bool("writeTable", false), a.Obj("insertPoint"), a.Str("title"), a.Str("layer"),
            a.Double("rowHeightMm", 400), a.Double("columnWidthMm", 3000), a.Double("textHeightMm", 200), a.Str("space"), null, a),
        ["arch_create_room_tags"] = (c, a) => AecTools.ArchCreateRoomTags(c.Db, c.Ed, c.Tr, c.Units, c.Ct, c.Log, a.Obj("filter"), a.Str("ruleSet"), a.Obj("tolerance"), a.Obj("labels"), a.Obj("detection"), a.Strings("roomIds"), a.Bool("onlyUnlabelled", false), a.Str("format"), a.Str("layer"),
            a.Double("textHeightMm", 250), a.Str("blockName"), a.Obj("attributes"), a.Str("space"), a.Bool("apply", true), a.Int("maxCandidates", 5000), null, a),
        ["arch_auto_dimension_plan"] = (c, a) => AecTools.ArchAutoDimensionPlan(c.Db, c.Ed, c.Tr, c.Units, c.Ct, c.Log, a.Obj("filter"), a.Str("ruleSet"), a.Obj("tolerance"), a.Obj("detection"), a.Str("subject"), a.Strings("roomIds"), a.List("rules"), a.Str("layer"), a.Str("dimStyle"), a.Str("space"),
            a.Bool("apply", true), a.Int("maxCandidates", 5000), null, a),
        ["aec_create_opening_requests"] = (c, a) => AecTools.AecCreateOpeningRequests(c.Db, c.Ed, c.Tr, c.Units, c.Ct, c.Log, a.Obj("routes"), a.Obj("hosts"), a.Str("ruleSet"), a.Obj("tolerance"), a.Obj("sizes"), a.Double("maxChordMm", 1000), a.Str("layer"), a.Double("textHeightMm", 150), a.Str("space"),
            a.Bool("apply", true), a.Int("maxCandidates", 5000), null, a),
    };

    /// <summary>The <c>op</c> values a tool accepts, when it has an op key — checked at record time so a typo does not wait for the commit.</summary>
    private static readonly Dictionary<string, IReadOnlyList<string>> Ops = new(StringComparer.Ordinal)
    {
        ["manage_annotations"] = AnnotationService.Ops,
        ["manage_blocks_attributes"] = BlockService.Ops,
        ["manage_hatches"] = HatchService.Ops,
        ["manage_xrefs"] = XrefService.Ops,
    };

    /// <summary>Xref ops other than attach change the block table, which a rollback cannot undo: they are refused, not recorded.</summary>
    public static readonly IReadOnlyList<string> UndoableXrefOps = ["attach"];

    public static IReadOnlyList<string> Names => Readers.Keys.Order(StringComparer.Ordinal).ToArray();

    public static bool Knows(string tool) => Readers.ContainsKey(tool);

    /// <summary>The reason a call cannot be recorded, or null: an unknown op, or an op a rollback could not undo.</summary>
    public static string? Validate(string tool, ScriptArgs args)
    {
        if (!Readers.ContainsKey(tool)) return $"'{tool}' is not a write tool a change set can hold (known: {string.Join(", ", Names)}).";
        if (!Ops.TryGetValue(tool, out var ops)) return null;
        var op = (args.Str("op") ?? "").Trim();
        if (!ops.Any(o => o.Equals(op, StringComparison.OrdinalIgnoreCase))) return $"{tool}: op must be one of {string.Join(", ", ops)}.";
        if (tool == "manage_xrefs" && !UndoableXrefOps.Contains(op, StringComparer.OrdinalIgnoreCase)) return $"manage_xrefs op '{op}' changes the block table and cannot be undone by a rollback: run it directly, not in a change set.";
        return null;
    }

    public static object Run(WriteContext cx, string tool, ScriptArgs args) =>
        Readers.TryGetValue(tool, out var reader) ? reader(cx, args) : throw new ArgumentException($"'{tool}' is not a write tool a change set can hold (known: {string.Join(", ", Names)}).");
}

/// <summary>
///     <c>commit_change_set</c>: replays the recorded calls in order inside the caller's run while a <see cref="SnapshotBag"/> remembers the
///     original state of everything they open for write. Atomic by default: an op that does not succeed throws an ArgumentException (the
///     set's content is the caller's), the bridge aborts the run and nothing of the set persists; with <c>atomic: false</c> the failed ops
///     are reported and the rest stand.
/// </summary>
public static class ChangeSetCommitter
{
    public static EditResult Commit(WriteContext cx, ChangeSetLedger ledger, ChangeSet set, bool atomic)
    {
        ChangeSetVerifier.Verify(cx.Db, cx.Tr, ledger, set);
        if (set.State != ChangeSetState.Pending) throw new ArgumentException($"change set {set.Id} is {set.State}; only a pending set commits{(set.Note is null ? "" : $" ({set.Note})")}.");
        if (set.Ops.Count == 0) throw new ArgumentException($"change set {set.Id} holds no ops; record write-tool calls into it (changeSetId) first.");
        var layersBefore = SymbolNames(cx, cx.Db.LayerTableId);
        var blocksBefore = SymbolNames(cx, cx.Db.BlockTableId);
        var bag = new SnapshotBag(cx.Db);
        var outcomes = new List<OpOutcome>();
        var warnings = new List<(int, string)>();
        var errors = new List<(int, ToolError)>();
        var created = new List<string>();
        try
        {
            foreach (var op in set.Ops)
            {
                cx.Ct.ThrowIfCancellationRequested();
                object outcome;
                try { outcome = WriteToolTable.Run(cx, op.Tool, new ScriptArgs(op.Args)); }
                catch (ArgumentException e) { throw new ArgumentException($"change set {set.Id}: op {op.Index} ({op.Tool}) — {e.Message}"); }
                if (outcome is not EditResult edit)
                {
                    outcomes.Add(new OpOutcome(op.Index, op.Tool, true, 0, 0, 0, null)); // a read op recorded by mistake: harmless
                    continue;
                }

                var error = edit.Success ? null : edit.Errors.FirstOrDefault()?.Message ?? "the op did not succeed";
                if (!edit.Success && atomic) throw new ArgumentException($"change set {set.Id}: op {op.Index} ({op.Tool}) failed — {error}; the run is aborted, nothing of the set is written.");
                outcomes.Add(new OpOutcome(op.Index, op.Tool, edit.Success, edit.CreatedCount, edit.ModifiedCount, edit.DeletedCount, error));
                created.AddRange(edit.CreatedHandles);
                warnings.AddRange(edit.Warnings.Select(w => (op.Index, w)));
                if (!edit.Success) errors.AddRange(edit.Errors.Select(e => (op.Index, e)));
            }
        }
        catch
        {
            bag.Dispose();
            throw;
        }

        bag.StopListening();
        var deleted = bag.Clones.Keys.Where(h => ChangeSetVerifier.IsErased(cx.Db, h)).ToArray();
        var modified = bag.Clones.Keys.Except(deleted, StringComparer.OrdinalIgnoreCase).Except(created, StringComparer.OrdinalIgnoreCase).ToArray();
        var layersCreated = SymbolNames(cx, cx.Db.LayerTableId).Except(layersBefore, StringComparer.OrdinalIgnoreCase).Order(StringComparer.Ordinal).ToArray();
        var blocksCreated = SymbolNames(cx, cx.Db.BlockTableId).Except(blocksBefore, StringComparer.OrdinalIgnoreCase).Order(StringComparer.Ordinal).ToArray();
        ledger.MarkCommitted(set, new CommitRecord(DateTimeOffset.Now, outcomes, created, modified, deleted, layersCreated, blocksCreated, bag, bag.Complete));
        return ChangeSetEnvelopes.Commit(new ChangeSetEnvelopes.CommitInput(set, outcomes, warnings, errors, created, modified, deleted, layersCreated, blocksCreated, bag.Clones.Count, bag.Complete, bag.Unsnapshotted, atomic));
    }

    private static HashSet<string> SymbolNames(WriteContext cx, ObjectId tableId)
    {
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var table = (SymbolTable)cx.Tr.GetObject(tableId, OpenMode.ForRead);
        foreach (var id in table) names.Add(((SymbolTableRecord)cx.Tr.GetObject(id, OpenMode.ForRead)).Name);
        return names;
    }
}
