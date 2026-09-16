using Autodesk.AutoCAD.DatabaseServices;
using HPAutoCad.Aec.Model;
using HPRebar.McpBridge.Core.Scripting;
using AcadException = Autodesk.AutoCAD.Runtime.Exception;

namespace HPAutoCad.Aec.Cad;

/// <summary>
///     Batch create / update with two modes. <b>atomic</b> (default): every item is validated before anything is
///     written — entities built but not appended, existing entities opened for read only — so one invalid item refuses
///     the whole batch (<c>success = false</c>, nothing created or modified, every error listed, the bridge's change counter
///     untouched); a failure while writing throws so the bridge aborts the transaction and nothing is kept.
///     <b>atomic = false</b>: invalid items are reported in <c>errors</c> and the rest is written; an item that fails midway
///     reports what it had already changed. Both run inside the bridge's transaction, so <c>dryRun</c> on the request rolls
///     everything back either way.
/// </summary>
public static class BatchEditService
{
    /// <summary>
    ///     One call writes at most this many entities: with every item refused (~200 B each) plus the grouped warnings and the
    ///     listed errors, the envelope stays under the bridge's 64 KB result cap.
    /// </summary>
    public const int MaxBatchItems = 200;

    public static EditResult CreateBatch(EditContext cx, IReadOnlyList<ScriptArgs> items, string? space, bool atomic, CancellationToken ct)
    {
        if (items.Count == 0) throw new ArgumentException("items must hold at least one entity to create.");
        if (items.Count > MaxBatchItems) throw new ArgumentException($"items holds {items.Count} entries; the maximum per call is {MaxBatchItems}.");
        var result = new EditResult();
        var target = cx.Space(space, out var spaceError) ?? throw new ArgumentException(spaceError!.Message);
        var factory = new EntityFactory(cx);

        var built = new EntityFactory.Built[items.Count];
        for (var i = 0; i < items.Count; i++)
        {
            ct.ThrowIfCancellationRequested();
            built[i] = factory.Create(items[i]);
            foreach (var warning in built[i].Warnings) result.WarnItem(i, warning);
        }

        if (atomic && built.Any(b => b.Entity is null))
        {
            foreach (var b in built) b.Entity?.Dispose();
            result.Settle(built.Select((b, i) => new ItemOutcome(i, false, null, Error: b.Error is null ? null : Indexed(b.Error, i))).ToArray());
            result.Summary = new { requested = items.Count, created = 0, refused = true, reason = $"atomic: {built.Count(b => b.Entity is null)} item(s) invalid, nothing created" };
            return result;
        }

        var outcomes = new ItemOutcome[items.Count];
        for (var i = 0; i < items.Count; i++)
        {
            ct.ThrowIfCancellationRequested();
            var b = built[i];
            if (b.Entity is null)
            {
                outcomes[i] = new ItemOutcome(i, false, null, Error: Indexed(b.Error!, i));
                continue;
            }

            try
            {
                target.AppendEntity(b.Entity);
                cx.Tr.AddNewlyCreatedDBObject(b.Entity, true);
                if (b.AfterAppend is not null)
                    foreach (var warning in b.AfterAppend(cx.Tr)) result.WarnItem(i, warning);
                var handle = b.Entity.Handle.ToString();
                outcomes[i] = new ItemOutcome(i, true, handle, b.Entity.ObjectId.ObjectClass.DxfName);
                result.Created(handle);
            }
            catch (AcadException exception)
            {
                if (atomic) throw new InvalidOperationException($"items[{i}] could not be written ({exception.ErrorStatus}); atomic batch aborted, nothing kept.", exception);
                if (!b.Entity.ObjectId.IsNull && !b.Entity.IsErased) b.Entity.Erase();
                outcomes[i] = new ItemOutcome(i, false, null, Error: new ToolError(ToolErrorCode.Internal, $"items[{i}] could not be written: {exception.ErrorStatus}."));
            }
        }

        result.Settle(outcomes);
        result.Summary = new
        {
            requested = items.Count,
            created = result.CreatedCount,
            byType = outcomes.Where(o => o.Ok).GroupBy(o => o.Type ?? "?").ToDictionary(g => g.Key, g => g.Count()),
            space = target.Name,
        };
        return result;
    }

