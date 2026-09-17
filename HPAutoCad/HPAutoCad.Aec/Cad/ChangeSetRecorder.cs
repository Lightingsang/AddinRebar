using System.Text.Json;
using Autodesk.AutoCAD.DatabaseServices;
using HPAutoCad.Aec.ChangeSets;
using HPAutoCad.Aec.Model;
using HPRebar.McpBridge.Core.Scripting;

namespace HPAutoCad.Aec.Cad;

/// <summary>
///     The first line of every write tool: with a <c>changeSetId</c> the call is recorded into the set instead of applied. The handles
///     the call names (<c>handles</c>, <c>items[].handle</c>, <c>issues[].handles</c>, <c>hatch.boundaryHandles</c>, every
///     <c>filter.handles</c>) must be entities now, and the <c>op</c> must be one the tool takes and a rollback can undo, so a typo is
///     refused at once; everything else is checked when the set is committed, by the tool itself.
/// </summary>
public static class ChangeSetRecorder
{
    public static bool TryRecord(Database db, Transaction tr, string? changeSetId, string tool, ScriptArgs args, out EditResult result)
    {
        if (string.IsNullOrWhiteSpace(changeSetId))
        {
            result = null!;
            return false;
        }

        var ledger = ChangeSetStore.For(db);
        var set = ledger.Get(changeSetId);
        ChangeSetVerifier.Verify(db, tr, ledger, set);
        result = new EditResult();
        if (set.State != ChangeSetState.Pending)
        {
            result = EditResult.Refused(ToolError.Argument($"change set {set.Id} is {set.State}; nothing can be recorded into it — begin_change_set starts a new one."));
            return true;
        }

        if (WriteToolTable.Validate(tool, args) is { } reason)
        {
            result = EditResult.Refused(ToolError.Argument(reason));
            return true;
        }

        var handles = DeclaredHandles(args);
        foreach (var handle in handles)
            if (HandleResolver.OpenEntity(db, tr, handle, out var error) is null) result.Fail(error!);
        if (!result.Success)
        {
            result.Summary = new { refused = true, changeSetId = set.Id, tool, reason = "a handle the call names is not an entity of this drawing; nothing recorded" };
            return true;
        }

        var op = ledger.Record(set, tool, args.Raw ?? default(JsonElement), handles, DateTimeOffset.Now);
        result.Summary = new { recorded = true, changeSetId = set.Id, opIndex = op.Index, tool, opSummary = op.Summary, ops = set.Ops.Count, handlesChecked = handles.Count, note = set.Note };
        result.Warn($"recorded as op {op.Index} of change set {set.Id}; nothing is written until commit_change_set (the record is in-process: dryRun on this request does not undo it).");
        if (set.Note is not null) result.Warn(set.Note);
        return true;
    }

    /// <summary>Every handle a write-tool call names up front: <c>handles</c>, <c>items[].handle</c>, <c>issues[].handles</c>, <c>hatch.boundaryHandles</c>, <c>filter.handles</c> (top level, <c>routes</c>, <c>hosts</c>).</summary>
    public static IReadOnlyList<string> DeclaredHandles(ScriptArgs args)
    {
        var handles = new List<string>();
        handles.AddRange(args.Strings("handles"));
        foreach (var item in args.List("items"))
            if (item.Str("handle") is { Length: > 0 } h) handles.Add(h);
        foreach (var issue in args.List("issues")) handles.AddRange(issue.Strings("handles"));
        handles.AddRange(args.Obj("hatch").Strings("boundaryHandles"));
        handles.AddRange(args.Obj("filter").Strings("handles"));
        handles.AddRange(args.Obj("routes").Obj("filter").Strings("handles"));
        handles.AddRange(args.Obj("hosts").Obj("filter").Strings("handles"));
        return handles.Where(h => h.Length > 0).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
    }
}