    /// <summary>Items are {handle, set}; alternatively <paramref name="handles"/> + one shared <paramref name="sharedSet"/> apply the same change to many.</summary>
    public static EditResult UpdateBatch(EditContext cx, IReadOnlyList<ScriptArgs> items, IReadOnlyList<string> handles, ScriptArgs sharedSet, bool atomic, CancellationToken ct)
    {
        var result = new EditResult();
        var work = items.Count > 0
            ? items.Select(i => (Handle: i.Str("handle"), Set: i.Obj("set"))).ToArray()
            : EditContext.DistinctHandles(handles, result.Warnings).Select(h => (Handle: (string?)h, Set: sharedSet)).ToArray();
        if (work.Length == 0) throw new ArgumentException("Give items [{handle, set}] or handles [] + set {}.");
        if (work.Length > MaxBatchItems) throw new ArgumentException($"{work.Length} items; the maximum per call is {MaxBatchItems}.");
        if (work.Any(w => w.Set.IsEmpty)) throw new ArgumentException($"every item needs a non-empty set (keys: {string.Join(", ", EntityUpdater.Keys)}).");

        // Phase 1: open for read, check the layer, validate the set — nothing is upgraded, nothing is written.
        var updater = new EntityUpdater(cx);
        var opened = new Entity?[work.Length];
        var errors = new ToolError?[work.Length];
        for (var i = 0; i < work.Length; i++)
        {
            ct.ThrowIfCancellationRequested();
            opened[i] = cx.OpenForEdit(work[i].Handle, out errors[i]);
            if (opened[i] is null) continue;
            var itemWarnings = new List<string>();
            var itemErrors = updater.Validate(opened[i]!, work[i].Set, itemWarnings);
            foreach (var w in itemWarnings) result.WarnItem(i, w);
            if (itemErrors.Count > 0) errors[i] = itemErrors[0] with { Message = itemErrors.Count == 1 ? itemErrors[0].Message : $"{itemErrors[0].Message} (+{itemErrors.Count - 1} more refusal(s) on this item)" };
        }

        if (atomic && errors.Any(e => e is not null))
        {
            result.Settle(work.Select((_, i) => new ItemOutcome(i, false, opened[i]?.Handle.ToString(), Error: errors[i] is null ? null : Indexed(errors[i]!, i))).ToArray());
            result.Summary = new { requested = work.Length, modified = 0, refused = true, reason = $"atomic: {errors.Count(e => e is not null)} item(s) invalid, nothing modified" };
            return result;
        }

        // Phase 2: upgrade and apply; a failure midway keeps the `changed` list built so far.
        var outcomes = new ItemOutcome[work.Length];
        for (var i = 0; i < work.Length; i++)
        {
            ct.ThrowIfCancellationRequested();
            if (errors[i] is not null || opened[i] is not { } entity)
            {
                outcomes[i] = new ItemOutcome(i, false, opened[i]?.Handle.ToString(), Error: Indexed(errors[i]!, i));
                continue;
            }

            var handle = entity.Handle.ToString();
            var type = entity.ObjectId.ObjectClass.DxfName;
            var changed = new List<string>();
            ToolError? failure = null;
            if (!cx.Upgrade(entity, out failure))
            {
                if (atomic) throw new InvalidOperationException($"items[{i}] ({handle}): {failure!.Message} Atomic batch aborted, nothing kept.");
            }
            else
            {
                try
                {
                    updater.Apply(entity, work[i].Set, changed);
                }
                catch (AcadException exception)
                {
                    if (atomic) throw new InvalidOperationException($"items[{i}] ({handle}) could not be modified ({exception.ErrorStatus}); atomic batch aborted, nothing kept.", exception);
                    failure = ToolError.ForHandle(ToolErrorCode.Internal, handle, $"{handle} could not be modified: {exception.ErrorStatus} (applied before the failure: {(changed.Count == 0 ? "nothing" : string.Join(", ", changed))}).");
                }
            }

            outcomes[i] = new ItemOutcome(i, failure is null, handle, type, changed, failure is null ? null : Indexed(failure, i));
            if (changed.Count > 0) result.Modified(handle);
        }

        result.Settle(outcomes);
        result.Summary = new { requested = work.Length, modified = result.ModifiedCount, byType = outcomes.Where(o => o.Ok).GroupBy(o => o.Type ?? "?").ToDictionary(g => g.Key, g => g.Count()) };
        return result;
    }

    internal static ToolError Indexed(ToolError error, int index) => error.Message.StartsWith($"items[{index}]") ? error : error with { Message = $"items[{index}]: {error.Message}" };
}
